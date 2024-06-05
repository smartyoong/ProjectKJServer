using KYCException;
using KYCLog;
using KYCPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace LoginServer
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

        private dynamic MakeMemoryToPacket(Memory<byte> packet)
        {
            LoginGamePacketListID ID = PacketUtils.GetIDFromPacket<LoginGamePacketListID>(ref packet);

            switch (ID)
            {
                case LoginGamePacketListID.RESPONSE_USER_HASH_INFO:
                    ResponseUserHashInfoPacket? ResponseUserHashInfoPacket = PacketUtils.GetPacketStruct<ResponseUserHashInfoPacket>(ref packet);
                    return ResponseUserHashInfoPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : ResponseUserHashInfoPacket;
                case LoginGamePacketListID.REQUEST_KICK_USER:
                    RequestKickUserPacket? RequestKickUserPacket = PacketUtils.GetPacketStruct<RequestKickUserPacket>(ref packet);
                    return RequestKickUserPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : RequestKickUserPacket;
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
                case ResponseUserHashInfoPacket ResponseUserHashInfoPacket:
                    Func_ResponseUserHashInfo(ResponseUserHashInfoPacket);
                    break;
                case RequestKickUserPacket RequestKickUserPacket:
                    Func_RequestKickUser(RequestKickUserPacket);
                    break;
                default:
                    LogManager.GetSingletone.WriteLog("GameServerRecvPacketPipeline.ProcessPacket: 알수 없는 패킷이 들어왔습니다.");
                    break;
            }
        }

        private void SP_ResponseUserInfoSummary(ResponseGameTestPacket packet)
        {
            if (IsErrorPacket(packet, "ResponseUserInfoSummary"))
                return;
            LogManager.GetSingletone.WriteLog($"AccountID: {packet.AccountID} NickName: {packet.NickName} Level: {packet.Level} Exp: {packet.Exp}");
        }

        private void Func_ResponseUserHashInfo(ResponseUserHashInfoPacket Packet)
        {
            const int MAX_TTL = 10;
            if (IsErrorPacket(Packet, "ResponseUserHashInfo"))
                return;
            if(Packet.TimeToLive == MAX_TTL)
            {
               LogManager.GetSingletone.WriteLog($"TimeToLive이 만료되었습니다. {Packet.NickName}의 해쉬값이 만료됩니다.");
               return;
            }
            else if(Packet.ErrorCode == (int)GeneralErrorCode.ERR_AUTH_FAIL)
            {
                string HashCode = ClientAcceptor.GetSingletone.MakeAuthHashCode(Packet.NickName, Packet.ClientLoginID);
                int ReturnValue = (int)GeneralErrorCode.ERR_AUTH_RETRY;
                if (string.IsNullOrEmpty(HashCode))
                {
                    LogManager.GetSingletone.WriteLog($"해시 코드 재생성에 실패했습니다. NickName : {Packet.NickName}");
                    ReturnValue = (int)GeneralErrorCode.ERR_AUTH_FAIL;
                }
                else
                {
                    GameServerSendPacketPipeline.GetSingletone.PushToPacketPipeline(LoginGamePacketListID.SEND_USER_HASH_INFO,
                        new SendUserHashInfoPacket(Packet.NickName, HashCode, Packet.ClientLoginID, 
                        ClientAcceptor.GetSingletone.GetIPAddrByClientID(Packet.ClientLoginID)));
                }
                // 클라이언트한테는 어떻게든 전달한다
                ClientSendPacketPipeline.GetSingletone.PushToPacketPipeline(LoginPacketListID.LOGIN_RESPONESE, 
                    new LoginResponsePacket(Packet.NickName, HashCode, ReturnValue), Packet.ClientLoginID);
            }
        }

        private void Func_RequestKickUser(RequestKickUserPacket Packet)
        {
            if (IsErrorPacket(Packet, "RequestKickUser"))
                return;
            Socket? ClientSocket = ClientAcceptor.GetSingletone.GetClientSocketByAddr(Packet.IPAddr);
            if (ClientSocket == null)
            {
                LogManager.GetSingletone.WriteLog($"게임서버 요청에 따라 다음 클라이언트를 강제 종료시킵니다. IPAddr: {Packet.IPAddr}");
                return;
            }
            ClientAcceptor.GetSingletone.KickClient(ClientSocket);
        }
    }
}
