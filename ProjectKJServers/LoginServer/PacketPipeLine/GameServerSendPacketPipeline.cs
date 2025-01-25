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
    internal class GameServerSendPacketPipeline
    {
        //private static readonly Lazy<GameServerSendPacketPipeline> instance = new Lazy<GameServerSendPacketPipeline>(() => new GameServerSendPacketPipeline());
        //public static GameServerSendPacketPipeline GetSingletone => instance.Value;
        private CancellationTokenSource CancelToken = new CancellationTokenSource();
        private ExecutionDataflowBlockOptions ProcessorOptions = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = 20,
            MaxDegreeOfParallelism = 10,
            TaskScheduler = TaskScheduler.Default,
            EnsureOrdered = false,
            NameFormat = "GameServerSendPacketPipeline",
            SingleProducerConstrained = false,
        };
        private TransformBlock<GameServerSendPipeLineWrapper<LoginGamePacketListID>, Memory<byte>> PacketToMemoryBlock;
        private ActionBlock<Memory<byte>> MemorySendBlock;
        private Dictionary<LoginGamePacketListID, Func<LoginGamePacketListID, GameSendPacket, Memory<byte>>> PacketLookUpTable;

        public GameServerSendPacketPipeline()
        {
            PacketLookUpTable = new Dictionary<LoginGamePacketListID, Func<LoginGamePacketListID, GameSendPacket, Memory<byte>>>()
            { 
                { LoginGamePacketListID.SEND_USER_HASH_INFO, MakeSendUserHashInfoPacket }
            };

            PacketToMemoryBlock = new TransformBlock<GameServerSendPipeLineWrapper<LoginGamePacketListID>, Memory<byte>>(MakePacketToMemory, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 5,
                MaxDegreeOfParallelism = 3,
                NameFormat = "GameServerSendPacketPipeline.PacketToMemoryBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            MemorySendBlock = new ActionBlock<Memory<byte>>(SendMemory, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 50,
                MaxDegreeOfParallelism = 15,
                NameFormat = "GameServerSendPacketPipeline.MemorySendBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            PacketToMemoryBlock.LinkTo(MemorySendBlock, new DataflowLinkOptions { PropagateCompletion = true });
            LogManager.GetSingletone.WriteLog("GameServerSendPacketPipeline 생성 완료");
        }

        public void PushToPacketPipeline(LoginGamePacketListID ID, dynamic packet)
        {
            PacketToMemoryBlock.Post(new GameServerSendPipeLineWrapper<LoginGamePacketListID>(ID, packet));
        }

        public void Cancel()
        {
            CancelToken.Cancel();
        }

        private Memory<byte> MakePacketToMemory(GameServerSendPipeLineWrapper<LoginGamePacketListID> GamePacket)
        {
            if (!PacketLookUpTable.TryGetValue(GamePacket.PacketID, out var PacketFunc))
            {
                LogManager.GetSingletone.WriteLog($"MakePacketToMemory에서 {GamePacket.PacketID}에 해당하는 패킷 함수가 없습니다.");
                return new byte[0];
            }
            return PacketFunc(GamePacket.PacketID, GamePacket.Packet);
        }

        private Memory<byte> MakeSendUserHashInfoPacket(LoginGamePacketListID ID, GameSendPacket Packet)
        {
            if(Packet is not SendUserHashInfoPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"MakeSendUserHashInfoPacket에서 SendUserHashInfoPacket이 아닌 패킷이 들어왔습니다.");
                return new byte[0];
            }

            return PacketUtils.MakePacket(ID, ValidPacket);
        }

        private async Task SendMemory(Memory<byte> data)
        {
            if (data.IsEmpty)
                return;
            await MainProxy.GetSingletone.SendToGameServer(data).ConfigureAwait(false);
        }
    }
}
