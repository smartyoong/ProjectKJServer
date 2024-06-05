using KYCException;
using KYCInterface;
using KYCLog;
using KYCPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Net.Sockets;

namespace GameServer
{   internal class LoginServerRecvPacketPipeline
    {
        private static readonly Lazy<LoginServerRecvPacketPipeline> instance = new Lazy<LoginServerRecvPacketPipeline>(() => new LoginServerRecvPacketPipeline());
        public static LoginServerRecvPacketPipeline GetSingletone => instance.Value;

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




        private LoginServerRecvPacketPipeline()
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
            var ErrorCode = ClientAcceptor.GetSingletone.AddHashCodeAndNickName(Packet.NickName,Packet.HashCode, Packet.ClientLoginID, 
                ClientAcceptor.GetSingletone.GetIPAddrByClientID(Packet.ClientLoginID));
            if(ErrorCode == GeneralErrorCode.ERR_AUTH_FAIL)
            {
                LoginServerSendPacketPipeline.GetSingletone.PushToPacketPipeline(GameLoginPacketListID.RESPONSE_USER_HASH_INFO,
                    new ResponseUserHashInfoPacket(Packet.ClientLoginID, Packet.NickName, (int)GeneralErrorCode.ERR_AUTH_FAIL, ++Packet.TimeToLive)
                    );
            }
            else if (ErrorCode == GeneralErrorCode.ERR_HASH_CODE_NICKNAME_DUPLICATED)
            {
                // 이미 로그인한 계정이라면 어캐하지? 나중에 처리할까?
                // 기존 유저를 짜르고 신규유저가 들어간다
                if(ClientAcceptor.GetSingletone.GetClientSocketByNickName(Packet.NickName) != null)
                {
                    LoginServerSendPacketPipeline.GetSingletone.PushToPacketPipeline(GameLoginPacketListID.REQUEST_KICK_USER,
                                               new RequestKickUserPacket(ClientAcceptor.GetSingletone.GetIPAddrByClientID(Packet.ClientLoginID),Packet.NickName));
                    ClientSendPacketPipeline.GetSingletone.PushToPacketPipeline(GamePacketListID.KICK_CLIENT, new SendKickClientPacket((int)KickReason.DUPLICATED_LOGIN),
                        ClientAcceptor.GetSingletone.GetClientID(ClientAcceptor.GetSingletone.GetClientSocketByNickName(Packet.NickName)!));

                    ClientAcceptor.GetSingletone.KickClient(ClientAcceptor.GetSingletone.GetClientSocketByNickName(Packet.NickName)!);
                }
                ClientAcceptor.GetSingletone.RemoveHashCodeByNickName(Packet.NickName);
                ClientAcceptor.GetSingletone.AddHashCodeAndNickName(Packet.NickName, Packet.HashCode, Packet.ClientLoginID, 
                    ClientAcceptor.GetSingletone.GetIPAddrByClientID(Packet.ClientLoginID));
            }

            // 성공했다면 딱히 처리할 로직이 없다.
            LogManager.GetSingletone.WriteLog($"Func_SendUserHashInfo: {Packet.NickName} \n {Packet.HashCode} {Packet.ClientLoginID}");
        }

    }
}
