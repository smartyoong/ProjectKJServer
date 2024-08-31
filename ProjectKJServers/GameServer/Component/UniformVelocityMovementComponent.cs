using CoreUtility.Utility;
using GameServer.GameSystem;
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
        private readonly object _lock = new object();
        //외부에서 호출될 가능성이 높은 변수들을 Lock을 반드시 걸어준다.
        //프로퍼티만 락을 잡아주면된다, lock이 크리티컬 섹션이니까 매우 빠르게 돌아갈듯
        private int _Speed;
        private Vector3 _Position;
        private Vector3 _TargetPosition;
        private bool _IsMoving;
        public int Speed
        {
            get
            {
                lock (_lock)
                {
                    return _Speed;
                }
            }
            private set
            {
                lock (_lock)
                {
                    _Speed = value;
                }

            }
        }
        public Vector3 Position 
        { 
            get
            {
                lock (_lock)
                {
                    return _Position;
                }
            }
            private set
            {
                lock (_lock)
                {
                    _Position = value;
                }
            }
        }
        public Vector3 TargetPosition
        {
            get
            {
                lock (_lock)
                {
                    return _TargetPosition;
                }
            }
            private set
            {
                lock (_lock)
                {
                    _TargetPosition = value;
                }
            }
        }
        public bool IsMoving
        {
            get
            {
                lock (_lock)
                {
                    return _IsMoving;
                }
            }
            private set
            {
                lock (_lock)
                {
                    _IsMoving = value;
                }
            }
        }

        public UniformVelocityMovementComponent()
        {
            Speed = 0;
            Position = Vector3.Zero;
            TargetPosition = Vector3.Zero;
            IsMoving = false;
        }

        public bool MoveToLocation(int MapID, Vector3 Target)
        {
            // 추후 CanMove로 가능 여부 체크해보자 이때 같은 맵에 있는 다른 유저에게도 이동 패킷 보내야 할 듯
            if (MainProxy.GetSingletone.CanMove(MapID, Target))
            {
                TargetPosition = Target;
                IsMoving = true;
                LogManager.GetSingletone.WriteLog($"이동 목표지점 설정 완료 {TargetPosition}");
                return true;
            }
            return false;
        }

        public void SetArrived()
        {
            Position = TargetPosition;
            IsMoving = false;
        }

        public void UpdatePosition(Vector3 NewPosition)
        {
            Position = NewPosition;
        }

        public void InitMovementComponent(int NewSpeed, Vector3 Position)
        {
            Speed = NewSpeed;
            this.Position = Position;
        }
    }
}
