using GameServer.MainUI;
using GameServer.Object;
using GameServer.PacketList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Component
{
    internal class MagicPointComponent
    {
        public int MaxMagicPoint { get; set; }
        public int CurrentMagicPoint { get; set; }

        private long LastUpdateTick = 0;
        private object LockObject;

        Pawn Owner;

        public MagicPointComponent(Pawn Onwer, int MaxMP, int CurrentMP)
        {
            MaxMagicPoint = MaxMP;
            CurrentMagicPoint = CurrentMP;
            LockObject = new object();
            Owner = Onwer;
        }

        public bool IsEnough(int ConsumeAmount)
        {
            return CurrentMagicPoint >= ConsumeAmount;
        }
        public void Consume(int ConsumeAmount)
        {
            CurrentMagicPoint -= ConsumeAmount;
            if (CurrentMagicPoint < 0)
            {
                CurrentMagicPoint = 0;
            }
        }
        public void Restore(int RestoreAmount)
        {
            if (CurrentMagicPoint >= MaxMagicPoint)
            {
                return;
            }
            CurrentMagicPoint += RestoreAmount;
            if (CurrentMagicPoint > MaxMagicPoint)
            {
                CurrentMagicPoint = MaxMagicPoint;
            }
        }

        public void UpdateMPInfoToDB()
        {
            if(Owner.GetPawnType != PawnType.Player)
            {
                return;
            }

            if (Environment.TickCount64 - LastUpdateTick < TimeSpan.FromMinutes(2).Ticks)
            {
                return;
            }
            UpdateMPInfoToDBForce();
        }

        // 시간 체크 제약 안받는 호출
        public void UpdateMPInfoToDBForce()
        {
            if (Owner.GetPawnType != PawnType.Player)
            {
                return;
            }

            lock (LockObject)
            {
                LastUpdateTick = Environment.TickCount64;
                // DB에 갱신하는 코드
                RequestDBUpdateMagicPointPacket Packet = new RequestDBUpdateMagicPointPacket(Owner.GetName, CurrentMagicPoint);
                MainProxy.GetSingletone.SendToDBServer(GameDBPacketListID.REQUEST_UPDATE_MAGIC_POINT, Packet);
            }
        }
    }
}
