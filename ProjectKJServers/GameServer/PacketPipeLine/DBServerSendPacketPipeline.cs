using CoreUtility.Utility;
using GameServer.PacketList;
using GameServer.SocketConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GameServer.PacketPipeLine
{
    internal class DBServerSendPacketPipeline
    {
        private static readonly Lazy<DBServerSendPacketPipeline> instance = new Lazy<DBServerSendPacketPipeline>(() => new DBServerSendPacketPipeline());
        public static DBServerSendPacketPipeline GetSingletone => instance.Value;
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
        private TransformBlock<DBServerSendPipeLineWrapper<GameDBPacketListID>, Memory<byte>> PacketToMemoryBlock;
        private ActionBlock<Memory<byte>> MemorySendBlock;

        private DBServerSendPacketPipeline()
        {
            PacketToMemoryBlock = new TransformBlock<DBServerSendPipeLineWrapper<GameDBPacketListID>, Memory<byte>>(MakePacketToMemory, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 5,
                MaxDegreeOfParallelism = 3,
                NameFormat = "DBServerSendPacketPipeline.PacketToMemoryBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            MemorySendBlock = new ActionBlock<Memory<byte>>(SendMemory, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 50,
                MaxDegreeOfParallelism = 15,
                NameFormat = "DBServerSendPacketPipeline.MemorySendBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            PacketToMemoryBlock.LinkTo(MemorySendBlock, new DataflowLinkOptions { PropagateCompletion = true });
            LogManager.GetSingletone.WriteLog("DBServerSendPacketPipeline 생성 완료");
        }

        public void PushToPacketPipeline(GameDBPacketListID ID, dynamic packet)
        {
            PacketToMemoryBlock.Post(new DBServerSendPipeLineWrapper<GameDBPacketListID>(ID, packet));
        }

        public void Cancel()
        {
            CancelToken.Cancel();
        }

        private Memory<byte> MakePacketToMemory(DBServerSendPipeLineWrapper<GameDBPacketListID> GamePacket)
        {
            switch (GamePacket.PacketID)
            {
                case GameDBPacketListID.REQUEST_DB_TEST:
                    return PacketUtils.MakePacket(GamePacket.PacketID, (RequestDBTestPacket)GamePacket.Packet);
                case GameDBPacketListID.REQUEST_CHAR_BASE_INFO:
                    return PacketUtils.MakePacket(GamePacket.PacketID, (RequestDBCharBaseInfoPacket)GamePacket.Packet);
                case GameDBPacketListID.REQUEST_CREATE_CHARACTER:
                    return PacketUtils.MakePacket(GamePacket.PacketID, (RequestDBCreateCharacterPacket)GamePacket.Packet);
                default:
                    LogManager.GetSingletone.WriteLog($"DBServerSendPacketPipeline에서 정의되지 않은 패킷이 들어왔습니다.{GamePacket.PacketID}");
                    return new byte[0];
            }
        }

        private async Task SendMemory(Memory<byte> data)
        {
            if (data.IsEmpty)
                return;
            await DBServerConnector.GetSingletone.Send(data).ConfigureAwait(false);
        }
    }
}
