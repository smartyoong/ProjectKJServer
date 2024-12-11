using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Component
{
    internal class MagicPointComponent
    {
        public uint MaxMagicPoint { get; set; }
        public uint CurrentMagicPoint { get; set; }
        public MagicPointComponent(uint MaxMP, uint CurrentMP)
        {
            MaxMagicPoint = MaxMP;
            CurrentMagicPoint = CurrentMP;
        }

        public bool IsEnough(uint ConsumeAmount)
        {
            return CurrentMagicPoint >= ConsumeAmount;
        }
        public void Consume(uint ConsumeAmount)
        {
            CurrentMagicPoint -= ConsumeAmount;
            if (CurrentMagicPoint < 0)
            {
                CurrentMagicPoint = 0;
            }
        }
        public void Restore(uint RestoreAmount)
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
