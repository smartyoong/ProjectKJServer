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
        private Dictionary<GameLoginPacketListID, Func<Memory<byte>, dynamic>> MemoryLookUpTable;
        private Dictionary<Type, Action<LoginRecvPacket>> PacketLookUpTable;


        public LoginServerRecvPacketPipeline()
        {
            MemoryLookUpTable = new Dictionary<GameLoginPacketListID, Func<Memory<byte>, dynamic>>
            {
                { GameLoginPacketListID.SEND_USER_HASH_INFO, MakeSendUserHashInfoPacket }
            };

            PacketLookUpTable = new Dictionary<Type, Action<LoginRecvPacket>>
            {
                { typeof(SendUserHashInfoPacket), Func_SendUserHashInfo }
            };

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
            if (MemoryLookUpTable.TryGetValue(ID, out var func))
            {
                return func(packet);
            }
            else
            {
                return new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED);
            }
        }

        private dynamic MakeSendUserHashInfoPacket(Memory<byte> Packet)
        {
            SendUserHashInfoPacket? SendUserHashInfoPacket = PacketUtils.GetPacketStruct<SendUserHashInfoPacket>(ref Packet);
            return SendUserHashInfoPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : SendUserHashInfoPacket;
        }

        public void ProcessPacket(dynamic Packet)
        {
            if (IsErrorPacket(Packet, "LoginServerRecvProcessPacket"))
                return;

            if (PacketLookUpTable.TryGetValue(Packet.GetType(), out Action<LoginRecvPacket> func))
            {
                func(Packet);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"LoginServerRecvPacketPipeline에서 정의되지 않은 패킷이 들어왔습니다.{Packet}");
            }
        }

        private void Func_SendUserHashInfo(LoginRecvPacket Packet)
        {
            if(Packet is not SendUserHashInfoPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog("Func_SendUserHashInfo: SendUserHashInfoPacket이 아닙니다.");
                return;
            }

            if (IsErrorPacket(ValidPacket, "SendUserHashInfo"))
                return;

            var ErrorCode = MainProxy.GetSingletone.AddHashCodeAndAccountID(ValidPacket.AccountID, ValidPacket.HashCode);

            if (ErrorCode == GeneralErrorCode.ERR_AUTH_FAIL)
            {
                MainProxy.GetSingletone.SendToLoginServer(GameLoginPacketListID.RESPONSE_USER_HASH_INFO,
                    new ResponseUserHashInfoPacket(ValidPacket.ClientLoginID, ValidPacket.AccountID, (int)GeneralErrorCode.ERR_AUTH_FAIL, ++ValidPacket.TimeToLive));

            }
            else if (ErrorCode == GeneralErrorCode.ERR_HASH_CODE_ACCOUNT_ID_DUPLICATED)
            {
                // 이미 로그인한 계정이라면 어캐하지? 나중에 처리할까?
                // 기존 유저를 짜르고 신규유저가 들어간다
                if (MainProxy.GetSingletone.GetClientSocketByAccountID(ValidPacket.AccountID) != null)
                {

                    MainProxy.GetSingletone.SendToLoginServer(GameLoginPacketListID.REQUEST_KICK_USER,
                        new RequestKickUserPacket(MainProxy.GetSingletone.GetIPAddrByClientSocket(MainProxy.GetSingletone.GetClientSocketByAccountID(ValidPacket.AccountID)!),
                        ValidPacket.AccountID));

                    MainProxy.GetSingletone.SendToClient(GamePacketListID.KICK_CLIENT, new SendKickClientPacket((int)KickReason.DUPLICATED_LOGIN), ValidPacket.AccountID);
                    Task.Delay(TimeSpan.FromSeconds(3)).Wait(); // 이러면 해결은 될건데 매우 안좋은 방향인데
                    MainProxy.GetSingletone.KickClient(MainProxy.GetSingletone.GetClientSocketByAccountID(ValidPacket.AccountID)!);
                    MainProxy.GetSingletone.RemoveHashCodeByAccountID(ValidPacket.AccountID);
                    MainProxy.GetSingletone.AddHashCodeAndAccountID(ValidPacket.AccountID, ValidPacket.HashCode);
                }
                // 만약 NickName으로 소켓을 못 얻어온다는 것은 아직 게임서버에게 로그인 시도를 요청 안했다는 것
                // 왠만해선 클라가 바로 게임서버로 로그인 요청을 보낼거니까 특수한 상황 혹은 디버그 상황에서만 가능
                else
                {
                    LogManager.GetSingletone.WriteLog($"{ValidPacket.AccountID}으로 소켓을 못 찾았습니다. 게임서버에게 로그인 요청을 보내지 않았다는 것입니다.");
                }
            }

            // 성공했다면 클라이언트 ID와 닉네임을 매핑 시키자 혹시 모르니까 맵을 너무 많이 사용하는듯 한데. 다른데에서 부여하자
            //LogManager.GetSingletone.WriteLog($"Func_SendUserHashInfo: {Packet.AccountID}에게 HashCode {Packet.HashCode}를 부여했습니다.");
        }

    }
}
