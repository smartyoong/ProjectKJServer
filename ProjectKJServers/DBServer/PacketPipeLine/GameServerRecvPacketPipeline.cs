using System.Text;
using System.Threading.Tasks.Dataflow;
using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using DBServer.MainUI;
using DBServer.Packet_SPList;

namespace DBServer.PacketPipeLine
{
    internal class GameServerRecvPacketPipeline
    {
        //private static readonly Lazy<GameServerRecvPacketPipeline> instance = new Lazy<GameServerRecvPacketPipeline>(() => new GameServerRecvPacketPipeline());
        //public static GameServerRecvPacketPipeline GetSingletone => instance.Value;

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




        public GameServerRecvPacketPipeline()
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
                case DBPacketListID.REQUEST_CHAR_BASE_INFO:
                    RequestDBCharBaseInfoPacket? CharBaseInfoPacket = PacketUtils.GetPacketStruct<RequestDBCharBaseInfoPacket>(ref packet);
                    return CharBaseInfoPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : CharBaseInfoPacket;
                case DBPacketListID.REQUEST_CREATE_CHARACTER:
                    RequestDBCreateCharacterPacket? CreateCharacterPacket = PacketUtils.GetPacketStruct<RequestDBCreateCharacterPacket>(ref packet);
                    return CreateCharacterPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : CreateCharacterPacket;
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
                case RequestDBCharBaseInfoPacket CharBaseInfoPacket:
                    SP_RequestCharBaseInfo(CharBaseInfoPacket);
                    break;
                case RequestDBCreateCharacterPacket CreateCharacterPacket:
                    SP_ReuquestCreateCharacter(CreateCharacterPacket);
                    break;
                case RequestDBUpdateHealthPointPacket UpdateHealthPointPacket:
                    SP_RequestUpdateHealthPoint(UpdateHealthPointPacket);
                    break;
                case RequestDBUpdateMagicPointPacket UpdateMagicPointPacket:
                    SP_RequestUpdateMagicPoint(UpdateMagicPointPacket);
                    break;
                case RequestDBUpdateLevelExpPacket UpdateLevelExpPacket:
                    SP_RequestUpdateLevelEXP(UpdateLevelExpPacket);
                    break;
                default:
                    LogManager.GetSingletone.WriteLog("GameServerRecvPacketPipeline.ProcessPacket: 알수 없는 패킷이 들어왔습니다.");
                    break;
            }
        }

        private void SP_RequestCharBaseInfo(RequestDBCharBaseInfoPacket packet)
        {
            if (IsErrorPacket(packet, "RequestCharBaseInfo"))
                return;
            GameSQLReadCharacterPacket ReadCharacterPacket = new GameSQLReadCharacterPacket(packet.AccountID,packet.NickName);
            MainProxy.GetSingletone.HandleSQLPacket(ReadCharacterPacket);
        }

        private void SP_ReuquestCreateCharacter(RequestDBCreateCharacterPacket packet)
        {
            if (IsErrorPacket(packet, "RequestCreateCharacter"))
                return;
            GameSQLCreateCharacterPacket CreateCharacterPacket = new GameSQLCreateCharacterPacket(packet.AccountID, packet.Gender, packet.PresetID);
            MainProxy.GetSingletone.HandleSQLPacket(CreateCharacterPacket);
        }

        private void SP_RequestUpdateHealthPoint(RequestDBUpdateHealthPointPacket Packet)
        {
            if(IsErrorPacket(Packet, "RequestUpdateHealth"))
                return;
            GameSQLUpdateHealthPoint UpdateHealthPacket = new GameSQLUpdateHealthPoint(Packet.AccountID, Packet.CurrentHP);
            MainProxy.GetSingletone.HandleSQLPacket(UpdateHealthPacket);
        }

        private void SP_RequestUpdateMagicPoint(RequestDBUpdateMagicPointPacket Packet)
        {
            if (IsErrorPacket(Packet, "RequestUpdateHealth"))
                return;
            GameSQLUpdateMagicPoint UpdateMagicPacket = new GameSQLUpdateMagicPoint(Packet.AccountID, Packet.CurrentMP);
            MainProxy.GetSingletone.HandleSQLPacket(UpdateMagicPacket);
        }

        private void SP_RequestUpdateLevelEXP(RequestDBUpdateLevelExpPacket Packet)
        {
            if (IsErrorPacket(Packet, "RequestUpdateHealth"))
                return;
            GameSQLUpdateLevelEXP UpdateLevelExpPacket = new GameSQLUpdateLevelEXP(Packet.AccountID, Packet.Level, Packet.CurrentEXP);
            MainProxy.GetSingletone.HandleSQLPacket(UpdateLevelExpPacket);
        }
    }
}
