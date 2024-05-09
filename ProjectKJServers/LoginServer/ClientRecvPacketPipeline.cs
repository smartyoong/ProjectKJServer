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

namespace LoginServer
{   internal class ClientRecvPacketPipeline
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

            MemoryToPacketBlock.Post(new ClientRecvMemoryPipeLineWrapper(packet,socketID));
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
                    LoginRequestPacket? RequestCharInfoPacket = PacketUtils.GetPacketStruct<LoginRequestPacket>(ref Data);
                    if (RequestCharInfoPacket == null)
                        return new ClientRecvPacketPipeLineWrapper(new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL), Packet.ClientID);
                    else
                        return new ClientRecvPacketPipeLineWrapper(RequestCharInfoPacket, Packet.ClientID);
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
            }
        }

        private void SP_LoginRequest(LoginRequestPacket packet, int ClientID)
        {
            if (IsErrorPacket(packet, "LoginRequest"))
                return;
            AccountSQLManager.GetSingletone.SQL_LOGIN_REQUEST(packet.AccountID, packet.Password, ClientID);
        }
    }
}
