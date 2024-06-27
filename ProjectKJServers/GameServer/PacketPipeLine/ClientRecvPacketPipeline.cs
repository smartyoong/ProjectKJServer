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

namespace GameServer.PacketPipeLine
{
    internal class ClientRecvPacketPipeline
    {
        private static readonly Lazy<ClientRecvPacketPipeline> instance = new Lazy<ClientRecvPacketPipeline>(() => new ClientRecvPacketPipeline());
        public static ClientRecvPacketPipeline GetSingletone => instance.Value;

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




        private ClientRecvPacketPipeline()
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
                case GamePacketListID.RESPONSE_CHAR_BASE_INFO:
                    ResponseCharBaseInfoPacket? ResponseCharBaseInfoPacket = PacketUtils.GetPacketStruct<ResponseCharBaseInfoPacket>(ref Data);
                    return ResponseCharBaseInfoPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), Packet.ClientID) :
                        new ClientRecvPacketPipeLineWrapper(ResponseCharBaseInfoPacket, Packet.ClientID);
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
            }
        }

        private void Func_HashAuthCheck(RequestHashAuthCheckPacket Packet, int ClientID)
        {
            string HashCode = string.Empty;
            // 닉네임이 최초에는 전부 Guest인데,,, 소켓은 이거 인증 못받으면 닉네임이랑 매핑안됨
            GeneralErrorCode ErrorCode = ClientAcceptor.GetSingletone.CheckAuthHashCode(Packet.AccountID, ref HashCode);
            switch (ErrorCode)
            {
                case GeneralErrorCode.ERR_HASH_CODE_IS_NOT_REGIST:
                    ClientSendPacketPipeline.GetSingletone.PushToPacketPipeline(GamePacketListID.RESPONSE_HASH_AUTH_CHECK,
                        new ResponseHashAuthCheckPacket(Packet.AccountID, (int)GeneralErrorCode.ERR_HASH_CODE_IS_NOT_REGIST), ClientID);
                    //LogManager.GetSingletone.WriteLog($"해시 코드 등록전입니다. {Packet.AccountID} {Packet.HashCode}");
                    break;
                case GeneralErrorCode.ERR_AUTH_SUCCESS:
                    if (Packet.HashCode == HashCode)
                    {
                        ClientAcceptor.GetSingletone.MappingSocketAccountID(ClientAcceptor.GetSingletone.GetClientSocket(ClientID)!, Packet.AccountID);

                        ClientSendPacketPipeline.GetSingletone.PushToPacketPipeline(GamePacketListID.RESPONSE_HASH_AUTH_CHECK,
                            new ResponseHashAuthCheckPacket(Packet.AccountID, (int)GeneralErrorCode.ERR_AUTH_SUCCESS), ClientID);

                        //LogManager.GetSingletone.WriteLog($"{Packet.AccountID} 해시코드 인증 성공 {Packet.HashCode}");
                    }
                    else
                    {
                        if (HashCode == "NONEHASH")
                            LogManager.GetSingletone.WriteLog($"{Packet.AccountID} 해시코드가 없습니다. {Packet.HashCode}");

                        ClientSendPacketPipeline.GetSingletone.PushToPacketPipeline(GamePacketListID.RESPONSE_HASH_AUTH_CHECK,
                            new ResponseHashAuthCheckPacket(Packet.AccountID, (int)GeneralErrorCode.ERR_AUTH_FAIL), ClientID);
                    }
                    break;
                case GeneralErrorCode.ERR_AUTH_FAIL:
                    ClientSendPacketPipeline.GetSingletone.PushToPacketPipeline(GamePacketListID.RESPONSE_HASH_AUTH_CHECK,
                        new ResponseHashAuthCheckPacket(Packet.AccountID, (int)GeneralErrorCode.ERR_AUTH_FAIL), ClientID);
                    break;

            }
        }

        private void DB_Func_RequestCharacterBaseInfo(RequestCharBaseInfoPacket Packet, int ClientID)
        {
            string HashCode = Packet.HashCode;
            if (GeneralErrorCode.ERR_AUTH_FAIL == ClientAcceptor.GetSingletone.CheckAuthHashCode(Packet.AccountID, ref HashCode))
            {
                ClientSendPacketPipeline.GetSingletone.PushToPacketPipeline(GamePacketListID.RESPONSE_HASH_AUTH_CHECK, new ResponseHashAuthCheckPacket(Packet.AccountID, (int)GeneralErrorCode.ERR_AUTH_FAIL), ClientID);
                // 해쉬 코드 인증 실패
                LogManager.GetSingletone.WriteLog($"해시 코드 인증 실패 DB_Func_RequestCharacterBaseInfo : {Packet.AccountID} {Packet.HashCode}");
                return;
            }
            // DB에서 캐릭터 정보를 가져온다.
            RequestDBCharBaseInfoPacket DBPacket = new RequestDBCharBaseInfoPacket(Packet.AccountID);
            DBServerSendPacketPipeline.GetSingletone.PushToPacketPipeline(GameDBPacketListID.REQUEST_CHAR_BASE_INFO, DBPacket);
        }
    }
}
