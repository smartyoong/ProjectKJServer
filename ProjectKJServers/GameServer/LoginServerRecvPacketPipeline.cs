﻿using KYCException;
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

namespace LoginServer
{   internal class LoginServerRecvPacketPipeline
    {
        private static readonly Lazy<LoginServerRecvPacketPipeline> instance = new Lazy<LoginServerRecvPacketPipeline>(() => new LoginServerRecvPacketPipeline());
        public static LoginServerRecvPacketPipeline GetSingletone => instance.Value;

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
        private TransformBlock<(Memory<byte>, Socket), (dynamic, Socket)> MemoryToPacketBlock;
        private ActionBlock<(dynamic, Socket)> PacketProcessBlock;




        private LoginServerRecvPacketPipeline()
        {

            MemoryToPacketBlock = new TransformBlock<(Memory<byte>,Socket), (dynamic,Socket)>(MakeMemoryToPacket, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 10,
                MaxDegreeOfParallelism = 5,
                NameFormat = "ClientRecvPipeLine.MemoryToPacketBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            PacketProcessBlock = new ActionBlock<(dynamic,Socket)>(ProcessPacket, new ExecutionDataflowBlockOptions
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

        public void PushToPacketPipeline(Memory<byte> packet, Socket socket)
        {
            MemoryToPacketBlock.Post((packet, socket));
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

        private (dynamic PacketStruct, Socket Sock) MakeMemoryToPacket((Memory<byte> packet, Socket Sock) Packet)
        {
            LoginPacketListID ID = PacketUtils.GetIDFromPacket<LoginPacketListID>(ref Packet.packet);

            switch (ID)
            {
                case LoginPacketListID.LOGIN_REQUEST:
                    LoginRequestPacket? RequestCharInfoPacket = PacketUtils.GetPacketStruct<LoginRequestPacket>(ref Packet.packet);
                    if (RequestCharInfoPacket == null)
                        return (new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), Packet.Sock);
                    else
                        return (RequestCharInfoPacket, Packet.Sock);
                default:
                    return (new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED), Packet.Sock);
            }
        }

        public void ProcessPacket((dynamic packet,Socket Sock) Packet)
        {
            if (IsErrorPacket(Packet.packet, "ProcessPacket"))
                return;
            switch (Packet.packet)
            {
                case LoginRequestPacket RequestPacket:
                    Func_LoginRequest(RequestPacket, Packet.Sock);
                    break;
            }
        }

        private void Func_LoginRequest(LoginRequestPacket packet, Socket Sock)
        {
            if (IsErrorPacket(packet, "LoginRequest"))
                return;
            AccountSQLManager.GetSingletone.SP_LOGIN_REQUEST(packet.AccountID, packet.Password, Sock);
        }
    }
}
