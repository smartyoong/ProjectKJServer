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


        public ClientRecvPacketPipeline()
        {

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

            switch (ID)
            {
                case GamePacketListID.REQUEST_HASH_AUTH_CHECK:
                    RequestHashAuthCheckPacket? RequestHashAuthCheckPacket = PacketUtils.GetPacketStruct<RequestHashAuthCheckPacket>(ref Data);
                    return RequestHashAuthCheckPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), Packet.ClientID) :
                        new ClientRecvPacketPipeLineWrapper(RequestHashAuthCheckPacket, Packet.ClientID);
                case GamePacketListID.REQUEST_CHAR_BASE_INFO:
                    RequestCharBaseInfoPacket? RequestCharBaseInfoPacket = PacketUtils.GetPacketStruct<RequestCharBaseInfoPacket>(ref Data);
                    return RequestCharBaseInfoPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), Packet.ClientID) :
                        new ClientRecvPacketPipeLineWrapper(RequestCharBaseInfoPacket, Packet.ClientID);
                case GamePacketListID.REQUEST_CREATE_CHARACTER:
                    RequestCreateCharacterPacket? RequestCreateCharacterPacket = PacketUtils.GetPacketStruct<RequestCreateCharacterPacket>(ref Data);
                    return RequestCreateCharacterPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), Packet.ClientID) :
                        new ClientRecvPacketPipeLineWrapper(RequestCreateCharacterPacket, Packet.ClientID);
                case GamePacketListID.REQUEST_MOVE:
                    RequestMovePacket? RequestMovePacket = PacketUtils.GetPacketStruct<RequestMovePacket>(ref Data);
                    return RequestMovePacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), Packet.ClientID) :
                        new ClientRecvPacketPipeLineWrapper(RequestMovePacket, Packet.ClientID);
                case GamePacketListID.REQUEST_GET_SAME_MAP_USER:
                    RequestGetSameMapUserPacket? RequestGetSameMapUserPacket = PacketUtils.GetPacketStruct<RequestGetSameMapUserPacket>(ref Data);
                    return RequestGetSameMapUserPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), Packet.ClientID) :
                        new ClientRecvPacketPipeLineWrapper(RequestGetSameMapUserPacket, Packet.ClientID);
                case GamePacketListID.REQUEST_PING_CHECK:
                    RequestPingCheckPacket? RequestPingCheckPacket = PacketUtils.GetPacketStruct<RequestPingCheckPacket>(ref Data);
                    return RequestPingCheckPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), Packet.ClientID) :
                        new ClientRecvPacketPipeLineWrapper(RequestPingCheckPacket, Packet.ClientID);
                default:
                    return new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED), Packet.ClientID);
            }
        }

        public void ProcessPacket(ClientRecvPacketPipeLineWrapper Packet)
        {
            if (IsErrorPacket(Packet.Packet, "ProcessPacket"))
                return;
            switch (Packet.Packet)
            {
                case RequestHashAuthCheckPacket RequestPacket:
                    Func_HashAuthCheck(RequestPacket, Packet.ClientID);
                    break;
                case RequestCharBaseInfoPacket RequestCharBaseInfoPacket:
                    DB_Func_RequestCharacterBaseInfo(RequestCharBaseInfoPacket, Packet.ClientID);
                    break;
                case RequestCreateCharacterPacket RequestCreateCharacterPacket:
                    DB_Func_RequestCreateCharacter(RequestCreateCharacterPacket, Packet.ClientID);
                    break;
                case RequestMovePacket RequestMovePacket:
                    Func_RequestMove(RequestMovePacket, Packet.ClientID);
                    break;
                case RequestGetSameMapUserPacket RequestGetSameMapUserPacket:
                    Func_GetSameMapUser(RequestGetSameMapUserPacket, Packet.ClientID);
                    break;
                case RequestPingCheckPacket RequestPingCheckPacket:
                    Func_RequestPingCheckPacket(RequestPingCheckPacket, Packet.ClientID);
                    break;
                default:
                    LogManager.GetSingletone.WriteLog($"ProcessPacket에서 패킷이 할당되지 않았습니다. {Packet.ToString()}");
                    break;
            }
        }

        private void SendAuthFailToClient(string AccountID ,int ClientID)
        {
            MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_HASH_AUTH_CHECK, 
                new ResponseHashAuthCheckPacket(AccountID, (int)GeneralErrorCode.ERR_AUTH_FAIL), ClientID);
        }

        private void Func_HashAuthCheck(RequestHashAuthCheckPacket Packet, int ClientID)
        {
            string HashCode = string.Empty;
            // 닉네임이 최초에는 전부 Guest인데,,, 소켓은 이거 인증 못받으면 닉네임이랑 매핑안됨
            GeneralErrorCode ErrorCode = MainProxy.GetSingletone.CheckAuthHashCode(Packet.AccountID, ref HashCode);
            switch (ErrorCode)
            {
                case GeneralErrorCode.ERR_HASH_CODE_IS_NOT_REGIST:
                    MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_HASH_AUTH_CHECK,
                        new ResponseHashAuthCheckPacket(Packet.AccountID, (int)GeneralErrorCode.ERR_HASH_CODE_IS_NOT_REGIST), ClientID);
                    //LogManager.GetSingletone.WriteLog($"해시 코드 등록전입니다. {Packet.AccountID} {Packet.HashCode}");
                    break;
                case GeneralErrorCode.ERR_AUTH_SUCCESS:
                    if (Packet.HashCode == HashCode)
                    {
                        MainProxy.GetSingletone.MappingSocketAccountID(MainProxy.GetSingletone.GetClientSocket(ClientID)!, Packet.AccountID);

                        MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_HASH_AUTH_CHECK,
                            new ResponseHashAuthCheckPacket(Packet.AccountID, (int)GeneralErrorCode.ERR_AUTH_SUCCESS), ClientID);

                        //LogManager.GetSingletone.WriteLog($"{Packet.AccountID} 해시코드 인증 성공 {Packet.HashCode}");
                    }
                    else
                    {
                        if (HashCode == "NONEHASH")
                            LogManager.GetSingletone.WriteLog($"{Packet.AccountID} 해시코드가 없습니다. {Packet.HashCode}");

                        MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_HASH_AUTH_CHECK,
                            new ResponseHashAuthCheckPacket(Packet.AccountID, (int)GeneralErrorCode.ERR_AUTH_FAIL), ClientID);
                    }
                    break;
                case GeneralErrorCode.ERR_AUTH_FAIL:
                    MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_HASH_AUTH_CHECK,
                        new ResponseHashAuthCheckPacket(Packet.AccountID, (int)GeneralErrorCode.ERR_AUTH_FAIL), ClientID);
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

        private void DB_Func_RequestCharacterBaseInfo(RequestCharBaseInfoPacket Packet, int ClientID)
        {
            string HashCode = Packet.HashCode;
            if(!CheckHashCodeIfFailSend(Packet.AccountID, ref HashCode, ClientID, "DB_Func_RequestCharacterBaseInfo"))
                return;
            // DB에서 캐릭터 정보를 가져온다.
            RequestDBCharBaseInfoPacket DBPacket = new RequestDBCharBaseInfoPacket(Packet.AccountID, Packet.NickName);
            MainProxy.GetSingletone.SendToDBServer(GameDBPacketListID.REQUEST_CHAR_BASE_INFO, DBPacket);
        }

        private void DB_Func_RequestCreateCharacter(RequestCreateCharacterPacket Packet, int ClientID)
        {
            string HashCode = Packet.HashCode;
            if(!CheckHashCodeIfFailSend(Packet.AccountID, ref HashCode, ClientID, "DB_Func_RequestCreateCharacter"))
                return;
            // DB에다가 캐릭터 생성을 요청한다.
            RequestDBCreateCharacterPacket DBPacket = new RequestDBCreateCharacterPacket(Packet.AccountID,Packet.Gender,Packet.PresetID);
            MainProxy.GetSingletone.SendToDBServer(GameDBPacketListID.REQUEST_CREATE_CHARACTER, DBPacket);
        }

        private void Func_RequestMove(RequestMovePacket Packet, int ClientID)
        {
            string HashCode = Packet.HashCode;
            if(!CheckHashCodeIfFailSend(Packet.AccountID, ref HashCode, ClientID, "Func_RequestMove"))
                return;
            // 이동 패킷을 처리한다.
            PlayerCharacter? Character = MainProxy.GetSingletone.GetPlayerCharacter(Packet.AccountID);
            if (Character == null)
            {
                LogManager.GetSingletone.WriteLog($"캐릭터가 없습니다. {Packet.AccountID}");
                MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_MOVE, new ResponseMovePacket(1/*실패*/), ClientID);
                return;
            }
            // 이동 패킷을 처리한다.
            if(Character.MoveToLocation(new System.Numerics.Vector3(Packet.X, Packet.Y, 0)))
            {
                MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_MOVE, new ResponseMovePacket(0/*성공*/), ClientID);
                MainProxy.GetSingletone.SendToSameMap(Packet.MapID,GamePacketListID.SEND_USER_MOVE,new SendUserMovePacket(Packet.AccountID,Packet.X,Packet.Y));
            }
            else
            {
                MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_MOVE, new ResponseMovePacket(1/*실패*/), ClientID);
            }
        }

        private void Func_GetSameMapUser(RequestGetSameMapUserPacket Packet, int ClientID)
        {
            string HashCode = Packet.HashCode;
            if(!CheckHashCodeIfFailSend(Packet.AccountID, ref HashCode, ClientID, "Func_GetSameMapUser"))
                return;

            // 같은 맵에 있는 유저들을 가져온다.
            //새로 생성된 유저에게 해당 맵의 유저들을 렌더링하도록 명령내린다.
            var MapUsers = MainProxy.GetSingletone.GetMapUsers(Packet.MapID);
            if (MapUsers == null)
                return;

            foreach (var User in MapUsers)
            {
                PlayerCharacter? Character = MainProxy.GetSingletone.GetCharacterByAccountID(User.GetAccountID());
                if (Character == null)
                    continue;
                if (Character.GetAccountInfo().AccountID == Packet.AccountID)
                    continue;
                System.Numerics.Vector3 Destination = Character.GetMovementComponent().TargetStaticData.Position;
                System.Numerics.Vector3 Position = Character.GetMovementComponent().CharcaterStaticData.Position;
                string NickName = MainProxy.GetSingletone.GetNickName(User.GetAccountID());
                // 목표점이 이동점과 같다는 것은 이동중이 아니라는 것을 의미한다.
                if (Destination == Position)
                {
                    Destination.X = -1;
                    Destination.Y = -1;
                    Destination.Z = -1;
                }
                SendAnotherCharBaseInfoPacket SendPacketToNewUser = new SendAnotherCharBaseInfoPacket(User.GetAccountID(), Character.GetAppearanceInfo().Gender,
                    Character.GetAppearanceInfo().PresetNumber, Character.GetJobInfo().Job, Character.GetJobInfo().Level, Character.GetCurrentMapID(),
                    (int)Position.X, (int)Position.Y, Character.GetLevelInfo().Level, Character.GetLevelInfo().CurrentExp, NickName, (int)Destination.X, (int)Destination.Y);

                MainProxy.GetSingletone.SendToClient(GamePacketListID.SEND_ANOTHER_CHAR_BASE_INFO, SendPacketToNewUser, Packet.AccountID);
                LogManager.GetSingletone.WriteLog($"Func_ResponseCharBaseInfo: {Packet.AccountID}에게 {User.GetAccountID()}의 정보를 보냈습니다.");
            }
        }

        private void Func_RequestPingCheckPacket(RequestPingCheckPacket Packet, int ClientID)
        {
            // 해쉬코드 인증이 필요없는 단순 핑 체크용 패킷
            MainProxy.GetSingletone.SendToClient(GamePacketListID.RESPONSE_PING_CHECK, new ResponsePingCheckPacket(Packet.Hour, Packet.Min,Packet.Secs,Packet.MSecs), ClientID);
        }
    }
}
