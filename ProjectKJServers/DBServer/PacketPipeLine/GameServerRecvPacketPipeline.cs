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
        private Dictionary<DBPacketListID, Func<Memory<byte>, dynamic>> MemoryLookUpTable;
        private Dictionary<Type, Action<GameRecvPacket>> PacketLookUpTable;




        public GameServerRecvPacketPipeline()
        {
            MemoryLookUpTable = new Dictionary<DBPacketListID, Func<Memory<byte>, dynamic>>()
            {
                { DBPacketListID.REQUEST_CHAR_BASE_INFO, MakeRequestDBCharBaseInfoPacket },
                { DBPacketListID.REQUEST_CREATE_CHARACTER, MakeRequestDBCreateCharacterPacket },
                { DBPacketListID.REQUEST_UPDATE_HEALTH_POINT, MakeRequestDBUpdateHealthPointPacket },
                { DBPacketListID.REQUEST_UPDATE_MAGIC_POINT, MakeRequestDBUpdateMagicPointPacket },
                { DBPacketListID.REQUEST_UPDATE_LEVEL_EXP, MakeRequestDBUpdateLevelExpPacket },
                { DBPacketListID.REQUEST_UPDATE_JOB_LEVEL, MakeRequestDBUpdateJobLevelPacket },
                { DBPacketListID.REQUEST_UPDATE_JOB, MakeRequestDBUpdateJobPacket },
                { DBPacketListID.REQUEST_UPDATE_GENDER, MakeRequestDBUpdateGenderPacket },
                { DBPacketListID.REQUEST_UPDATE_PRESET, MakeRequestDBUpdatePresetPacket }
            };

            PacketLookUpTable = new Dictionary<Type, Action<GameRecvPacket>>()
            {
                { typeof(RequestDBCharBaseInfoPacket), SP_RequestCharBaseInfo },
                { typeof(RequestDBCreateCharacterPacket), SP_ReuquestCreateCharacter },
                { typeof(RequestDBUpdateHealthPointPacket), SP_RequestUpdateHealthPoint },
                { typeof(RequestDBUpdateMagicPointPacket), SP_RequestUpdateMagicPoint },
                { typeof(RequestDBUpdateLevelExpPacket), SP_RequestUpdateLevelEXP },
                { typeof(RequestDBUpdateJobLevelPacket), SP_RequestUpdateJobLevel },
                { typeof(RequestDBUpdateJobPacket), SP_RequestUpdateJob },
                { typeof(RequestDBUpdateGenderPacket), SP_RequestUpdateGender },
                { typeof(RequestDBUpdatePresetPacket), SP_RequestUpdatePreset }
            };

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

        private dynamic MakeMemoryToPacket(Memory<byte> Packet)
        {
            DBPacketListID ID = PacketUtils.GetIDFromPacket<DBPacketListID>(ref Packet);
            if(MemoryLookUpTable.TryGetValue(ID,out var MemoryFunc))
            {
                return MemoryFunc(Packet);
            }
            else
            {
                return new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED);
            }
        }

        private dynamic MakeRequestDBCharBaseInfoPacket(Memory<byte> packet)
        {
            RequestDBCharBaseInfoPacket? CharBaseInfoPacket = PacketUtils.GetPacketStruct<RequestDBCharBaseInfoPacket>(ref packet);
            return CharBaseInfoPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : CharBaseInfoPacket;
        }

        private dynamic MakeRequestDBCreateCharacterPacket(Memory<byte> packet)
        {
            RequestDBCreateCharacterPacket? CreateCharacterPacket = PacketUtils.GetPacketStruct<RequestDBCreateCharacterPacket>(ref packet);
            return CreateCharacterPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : CreateCharacterPacket;
        }

        private dynamic MakeRequestDBUpdateHealthPointPacket(Memory<byte> packet)
        {
            RequestDBUpdateHealthPointPacket? UpdateHealthPointPacket = PacketUtils.GetPacketStruct<RequestDBUpdateHealthPointPacket>(ref packet);
            return UpdateHealthPointPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : UpdateHealthPointPacket;
        }

        private dynamic MakeRequestDBUpdateMagicPointPacket(Memory<byte> packet)
        {
            RequestDBUpdateMagicPointPacket? UpdateMagicPointPacket = PacketUtils.GetPacketStruct<RequestDBUpdateMagicPointPacket>(ref packet);
            return UpdateMagicPointPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : UpdateMagicPointPacket;
        }

        private dynamic MakeRequestDBUpdateLevelExpPacket(Memory<byte> packet)
        {
            RequestDBUpdateLevelExpPacket? UpdateLevelExpPacket = PacketUtils.GetPacketStruct<RequestDBUpdateLevelExpPacket>(ref packet);
            return UpdateLevelExpPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : UpdateLevelExpPacket;
        }

        private dynamic MakeRequestDBUpdateJobLevelPacket(Memory<byte> packet)
        {
            RequestDBUpdateJobLevelPacket? UpdateJobLevelPacket = PacketUtils.GetPacketStruct<RequestDBUpdateJobLevelPacket>(ref packet);
            return UpdateJobLevelPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : UpdateJobLevelPacket;
        }

        private dynamic MakeRequestDBUpdateJobPacket(Memory<byte> packet)
        {
            RequestDBUpdateJobPacket? UpdateJobPacket = PacketUtils.GetPacketStruct<RequestDBUpdateJobPacket>(ref packet);
            return UpdateJobPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : UpdateJobPacket;
        }

        private dynamic MakeRequestDBUpdateGenderPacket(Memory<byte> packet)
        {
            RequestDBUpdateGenderPacket? UpdateGenderPacket = PacketUtils.GetPacketStruct<RequestDBUpdateGenderPacket>(ref packet);
            return UpdateGenderPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : UpdateGenderPacket;
        }

        private dynamic MakeRequestDBUpdatePresetPacket(Memory<byte> packet)
        {
            RequestDBUpdatePresetPacket? UpdatePresetPacket = PacketUtils.GetPacketStruct<RequestDBUpdatePresetPacket>(ref packet);
            return UpdatePresetPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : UpdatePresetPacket;
        }

        public void ProcessPacket(dynamic Packet)
        {
            if (IsErrorPacket(Packet, "GameServerRecvProcessPacket"))
                return;

            if (PacketLookUpTable.TryGetValue(Packet.GetType(), out Action<GameRecvPacket> PacketFunc))
            {
                PacketFunc(Packet);
            }
            else
            {
                LogManager.GetSingletone.WriteLog("GameServerRecvPacketPipeline.ProcessPacket: 알수 없는 패킷이 들어왔습니다.");
            }
        }

        private void SP_RequestCharBaseInfo(GameRecvPacket Packet)
        {
            if(Packet is not RequestDBCharBaseInfoPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SP_RequestCharBaseInfo: RequestDBCharBaseInfoPacket이 아닙니다. {Packet}");
                return;
            }

            if (IsErrorPacket(ValidPacket, "RequestCharBaseInfo"))
                return;

            GameSQLReadCharacterPacket ReadCharacterPacket = new GameSQLReadCharacterPacket(ValidPacket.AccountID, ValidPacket.NickName);
            MainProxy.GetSingletone.HandleSQLPacket(ReadCharacterPacket);
        }

        private void SP_ReuquestCreateCharacter(GameRecvPacket Packet)
        {
            if(Packet is not RequestDBCreateCharacterPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SP_ReuquestCreateCharacter: RequestDBCreateCharacterPacket이 아닙니다. {Packet}");
                return;
            }

            if (IsErrorPacket(ValidPacket, "RequestCreateCharacter"))
                return;

            GameSQLCreateCharacterPacket CreateCharacterPacket = new GameSQLCreateCharacterPacket(ValidPacket.AccountID, ValidPacket.Gender, ValidPacket.PresetID);
            MainProxy.GetSingletone.HandleSQLPacket(CreateCharacterPacket);
        }

        private void SP_RequestUpdateHealthPoint(GameRecvPacket Packet)
        {
            if(Packet is not RequestDBUpdateHealthPointPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SP_RequestUpdateHealthPoint: RequestDBUpdateHealthPointPacket이 아닙니다. {Packet}");
                return;
            }

            if (IsErrorPacket(ValidPacket, "RequestUpdateHealthPoint"))
                return;

            GameSQLUpdateHealthPoint UpdateHealthPacket = new GameSQLUpdateHealthPoint(ValidPacket.AccountID, ValidPacket.CurrentHP);
            MainProxy.GetSingletone.HandleSQLPacket(UpdateHealthPacket);
        }

        private void SP_RequestUpdateMagicPoint(GameRecvPacket Packet)
        {
            if (Packet is not RequestDBUpdateMagicPointPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SP_RequestUpdateMagicPoint: RequestDBUpdateMagicPointPacket이 아닙니다. {Packet}");
                return;
            }

            if (IsErrorPacket(ValidPacket, "RequestUpdateMagicPoint"))
                return;

            GameSQLUpdateMagicPoint UpdateMagicPacket = new GameSQLUpdateMagicPoint(ValidPacket.AccountID, ValidPacket.CurrentMP);
            MainProxy.GetSingletone.HandleSQLPacket(UpdateMagicPacket);
        }

        private void SP_RequestUpdateLevelEXP(GameRecvPacket Packet)
        {
            if(Packet is not RequestDBUpdateLevelExpPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SP_RequestUpdateLevelEXP: RequestDBUpdateLevelExpPacket이 아닙니다. {Packet}");
                return;
            }

            if (IsErrorPacket(ValidPacket, "RequestUpdateLevelEXP"))
                return;

            GameSQLUpdateLevelEXP UpdateLevelExpPacket = new GameSQLUpdateLevelEXP(ValidPacket.AccountID, ValidPacket.Level, ValidPacket.CurrentEXP);
            MainProxy.GetSingletone.HandleSQLPacket(UpdateLevelExpPacket);
        }

        private void SP_RequestUpdateJobLevel(GameRecvPacket Packet)
        {
            if(Packet is not RequestDBUpdateJobLevelPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SP_RequestUpdateJobLevel: RequestDBUpdateJobLevelPacket이 아닙니다. {Packet}");
                return;
            }

            if (IsErrorPacket(ValidPacket, "RequestUpdateJobLevel"))
                return;

            GameSQLUpdateJobLevel UpdateJobLevelPacket = new GameSQLUpdateJobLevel(ValidPacket.AccountID, ValidPacket.Level);
            MainProxy.GetSingletone.HandleSQLPacket(UpdateJobLevelPacket);
        }

        private void SP_RequestUpdateJob(GameRecvPacket Packet)
        {
            if(Packet is not RequestDBUpdateJobPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SP_RequestUpdateJob: RequestDBUpdateJobPacket이 아닙니다. {Packet}");
                return;
            }

            if (IsErrorPacket(ValidPacket, "RequestUpdateJob"))
                return;

            GameSQLUpdateJob UpdateJobPacket = new GameSQLUpdateJob(ValidPacket.AccountID, ValidPacket.Job);
            MainProxy.GetSingletone.HandleSQLPacket(UpdateJobPacket);
        }

        private void SP_RequestUpdateGender(GameRecvPacket Packet)
        {
            if (Packet is not RequestDBUpdateGenderPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SP_RequestUpdateGender: RequestDBUpdateGenderPacket이 아닙니다. {Packet}");
                return;
            }

                if (IsErrorPacket(ValidPacket, "RequestUpdateGender"))
                return;

            GameSQLUpdateGender UpdateGenderPacket = new GameSQLUpdateGender(ValidPacket.AccountID, ValidPacket.Gender);
            MainProxy.GetSingletone.HandleSQLPacket(UpdateGenderPacket);
        }

        private void SP_RequestUpdatePreset(GameRecvPacket Packet)
        {
            if(Packet is not RequestDBUpdatePresetPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SP_RequestUpdatePreset: RequestDBUpdatePresetPacket이 아닙니다. {Packet}");
                return;
            }

            if (IsErrorPacket(ValidPacket, "RequestUpdatePreset"))
                return;

            GameSQLUpdatePreset UpdatePresetPacket = new GameSQLUpdatePreset(ValidPacket.AccountID, ValidPacket.PresetNumber);
            MainProxy.GetSingletone.HandleSQLPacket(UpdatePresetPacket);
        }
    }
}
