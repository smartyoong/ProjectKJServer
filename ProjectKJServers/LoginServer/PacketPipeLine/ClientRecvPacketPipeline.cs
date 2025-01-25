using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Net.Sockets;
using System.Net;
using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using LoginServer.SocketConnect;
using LoginServer.Packet_SPList;
using LoginServer.MainUI;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LoginServer.PacketPipeLine
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
        private Dictionary<LoginPacketListID, Func<Memory<byte>, int, ClientRecvPacketPipeLineWrapper>> MemoryLookUpTable;
        private Dictionary<Type, Action<ClientRecvPacket, int>> PacketLookUpTable;




        public ClientRecvPacketPipeline()
        {
            MemoryLookUpTable = new Dictionary<LoginPacketListID, Func<Memory<byte>, int, ClientRecvPacketPipeLineWrapper>>
            {
                { LoginPacketListID.LOGIN_REQUEST, MakeLoginRequestPacket },
                { LoginPacketListID.ID_UNIQUE_CHECK_REQUEST, MakeIDUniqueCheckRequestPacket },
                { LoginPacketListID.REGIST_ACCOUNT_REQUEST, MakeRegistAccountRequestPacket },
                { LoginPacketListID.CREATE_NICKNAME_REQUEST, MakeCreateNickNameRequestPacket }
            };
            
            PacketLookUpTable = new Dictionary<Type, Action<ClientRecvPacket, int>>
            {
                { typeof(LoginRequestPacket), SP_LoginRequest },
                { typeof(IDUniqueCheckRequestPacket), SP_IDUniqueCheckRequest },
                { typeof(RegistAccountRequestPacket), SP_RegistAccountRequest },
                { typeof(CreateNickNameRequestPacket), SP_CreateNickNameRequest }
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
            LoginPacketListID ID = PacketUtils.GetIDFromPacket<LoginPacketListID>(ref Data);
            if (MemoryLookUpTable.TryGetValue(ID,out var Func))
            {
                return Func(Data, Packet.ClientID);
            }
            else
            {
                return new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED), Packet.ClientID);
            }
        }

        private ClientRecvPacketPipeLineWrapper MakeLoginRequestPacket(Memory<byte> Data, int ClientID)
        {
            LoginRequestPacket? RequestLoginPacket = PacketUtils.GetPacketStruct<LoginRequestPacket>(ref Data);
            return RequestLoginPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), ClientID) : new ClientRecvPacketPipeLineWrapper(RequestLoginPacket, ClientID);
        }

        private ClientRecvPacketPipeLineWrapper MakeIDUniqueCheckRequestPacket(Memory<byte> Data, int ClientID)
        {
            IDUniqueCheckRequestPacket? RequestIDUniqueCheckPacket = PacketUtils.GetPacketStruct<IDUniqueCheckRequestPacket>(ref Data);
            return RequestIDUniqueCheckPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), ClientID) : new ClientRecvPacketPipeLineWrapper(RequestIDUniqueCheckPacket, ClientID);
        }

        private ClientRecvPacketPipeLineWrapper MakeRegistAccountRequestPacket(Memory<byte> Data, int ClientID)
        {
            RegistAccountRequestPacket? RequestRegistAccountPacket = PacketUtils.GetPacketStruct<RegistAccountRequestPacket>(ref Data);
            return RequestRegistAccountPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), ClientID) : new ClientRecvPacketPipeLineWrapper(RequestRegistAccountPacket, ClientID);
        }

        private ClientRecvPacketPipeLineWrapper MakeCreateNickNameRequestPacket(Memory<byte> Data, int ClientID)
        {
            CreateNickNameRequestPacket? RequestCreateNickNamePacket = PacketUtils.GetPacketStruct<CreateNickNameRequestPacket>(ref Data);
            return RequestCreateNickNamePacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), ClientID) : new ClientRecvPacketPipeLineWrapper(RequestCreateNickNamePacket, ClientID);
        }

        public void ProcessPacket(ClientRecvPacketPipeLineWrapper Packet)
        {
            if (IsErrorPacket(Packet.Packet, "ProcessPacket"))
                return;

            if (PacketLookUpTable.TryGetValue(Packet.Packet.GetType(), out Action<ClientRecvPacket,int> Func))
            {
                Func(Packet.Packet, Packet.ClientID);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"ClientRecvPacketPipeline ProcessPacket에 잘못된 타입이 들어왔습니다. {Packet.Packet}");
            }
        }

        private void SP_LoginRequest(ClientRecvPacket Packet, int ClientID)
        {
            if(Packet is not LoginRequestPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"ClientRecvPacketPipeline SP_LoginRequest에 잘못된 타입이 들어왔습니다. {Packet}");
                return;
            }

            if (IsErrorPacket(ValidPacket, "LoginRequest"))
                return;

            MainProxy.GetSingletone.HandleSQLPacket(new SQLLoginRequest(ValidPacket.AccountID, ValidPacket.Password, ClientID));
        }

        private void SP_IDUniqueCheckRequest(ClientRecvPacket Packet, int ClientID)
        {
            if(Packet is not IDUniqueCheckRequestPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"ClientRecvPacketPipeline SP_IDUniqueCheckRequest에 잘못된 타입이 들어왔습니다. {Packet}");
                return;
            }

            if (IsErrorPacket(ValidPacket, "IDUniqueCheckRequest"))
                return;

            MainProxy.GetSingletone.HandleSQLPacket(new SQLIDUniqueCheckRequest(ValidPacket.AccountID, ClientID));
        }

        private void SP_RegistAccountRequest(ClientRecvPacket Packet, int ClientID)
        {
            if(Packet is not RegistAccountRequestPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"ClientRecvPacketPipeline SP_RegistAccountRequest에 잘못된 타입이 들어왔습니다. {Packet}");
                return;
            }

            if (IsErrorPacket(ValidPacket, "RegistAccountRequest"))
                return;

            IPEndPoint? ClientIPEndPoint = MainProxy.GetSingletone.GetClientSocket(ClientID)!.RemoteEndPoint as IPEndPoint;
            if (ClientIPEndPoint != null)
                MainProxy.GetSingletone.HandleSQLPacket(new SQLRegistAccountRequest(ValidPacket.AccountID, ValidPacket.Password, ClientIPEndPoint.Address.ToString(), ClientID));
            else
                LogManager.GetSingletone.WriteLog("ClientRecvPacketPipeline SP_RegistAccountRequest에서 ClientIPEndPoint가 null입니다.");
        }

        private void SP_CreateNickNameRequest(ClientRecvPacket Packet, int ClientID)
        {
            if (Packet is not CreateNickNameRequestPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"ClientRecvPacketPipeline SP_CreateNickNameRequest에 잘못된 타입이 들어왔습니다. {Packet}");
                return;
            }

            if (IsErrorPacket(ValidPacket, "CreateNickNameRequest"))
                return;

            MainProxy.GetSingletone.HandleSQLPacket(new SQLCreateNickNameRequest(ValidPacket.AccountID, ValidPacket.NickName, ClientID));
        }
    }
}
