using GameServer.GameSystem;
using GameServer.MainUI;
using GameServer.PacketList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Object;

namespace GameServer.Component
{
    internal class LevelComponent
    {
        private int Level = 1;
        private int CurrentEXP = 0;
        private long LastUpdateTick = 0;
        private object LockObject;
        private Pawn Owner;

        public int GetLevel { get { return Level; } }
        public int GetEXP { get { return CurrentEXP; } }

        public LevelComponent(Pawn Owner,int CurrentLevel, int Exp)
        {
            this.Owner = Owner;
            Level = CurrentLevel;
            CurrentEXP = Exp;
            LockObject = new object();
        }

        public void AddEXP(int Exp)
        {
            CurrentEXP += Exp;
            int StandardEXP = MainProxy.GetSingletone.GetRequireEXP(Level);
            if (CurrentEXP >= StandardEXP)
            {
                LevelUp(1);
            }
        }

        public void DecreaseEXP(int Exp)
        {
            CurrentEXP -= Exp;
            if (CurrentEXP < 0)
            {
                CurrentEXP = 0;
            }
        }

        // 운영자 명령어등을 위한 함수
        public void SetLevel(int NewLevel)
        {
            Level = NewLevel;
        }

        private void LevelUp(int Increasement)
        {
            int BeforeLevel = Level;
            Level += Increasement;
            int StandardEXP = MainProxy.GetSingletone.GetRequireEXP(BeforeLevel);
            DecreaseEXP(StandardEXP);
            UpdateEXPInfoToDB();
        }

        public void UpdateEXPInfoToDB()
        {
            long CurrentTick = Environment.TickCount64;
            if (CurrentTick - LastUpdateTick < TimeSpan.FromMinutes(2).Ticks)
            {
                return;
            }
            UpdateEXPInfoToDBForce();
        }

        // 시간 체크를 하지 않고 강제로 호출
        public void UpdateEXPInfoToDBForce()
        {
            if (Owner.GetPawnType != PawnType.Player)
            {
                return;
            }

            lock (LockObject)
            {
                LastUpdateTick = Environment.TickCount64;
                // DB에 업데이트
                RequestDBUpdateLevelExpPacket Packet = new RequestDBUpdateLevelExpPacket(Owner.GetName, Level, CurrentEXP);
                MainProxy.GetSingletone.SendToDBServer(GameDBPacketListID.REQUEST_UPDATE_LEVEL_EXP,Packet);
            }
        }
    }
}
