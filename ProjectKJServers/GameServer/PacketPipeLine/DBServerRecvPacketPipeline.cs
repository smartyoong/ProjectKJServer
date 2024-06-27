using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using GameServer.PacketList;
using GameServer.SocketConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GameServer.PacketPipeLine
{
    internal class DBServerRecvPacketPipeline
    {
        private static readonly Lazy<DBServerRecvPacketPipeline> instance = new Lazy<DBServerRecvPacketPipeline>(() => new DBServerRecvPacketPipeline());
        public static DBServerRecvPacketPipeline GetSingletone => instance.Value;

        private CancellationTokenSource CancelToken = new CancellationTokenSource();
        private ExecutionDataflowBlockOptions ProcessorOptions = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = 5,
            MaxDegreeOfParallelism = 3,
            TaskScheduler = TaskScheduler.Default,
            EnsureOrdered = false,
            NameFormat = "DBServerRecvPipeLine",
            SingleProducerConstrained = false,
        };
        private TransformBlock<Memory<byte>, dynamic> MemoryToPacketBlock;
        private ActionBlock<dynamic> PacketProcessBlock;




        private DBServerRecvPacketPipeline()
        {

            MemoryToPacketBlock = new TransformBlock<Memory<byte>, dynamic>(MakeMemoryToPacket, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 5,
                MaxDegreeOfParallelism = 3,
                NameFormat = "DBServerRecvPacketPipeline.MemoryToPacketBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            PacketProcessBlock = new ActionBlock<dynamic>(ProcessPacket, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 40,
                MaxDegreeOfParallelism = 10,
                NameFormat = "DBServerRecvPacketPipeline.PacketProcessBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            MemoryToPacketBlock.LinkTo(PacketProcessBlock, new DataflowLinkOptions { PropagateCompletion = true });
            ProcessBlock();
            LogManager.GetSingletone.WriteLog("DBServerRecvPacketPipeline 생성 완료");
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
                ProcessGeneralErrorCode(Packet.ErrorCode, $"DBPacketProcessor {message}에서 에러 발생");
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
            GameDBPacketListID ID = PacketUtils.GetIDFromPacket<GameDBPacketListID>(ref packet);

            switch (ID)
            {
                case GameDBPacketListID.RESPONSE_DB_TEST:
                    ResponseDBTestPacket? TestPacket = PacketUtils.GetPacketStruct<ResponseDBTestPacket>(ref packet);
                    if (TestPacket == null)
                        return new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL);
                    else
                        return TestPacket;
                case GameDBPacketListID.RESPONSE_NEED_TO_MAKE_CHARACTER:
                    ResponseDBNeedToMakeCharacterPacket? NeedToMakeCharacterPacket = PacketUtils.GetPacketStruct<ResponseDBNeedToMakeCharacterPacket>(ref packet);
                    return NeedToMakeCharacterPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : NeedToMakeCharacterPacket;
                default:
                    return new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED);
            }
        }

        public void ProcessPacket(dynamic Packet)
        {
            if (IsErrorPacket(Packet, "DBServerRecvProcessPacket"))
                return;
            switch (Packet)
            {
                case ResponseDBTestPacket TestPacket:
                    Func_DBTest(TestPacket);
                    break;
                case ResponseDBNeedToMakeCharacterPacket NeedToMakeCharacterPacket:
                    Func_ResponseNeedToMakeCharacter(NeedToMakeCharacterPacket);
                    break;
                default:
                    LogManager.GetSingletone.WriteLog("DBServerRecvPacketPipeline.ProcessPacket: 알수 없는 패킷이 들어왔습니다.");
                    break;
            }
        }

        private void Func_DBTest(ResponseDBTestPacket packet)
        {
            if (IsErrorPacket(packet, "ResponseDBTest"))
                return;
            LogManager.GetSingletone.WriteLog($"AccountID: {packet.AccountID} NickName: {packet.NickName} {packet.Level} {packet.Exp}");
            LoginServerSendPacketPipeline.GetSingletone.PushToPacketPipeline(GameLoginPacketListID.RESPONSE_USER_HASH_INFO, new ResponseLoginTestPacket(packet.AccountID, packet.NickName, packet.Level, packet.Exp));
        }

        private void Func_ResponseNeedToMakeCharacter(ResponseDBNeedToMakeCharacterPacket Packet)
        {
            if (IsErrorPacket(Packet, "ResponseDBNeedToMakeNewCharcter"))
                return;
            const int NEED_TO_MAKE_CHARACTER = 1;
            ResponseNeedToMakeCharcterPacket ResponsePacket = new ResponseNeedToMakeCharcterPacket(NEED_TO_MAKE_CHARACTER);
            if (ClientAcceptor.GetSingletone.GetClientSocketByAccountID(Packet.AccountID) == null)
            {
                LogManager.GetSingletone.WriteLog($"Func_ResponseNeedToMakeCharacter: {Packet.AccountID}에 해당하는 클라이언트 소켓이 없습니다.");
                return;
            }
            ClientSendPacketPipeline.GetSingletone.PushToPacketPipeline(GamePacketListID.RESPONSE_NEED_TO_MAKE_CHARACTER, ResponsePacket,
                ClientAcceptor.GetSingletone.GetClientID(ClientAcceptor.GetSingletone.GetClientSocketByAccountID(Packet.AccountID)!));
        }
    }
}
