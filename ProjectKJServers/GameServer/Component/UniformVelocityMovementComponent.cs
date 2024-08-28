using GameServer.MainUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Component
{
    internal class UniformVelocityMovementComponent
    {
        private int LastUpdateTick = 0;
        private int Speed { get; set; }
        private Vector3 Position { get; set; }
        private Vector3 TargetPosition { get; set; }
        public bool IsMoving { get; private set; }

        public UniformVelocityMovementComponent()
        {
            Speed = 0;
            Position = Vector3.Zero;
            TargetPosition = Vector3.Zero;
            IsMoving = false;
            MainProxy.GetSingletone.AddUniformVelocityMovementComponent(this);
        }

        public bool UpdateCheck()
        {
            if(Environment.TickCount - LastUpdateTick >= 16.67) // 60FPS
            {
                LastUpdateTick = Environment.TickCount;
                return true;
            }
            return false;
        }

        public void MoveToLocation(Vector3 Target)
        {
            TargetPosition = Target;
            IsMoving = true;
        }

        public void SetArrived()
        {
            Position = TargetPosition;
            IsMoving = false;
        }

        public bool CheckArrive()
        {
            if (Vector3.Distance(Position, TargetPosition) <= Speed * 0.1f) // 10% 범위까지는 도착으로 판정
            {
                return true;
            }
            return false;
        }
    }
}
