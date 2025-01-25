using CoreUtility.Utility;
using DBServer.MainUI;
using DBServer.Packet_SPList;
using DBServer.SocketConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DBServer.PacketPipeLine
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
        private TransformBlock<GameServerSendPipeLineWrapper<DBPacketListID>, Memory<byte>> PacketToMemoryBlock;
        private ActionBlock<Memory<byte>> MemorySendBlock;
        private Dictionary<DBPacketListID, Func<GameSendPacket, Memory<byte>>> PacketLookUpTable;

        public GameServerSendPacketPipeline()
        {
            PacketLookUpTable = new Dictionary<DBPacketListID, Func<GameSendPacket, Memory<byte>>>()
            {
                { DBPacketListID.RESPONSE_NEED_TO_MAKE_CHARACTER, MakeResponseDBNeedToMakeCharacterPacket },
                { DBPacketListID.RESPONSE_CREATE_CHARACTER, MakeResponseDBCreateCharacterPacket },
                { DBPacketListID.RESPONSE_CHAR_BASE_INFO, MakeResponseDBCharBaseInfoPacket },
                { DBPacketListID.RESPONSE_UPDATE_GENDER, MakeResponseDBUpdateGenderPacket },
                { DBPacketListID.RESPONSE_UPDATE_PRESET, MakeResponseDBUpdatePresetPacket }
            };

            PacketToMemoryBlock = new TransformBlock<GameServerSendPipeLineWrapper<DBPacketListID>, Memory<byte>>(MakePacketToMemory, new ExecutionDataflowBlockOptions
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

        public void PushToPacketPipeline(DBPacketListID ID, dynamic packet)
        {
            PacketToMemoryBlock.Post(new GameServerSendPipeLineWrapper<DBPacketListID>(ID, packet));
        }

        public void Cancel()
        {
            CancelToken.Cancel();
        }

        private Memory<byte> MakePacketToMemory(GameServerSendPipeLineWrapper<DBPacketListID> GamePacket)
        {
            switch (GamePacket.PacketID)
            {
                case DBPacketListID.RESPONSE_NEED_TO_MAKE_CHARACTER:
                    return PacketUtils.MakePacket(GamePacket.PacketID, (ResponseDBNeedToMakeCharacterPacket)GamePacket.Packet);
                case DBPacketListID.RESPONSE_CREATE_CHARACTER:
                    return PacketUtils.MakePacket(GamePacket.PacketID, (ResponseDBCreateCharacterPacket)GamePacket.Packet);
                case DBPacketListID.RESPONSE_CHAR_BASE_INFO:
                    return PacketUtils.MakePacket(GamePacket.PacketID, (ResponseDBCharBaseInfoPacket)GamePacket.Packet);
                case DBPacketListID.RESPONSE_UPDATE_GENDER:
                    return PacketUtils.MakePacket(GamePacket.PacketID, (ResponseDBUpdateGenderPacket)GamePacket.Packet);
                case DBPacketListID.RESPONSE_UPDATE_PRESET:
                    return PacketUtils.MakePacket(GamePacket.PacketID, (ResponseDBUpdatePresetPacket)GamePacket.Packet);
                default:
                    LogManager.GetSingletone.WriteLog($"GameServerSendPacketPipeline에서 정의되지 않은 패킷이 들어왔습니다.{GamePacket.PacketID}");
                    return new byte[0];
            }
        }

        private Memory<byte> MakeResponseDBNeedToMakeCharacterPacket(GameSendPacket Packet)
        {
            if(Packet is ResponseDBNeedToMakeCharacterPacket ValidPacket)
                return PacketUtils.MakePacket(DBPacketListID.RESPONSE_NEED_TO_MAKE_CHARACTER, ValidPacket);
            else
            {
                LogManager.GetSingletone.WriteLog($"MakeResponseDBNeedToMakeCharacterPacket에서 잘못된 패킷이 들어왔습니다.");
                return new byte[0];
            }
        }

        private Memory<byte> MakeResponseDBCreateCharacterPacket(GameSendPacket Packet)
        {
            if (Packet is ResponseDBCreateCharacterPacket ValidPacket)
                return PacketUtils.MakePacket(DBPacketListID.RESPONSE_CREATE_CHARACTER, ValidPacket);
            else
            {
                LogManager.GetSingletone.WriteLog($"MakeResponseDBCreateCharacterPacket에서 잘못된 패킷이 들어왔습니다.");
                return new byte[0];
            }
        }

        private Memory<byte> MakeResponseDBCharBaseInfoPacket(GameSendPacket Packet)
        {
            if (Packet is ResponseDBCharBaseInfoPacket ValidPacket)
                return PacketUtils.MakePacket(DBPacketListID.RESPONSE_CHAR_BASE_INFO, ValidPacket);
            else
            {
                LogManager.GetSingletone.WriteLog($"MakeResponseDBCharBaseInfoPacket에서 잘못된 패킷이 들어왔습니다.");
                return new byte[0];
            }
        }

        private Memory<byte> MakeResponseDBUpdateGenderPacket(GameSendPacket Packet)
        {
            if (Packet is ResponseDBUpdateGenderPacket ValidPacket)
                return PacketUtils.MakePacket(DBPacketListID.RESPONSE_UPDATE_GENDER, ValidPacket);
            else
            {
                LogManager.GetSingletone.WriteLog($"MakeResponseDBUpdateGenderPacket에서 잘못된 패킷이 들어왔습니다.");
                return new byte[0];
            }
        }

        private Memory<byte> MakeResponseDBUpdatePresetPacket(GameSendPacket Packet)
        {
            if (Packet is ResponseDBUpdatePresetPacket ValidPacket)
                return PacketUtils.MakePacket(DBPacketListID.RESPONSE_UPDATE_PRESET, ValidPacket);
            else
            {
                LogManager.GetSingletone.WriteLog($"MakeResponseDBUpdatePresetPacket에서 잘못된 패킷이 들어왔습니다.");
                return new byte[0];
            }
        }

        private async Task SendMemory(Memory<byte> data)
        {
            if (data.IsEmpty)
                return;
            await MainProxy.GetSingletone.SendDataToGameServer(data).ConfigureAwait(false);
        }
    }
}
