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
                case GameLoginPacketListID.REQUEST_USER_INFO_SUMMARY:
                    RequestUserInfoSummaryPacket? ResponseSummaryCharInfoPacket = PacketUtils.GetPacketStruct<RequestUserInfoSummaryPacket>(ref packet);
                    if (ResponseSummaryCharInfoPacket == null)
                        return new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NULL);
                    else
                        return ResponseSummaryCharInfoPacket;
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
                case ResponseUserInfoSummaryPacket ResponseSummaryUserInfoPacket:
                    Func_ResponseUserInfoSummary(ResponseSummaryUserInfoPacket);
                    break;
                case RequestUserInfoSummaryPacket RequestSummaryUserInfoPacket:
                    Func_RequestUserInfoSummary(RequestSummaryUserInfoPacket);
                    break;
                default:
                    LogManager.GetSingletone.WriteLog("LoginServerRecvPacketPipeline.ProcessPacket: 알수 없는 패킷이 들어왔습니다.");
                    break;
            }
        }

        private void Func_ResponseUserInfoSummary(ResponseUserInfoSummaryPacket packet)
        {
            if (IsErrorPacket(packet, "ResponseUserInfoSummary"))
                return;
            LogManager.GetSingletone.WriteLog($"AccountID: {packet.AccountID} NickName: {packet.NickName} Level: {packet.Level} Exp: {packet.Exp}");
            // 유저 Socket 정보를 들고 있는 Map이 필요할듯? 그러면 Socket을 매개변수로 넘길필요가 없을 수도 있음 (Client 파이프라인에서)
            // 현재는 이 패킷 자체가 임시이니까 보류
        }

        private void Func_RequestUserInfoSummary(RequestUserInfoSummaryPacket packet)
        {
            if (IsErrorPacket(packet, "RequestUserInfoSummary"))
                return;
            LogManager.GetSingletone.WriteLog($"AccountID: {packet.AccountID} NickName: {packet.NickName}");
            // 유저 Socket 정보를 들고 있는 Map이 필요할듯? 그러면 Socket을 매개변수로 넘길필요가 없을 수도 있음 (Client 파이프라인에서)
            LoginServerSendPacketPipeline.GetSingletone.PushToPacketPipeline(GameLoginPacketListID.RESPONSE_USER_INFO_SUMMARY, new ResponseUserInfoSummaryPacket(packet.AccountID, packet.NickName, 1, 0));
        }

    }
}
