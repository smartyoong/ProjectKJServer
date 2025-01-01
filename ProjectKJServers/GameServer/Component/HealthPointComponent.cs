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
    internal class HealthPointComponent
    {
        public int MaxHealthPoint { get; set; }
        public int CurrentHealthPoint { get; set; }
        private Action DeathAction;
        private long LastUpdateTick = 0;
        private object LockObject;
        Pawn Owner;

        public HealthPointComponent(Pawn Owner, int MaxHP, int CurrentHP, Action DeathAction)
        {
            this.Owner = Owner;
            MaxHealthPoint = MaxHP;
            CurrentHealthPoint = CurrentHP;
            this.DeathAction = DeathAction;
            LockObject = new object();
        }

        public bool IsDead()
        {
            return CurrentHealthPoint == 0;
        }

        private void Death()
        {
            DeathAction();
            UpdateHPInfoToDB();
        }

        public void TakeDamage(int Damage)
        {
            CurrentHealthPoint -= Damage;
            if (CurrentHealthPoint < 0)
            {
                // 1로 지정하자 0으로 저장하면 무한 죽음 발생할 수도?
                CurrentHealthPoint = 1;
                Death();
            }
        }

        public void Heal(int HealAmount)
        {
            if(CurrentHealthPoint >= MaxHealthPoint)
            {
                return;
            }

            CurrentHealthPoint += HealAmount;
            if (CurrentHealthPoint > MaxHealthPoint)
            {
                CurrentHealthPoint = MaxHealthPoint;
            }
        }

        public void UpdateHPInfoToDB()
        {
            if (Environment.TickCount64 - LastUpdateTick < TimeSpan.FromMinutes(2).Ticks)
            {
                return;
            }
            UpdateHPInfoToDBForce();
        }

        public void UpdateHPInfoToDBForce()
        {
            if (Owner.GetPawnType != PawnType.Player)
            {
                return;
            }

            lock (LockObject)
            {
                LastUpdateTick = Environment.TickCount64;
                // DB에 업데이트
                RequestDBUpdateHealthPointPacket Packet = new RequestDBUpdateHealthPointPacket(Owner.GetName, CurrentHealthPoint);
                MainProxy.GetSingletone.SendToDBServer(GameDBPacketListID.REQUEST_UPDATE_HEALTH_POINT, Packet);
            }
        }
    }
}
