using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using GameServer.MainUI;
using GameServer.Object;
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
        //private static readonly Lazy<DBServerRecvPacketPipeline> instance = new Lazy<DBServerRecvPacketPipeline>(() => new DBServerRecvPacketPipeline());
        //public static DBServerRecvPacketPipeline GetSingletone => instance.Value;

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

        public DBServerRecvPacketPipeline()
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
                case GameDBPacketListID.RESPONSE_CREATE_CHARACTER:
                    ResponseDBCreateCharacterPacket? CreateCharacterPacket = PacketUtils.GetPacketStruct<ResponseDBCreateCharacterPacket>(ref packet);
                    return CreateCharacterPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : CreateCharacterPacket;
                case GameDBPacketListID.RESPONSE_CHAR_BASE_INFO:
                    ResponseDBCharBaseInfoPacket? CharBaseInfoPacket = PacketUtils.GetPacketStruct<ResponseDBCharBaseInfoPacket>(ref packet);
                    return CharBaseInfoPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : CharBaseInfoPacket;
                case GameDBPacketListID.RESPONSE_UPDATE_GENDER:
                    ResponseDBUpdateGenderPacket? UpdateGenderPacket = PacketUtils.GetPacketStruct<ResponseDBUpdateGenderPacket>(ref packet);
                    return UpdateGenderPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : UpdateGenderPacket;
                case GameDBPacketListID.RESPONSE_UPDATE_PRESET:
                    ResponseDBUpdatePresetPacket? UpdatePresetPacket = PacketUtils.GetPacketStruct<ResponseDBUpdatePresetPacket>(ref packet);
                    return UpdatePresetPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : UpdatePresetPacket;
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
                case ResponseDBCreateCharacterPacket CreateCharacterPacket:
                    Func_ResponseCreateCharacter(CreateCharacterPacket);
                    break;
                case ResponseDBCharBaseInfoPacket CharBaseInfoPacket:
                    Func_ResponseCharBaseInfo(CharBaseInfoPacket);
                    break;
                case ResponseDBUpdateGenderPacket UpdateGenderPacket:
                    Func_ResponseUpdateGender(UpdateGenderPacket);
                    break;
                case ResponseDBUpdatePresetPacket UpdatePresetPacket:
                    Func_ResponseUpdatePreset(UpdatePresetPacket);
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
            MainProxy.GetSingletone.SendToLoginServer(GameLoginPacketListID.RESPONSE_USER_HASH_INFO, new ResponseLoginTestPacket(packet.AccountID, packet.NickName, packet.Level, packet.Exp));
        }

        private void Func_ResponseNeedToMakeCharacter(ResponseDBNeedToMakeCharacterPacket Packet)
        {
            if (IsErrorPacket(Packet, "ResponseDBNeedToMakeNewCharcter"))
                return;
            const int NEED_TO_MAKE_CHARACTER = 1;
            ResponseNeedToMakeCharcterPacket ResponsePacket = new ResponseNeedToMakeCharcterPacket(NEED_TO_MAKE_CHARACTER);
            if (MainProxy.GetSingletone.GetClientSocketByAccountID(Packet.AccountID) == null)
            {
                LogManager.GetSingletone.WriteLog($"Func_ResponseNeedToMakeCharacter: {Packet.AccountID}에 해당하는 클라이언트 소켓이 없습니다.");
                return;
            }
            MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_NEED_TO_MAKE_CHARACTER, ResponsePacket, Packet.AccountID);
        }

        private void Func_ResponseCreateCharacter(ResponseDBCreateCharacterPacket Packet)
        {
            const int FAIL = -1;
            if (IsErrorPacket(Packet, "ResponseCreateCharacter"))
                return;
            if (Packet.ErrorCode == FAIL)
            {
                LogManager.GetSingletone.WriteLog($"Func_ResponseCreateCharacter: {Packet.AccountID}에 캐릭터 생성 실패 ErrorCode: {Packet.ErrorCode}");
            }
            MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_CREATE_CHARACTER, new ResponseCreateCharacterPacket(Packet.ErrorCode), Packet.AccountID);
        }

        private void Func_ResponseCharBaseInfo(ResponseDBCharBaseInfoPacket Packet)
        {
            if (IsErrorPacket(Packet, "ResponseCharBaseInfo"))
                return;
            ResponseCharBaseInfoPacket ResponsePacket = new ResponseCharBaseInfoPacket(Packet.AccountID, Packet.Gender, Packet.PresetNumber, Packet.Job,
                Packet.JobLevel, Packet.MapID, Packet.X, Packet.Y, Packet.Level, Packet.EXP, Packet.HP, Packet.MP);
            MainProxy.GetSingletone.CreateCharacter(Packet);
            MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_CHAR_BASE_INFO, ResponsePacket, Packet.AccountID);
            //같은 맵 유저에게 해당 캐릭터를 렌더링하도록 명령내린다.
            SendAnotherCharBaseInfoPacket SendPacket = new SendAnotherCharBaseInfoPacket(Packet.AccountID, Packet.Gender, Packet.PresetNumber, Packet.Job,
                Packet.JobLevel, Packet.MapID, Packet.X, Packet.Y, Packet.Level, Packet.EXP, MainProxy.GetSingletone.GetNickName(Packet.AccountID),
                -1, -1 /*최초 생성시에는 이동 목표가 없다*/, Packet.HP, Packet.MP);

            MainProxy.GetSingletone.SendToSameMap(Packet.MapID, GamePacketListID.SEND_ANOTHER_CHAR_BASE_INFO, SendPacket);
        }

        private void Func_ResponseUpdateGender(ResponseDBUpdateGenderPacket Packet)
        {
            if (IsErrorPacket(Packet, "ResponseUpdateGender"))
                return;
            PlayerCharacter? User = MainProxy.GetSingletone.GetCharacterByAccountID(Packet.AccountID);

            if (User == null)
            {
                LogManager.GetSingletone.WriteLog($"Func_ResponseUpdateGender: {Packet.AccountID}에 해당하는 캐릭터가 없습니다.");
                return;
            }

            User.GetAppearanceComponent.ApplyChangeGender();
        }

        private void Func_ResponseUpdatePreset(ResponseDBUpdatePresetPacket Packet)
        {
            if (IsErrorPacket(Packet, "ResponseUpdatePreset"))
                return;
            PlayerCharacter? User = MainProxy.GetSingletone.GetCharacterByAccountID(Packet.AccountID);

            if (User == null)
            {
                LogManager.GetSingletone.WriteLog($"Func_ResponseUpdatePreset: {Packet.AccountID}에 해당하는 캐릭터가 없습니다.");
                return;
            }

            User.GetAppearanceComponent.ApplyChangePresetNumber(Packet.PresetNumber);
        }
    }
}
