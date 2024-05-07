using System.Text;
using KYCLog;
using System.Threading.Tasks.Dataflow;
using KYCPacket;
using KYCInterface;
using KYCException;

namespace DBServer
{
    internal class GameServerRecvPacketPipeline
    {
        private static readonly Lazy<GameServerRecvPacketPipeline> instance = new Lazy<GameServerRecvPacketPipeline>(() => new GameServerRecvPacketPipeline());
        public static GameServerRecvPacketPipeline GetSingletone => instance.Value;
        private CancellationTokenSource CancelToken = new CancellationTokenSource();
        private ExecutionDataflowBlockOptions ProcessorOptions = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = 20,
            MaxDegreeOfParallelism = 10,
            TaskScheduler = TaskScheduler.Default,
            EnsureOrdered = false,
            NameFormat = "this.NameFormat",
            SingleProducerConstrained = false,
        };
        private TransformBlock<Memory<byte>, dynamic> MemoryToPacketBlock;
        private ActionBlock<dynamic> PacketProcessBlock;
        


        private GameServerRecvPacketPipeline()
        {

            MemoryToPacketBlock = new TransformBlock<Memory<byte>, dynamic>(MakeMemoryToPacket, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 10,
                MaxDegreeOfParallelism = 5,
                NameFormat = "LoginPacketProcessor.MemoryToPacketBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            PacketProcessBlock = new ActionBlock<dynamic>(ProcessPacket,new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 100,
                MaxDegreeOfParallelism = 50,
                NameFormat = "LoginPacketProcessor.PacketProcessBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            MemoryToPacketBlock.LinkTo(PacketProcessBlock, new DataflowLinkOptions { PropagateCompletion = true });
            ProcessBlock();
        }

        public void PushToPacketPipeline(Memory<byte> Packet)
        {
            MemoryToPacketBlock.Post(Packet);
        }

        public void Cancel()
        {
            CancelToken.Cancel();
            MemoryToPacketBlock.Complete();
            PacketProcessBlock.Complete();
        }

        // 에러가 발생할경우 여기서 처리
        private void ProcessBlock()
        {
            Task.Run(async () =>
            {
                while (!CancelToken.Token.IsCancellationRequested)
                {
                    try
                    {
                        await MemoryToPacketBlock.Completion;
                        await PacketProcessBlock.Completion;
                    }
                    catch (AggregateException e)
                    {
                        LogManager.GetSingletone.WriteLog(e.Flatten());
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        LogManager.GetSingletone.WriteLog(e);
                    }
                }
            }, CancelToken.Token);
        }

        private bool IsErrorPacket(dynamic Packet, string message)
        {
            if (Packet is ErrorPacket)
            {
                ProcessGeneralErrorCode(Packet.ErrorCode, $"LoginPacketProcessor {message}에서 에러 발생");
                return true;
            }
            return false;

        }

        private void ProcessGeneralErrorCode(GeneralErrorCode ErrorCode, string Message)
        {
            StringBuilder ErrorLog = new StringBuilder();
            switch (ErrorCode)
            {
                case GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED:
                    ErrorLog.Append("Error: Packet is not assigned ");
                    ErrorLog.Append(Message);
                    LogManager.GetSingletone.WriteLog(ErrorLog.ToString());
                    break;
                case GeneralErrorCode.ERR_PACKET_IS_NULL:
                    ErrorLog.Append("Error: Packet is null ");
                    ErrorLog.Append(Message);
                    LogManager.GetSingletone.WriteLog(ErrorLog.ToString());
                    break;
            }
        }

        public dynamic MakePacketStruct(DBPacketListID ID, params dynamic[] PacketParams)
        {
            switch(ID)
            {
                case DBPacketListID.REQUST_CHRACTER_INFO:  
                    return new RequestCharacterInfoPacket(PacketParams[0], PacketParams[1]);
                default:
                    return new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED);
            }
        }

        private dynamic MakeMemoryToPacket(Memory<byte> packet)
        {
           DBPacketListID ID = PacketUtils.GetIDFromPacket<DBPacketListID>(ref packet);

            switch (ID)
            {
                case DBPacketListID.REQUST_CHRACTER_INFO:
                    RequestCharacterInfoPacket? RequestCharInfoPacket = PacketUtils.GetPacketStruct<RequestCharacterInfoPacket>(ref packet);
                    if (RequestCharInfoPacket == null)
                        return new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL);
                    else
                        return RequestCharInfoPacket;
                default:
                    return new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED);
            }
        }

        public void ProcessPacket(dynamic packet)
        {
            if(IsErrorPacket(packet, "ProcessPacket"))
                return;
            switch(packet)
            {
                case RequestCharacterInfoPacket RequestPacket:
                    RequestCharacterInfo(RequestPacket);
                    break;
            }
        }

        private void RequestCharacterInfo(RequestCharacterInfoPacket packet)
        {
            if (IsErrorPacket(packet, "LoginRequest"))
                return;
            //SQLManager가 있었네! 이걸로 DB에 접근해서 처리하면 될듯
        }
    }
}
