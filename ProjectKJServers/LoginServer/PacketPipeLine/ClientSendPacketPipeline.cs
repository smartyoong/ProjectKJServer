using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Net.Sockets;
using CoreUtility.Utility;
using LoginServer.SocketConnect;
using LoginServer.Packet_SPList;
using LoginServer.MainUI;

namespace LoginServer.PacketPipeLine
{
    internal class ClientSendPacketPipeline
    {
        //private static readonly Lazy<ClientSendPacketPipeline> instance = new Lazy<ClientSendPacketPipeline>(() => new ClientSendPacketPipeline());
        //public static ClientSendPacketPipeline GetSingletone => instance.Value;
        private CancellationTokenSource CancelToken = new CancellationTokenSource();
        private ExecutionDataflowBlockOptions ProcessorOptions = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = 20,
            MaxDegreeOfParallelism = 10,
            TaskScheduler = TaskScheduler.Default,
            EnsureOrdered = false,
            NameFormat = "ClientSendPipeLine",
            SingleProducerConstrained = false,
        };
        private TransformBlock<ClientSendPacketPipeLineWrapper<LoginPacketListID>, ClientSendMemoryPipeLineWrapper> PacketToMemoryBlock;
        private ActionBlock<ClientSendMemoryPipeLineWrapper> MemorySendBlock;

        public ClientSendPacketPipeline()
        {
            PacketToMemoryBlock = new TransformBlock<ClientSendPacketPipeLineWrapper<LoginPacketListID>, ClientSendMemoryPipeLineWrapper>(MakePacketToMemory, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 10,
                MaxDegreeOfParallelism = 5,
                NameFormat = "ClientSendPipeLine.PacketToMemoryBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            MemorySendBlock = new ActionBlock<ClientSendMemoryPipeLineWrapper>(SendMemory, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 100,
                MaxDegreeOfParallelism = 50,
                NameFormat = "ClientSendPipeLine.MemorySendBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            PacketToMemoryBlock.LinkTo(MemorySendBlock, new DataflowLinkOptions { PropagateCompletion = true });
            LogManager.GetSingletone.WriteLog("ClientSendPacketPipeline 생성 완료");
        }

        public void PushToPacketPipeline(LoginPacketListID ID, dynamic packet, int ClientID)
        {
            PacketToMemoryBlock.Post(new ClientSendPacketPipeLineWrapper<LoginPacketListID>(ID, packet, ClientID));
        }

        public void Cancel()
        {
            CancelToken.Cancel();
        }

        private ClientSendMemoryPipeLineWrapper MakePacketToMemory(ClientSendPacketPipeLineWrapper<LoginPacketListID> packet)
        {
            switch (packet.ID)
            {
                case LoginPacketListID.LOGIN_RESPONESE:
                    return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(packet.ID, (LoginResponsePacket)packet.Packet), packet.ClientID);
                case LoginPacketListID.ID_UNIQUE_CHECK_RESPONESE:
                    return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(packet.ID, (IDUniqueCheckResponsePacket)packet.Packet), packet.ClientID);
                case LoginPacketListID.REGIST_ACCOUNT_RESPONESE:
                    return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(packet.ID, (RegistAccountResponsePacket)packet.Packet), packet.ClientID);
                case LoginPacketListID.CREATE_NICKNAME_RESPONESE:
                    return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(packet.ID, (CreateNickNameResponsePacket)packet.Packet), packet.ClientID);
                default:
                    LogManager.GetSingletone.WriteLog($"ClientSendPacketPipeline에서 정의되지 않은 패킷이 들어왔습니다.{packet.ID}");
                    return new ClientSendMemoryPipeLineWrapper(new byte[0], packet.ClientID);
            }
        }

        private async Task SendMemory(ClientSendMemoryPipeLineWrapper packet)
        {
            if (packet.MemoryData.IsEmpty)
                return;
            await MainProxy.GetSingletone.SendToClient(MainProxy.GetSingletone.GetClientSocket(packet.ClientID)!, packet.MemoryData).ConfigureAwait(false);
        }
    }
}
