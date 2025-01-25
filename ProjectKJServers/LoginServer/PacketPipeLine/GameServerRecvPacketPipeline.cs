using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using LoginServer.MainUI;
using LoginServer.Packet_SPList;
using LoginServer.SocketConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace LoginServer.PacketPipeLine
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
        private Dictionary<LoginGamePacketListID, Func<Memory<byte>, dynamic>> MemoryLookUpTable;
        private Dictionary<Type, Action<GameRecvPacket>> PacketLookUpTable;




        public GameServerRecvPacketPipeline()
        {
            MemoryLookUpTable = new Dictionary<LoginGamePacketListID, Func<Memory<byte>, dynamic>>
            {
                { LoginGamePacketListID.RESPONSE_USER_HASH_INFO, MakeResponseUserHashInfoPacket },
                { LoginGamePacketListID.REQUEST_KICK_USER, MakeRequestKickUserPacket }
            };

            PacketLookUpTable = new Dictionary<Type, Action<GameRecvPacket>>
            {
                { typeof(ResponseUserHashInfoPacket), Func_ResponseUserHashInfo },
                { typeof(RequestKickUserPacket), Func_RequestKickUser }
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

            if (MemoryLookUpTable.TryGetValue(ID, out var func))
            {
                return func(packet);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"GameServerRecvPacketPipeline.MakeMemoryToPacket: 패킷이 할당되지 않았습니다. {ID}");
                return new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED);
            }
        }

        private dynamic MakeResponseUserHashInfoPacket(Memory<byte> packet)
        {
            ResponseUserHashInfoPacket? ResponseUserHashInfoPacket = PacketUtils.GetPacketStruct<ResponseUserHashInfoPacket>(ref packet);
            return ResponseUserHashInfoPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : ResponseUserHashInfoPacket;
        }

        private dynamic MakeRequestKickUserPacket(Memory<byte> packet)
        {
            RequestKickUserPacket? RequestKickUserPacket = PacketUtils.GetPacketStruct<RequestKickUserPacket>(ref packet);
            return RequestKickUserPacket == null ? new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL) : RequestKickUserPacket;
        }

        public void ProcessPacket(dynamic Packet)
        {
            if (IsErrorPacket(Packet, "GameServerRecvProcessPacket"))
                return;

            if (PacketLookUpTable.TryGetValue(Packet.GetType(), out Action<GameRecvPacket> func))
            {
                func(Packet);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"GameServerRecvPacketPipeline.ProcessPacket: 패킷이 할당되지 않았습니다. {Packet}");
            }
        }


        private void Func_ResponseUserHashInfo(GameRecvPacket Packet)
        {
            if(Packet is not ResponseUserHashInfoPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"Func_ResponseUserHashInfo: ResponseUserHashInfoPacket이 아닙니다. {Packet}");
                return;
            }

            const int MAX_TTL = 10;
            if (IsErrorPacket(ValidPacket, "ResponseUserHashInfo"))
                return;
            if (ValidPacket.TimeToLive == MAX_TTL)
            {
                LogManager.GetSingletone.WriteLog($"TimeToLive이 만료되었습니다. {ValidPacket.NickName}의 해쉬값이 만료됩니다.");
                return;
            }
            else if (ValidPacket.ErrorCode == (int)GeneralErrorCode.ERR_AUTH_FAIL)
            {
                string HashCode = MainProxy.GetSingletone.MakeAuthHashCode(ValidPacket.NickName, ValidPacket.ClientLoginID);
                int ReturnValue = (int)GeneralErrorCode.ERR_AUTH_RETRY;
                if (string.IsNullOrEmpty(HashCode))
                {
                    LogManager.GetSingletone.WriteLog($"해시 코드 재생성에 실패했습니다. NickName : {ValidPacket.NickName}");
                    ReturnValue = (int)GeneralErrorCode.ERR_AUTH_FAIL;
                }
                else
                {
                    MainProxy.GetSingletone.ProcessSendPacketToGameServer(LoginGamePacketListID.SEND_USER_HASH_INFO,
                        new SendUserHashInfoPacket(ValidPacket.NickName, HashCode, ValidPacket.ClientLoginID,
                        MainProxy.GetSingletone.GetIPAddrByClientID(ValidPacket.ClientLoginID)));
                }
                // 클라이언트한테는 어떻게든 전달한다
                MainProxy.GetSingletone.SendToClient(LoginPacketListID.LOGIN_RESPONESE, new LoginResponsePacket(ValidPacket.NickName, HashCode, ReturnValue), ValidPacket.ClientLoginID);
            }
        }

        private void Func_RequestKickUser(GameRecvPacket Packet)
        {
            if (Packet is not RequestKickUserPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"Func_RequestKickUser: RequestKickUserPacket이 아닙니다. {Packet}");
                return;
            }

            if (IsErrorPacket(Packet, "RequestKickUser"))
                return;
            Socket? ClientSocket = MainProxy.GetSingletone.GetClientSocketByAccountID(ValidPacket.AccountID);
            if (ClientSocket == null)
            {
                return;
            }
            LogManager.GetSingletone.WriteLog($"게임서버 요청에 따라 다음 클라이언트를 강제 종료시킵니다. IPAddr: {ValidPacket.IPAddr} {ValidPacket.AccountID}");
            MainProxy.GetSingletone.KickClient(ClientSocket);
        }
    }
}
