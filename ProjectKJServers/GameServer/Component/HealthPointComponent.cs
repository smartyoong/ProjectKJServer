using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Component
{
    internal class HealthPointComponent
    {
        public uint MaxHealthPoint { get; set; }
        public uint CurrentHealthPoint { get; set; }
        private Action DeathAction;

        public HealthPointComponent(uint MaxHP, uint CurrentHP, Action DeathAction)
        {
            MaxHealthPoint = MaxHP;
            CurrentHealthPoint = CurrentHP;
            this.DeathAction = DeathAction;
        }

        public bool IsDead()
        {
            return CurrentHealthPoint == 0;
        }

        private void Death()
        {
            DeathAction();
        }

        public void TakeDamage(uint Damage)
        {
            CurrentHealthPoint -= Damage;
            if (CurrentHealthPoint < 0)
            {
                CurrentHealthPoint = 0;
                Death();
            }
        }

        public void Heal(uint HealAmount)
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
    }
}
