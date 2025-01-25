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
        private Dictionary<LoginPacketListID, Func<ClientSendPacketPipeLineWrapper<LoginPacketListID>, ClientSendMemoryPipeLineWrapper>> PacketLookUpTable;

        public ClientSendPacketPipeline()
        {
            PacketLookUpTable = new Dictionary<LoginPacketListID, Func<ClientSendPacketPipeLineWrapper<LoginPacketListID>, ClientSendMemoryPipeLineWrapper>>
            {
                { LoginPacketListID.LOGIN_RESPONESE, MakeLoginResponseMemory },
                { LoginPacketListID.ID_UNIQUE_CHECK_RESPONESE, MakeIDUniqueCheckResponseMemory },
                { LoginPacketListID.REGIST_ACCOUNT_RESPONESE, MakeRegistAccountResponseMemory },
                { LoginPacketListID.CREATE_NICKNAME_RESPONESE, MakeCreateNickNameResponseMemory }
            };

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
            if(PacketLookUpTable.TryGetValue(packet.ID, out var func))
            {
                return func(packet);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"ClientSendPacketPipeline에서 정의되지 않은 패킷이 들어왔습니다.{packet.ID}");
                return new ClientSendMemoryPipeLineWrapper(new byte[0], packet.ClientID);
            }
        }

        private ClientSendMemoryPipeLineWrapper MakeLoginResponseMemory(ClientSendPacketPipeLineWrapper<LoginPacketListID> Packet)
        {
            return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(Packet.ID, (LoginResponsePacket)Packet.Packet), Packet.ClientID);
        }

        private ClientSendMemoryPipeLineWrapper MakeIDUniqueCheckResponseMemory(ClientSendPacketPipeLineWrapper<LoginPacketListID> Packet)
        {
            return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(Packet.ID, (IDUniqueCheckResponsePacket)Packet.Packet), Packet.ClientID);
        }

        private ClientSendMemoryPipeLineWrapper MakeRegistAccountResponseMemory(ClientSendPacketPipeLineWrapper<LoginPacketListID> Packet)
        {
            return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(Packet.ID, (RegistAccountResponsePacket)Packet.Packet), Packet.ClientID);
        }

        private ClientSendMemoryPipeLineWrapper MakeCreateNickNameResponseMemory(ClientSendPacketPipeLineWrapper<LoginPacketListID> Packet)
        {
            return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(Packet.ID, (CreateNickNameResponsePacket)Packet.Packet), Packet.ClientID);
        }

        private async Task SendMemory(ClientSendMemoryPipeLineWrapper packet)
        {
            if (packet.MemoryData.IsEmpty)
                return;
            await MainProxy.GetSingletone.SendToClient(MainProxy.GetSingletone.GetClientSocket(packet.ClientID)!, packet.MemoryData).ConfigureAwait(false);
        }
    }
}
