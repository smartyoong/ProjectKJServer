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
    internal struct KinematicHandle
    {
        public Vector3 Linear; // 직선 가속도 -> 이걸 통해서 속도를 증가시키자
        public float Angular; // 각속도 -> 이걸 통해서 회전 속도를 증가시키자
    }

    internal class KinematicComponent
    {
        private readonly object _lock = new object();
        Vector3 Position;
        float Orientation; // 라디언
        Vector3 Velocity; // 속도
        float Rotation; // 회전 속도
        float MaxSpeed;
        float MaxRotation;

        public KinematicComponent(float MaxSpeed, float MaxRotation, Vector3 Position)
        {
            this.Position = Position;
            Orientation = 0;
            Velocity = Vector3.Zero;
            Rotation = 0;
            this.MaxRotation = MaxRotation;
            this.MaxSpeed = MaxSpeed;
        }

        //핸들과 컴포넌트를 세트로 묶어야 하나,,
        public void Update(KinematicHandle Handle,float DeltaTime)
        {
            lock (_lock)
            {
                // 위치 업데이트
                Position += Velocity * DeltaTime;
                // 방위 업데이트 (기본적인 회전 속도)
                Orientation += Rotation * DeltaTime;
                // 속도 업데이트
                Velocity += Handle.Linear * DeltaTime;
                // 회전 속도 업데이트
                Orientation += Handle.Angular * DeltaTime;
            }
        }

        public bool MoveToLocation(int MapID, Vector3 Target)
        {
            // 추후 CanMove로 가능 여부 체크해보자 이때 같은 맵에 있는 다른 유저에게도 이동 패킷 보내야 할 듯
            if (MainProxy.GetSingletone.CanMove(MapID, Target))
            {
                TargetPosition = Target;
                LogManager.GetSingletone.WriteLog($"이동 목표지점 설정 완료 {TargetPosition}");
                return true;
            }
            return false;
        }

        public void SetArrived()
        {
            Position = TargetPosition;
            LogManager.GetSingletone.WriteLog($"이동 완료 {Position}");
        }

        public void UpdatePosition(Vector3 NewPosition)
        {
            Position = NewPosition;
        }
    }
}
