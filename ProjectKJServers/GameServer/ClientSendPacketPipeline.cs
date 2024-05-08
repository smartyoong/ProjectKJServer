using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Net.Sockets;
using KYCLog;
using KYCPacket;

namespace LoginServer
{
    internal class ClientSendPacketPipeline
    {
        private static readonly Lazy<ClientSendPacketPipeline> instance = new Lazy<ClientSendPacketPipeline>(() => new ClientSendPacketPipeline());
        public static ClientSendPacketPipeline GetSingletone => instance.Value;
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
        private TransformBlock<(LoginPacketListID ID,dynamic Packet, Socket Sock), (Memory<byte>, Socket)> PacketToMemoryBlock;
        private ActionBlock<(Memory<byte>, Socket)> MemorySendBlock;

        private ClientSendPacketPipeline()
        {
            PacketToMemoryBlock = new TransformBlock<(LoginPacketListID ID ,dynamic Packet, Socket Sock), (Memory<byte>, Socket)>(MakePacketToMemory, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 10,
                MaxDegreeOfParallelism = 5,
                NameFormat = "ClientSendPipeLine.PacketToMemoryBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            MemorySendBlock = new ActionBlock<(Memory<byte>, Socket)>(SendMemory, new ExecutionDataflowBlockOptions
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

        public void PushToPacketPipeline(LoginPacketListID ID ,dynamic packet, Socket socket)
        {
            PacketToMemoryBlock.Post((ID, packet, socket));
        }

        public void Cancel()
        {
            CancelToken.Cancel();
        }

        private (Memory<byte>, Socket) MakePacketToMemory((LoginPacketListID ID,dynamic Packet, Socket Sock) packet)
        {
           switch(packet.ID)
            {
                case LoginPacketListID.LOGIN_RESPONESE:
                    return (PacketUtils.MakePacket(packet.ID, (LoginResponsePacket)packet.Packet), packet.Sock);
                default:
                    LogManager.GetSingletone.WriteLog($"ClientSendPacketPipeline에서 정의되지 않은 패킷이 들어왔습니다.{packet.ID}");
                    return (new byte[0], packet.Sock);
            }
        }

        private async Task SendMemory((Memory<byte> data, Socket Sock) packet)
        {
            if (packet.data.IsEmpty)
                return;
            int SendBytes = await ClientAcceptor.GetSingletone.Send(packet.Sock, packet.data).ConfigureAwait(false);
            if(SendBytes <=0)
            {
                LogManager.GetSingletone.WriteLog($"ClientSendPacketPipeline에서 데이터를 보내는데 실패했습니다.");
            }

        }
    }
}
