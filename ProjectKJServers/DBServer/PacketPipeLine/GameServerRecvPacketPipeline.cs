using System.Text;
using System.Threading.Tasks.Dataflow;
using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using DBServer.Packet_SPList;

namespace DBServer.PacketPipeLine
{
    internal class GameServerRecvPacketPipeline
    {
        private static readonly Lazy<GameServerRecvPacketPipeline> instance = new Lazy<GameServerRecvPacketPipeline>(() => new GameServerRecvPacketPipeline());
        public static GameServerRecvPacketPipeline GetSingletone => instance.Value;

        private CancellationTokenSource CancelToken = new CancellationTokenSource();
        private ExecutionDataflowBlockOptions ProcessorOptions = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = 5,
            MaxDegreeOfParallelism = 3,
            TaskScheduler = TaskScheduler.Default,
            EnsureOrdered = false,
            NameFormat = "GameServerRecvPipeLine",
            SingleProducerConstrained = false,
        };
        private TransformBlock<Memory<byte>, dynamic> MemoryToPacketBlock;
        private ActionBlock<dynamic> PacketProcessBlock;




        private GameServerRecvPacketPipeline()
        {

            MemoryToPacketBlock = new TransformBlock<Memory<byte>, dynamic>(MakeMemoryToPacket, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 5,
                MaxDegreeOfParallelism = 3,
                NameFormat = "GameServerRecvPacketPipeline.MemoryToPacketBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            PacketProcessBlock = new ActionBlock<dynamic>(ProcessPacket, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 40,
                MaxDegreeOfParallelism = 10,
                NameFormat = "GameServerRecvPacketPipeline.PacketProcessBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            MemoryToPacketBlock.LinkTo(PacketProcessBlock, new DataflowLinkOptions { PropagateCompletion = true });
            ProcessBlock();
            LogManager.GetSingletone.WriteLog("GameServerRecvPacketPipeline 생성 완료");
        }

        public void PushToPacketPipeline(Memory<byte> packet)
        {
            MemoryToPacketBlock.Post(packet);
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
                ProcessGeneralErrorCode(Packet.ErrorCode, $"GamePacketProcessor {message}에서 에러 발생");
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

        private dynamic MakeMemoryToPacket(Memory<byte> packet)
        {
            DBPacketListID ID = PacketUtils.GetIDFromPacket<DBPacketListID>(ref packet);

            switch (ID)
            {
                case DBPacketListID.REQUEST_DB_TEST:
                    RequestDBTestPacket? TestPacket = PacketUtils.GetPacketStruct<RequestDBTestPacket>(ref packet);
                    if (TestPacket == null)
                        return new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL);
                    else
                        return TestPacket;
                case DBPacketListID.REQUEST_CHAR_BASE_INFO:
                    RequestDBCharBaseInfoPacket? CharBaseInfoPacket = PacketUtils.GetPacketStruct<RequestDBCharBaseInfoPacket>(ref packet);
                    return CharBaseInfoPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : CharBaseInfoPacket;
                default:
                    return new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED);
            }
        }

        public void ProcessPacket(dynamic Packet)
        {
            if (IsErrorPacket(Packet, "GameServerRecvProcessPacket"))
                return;
            switch (Packet)
            {
                case RequestDBTestPacket TestPacket:
                    SP_DBTest(TestPacket);
                    break;
                case RequestDBCharBaseInfoPacket CharBaseInfoPacket:
                    SP_RequestCharBaseInfo(CharBaseInfoPacket);
                    break;
                default:
                    LogManager.GetSingletone.WriteLog("GameServerRecvPacketPipeline.ProcessPacket: 알수 없는 패킷이 들어왔습니다.");
                    break;
            }
        }

        private void SP_DBTest(RequestDBTestPacket packet)
        {
            if (IsErrorPacket(packet, "RequestDBTest"))
                return;
            // 유저 Socket 정보를 들고 있는 Map이 필요할듯? 그러면 Socket을 매개변수로 넘길필요가 없을 수도 있음 (Client 파이프라인에서)
            GameSQLPipeLine.GetSingletone.SQL_DB_TEST(packet.AccountID, packet.NickName);
        }

        private void SP_RequestCharBaseInfo(RequestDBCharBaseInfoPacket packet)
        {
            if (IsErrorPacket(packet, "RequestCharBaseInfo"))
                return;
            LogManager.GetSingletone.WriteLog($"AccountID: {packet.AccountID}");
            GameSQLPipeLine.GetSingletone.SQL_READ_CHARACTER(packet.AccountID);
        }
    }
}
