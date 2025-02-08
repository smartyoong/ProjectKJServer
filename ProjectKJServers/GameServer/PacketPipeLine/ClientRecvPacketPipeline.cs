using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Net.Sockets;
using GameServer.SocketConnect;
using GameServer.PacketList;
using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using GameServer.MainUI;
using GameServer.Object;

// TODO:
//1. 객체 할당 최소화
//2. 메모리 관리 최적화
//3. 가비지 컬렉션 튜닝
//4. 메모리 누수 방지
//5. 최적화된 데이터 구조 사용

namespace GameServer.PacketPipeLine
{
    internal class ClientRecvPacketPipeline
    {
        //private static readonly Lazy<ClientRecvPacketPipeline> instance = new Lazy<ClientRecvPacketPipeline>(() => new ClientRecvPacketPipeline());
        //public static ClientRecvPacketPipeline GetSingletone => instance.Value;

        private CancellationTokenSource CancelToken = new CancellationTokenSource();
        private ExecutionDataflowBlockOptions ProcessorOptions = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = 20,
            MaxDegreeOfParallelism = 10,
            TaskScheduler = TaskScheduler.Default,
            EnsureOrdered = false,
            NameFormat = "ClientRecvPipeLine",
            SingleProducerConstrained = false,
        };
        private TransformBlock<ClientRecvMemoryPipeLineWrapper, ClientRecvPacketPipeLineWrapper> MemoryToPacketBlock;
        private ActionBlock<ClientRecvPacketPipeLineWrapper> PacketProcessBlock;
        private Dictionary<GamePacketListID, Func<Memory<byte>, int, ClientRecvPacketPipeLineWrapper>> MemoryToPacketLookUpTable;
        private Dictionary<Type, Action<ClientRecvPacket, int>> PacketLookUpTable;

        public ClientRecvPacketPipeline()
        {
            MemoryToPacketLookUpTable = new Dictionary<GamePacketListID, Func<Memory<byte>, int, ClientRecvPacketPipeLineWrapper>>
            {
                { GamePacketListID.REQUEST_HASH_AUTH_CHECK, MakeRequestHashAuthCheckPacket },
                { GamePacketListID.REQUEST_CHAR_BASE_INFO, MakeRequestCharBaseInfoPacket },
                { GamePacketListID.REQUEST_CREATE_CHARACTER, MakeRequestCreateCharacterPacket },
                { GamePacketListID.REQUEST_MOVE, MakeRequestMovePacket },
                { GamePacketListID.REQUEST_GET_SAME_MAP_USER, MakeRequestGetSameMapUserPacket },
                { GamePacketListID.REQUEST_PING_CHECK, MakeRequestPingCheckPacket },
                { GamePacketListID.REQUEST_USER_SAY, MakeRequestUserSayPacket }
            };

            PacketLookUpTable = new Dictionary<Type, Action<ClientRecvPacket, int>>
            {
                { typeof(RequestHashAuthCheckPacket), Func_HashAuthCheck },
                { typeof(RequestCharBaseInfoPacket), DB_Func_RequestCharacterBaseInfo },
                { typeof(RequestCreateCharacterPacket), DB_Func_RequestCreateCharacter },
                { typeof(RequestMovePacket), Func_RequestMove },
                { typeof(RequestGetSameMapUserPacket), Func_GetSameMapUser },
                { typeof(RequestPingCheckPacket), Func_RequestPingCheckPacket },
                { typeof(RequestUserSayPacket), Func_ReuqestUserSayPacket }
            };

            MemoryToPacketBlock = new TransformBlock<ClientRecvMemoryPipeLineWrapper, ClientRecvPacketPipeLineWrapper>(MakeMemoryToPacket, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 10,
                MaxDegreeOfParallelism = 5,
                NameFormat = "ClientRecvPipeLine.MemoryToPacketBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            PacketProcessBlock = new ActionBlock<ClientRecvPacketPipeLineWrapper>(ProcessPacket, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 100,
                MaxDegreeOfParallelism = 50,
                NameFormat = "ClientRecvPipeLine.PacketProcessBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            MemoryToPacketBlock.LinkTo(PacketProcessBlock, new DataflowLinkOptions { PropagateCompletion = true });
            ProcessBlock();
            LogManager.GetSingletone.WriteLog("ClientRecvPacketPipeline 생성 완료");
        }

        public void PushToPacketPipeline(Memory<byte> packet, int socketID)
        {

            MemoryToPacketBlock.Post(new ClientRecvMemoryPipeLineWrapper(packet, socketID));
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
                ProcessGeneralErrorCode(Packet.ErrorCode, $"ClientRecvPacketPipeline {message}에서 에러 발생");
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

        private ClientRecvPacketPipeLineWrapper MakeMemoryToPacket(ClientRecvMemoryPipeLineWrapper Packet)
        {
            var Data = Packet.MemoryData;
            GamePacketListID ID = PacketUtils.GetIDFromPacket<GamePacketListID>(ref Data);

            if(MemoryToPacketLookUpTable.TryGetValue(ID, out var PacketFunc))
            {
                return PacketFunc(Data,Packet.ClientID);
            }
            else
            {
                return new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED), Packet.ClientID);
            }
        }

        public void ProcessPacket(ClientRecvPacketPipeLineWrapper Packet)
        {
            if (IsErrorPacket(Packet.Packet, "ProcessPacket"))
                return;
            Type PacketType = Packet.Packet.GetType();
            if (PacketLookUpTable.TryGetValue(PacketType, out var PacketFunc))
            {
                PacketFunc(Packet.Packet, Packet.ClientID);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"ProcessPacket에서 패킷이 할당되지 않았습니다. {Packet.ToString()}");
            }
        }

        private ClientRecvPacketPipeLineWrapper MakeRequestHashAuthCheckPacket(Memory<byte> Packet, int ClientID)
        {
            RequestHashAuthCheckPacket? RequestPacket = PacketUtils.GetPacketStruct<RequestHashAuthCheckPacket>(ref Packet);
            return RequestPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), ClientID) :
                new ClientRecvPacketPipeLineWrapper(RequestPacket, ClientID);
        }

        private ClientRecvPacketPipeLineWrapper MakeRequestCharBaseInfoPacket(Memory<byte> Packet, int ClientID)
        {
            RequestCharBaseInfoPacket? RequestPacket = PacketUtils.GetPacketStruct<RequestCharBaseInfoPacket>(ref Packet);
            return RequestPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), ClientID) :
                new ClientRecvPacketPipeLineWrapper(RequestPacket, ClientID);
        }

        private ClientRecvPacketPipeLineWrapper MakeRequestCreateCharacterPacket(Memory<byte> Packet, int ClientID)
        {
            RequestCreateCharacterPacket? RequestPacket = PacketUtils.GetPacketStruct<RequestCreateCharacterPacket>(ref Packet);
            return RequestPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), ClientID) :
                new ClientRecvPacketPipeLineWrapper(RequestPacket, ClientID);
        }

        private ClientRecvPacketPipeLineWrapper MakeRequestMovePacket(Memory<byte> Packet, int ClientID)
        {
            RequestMovePacket? RequestPacket = PacketUtils.GetPacketStruct<RequestMovePacket>(ref Packet);
            return RequestPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), ClientID) :
                new ClientRecvPacketPipeLineWrapper(RequestPacket, ClientID);
        }

        private ClientRecvPacketPipeLineWrapper MakeRequestGetSameMapUserPacket(Memory<byte> Packet, int ClientID)
        {
            RequestGetSameMapUserPacket? RequestPacket = PacketUtils.GetPacketStruct<RequestGetSameMapUserPacket>(ref Packet);
            return RequestPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), ClientID) :
                new ClientRecvPacketPipeLineWrapper(RequestPacket, ClientID);
        }

        private ClientRecvPacketPipeLineWrapper MakeRequestPingCheckPacket(Memory<byte> Packet, int ClientID)
        {
            RequestPingCheckPacket? RequestPacket = PacketUtils.GetPacketStruct<RequestPingCheckPacket>(ref Packet);
            return RequestPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), ClientID) :
                new ClientRecvPacketPipeLineWrapper(RequestPacket, ClientID);
        }

        private ClientRecvPacketPipeLineWrapper MakeRequestUserSayPacket(Memory<byte> Packet, int ClientID)
        {
            RequestUserSayPacket? RequestPacket = PacketUtils.GetPacketStruct<RequestUserSayPacket>(ref Packet);
            return RequestPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), ClientID) :
                new ClientRecvPacketPipeLineWrapper(RequestPacket, ClientID);
        }

        private void SendAuthFailToClient(string AccountID, int ClientID)
        {
            MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_HASH_AUTH_CHECK,
                new ResponseHashAuthCheckPacket(AccountID, (int)GeneralErrorCode.ERR_AUTH_FAIL), ClientID);
        }

        private void Func_HashAuthCheck(ClientRecvPacket Packet, int ClientID)
        {
            if (Packet is not RequestHashAuthCheckPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"Func_HashAuthCheck 메서드에 다른 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            string HashCode = string.Empty;
            // 닉네임이 최초에는 전부 Guest인데,,, 소켓은 이거 인증 못받으면 닉네임이랑 매핑안됨
            GeneralErrorCode ErrorCode = MainProxy.GetSingletone.CheckAuthHashCode(ValidPacket.AccountID, ref HashCode);
            switch (ErrorCode)
            {
                case GeneralErrorCode.ERR_HASH_CODE_IS_NOT_REGIST:
                    MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_HASH_AUTH_CHECK,
                        new ResponseHashAuthCheckPacket(ValidPacket.AccountID, (int)GeneralErrorCode.ERR_HASH_CODE_IS_NOT_REGIST), ClientID);
                    //LogManager.GetSingletone.WriteLog($"해시 코드 등록전입니다. {Packet.AccountID} {Packet.HashCode}");
                    break;
                case GeneralErrorCode.ERR_AUTH_SUCCESS:
                    if (ValidPacket.HashCode == HashCode)
                    {
                        MainProxy.GetSingletone.MappingSocketAccountID(MainProxy.GetSingletone.GetClientSocket(ClientID)!, ValidPacket.AccountID);

                        MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_HASH_AUTH_CHECK,
                            new ResponseHashAuthCheckPacket(ValidPacket.AccountID, (int)GeneralErrorCode.ERR_AUTH_SUCCESS), ClientID);

                        //LogManager.GetSingletone.WriteLog($"{Packet.AccountID} 해시코드 인증 성공 {Packet.HashCode}");
                    }
                    else
                    {
                        if (HashCode == "NONEHASH")
                            LogManager.GetSingletone.WriteLog($"{ValidPacket.AccountID} 해시코드가 없습니다. {ValidPacket.HashCode}");

                        MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_HASH_AUTH_CHECK,
                            new ResponseHashAuthCheckPacket(ValidPacket.AccountID, (int)GeneralErrorCode.ERR_AUTH_FAIL), ClientID);
                    }
                    break;
                case GeneralErrorCode.ERR_AUTH_FAIL:
                    MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_HASH_AUTH_CHECK,
                        new ResponseHashAuthCheckPacket(ValidPacket.AccountID, (int)GeneralErrorCode.ERR_AUTH_FAIL), ClientID);
                    break;

            }
        }

        private bool CheckHashCodeIfFailSend(string AccountID, ref string HashCode, int ClientID, string DebugStr = "default")
        {
            var ErrorCode = MainProxy.GetSingletone.CheckAuthHashCode(AccountID, ref HashCode);
            if (GeneralErrorCode.ERR_AUTH_FAIL == ErrorCode || ErrorCode == GeneralErrorCode.ERR_HASH_CODE_IS_NOT_REGIST)
            {
                SendAuthFailToClient(AccountID, ClientID);
                // 해쉬 코드 인증 실패
                LogManager.GetSingletone.WriteLog($"해시 코드 인증 실패 {DebugStr} : {AccountID} {HashCode}");
                return false;
            }
            return true;
        }

        private void DB_Func_RequestCharacterBaseInfo(ClientRecvPacket Packet, int ClientID)
        {
            if (Packet is not RequestCharBaseInfoPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"DB_Func_RequestCharacterBaseInfo 메서드에 다른 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            string HashCode = ValidPacket.HashCode;
            if(!CheckHashCodeIfFailSend(ValidPacket.AccountID, ref HashCode, ClientID, "DB_Func_RequestCharacterBaseInfo"))
                return;
            // DB에서 캐릭터 정보를 가져온다.
            RequestDBCharBaseInfoPacket DBPacket = new RequestDBCharBaseInfoPacket(ValidPacket.AccountID, ValidPacket.NickName);
            MainProxy.GetSingletone.SendToDBServer(GameDBPacketListID.REQUEST_CHAR_BASE_INFO, DBPacket);
        }

        private void DB_Func_RequestCreateCharacter(ClientRecvPacket Packet, int ClientID)
        {
            if (Packet is not RequestCreateCharacterPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"DB_Func_RequestCreateCharacter 메서드에 다른 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            string HashCode = ValidPacket.HashCode;
            if(!CheckHashCodeIfFailSend(ValidPacket.AccountID, ref HashCode, ClientID, "DB_Func_RequestCreateCharacter"))
                return;
            // DB에다가 캐릭터 생성을 요청한다.
            RequestDBCreateCharacterPacket DBPacket = new RequestDBCreateCharacterPacket(ValidPacket.AccountID, ValidPacket.Gender, ValidPacket.PresetID);
            MainProxy.GetSingletone.SendToDBServer(GameDBPacketListID.REQUEST_CREATE_CHARACTER, DBPacket);
        }

        private void Func_RequestMove(ClientRecvPacket Packet, int ClientID)
        {

            if (Packet is not RequestMovePacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"Func_RequestMove 메서드에 다른 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            string HashCode = ValidPacket.HashCode;
            if(!CheckHashCodeIfFailSend(ValidPacket.AccountID, ref HashCode, ClientID, "Func_RequestMove"))
                return;
            // 이동 패킷을 처리한다.
            PlayerCharacter? Character = MainProxy.GetSingletone.GetPlayerCharacter(ValidPacket.AccountID);
            if (Character == null)
            {
                LogManager.GetSingletone.WriteLog($"캐릭터가 없습니다. {ValidPacket.AccountID}");
                MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_MOVE, new ResponseMovePacket(1/*실패*/), ClientID);
                return;
            }
            // 이동 패킷을 처리한다.
            if(Character.MoveToLocation(new System.Numerics.Vector3(ValidPacket.X, ValidPacket.Y, 0)))
            {
                MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_MOVE, new ResponseMovePacket(0/*성공*/), ClientID);
                MainProxy.GetSingletone.SendToSameMap(ValidPacket.MapID,GamePacketListID.SEND_USER_MOVE,new SendUserMovePacket(ValidPacket.AccountID, ValidPacket.X, ValidPacket.Y));
            }
            else
            {
                MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_MOVE, new ResponseMovePacket(1/*실패*/), ClientID);
            }
        }

        private void Func_GetSameMapUser(ClientRecvPacket Packet, int ClientID)
        {
            if (Packet is not RequestGetSameMapUserPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"Func_GetSameMapUser 메서드에 다른 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            string HashCode = ValidPacket.HashCode;
            if(!CheckHashCodeIfFailSend(ValidPacket.AccountID, ref HashCode, ClientID, "Func_GetSameMapUser"))
                return;

            // 같은 맵에 있는 유저들을 가져온다.
            //새로 생성된 유저에게 해당 맵의 유저들을 렌더링하도록 명령내린다.
            var MapUsers = MainProxy.GetSingletone.GetMapUsers(ValidPacket.MapID);
            if (MapUsers == null)
                return;

            foreach (var User in MapUsers)
            {
                PlayerCharacter? Character = MainProxy.GetSingletone.GetCharacterByAccountID(User.GetName);
                if (Character == null)
                    continue;
                if (Character.GetAccountInfo.AccountID == ValidPacket.AccountID)
                    continue;
                System.Numerics.Vector3 Destination = Character.GetMovementComponent.TargetStaticData.Position;
                System.Numerics.Vector3 Position = Character.GetMovementComponent.CharcaterStaticData.Position;
                string NickName = MainProxy.GetSingletone.GetNickName(User.GetName);
                // 목표점이 이동점과 같다는 것은 이동중이 아니라는 것을 의미한다.
                if (Destination == Position)
                {
                    Destination.X = -1;
                    Destination.Y = -1;
                    Destination.Z = -1;
                }
                SendAnotherCharBaseInfoPacket SendPacketToNewUser = new SendAnotherCharBaseInfoPacket(User.GetName, (int)Character.GetAppearanceComponent.GetGender,
                    Character.GetAppearanceComponent.GetPresetNumber, (int)Character.GetJobComponent.GetJob, Character.GetJobComponent.GetJobLevel, Character.GetCurrentMapID,
                    (int)Position.X, (int)Position.Y, Character.GetLevelComponent.GetLevel, Character.GetLevelComponent.GetEXP, NickName, (int)Destination.X, (int)Destination.Y
                    , Character.GetHPComponent.CurrentHealthPoint, Character.GetMPComponent.CurrentMagicPoint);

                MainProxy.GetSingletone.SendToClient(GamePacketListID.SEND_ANOTHER_CHAR_BASE_INFO, SendPacketToNewUser, ValidPacket.AccountID);
                LogManager.GetSingletone.WriteLog($"Func_ResponseCharBaseInfo: {ValidPacket.AccountID}에게 {User.GetName}의 정보를 보냈습니다.");
            }
        }

        private void Func_RequestPingCheckPacket(ClientRecvPacket Packet, int ClientID)
        {
            if(Packet is not RequestPingCheckPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"Func_RequestPingCheckPacket 메서드에 다른 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }
            // 해쉬코드 인증이 필요없는 단순 핑 체크용 패킷
            MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_PING_CHECK, new ResponsePingCheckPacket(ValidPacket.Hour, ValidPacket.Min, ValidPacket.Secs, ValidPacket.MSecs), ClientID);
        }

        private void Func_ReuqestUserSayPacket(ClientRecvPacket Packet, int ClientID)
        {
            if (Packet is not RequestUserSayPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"Func_ReuqestUserSayPacket 메서드에 다른 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            // 채팅은 해쉬 코드 무시하자 DB 처리까지 필요 없으니까 필요하면 주석 제거
            //string HashCode = ValidPacket.HashCode;
            //if(!CheckHashCodeIfFailSend(ValidPacket.AccountID, ref HashCode, ClientID, "Func_ReuqestUserSayPacket"))
            //    return;

            // 채팅 패킷을 처리한다.
            PlayerCharacter? Character = MainProxy.GetSingletone.GetPlayerCharacter(ValidPacket.AccountID);
            if (Character == null)
            {
                LogManager.GetSingletone.WriteLog($"캐릭터가 없습니다. {ValidPacket.AccountID}");
                return;
            }
            Character.Say(ValidPacket.Message);
        }
    }
}
