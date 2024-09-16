using CoreUtility.Utility;
using GameServer.MainUI;
using System.Numerics;

namespace GameServer.Component
{
    // 조종할때 사용
    internal struct SteeringHandle
    {
        public Vector3 Linear; // 직선 가속도 -> 이걸 통해서 속도를 증가시키자
        public float Angular; // 각속도 -> 이걸 통해서 회전 속도를 증가시키자
    }

    internal struct StaticData
    {
        public Vector3 Position;
        public float Orientation; // 라디언
    }

    internal struct LimitData
    {
        public float MaxSpeed;
        public float MaxRotation;
    }

    internal struct KinematicHandle
    {
        public Vector3 Velocity; // 속도
        public float Rotation; // 회전 속도
    }

    internal class KinematicComponent
    {
        private readonly object _lock = new object();
        StaticData Data;
        KinematicHandle Handle;
        LimitData Limit;
        SteeringHandle SteeringHandle;

        public KinematicComponent(float MaxSpeed, float MaxRotation, Vector3 Position)
        {
            Data = new StaticData();
            Data.Position = Position;
            Data.Orientation = 0;
            Limit = new LimitData();
            Limit.MaxRotation = MaxRotation;
            Limit.MaxSpeed = MaxSpeed;
            Handle = new KinematicHandle();
            Handle.Rotation = 0;
            Handle.Velocity = Vector3.Zero;
            SteeringHandle = new SteeringHandle();
            SteeringHandle.Angular = 0;
            SteeringHandle.Linear = Vector3.Zero;
        }

        public KinematicHandle GetKinematicHandle()
        {
            return Handle;
        }

        public SteeringHandle GetSteeringHandle()
        {
            return SteeringHandle;
        }

        //핸들과 컴포넌트를 세트로 묶어야 하나,,
        public void Update(float DeltaTime)
        {
            lock (_lock)
            {
                // 위치 업데이트
                Data.Position += Handle.Velocity * DeltaTime;
                // 방위 업데이트 (기본적인 회전 속도)
                Data.Orientation += Handle.Rotation * DeltaTime;
                // 속도 업데이트
                Handle.Velocity += SteeringHandle.Linear * DeltaTime;
                // 회전 속도 업데이트
                Data.Orientation += SteeringHandle.Angular * DeltaTime;
            }
            LogManager.GetSingletone.WriteLog($"위치 업데이트 완료 {Data.Position}");
        }

        float NewOrientation(float Current, Vector3 Velocity)
        {
            if (Velocity.Length() > 0)
            {
                return (float)Math.Atan2(Velocity.Y, Velocity.X);
            }
            return Current;
        }

        public bool MoveToLocation(int MapID, Vector3 Target)
        {
            // 추후 CanMove로 가능 여부 체크해보자 이때 같은 맵에 있는 다른 유저에게도 이동 패킷 보내야 할 듯
            if (!MainProxy.GetSingletone.CanMove(MapID, Target))
            {
                return false;
            }
            LogManager.GetSingletone.WriteLog($"이동 목표지점 설정 완료 {Target}");

            KinematicHandle NewHandle = new KinematicHandle();
            NewHandle.Velocity = Target - Data.Position;
            NewHandle.Velocity = Vector3.Normalize(NewHandle.Velocity) * Limit.MaxSpeed;
            NewHandle.Rotation = 0;
            lock (_lock)
            {
                Handle = NewHandle;
                Data.Orientation = NewOrientation(Data.Orientation, Handle.Velocity);
            }

            return true;
        }

        public void RunFromTarget(Vector3 Target)
        {
            KinematicHandle NewHandle = new KinematicHandle();
            NewHandle.Velocity = Data.Position - Target;
            NewHandle.Velocity = Vector3.Normalize(NewHandle.Velocity) * Limit.MaxSpeed;
            NewHandle.Rotation = 0;
            lock (_lock)
            {
                Handle = NewHandle;
                Data.Orientation = NewOrientation(Data.Orientation, Handle.Velocity);
            }
        }
    }
}
