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
    internal class LoginServerSendPacketPipeline
    {
        //private static readonly Lazy<LoginServerSendPacketPipeline> instance = new Lazy<LoginServerSendPacketPipeline>(() => new LoginServerSendPacketPipeline());
        //public static LoginServerSendPacketPipeline GetSingletone => instance.Value;
        private CancellationTokenSource CancelToken = new CancellationTokenSource();
        private ExecutionDataflowBlockOptions ProcessorOptions = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = 20,
            MaxDegreeOfParallelism = 10,
            TaskScheduler = TaskScheduler.Default,
            EnsureOrdered = false,
            NameFormat = "LoginServerSendPacketPipeline",
            SingleProducerConstrained = false,
        };
        private TransformBlock<LoginServerSendPipeLineWrapper<GameLoginPacketListID>, Memory<byte>> PacketToMemoryBlock;
        private ActionBlock<Memory<byte>> MemorySendBlock;
        private Dictionary<GameLoginPacketListID, Func<LoginSendPacket, Memory<byte>>> PacketLookUpTable;

        public LoginServerSendPacketPipeline()
        {
            PacketLookUpTable = new Dictionary<GameLoginPacketListID, Func<LoginSendPacket, Memory<byte>>>
            {
                { GameLoginPacketListID.RESPONSE_USER_HASH_INFO, MakeResponseUserHashInfoMemory },
                { GameLoginPacketListID.REQUEST_KICK_USER, MakeRequestKickUserMemory }
            };

            PacketToMemoryBlock = new TransformBlock<LoginServerSendPipeLineWrapper<GameLoginPacketListID>, Memory<byte>>(MakePacketToMemory, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 5,
                MaxDegreeOfParallelism = 3,
                NameFormat = "LoginServerSendPacketPipeline.PacketToMemoryBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            MemorySendBlock = new ActionBlock<Memory<byte>>(SendMemory, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 50,
                MaxDegreeOfParallelism = 15,
                NameFormat = "LoginServerSendPacketPipeline.MemorySendBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            PacketToMemoryBlock.LinkTo(MemorySendBlock, new DataflowLinkOptions { PropagateCompletion = true });
            LogManager.GetSingletone.WriteLog("LoginServerSendPacketPipeline 생성 완료");
        }

        public void PushToPacketPipeline(GameLoginPacketListID ID, dynamic packet)
        {
            PacketToMemoryBlock.Post(new LoginServerSendPipeLineWrapper<GameLoginPacketListID>(ID, packet));
        }

        public void Cancel()
        {
            CancelToken.Cancel();
        }

        private Memory<byte> MakePacketToMemory(LoginServerSendPipeLineWrapper<GameLoginPacketListID> GamePacket)
        {
            if (PacketLookUpTable.TryGetValue(GamePacket.ID, out var PacketMaker))
            {
                return PacketMaker(GamePacket.Packet);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"ServerSendPacketPipeline에서 정의되지 않은 패킷이 들어왔습니다.{GamePacket.PacketID}");
                return new byte[0];
            }
        }

        private Memory<byte> MakeResponseUserHashInfoMemory(LoginSendPacket Packet)
        {
            if (Packet is ResponseUserHashInfoPacket ValidPacket)
            {
                return PacketUtils.MakePacket(GameLoginPacketListID.RESPONSE_USER_HASH_INFO, ValidPacket);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"ServerSendPacketPipeline에서 ResponseUserHashInfoPacket이 아닌 패킷이 들어왔습니다.{Packet}");
                return new byte[0];
            }
        }

        private Memory<byte> MakeRequestKickUserMemory(LoginSendPacket Packet)
        {
            if (Packet is RequestKickUserPacket ValidPacket)
            {
                return PacketUtils.MakePacket(GameLoginPacketListID.REQUEST_KICK_USER, ValidPacket);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"ServerSendPacketPipeline에서 RequestKickUserPacket이 아닌 패킷이 들어왔습니다.{Packet}");
                return new byte[0];
            }
        }

        private async Task SendMemory(Memory<byte> data)
        {
            if (data.IsEmpty)
                return;
            await MainProxy.GetSingletone.SendToLoginServer(data).ConfigureAwait(false);
        }
    }
}
