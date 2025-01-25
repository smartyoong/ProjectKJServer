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
        private Dictionary<GameDBPacketListID, Func<Memory<byte>, dynamic>> MemoryLookUpTable;
        private Dictionary<Type, Action<DBRecvPacket>> PacketLookUpTable;

        public DBServerRecvPacketPipeline()
        {
            MemoryLookUpTable = new Dictionary<GameDBPacketListID, Func<Memory<byte>, dynamic>>
            {
                { GameDBPacketListID.RESPONSE_NEED_TO_MAKE_CHARACTER, MakeResponseDBNeedToMakeCharacterPacket},
                { GameDBPacketListID.RESPONSE_CREATE_CHARACTER, MakeResponseDBCreateCharacterPacket},
                { GameDBPacketListID.RESPONSE_CHAR_BASE_INFO, MakeResponseDBCharBaseInfoPacket},
                { GameDBPacketListID.RESPONSE_UPDATE_GENDER, MakeResponseDBUpdateGenderPacket},
                { GameDBPacketListID.RESPONSE_UPDATE_PRESET, MakeResponseDBUpdatePresetPacket}
            };

            PacketLookUpTable = new Dictionary<Type, Action<DBRecvPacket>>
            {
                { typeof(ResponseDBNeedToMakeCharacterPacket), Func_ResponseNeedToMakeCharacter },
                { typeof(ResponseDBCreateCharacterPacket), Func_ResponseCreateCharacter},
                { typeof(ResponseDBCharBaseInfoPacket), Func_ResponseCharBaseInfo },
                { typeof(ResponseDBUpdateGenderPacket), Func_ResponseUpdateGender },
                { typeof(ResponseDBUpdatePresetPacket), Func_ResponseUpdatePreset }
            };

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

        private dynamic MakeMemoryToPacket(Memory<byte> Packet)
        {
            GameDBPacketListID ID = PacketUtils.GetIDFromPacket<GameDBPacketListID>(ref Packet);

            if(MemoryLookUpTable.TryGetValue(ID,out var MemoryToPacketFunc))
            {
                var TransferedMemory = MemoryToPacketFunc(Packet);
                return TransferedMemory;
            }
            else
            {
                return new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED);
            }
        }

        public void ProcessPacket(dynamic Packet)
        {
            if (IsErrorPacket(Packet, "DBServerRecvProcessPacket"))
                return;
            var PacketType = Packet.GetType();

            if(PacketLookUpTable.TryGetValue(PacketType, out Action<DBRecvPacket> ProcessFunc))
            {
                ProcessFunc(Packet);
            }
            else
            {
                LogManager.GetSingletone.WriteLog("DBServerRecvPacketPipeline.ProcessPacket: 알수 없는 패킷이 들어왔습니다.");
            }
        }

        private dynamic MakeResponseDBNeedToMakeCharacterPacket(Memory<byte> Packet)
        {
            ResponseDBNeedToMakeCharacterPacket? NeedToMakeCharacterPacket = PacketUtils.GetPacketStruct<ResponseDBNeedToMakeCharacterPacket>(ref Packet);
            return NeedToMakeCharacterPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : NeedToMakeCharacterPacket;
        }

        private dynamic MakeResponseDBCreateCharacterPacket(Memory<byte> Packet)
        {
            ResponseDBCreateCharacterPacket? CreateCharacterPacket = PacketUtils.GetPacketStruct<ResponseDBCreateCharacterPacket>(ref Packet);
            return CreateCharacterPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : CreateCharacterPacket;
        }

        private dynamic MakeResponseDBCharBaseInfoPacket(Memory<byte> Packet)
        {
            ResponseDBCharBaseInfoPacket? CharBaseInfoPacket = PacketUtils.GetPacketStruct<ResponseDBCharBaseInfoPacket>(ref Packet);
            return CharBaseInfoPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : CharBaseInfoPacket;
        }

        private dynamic MakeResponseDBUpdateGenderPacket(Memory<byte> Packet)
        {
            ResponseDBUpdateGenderPacket? UpdateGenderPacket = PacketUtils.GetPacketStruct<ResponseDBUpdateGenderPacket>(ref Packet);
            return UpdateGenderPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : UpdateGenderPacket;
        }

        private dynamic MakeResponseDBUpdatePresetPacket(Memory<byte> Packet)
        {
            ResponseDBUpdatePresetPacket? UpdatePresetPacket = PacketUtils.GetPacketStruct<ResponseDBUpdatePresetPacket>(ref Packet);
            return UpdatePresetPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : UpdatePresetPacket;
        }

        private void Func_ResponseNeedToMakeCharacter(DBRecvPacket Packet)
        {
            const int NEED_TO_MAKE_CHARACTER = 1;

            if(Packet is not ResponseDBNeedToMakeCharacterPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog("Func_ResponseNeedToMakeCharacter: ResponseDBNeedToMakeCharacterPacket이 아닌 패킷이 들어왔습니다.");
                return;
            }

            ResponseNeedToMakeCharcterPacket ResponsePacket = new ResponseNeedToMakeCharcterPacket(NEED_TO_MAKE_CHARACTER);
            if (MainProxy.GetSingletone.GetClientSocketByAccountID(ValidPacket.AccountID) == null)
            {
                LogManager.GetSingletone.WriteLog($"Func_ResponseNeedToMakeCharacter: {ValidPacket.AccountID}에 해당하는 클라이언트 소켓이 없습니다.");
                return;
            }
            MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_NEED_TO_MAKE_CHARACTER, ResponsePacket, ValidPacket.AccountID);
        }

        private void Func_ResponseCreateCharacter(DBRecvPacket Packet)
        {
            const int FAIL = -1;

            if(Packet is not ResponseDBCreateCharacterPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog("Func_ResponseCreateCharacter: ResponseDBCreateCharacterPacket이 아닌 패킷이 들어왔습니다.");
                return;
            }

            if (ValidPacket.ErrorCode == FAIL)
            {
                LogManager.GetSingletone.WriteLog($"Func_ResponseCreateCharacter: {ValidPacket.AccountID}에 캐릭터 생성 실패 ErrorCode: {ValidPacket.ErrorCode}");
            }
            MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_CREATE_CHARACTER, new ResponseCreateCharacterPacket(ValidPacket.ErrorCode), ValidPacket.AccountID);
        }

        private void Func_ResponseCharBaseInfo(DBRecvPacket Packet)
        {

            if(Packet is not ResponseDBCharBaseInfoPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog("Func_ResponseCharBaseInfo: ResponseDBCharBaseInfoPacket이 아닌 패킷이 들어왔습니다.");
                return;
            }

            ResponseCharBaseInfoPacket ResponsePacket = new ResponseCharBaseInfoPacket(ValidPacket.AccountID, ValidPacket.Gender, ValidPacket.PresetNumber, ValidPacket.Job,
                ValidPacket.JobLevel, ValidPacket.MapID, ValidPacket.X, ValidPacket.Y, ValidPacket.Level, ValidPacket.EXP, ValidPacket.HP, ValidPacket.MP);
            MainProxy.GetSingletone.CreateCharacter(ValidPacket);
            MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_CHAR_BASE_INFO, ResponsePacket, ValidPacket.AccountID);
            //같은 맵 유저에게 해당 캐릭터를 렌더링하도록 명령내린다.
            SendAnotherCharBaseInfoPacket SendPacket = new SendAnotherCharBaseInfoPacket(ValidPacket.AccountID, ValidPacket.Gender, ValidPacket.PresetNumber, ValidPacket.Job,
                ValidPacket.JobLevel, ValidPacket.MapID, ValidPacket.X, ValidPacket.Y, ValidPacket.Level, ValidPacket.EXP, MainProxy.GetSingletone.GetNickName(ValidPacket.AccountID),
                -1, -1 /*최초 생성시에는 이동 목표가 없다*/, ValidPacket.HP, ValidPacket.MP);

            MainProxy.GetSingletone.SendToSameMap(ValidPacket.MapID, GamePacketListID.SEND_ANOTHER_CHAR_BASE_INFO, SendPacket);
        }

        private void Func_ResponseUpdateGender(DBRecvPacket Packet)
        {

            if(Packet is not ResponseDBUpdateGenderPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog("Func_ResponseUpdateGender: ResponseDBUpdateGenderPacket이 아닌 패킷이 들어왔습니다.");
                return;
            }

            PlayerCharacter? User = MainProxy.GetSingletone.GetCharacterByAccountID(ValidPacket.AccountID);

            if (User == null)
            {
                LogManager.GetSingletone.WriteLog($"Func_ResponseUpdateGender: {ValidPacket.AccountID}에 해당하는 캐릭터가 없습니다.");
                return;
            }

            User.GetAppearanceComponent.ApplyChangeGender();
        }

        private void Func_ResponseUpdatePreset(DBRecvPacket Packet)
        {

            if (Packet is not ResponseDBUpdatePresetPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog("Func_ResponseUpdatePreset: ResponseDBUpdatePresetPacket이 아닌 패킷이 들어왔습니다.");
                return;
            }

            PlayerCharacter? User = MainProxy.GetSingletone.GetCharacterByAccountID(ValidPacket.AccountID);

            if (User == null)
            {
                LogManager.GetSingletone.WriteLog($"Func_ResponseUpdatePreset: {ValidPacket.AccountID}에 해당하는 캐릭터가 없습니다.");
                return;
            }

            User.GetAppearanceComponent.ApplyChangePresetNumber(ValidPacket.PresetNumber);
        }
    }
}
