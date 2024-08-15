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
    internal class LoginServerRecvPacketPipeline
    {
        //private static readonly Lazy<LoginServerRecvPacketPipeline> instance = new Lazy<LoginServerRecvPacketPipeline>(() => new LoginServerRecvPacketPipeline());
        //public static LoginServerRecvPacketPipeline GetSingletone => instance.Value;

        private CancellationTokenSource CancelToken = new CancellationTokenSource();
        private ExecutionDataflowBlockOptions ProcessorOptions = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = 5,
            MaxDegreeOfParallelism = 3,
            TaskScheduler = TaskScheduler.Default,
            EnsureOrdered = false,
            NameFormat = "LoginServerRecvPipeLine",
            SingleProducerConstrained = false,
        };
        private TransformBlock<Memory<byte>, dynamic> MemoryToPacketBlock;
        private ActionBlock<dynamic> PacketProcessBlock;


        public LoginServerRecvPacketPipeline()
        {

            MemoryToPacketBlock = new TransformBlock<Memory<byte>, dynamic>(MakeMemoryToPacket, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 5,
                MaxDegreeOfParallelism = 3,
                NameFormat = "LoginServerRecvPacketPipeline.MemoryToPacketBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            PacketProcessBlock = new ActionBlock<dynamic>(ProcessPacket, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 40,
                MaxDegreeOfParallelism = 10,
                NameFormat = "LoginServerRecvPacketPipeline.PacketProcessBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            MemoryToPacketBlock.LinkTo(PacketProcessBlock, new DataflowLinkOptions { PropagateCompletion = true });
            ProcessBlock();
            LogManager.GetSingletone.WriteLog("LoginServerRecvPacketPipeline 생성 완료");
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
                ProcessGeneralErrorCode(Packet.ErrorCode, $"LoginServerSendPipeLine {message}에서 에러 발생");
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
            GameLoginPacketListID ID = PacketUtils.GetIDFromPacket<GameLoginPacketListID>(ref packet);

            switch (ID)
            {
                case GameLoginPacketListID.SEND_USER_HASH_INFO:
                    SendUserHashInfoPacket? SendUserHashInfoPacket = PacketUtils.GetPacketStruct<SendUserHashInfoPacket>(ref packet);
                    return SendUserHashInfoPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : SendUserHashInfoPacket;
                default:
                    return new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED);
            }
        }

        public void ProcessPacket(dynamic Packet)
        {
            if (IsErrorPacket(Packet, "LoginServerRecvProcessPacket"))
                return;
            switch (Packet)
            {
                case SendUserHashInfoPacket SendUserHashInfoPacket:
                    Func_SendUserHashInfo(SendUserHashInfoPacket);
                    break;
                default:
                    LogManager.GetSingletone.WriteLog("LoginServerRecvPacketPipeline.ProcessPacket: 알수 없는 패킷이 들어왔습니다.");
                    break;
            }
        }

        private void Func_SendUserHashInfo(SendUserHashInfoPacket Packet)
        {
            if (IsErrorPacket(Packet, "SendUserHashInfo"))
                return;

            var ErrorCode = ClientAcceptor.GetSingletone.AddHashCodeAndAccountID(Packet.AccountID, Packet.HashCode);

            if (ErrorCode == GeneralErrorCode.ERR_AUTH_FAIL)
            {
                LoginServerSendPacketPipeline.GetSingletone.PushToPacketPipeline(GameLoginPacketListID.RESPONSE_USER_HASH_INFO,
                    new ResponseUserHashInfoPacket(Packet.ClientLoginID, Packet.AccountID, (int)GeneralErrorCode.ERR_AUTH_FAIL, ++Packet.TimeToLive));

            }
            else if (ErrorCode == GeneralErrorCode.ERR_HASH_CODE_ACCOUNT_ID_DUPLICATED)
            {
                // 이미 로그인한 계정이라면 어캐하지? 나중에 처리할까?
                // 기존 유저를 짜르고 신규유저가 들어간다
                if (ClientAcceptor.GetSingletone.GetClientSocketByAccountID(Packet.AccountID) != null)
                {

                    LoginServerSendPacketPipeline.GetSingletone.PushToPacketPipeline(GameLoginPacketListID.REQUEST_KICK_USER,
                        new RequestKickUserPacket(ClientAcceptor.GetSingletone.GetIPAddrByClientSocket(ClientAcceptor.GetSingletone.GetClientSocketByAccountID(Packet.AccountID)!),
                        Packet.AccountID));

                    ClientSendPacketPipeline.GetSingletone.PushToPacketPipeline(GamePacketListID.KICK_CLIENT, new SendKickClientPacket((int)KickReason.DUPLICATED_LOGIN),
                        ClientAcceptor.GetSingletone.GetClientID(ClientAcceptor.GetSingletone.GetClientSocketByAccountID(Packet.AccountID)!));
                    Task.Delay(TimeSpan.FromSeconds(3)).Wait(); // 이러면 해결은 될건데 매우 안좋은 방향인데
                    ClientAcceptor.GetSingletone.KickClient(ClientAcceptor.GetSingletone.GetClientSocketByAccountID(Packet.AccountID)!);
                    ClientAcceptor.GetSingletone.RemoveHashCodeByAccountID(Packet.AccountID);
                    ClientAcceptor.GetSingletone.AddHashCodeAndAccountID(Packet.AccountID, Packet.HashCode);
                }
                // 만약 NickName으로 소켓을 못 얻어온다는 것은 아직 게임서버에게 로그인 시도를 요청 안했다는 것
                // 왠만해선 클라가 바로 게임서버로 로그인 요청을 보낼거니까 특수한 상황 혹은 디버그 상황에서만 가능
                else
                {
                    LogManager.GetSingletone.WriteLog($"{Packet.AccountID}으로 소켓을 못 찾았습니다. 게임서버에게 로그인 요청을 보내지 않았다는 것입니다.");
                }
            }

            // 성공했다면 딱히 처리할 로직이 없다.
            //LogManager.GetSingletone.WriteLog($"Func_SendUserHashInfo: {Packet.AccountID}에게 HashCode {Packet.HashCode}를 부여했습니다.");
        }

    }
}
