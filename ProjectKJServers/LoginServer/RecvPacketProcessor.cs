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

namespace LoginServer
{   internal class RecvPacketProcessor : IPacketProcessor<LoginPacketListID>
    {
        private CancellationTokenSource CancelToken = new CancellationTokenSource();
        private ExecutionDataflowBlockOptions ProcessorOptions = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = 20,
            MaxDegreeOfParallelism = 10,
            TaskScheduler = TaskScheduler.Default,
            EnsureOrdered = false,
            NameFormat = "this.NameFormat",
            SingleProducerConstrained = false,
        };
        private TransformBlock<byte[], Memory<byte>> ByteToMemoryBlock;
        private TransformBlock<Memory<byte>, dynamic> MemoryToPacketBlock;
        private ActionBlock<dynamic> PacketProcessBlock;



        public RecvPacketProcessor()
        {
            ByteToMemoryBlock = new TransformBlock<byte[], Memory<byte>>(MakeByteToMemory, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 10,
                MaxDegreeOfParallelism = 5,
                NameFormat = "LoginPacketProcessor.ByteToMemoryBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            MemoryToPacketBlock = new TransformBlock<Memory<byte>, dynamic>(MakeMemoryToPacket, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 10,
                MaxDegreeOfParallelism = 5,
                NameFormat = "LoginPacketProcessor.MemoryToPacketBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            PacketProcessBlock = new ActionBlock<dynamic>(ProcessPacket, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 100,
                MaxDegreeOfParallelism = 50,
                NameFormat = "LoginPacketProcessor.PacketProcessBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            ByteToMemoryBlock.LinkTo(MemoryToPacketBlock, new DataflowLinkOptions { PropagateCompletion = true });
            MemoryToPacketBlock.LinkTo(PacketProcessBlock, new DataflowLinkOptions { PropagateCompletion = true });
            ProcessBlock();
        }

        public void PushToPacketPipeline(byte[] Packet)
        {
            ByteToMemoryBlock.Post(Packet);
        }

        public void Cancel()
        {
            CancelToken.Cancel();
            ByteToMemoryBlock.Complete();
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
                        await ByteToMemoryBlock.Completion;
                        await MemoryToPacketBlock.Completion;
                        await PacketProcessBlock.Completion;
                    }
                    catch (AggregateException e)
                    {
                        LogManager.GetSingletone.WriteLog(e.Flatten()).Wait();
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        LogManager.GetSingletone.WriteLog(e).Wait();
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
                    LogManager.GetSingletone.WriteLog(ErrorLog.ToString()).Wait();
                    break;
                case GeneralErrorCode.ERR_PACKET_IS_NULL:
                    ErrorLog.Append("Error: Packet is null ");
                    ErrorLog.Append(Message);
                    LogManager.GetSingletone.WriteLog(ErrorLog.ToString()).Wait();
                    break;
            }
        }

        private Memory<byte> MakeByteToMemory(byte[] data)
        {
            return PacketUtils.ByteToMemory(ref data);
        }

        public dynamic MakePacketStruct(LoginPacketListID ID, params dynamic[] PacketParams)
        {
            switch (ID)
            {
                case LoginPacketListID.LOGIN_REQUEST:
                    return new LoginRequestPacket(PacketParams[0], PacketParams[1]);
                default:
                    return new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED);
            }
        }

        private dynamic MakeMemoryToPacket(Memory<byte> packet)
        {
            LoginPacketListID ID = PacketUtils.GetIDFromPacket<LoginPacketListID>(ref packet);

            switch (ID)
            {
                case LoginPacketListID.LOGIN_REQUEST:
                    LoginRequestPacket? RequestCharInfoPacket = PacketUtils.GetPacketStruct<LoginRequestPacket>(ref packet);
                    if (RequestCharInfoPacket == null)
                        return new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL);
                    else
                        return RequestCharInfoPacket;
                default:
                    return new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED);
            }
        }

        public void ProcessPacket(dynamic packet)
        {
            if (IsErrorPacket(packet, "ProcessPacket"))
                return;
            switch (packet)
            {
                case LoginRequestPacket RequestPacket:
                    Func_LoginRequest(RequestPacket);
                    break;
            }
        }

        private void Func_LoginRequest(LoginRequestPacket packet)
        {
            if (IsErrorPacket(packet, "LoginRequest"))
                return;
            //SQLManager가 있었네! 이걸로 DB에 접근해서 처리하면 될듯
        }
    }
}