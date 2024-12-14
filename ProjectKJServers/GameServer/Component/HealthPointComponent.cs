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

        public HealthPointComponent(int MaxHP, int CurrentHP, Action DeathAction)
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

        public void TakeDamage(int Damage)
        {
            CurrentHealthPoint -= Damage;
            if (CurrentHealthPoint < 0)
            {
                CurrentHealthPoint = 0;
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
    }
}
