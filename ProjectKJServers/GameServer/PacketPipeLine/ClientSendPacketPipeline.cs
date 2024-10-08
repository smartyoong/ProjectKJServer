﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Net.Sockets;
using GameServer.SocketConnect;
using GameServer.PacketList;
using CoreUtility.Utility;
using GameServer.MainUI;

namespace GameServer.PacketPipeLine
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
        private TransformBlock<ClientSendPacketPipeLineWrapper<GamePacketListID>, ClientSendMemoryPipeLineWrapper> PacketToMemoryBlock;
        private ActionBlock<ClientSendMemoryPipeLineWrapper> MemorySendBlock;

        public ClientSendPacketPipeline()
        {
            PacketToMemoryBlock = new TransformBlock<ClientSendPacketPipeLineWrapper<GamePacketListID>, ClientSendMemoryPipeLineWrapper>(MakePacketToMemory, new ExecutionDataflowBlockOptions
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

        public void PushToPacketPipeline(GamePacketListID ID, dynamic packet, int ClientID)
        {
            PacketToMemoryBlock.Post(new ClientSendPacketPipeLineWrapper<GamePacketListID>(ID, packet, ClientID));
        }

        public void Cancel()
        {
            CancelToken.Cancel();
        }

        private ClientSendMemoryPipeLineWrapper MakePacketToMemory(ClientSendPacketPipeLineWrapper<GamePacketListID> packet)
        {
            switch (packet.ID)
            {
                case GamePacketListID.KICK_CLIENT:
                    return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(packet.ID, (SendKickClientPacket)packet.Packet), packet.ClientID);
                case GamePacketListID.RESPONSE_HASH_AUTH_CHECK:
                    return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(packet.ID, (ResponseHashAuthCheckPacket)packet.Packet), packet.ClientID);
                case GamePacketListID.RESPONSE_NEED_TO_MAKE_CHARACTER:
                    return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(packet.ID, (ResponseNeedToMakeCharcterPacket)packet.Packet), packet.ClientID);
                case GamePacketListID.RESPONSE_CREATE_CHARACTER:
                    return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(packet.ID, (ResponseCreateCharacterPacket)packet.Packet), packet.ClientID);
                case GamePacketListID.RESPONSE_CHAR_BASE_INFO:
                    return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(packet.ID, (ResponseCharBaseInfoPacket)packet.Packet), packet.ClientID);
                case GamePacketListID.RESPONSE_MOVE:
                    return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(packet.ID, (ResponseMovePacket)packet.Packet), packet.ClientID);
                case GamePacketListID.SEND_ANOTHER_CHAR_BASE_INFO:
                    return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(packet.ID, (SendAnotherCharBaseInfoPacket)packet.Packet), packet.ClientID);
                case GamePacketListID.SEND_USER_MOVE:
                    return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(packet.ID, (SendUserMovePacket)packet.Packet), packet.ClientID);
                case GamePacketListID.RESPONSE_PING_CHECK:
                    return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(packet.ID, (ResponsePingCheckPacket)packet.Packet), packet.ClientID);
                case GamePacketListID.SEND_USER_MOVE_ARRIVED:
                    return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(packet.ID, (SendUserMoveArrivedPacket)packet.Packet), packet.ClientID);
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
