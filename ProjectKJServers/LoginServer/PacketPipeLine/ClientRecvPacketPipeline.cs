﻿using System;
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

            switch (ID)
            {
                case LoginPacketListID.LOGIN_REQUEST:
                    LoginRequestPacket? RequestLoginPacket = PacketUtils.GetPacketStruct<LoginRequestPacket>(ref Data);
                    return RequestLoginPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), Packet.ClientID) : new ClientRecvPacketPipeLineWrapper(RequestLoginPacket, Packet.ClientID);
                case LoginPacketListID.ID_UNIQUE_CHECK_REQUEST:
                    IDUniqueCheckRequestPacket? RequestIDUniqueCheckPacket = PacketUtils.GetPacketStruct<IDUniqueCheckRequestPacket>(ref Data);
                    return RequestIDUniqueCheckPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), Packet.ClientID) : new ClientRecvPacketPipeLineWrapper(RequestIDUniqueCheckPacket, Packet.ClientID);
                case LoginPacketListID.REGIST_ACCOUNT_REQUEST:
                    RegistAccountRequestPacket? RequestRegistAccountPacket = PacketUtils.GetPacketStruct<RegistAccountRequestPacket>(ref Data);
                    return RequestRegistAccountPacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), Packet.ClientID) : new ClientRecvPacketPipeLineWrapper(RequestRegistAccountPacket, Packet.ClientID);
                case LoginPacketListID.CREATE_NICKNAME_REQUEST:
                    CreateNickNameRequestPacket? RequestCreateNickNamePacket = PacketUtils.GetPacketStruct<CreateNickNameRequestPacket>(ref Data);
                    return RequestCreateNickNamePacket == null ? new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), Packet.ClientID) : new ClientRecvPacketPipeLineWrapper(RequestCreateNickNamePacket, Packet.ClientID);
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
                case LoginRequestPacket RequestPacket:
                    SP_LoginRequest(RequestPacket, Packet.ClientID);
                    break;
                case IDUniqueCheckRequestPacket RequestPacket:
                    SP_IDUniqueCheckRequest(RequestPacket, Packet.ClientID);
                    break;
                case RegistAccountRequestPacket RequestPacket:
                    SP_RegistAccountRequest(RequestPacket, Packet.ClientID);
                    break;
                case CreateNickNameRequestPacket RequestPacket:
                    SP_CreateNickNameRequest(RequestPacket, Packet.ClientID);
                    break;
                default:
                    LogManager.GetSingletone.WriteLog("ClientRecvPipeline ProcessPacket에서 할당되지 않은 패킷이 들어왔습니다.");
                    break;
            }
        }

        private void SP_LoginRequest(LoginRequestPacket packet, int ClientID)
        {
            if (IsErrorPacket(packet, "LoginRequest"))
                return;
            MainProxy.GetSingletone.HandleSQLPacket(new SQLLoginRequest(packet.AccountID, packet.Password, ClientID));
        }

        private void SP_IDUniqueCheckRequest(IDUniqueCheckRequestPacket packet, int ClientID)
        {
            if (IsErrorPacket(packet, "IDUniqueCheckRequest"))
                return;
            MainProxy.GetSingletone.HandleSQLPacket(new SQLIDUniqueCheckRequest(packet.AccountID, ClientID));
        }

        private void SP_RegistAccountRequest(RegistAccountRequestPacket packet, int ClientID)
        {
            if (IsErrorPacket(packet, "RegistAccountRequest"))
                return;
            IPEndPoint? ClientIPEndPoint = MainProxy.GetSingletone.GetClientSocket(ClientID)!.RemoteEndPoint as IPEndPoint;
            if (ClientIPEndPoint != null)
                MainProxy.GetSingletone.HandleSQLPacket(new SQLRegistAccountRequest(packet.AccountID, packet.Password, ClientIPEndPoint.Address.ToString(), ClientID));
            else
                LogManager.GetSingletone.WriteLog("ClientRecvPacketPipeline SP_RegistAccountRequest에서 ClientIPEndPoint가 null입니다.");
        }

        private void SP_CreateNickNameRequest(CreateNickNameRequestPacket packet, int ClientID)
        {
            if (IsErrorPacket(packet, "CreateNickNameRequest"))
                return;
            MainProxy.GetSingletone.HandleSQLPacket(new SQLCreateNickNameRequest(packet.AccountID, packet.NickName, ClientID));
        }
    }
}
