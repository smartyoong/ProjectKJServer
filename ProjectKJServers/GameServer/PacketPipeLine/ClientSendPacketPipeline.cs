using System;
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
        private Dictionary<GamePacketListID, Func<GamePacketListID, ClientSendPacket, int, ClientSendMemoryPipeLineWrapper>> PacketLookUpTable;

        public ClientSendPacketPipeline()
        {
            PacketLookUpTable = new Dictionary<GamePacketListID, Func<GamePacketListID, ClientSendPacket, int, ClientSendMemoryPipeLineWrapper>>
            {
                { GamePacketListID.KICK_CLIENT, MakeSendKickClientPacket },
                { GamePacketListID.RESPONSE_HASH_AUTH_CHECK, MakeResponseHashAuthCheckPacket },
                { GamePacketListID.RESPONSE_NEED_TO_MAKE_CHARACTER, MakeResponseNeedToMakeCharacterPacket },
                { GamePacketListID.RESPONSE_CREATE_CHARACTER, MakeResponseCreateCharacterPacket },
                { GamePacketListID.RESPONSE_CHAR_BASE_INFO, MakeResponseCharBaseInfoPacket },
                { GamePacketListID.RESPONSE_MOVE, MakeResponseMovePacket },
                { GamePacketListID.SEND_ANOTHER_CHAR_BASE_INFO, MakeSendAnotherCharBaseInfoPacket },
                { GamePacketListID.SEND_USER_MOVE, MakeSendUserMovePacket },
                { GamePacketListID.RESPONSE_PING_CHECK, MakeResponsePingCheckPacket },
                { GamePacketListID.SEND_USER_MOVE_ARRIVED, MakeSendUserMoveArrivedPacket },
                { GamePacketListID.SEND_USER_SAY, MakeSendUserSayPacket }
            };

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

        private ClientSendMemoryPipeLineWrapper MakePacketToMemory(ClientSendPacketPipeLineWrapper<GamePacketListID> Packet)
        {

            if(PacketLookUpTable.TryGetValue(Packet.ID, out var func))
            {
                return func(Packet.ID, Packet.Packet, Packet.ClientID);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"ClientSendPacketPipeline에서 정의되지 않은 패킷이 들어왔습니다.{Packet.ID}");
                return new ClientSendMemoryPipeLineWrapper(new byte[0], Packet.ClientID);
            }
        }

        private async Task SendMemory(ClientSendMemoryPipeLineWrapper packet)
        {
            if (packet.MemoryData.IsEmpty)
                return;
            await MainProxy.GetSingletone.SendToClient(MainProxy.GetSingletone.GetClientSocket(packet.ClientID)!, packet.MemoryData).ConfigureAwait(false);
        }

        private ClientSendMemoryPipeLineWrapper MakeSendKickClientPacket(GamePacketListID ID, ClientSendPacket Packet, int ClientID)
        {
            return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(ID, (SendKickClientPacket)Packet), ClientID);
        }

        private ClientSendMemoryPipeLineWrapper MakeResponseHashAuthCheckPacket(GamePacketListID ID, ClientSendPacket Packet, int ClientID)
        {
            return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(ID, (ResponseHashAuthCheckPacket)Packet), ClientID);
        }

        private ClientSendMemoryPipeLineWrapper MakeResponseNeedToMakeCharacterPacket(GamePacketListID ID, ClientSendPacket Packet, int ClientID)
        {
            return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(ID, (ResponseNeedToMakeCharcterPacket)Packet), ClientID);
        }

        private ClientSendMemoryPipeLineWrapper MakeResponseCreateCharacterPacket(GamePacketListID ID, ClientSendPacket Packet, int ClientID)
        {
            return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(ID, (ResponseCreateCharacterPacket)Packet), ClientID);
        }

        private ClientSendMemoryPipeLineWrapper MakeResponseCharBaseInfoPacket(GamePacketListID ID, ClientSendPacket Packet, int ClientID)
        {
            return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(ID, (ResponseCharBaseInfoPacket)Packet), ClientID);
        }

        private ClientSendMemoryPipeLineWrapper MakeResponseMovePacket(GamePacketListID ID, ClientSendPacket Packet, int ClientID)
        {
            return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(ID, (ResponseMovePacket)Packet), ClientID);
        }

        private ClientSendMemoryPipeLineWrapper MakeSendAnotherCharBaseInfoPacket(GamePacketListID ID, ClientSendPacket Packet, int ClientID)
        {
            return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(ID, (SendAnotherCharBaseInfoPacket)Packet), ClientID);
        }

        private ClientSendMemoryPipeLineWrapper MakeSendUserMovePacket(GamePacketListID ID, ClientSendPacket Packet, int ClientID)
        {
            return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(ID, (SendUserMovePacket)Packet), ClientID);
        }

        private ClientSendMemoryPipeLineWrapper MakeResponsePingCheckPacket(GamePacketListID ID, ClientSendPacket Packet, int ClientID)
        {
            return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(ID, (ResponsePingCheckPacket)Packet), ClientID);
        }

        private ClientSendMemoryPipeLineWrapper MakeSendUserMoveArrivedPacket(GamePacketListID ID, ClientSendPacket Packet, int ClientID)
        {
            return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(ID, (SendUserMoveArrivedPacket)Packet), ClientID);
        }

        private ClientSendMemoryPipeLineWrapper MakeSendUserSayPacket(GamePacketListID ID, ClientSendPacket Packet, int ClientID)
        {
            return new ClientSendMemoryPipeLineWrapper(PacketUtils.MakePacket(ID, (SendUserSayPacket)Packet), ClientID);
        }
    }
}
