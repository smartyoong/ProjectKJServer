using CoreUtility.Utility;
using GameServer.MainUI;
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
        //private static readonly Lazy<DBServerSendPacketPipeline> instance = new Lazy<DBServerSendPacketPipeline>(() => new DBServerSendPacketPipeline());
        //public static DBServerSendPacketPipeline GetSingletone => instance.Value;
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
        private Dictionary<GameDBPacketListID, Func<GameDBPacketListID, DBSendPacket, Memory<byte>>> PacketLookUpTable;

        public DBServerSendPacketPipeline()
        {
            PacketLookUpTable = new Dictionary<GameDBPacketListID, Func<GameDBPacketListID, DBSendPacket, Memory<byte>>>
            {
                { GameDBPacketListID.REQUEST_CHAR_BASE_INFO, MakeRequestDBCharBaseInfoPacket },
                { GameDBPacketListID.REQUEST_CREATE_CHARACTER, MakeRequestDBCreateCharacterPacket },
                { GameDBPacketListID.REQUEST_UPDATE_HEALTH_POINT, MakeRequestDBUpdateHealthPointPacket },
                { GameDBPacketListID.REQUEST_UPDATE_MAGIC_POINT, MakeRequestDBUpdateMagicPointPacket },
                { GameDBPacketListID.REQUEST_UPDATE_LEVEL_EXP, MakeRequestDBUpdateLevelExpPacket },
                { GameDBPacketListID.REQUEST_UPDATE_JOB_LEVEL, MakeRequestDBUpdateJobLevelPacket },
                { GameDBPacketListID.REQUEST_UPDATE_JOB, MakeRequestDBUpdateJobPacket },
                { GameDBPacketListID.REQUEST_UPDATE_GENDER, MakeRequestDBUpdateGenderPacket },
                { GameDBPacketListID.REQUEST_UPDATE_PRESET, MakeRequestDBUpdatePresetPacket }
            };

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
            if(PacketLookUpTable.TryGetValue(GamePacket.PacketID, out var func))
            {
                return func(GamePacket.PacketID, GamePacket.Packet);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"DBServerSendPacketPipeline에서 정의되지 않은 패킷이 들어왔습니다.{GamePacket.PacketID}");
                return new byte[0];
            }
        }

        private Memory<byte> MakeRequestDBCharBaseInfoPacket(GameDBPacketListID ID, DBSendPacket Packet)
        {
            if (Packet is RequestDBCharBaseInfoPacket ValidPacket)
            {
                return PacketUtils.MakePacket(ID, ValidPacket);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"DBServerSendPacketPipeline에서 RequestDBCharBaseInfoPacket이 아닌 패킷이 들어왔습니다.{Packet}");
                return new byte[0];
            }
        }

        private Memory<byte> MakeRequestDBCreateCharacterPacket(GameDBPacketListID ID, DBSendPacket Packet)
        {
            if(Packet is RequestDBCreateCharacterPacket ValidPacket)
            {
                return PacketUtils.MakePacket(ID, ValidPacket);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"DBServerSendPacketPipeline에서 RequestDBCreateCharacterPacket이 아닌 패킷이 들어왔습니다.{Packet}");
                return new byte[0];
            }
        }

        private Memory<byte> MakeRequestDBUpdateHealthPointPacket(GameDBPacketListID ID, DBSendPacket Packet)
        {
            if (Packet is RequestDBUpdateHealthPointPacket ValidPacket)
            {
                return PacketUtils.MakePacket(ID, ValidPacket);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"DBServerSendPacketPipeline에서 RequestDBUpdateHealthPointPacket이 아닌 패킷이 들어왔습니다.{Packet}");
                return new byte[0];
            }
        }

        private Memory<byte> MakeRequestDBUpdateMagicPointPacket(GameDBPacketListID ID, DBSendPacket Packet)
        {
            if (Packet is RequestDBUpdateMagicPointPacket ValidPacket)
            {
                return PacketUtils.MakePacket(ID, ValidPacket);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"DBServerSendPacketPipeline에서 RequestDBUpdateMagicPointPacket이 아닌 패킷이 들어왔습니다.{Packet}");
                return new byte[0];
            }
        }

        private Memory<byte> MakeRequestDBUpdateLevelExpPacket(GameDBPacketListID ID, DBSendPacket Packet)
        {
            if(Packet is RequestDBUpdateLevelExpPacket ValidPacket)
            {
                return PacketUtils.MakePacket(ID, ValidPacket);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"DBServerSendPacketPipeline에서 RequestDBUpdateLevelExpPacket이 아닌 패킷이 들어왔습니다.{Packet}");
                return new byte[0];
            }
        }

        private Memory<byte> MakeRequestDBUpdateJobLevelPacket(GameDBPacketListID ID, DBSendPacket Packet)
        {
            if(Packet is RequestDBUpdateJobLevelPacket ValidPacket)
            {
                return PacketUtils.MakePacket(ID, ValidPacket);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"DBServerSendPacketPipeline에서 RequestDBUpdateJobLevelPacket이 아닌 패킷이 들어왔습니다.{Packet}");
                return new byte[0];
            }
        }

        private Memory<byte> MakeRequestDBUpdateJobPacket(GameDBPacketListID ID, DBSendPacket Packet)
        {
            if(Packet is RequestDBUpdateJobPacket ValidPacket)
            {
                return PacketUtils.MakePacket(ID, ValidPacket);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"DBServerSendPacketPipeline에서 RequestDBUpdateJobPacket이 아닌 패킷이 들어왔습니다.{Packet}");
                return new byte[0];
            }
        }

        private Memory<byte> MakeRequestDBUpdateGenderPacket(GameDBPacketListID ID, DBSendPacket Packet)
        {
            if(Packet is RequestDBUpdateGenderPacket ValidPacket)
            {
                return PacketUtils.MakePacket(ID, ValidPacket);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"DBServerSendPacketPipeline에서 RequestDBUpdateGenderPacket이 아닌 패킷이 들어왔습니다.{Packet}");
                return new byte[0];
            }
        }

        private Memory<byte> MakeRequestDBUpdatePresetPacket(GameDBPacketListID ID, DBSendPacket Packet)
        {
            if(Packet is RequestDBUpdatePresetPacket ValidPacket)
            {
                return PacketUtils.MakePacket(ID, ValidPacket);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"DBServerSendPacketPipeline에서 RequestDBUpdatePresetPacket이 아닌 패킷이 들어왔습니다.{Packet}");
                return new byte[0];
            }
        }

        private async Task SendMemory(Memory<byte> data)
        {
            if (data.IsEmpty)
                return;
            await MainProxy.GetSingletone.SendToDBServer(data).ConfigureAwait(false);
        }
    }
}
