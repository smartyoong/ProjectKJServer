using KYCLog;
using KYCPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace LoginServer
{
    internal class GameServerSendPacketPipeline
    {
        private static readonly Lazy<GameServerSendPacketPipeline> instance = new Lazy<GameServerSendPacketPipeline>(() => new GameServerSendPacketPipeline());
        public static GameServerSendPacketPipeline GetSingletone => instance.Value;
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

        private GameServerSendPacketPipeline()
        {
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
            switch (GamePacket.PacketID)
            {
                case LoginGamePacketListID.SEND_USER_HASH_INFO:
                    return PacketUtils.MakePacket(GamePacket.PacketID, (SendUserHashInfoPacket)GamePacket.Packet);
                default:
                    LogManager.GetSingletone.WriteLog($"GameServerSendPacketPipeline에서 정의되지 않은 패킷이 들어왔습니다.{GamePacket.PacketID}");
                    return new byte[0];
            }
        }

        private async Task SendMemory(Memory<byte> data)
        {
            if (data.IsEmpty)
                return;
            await GameServerConnector.GetSingletone.Send(data).ConfigureAwait(false);
        }
    }
}
