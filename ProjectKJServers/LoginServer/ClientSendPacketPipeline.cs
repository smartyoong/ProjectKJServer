using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Net.Sockets;

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
            NameFormat = "this.NameFormat",
            SingleProducerConstrained = false,
        };
        private TransformBlock<(dynamic, Socket), (Memory<byte>, Socket)> PacketToMemoryBlock;
        private ActionBlock<(Memory<byte>, Socket)> MemorySendBlock;

        private ClientSendPacketPipeline()
        {
            PacketToMemoryBlock = new TransformBlock<(dynamic, Socket), (Memory<byte>, Socket)>(MakePacketToMemory, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 10,
                MaxDegreeOfParallelism = 5,
                NameFormat = "LoginPacketProcessor.PacketToMemoryBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            MemorySendBlock = new ActionBlock<(Memory<byte>, Socket)>(SendMemory, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 100,
                MaxDegreeOfParallelism = 50,
                NameFormat = "LoginPacketProcessor.MemorySendBlock",
                EnsureOrdered = false,
                SingleProducerConstrained = false,
                CancellationToken = CancelToken.Token
            });

            PacketToMemoryBlock.LinkTo(MemorySendBlock, new DataflowLinkOptions { PropagateCompletion = true });
        }

        public void PushToPacketPipeline(dynamic packet, Socket socket)
        {
            PacketToMemoryBlock.Post((packet, socket));
        }

        public void Cancel()
        {
            CancelToken.Cancel();
        }

        private (Memory<byte>, Socket) MakePacketToMemory((dynamic, Socket) packet)
        {
            return (packet.Item1.ToMemory(), packet.Item2);
        }

        private void SendMemory((Memory<byte>, Socket) packet)
        {
            packet.Item2.Send(packet.Item1.Span);
        }
    }
}
