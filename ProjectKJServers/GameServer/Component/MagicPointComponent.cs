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
        public MagicPointComponent(int MaxMP, int CurrentMP)
        {
            MaxMagicPoint = MaxMP;
            CurrentMagicPoint = CurrentMP;
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
    }
}
