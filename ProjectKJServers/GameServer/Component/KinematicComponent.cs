using CoreUtility.Utility;
using GameServer.MainUI;
using System.Numerics;
using WinRT;

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
        public float Radius;
    }

    internal struct KinematicHandle
    {
        public Vector3 Velocity; // 속도
        public float Rotation; // 회전 속도
    }

    internal class KinematicComponent
    {
        private readonly object _lock = new object();
        private StaticData Data;
        private KinematicHandle Handle;
        private LimitData Limit;
        private SteeringHandle SteeringHandle;
        private Vector3 Destination;

        public KinematicComponent(float MaxSpeed, float MaxRotation, Vector3 Position)
        {
            Data = new StaticData();
            Data.Position = Position;
            Data.Orientation = 0;
            Limit = new LimitData();
            Limit.MaxRotation = MaxRotation;
            Limit.MaxSpeed = MaxSpeed;
            Limit.Radius = 10f;
            Handle = new KinematicHandle();
            Handle.Rotation = 0;
            Handle.Velocity = Vector3.Zero;
            SteeringHandle = new SteeringHandle();
            SteeringHandle.Angular = 0;
            SteeringHandle.Linear = Vector3.Zero;
            Destination = Vector3.Zero;
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
            DeltaTime /= 1000;
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

            if(Destination != Vector3.Zero)
            {
                Arrive();
            }
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

            KinematicHandle NewHandle = new KinematicHandle();
            NewHandle.Velocity = Target - Data.Position;
            NewHandle.Velocity = Vector3.Normalize(NewHandle.Velocity) * Limit.MaxSpeed;
            NewHandle.Rotation = 0;
            lock (_lock)
            {
                Destination = Target;
                Handle = NewHandle;
                Data.Orientation = NewOrientation(Data.Orientation, Handle.Velocity);
            }

            return true;
        }

        // 도망 가기
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

        public void Arrive()
        {
            // 고정시간으로 목표지점까지 이동 시킬거면 필요함 주석 해제하면됨
            //float TimeToTarget = 0.25f;
            KinematicHandle NewHandle = new KinematicHandle();
            NewHandle.Velocity = Destination - Data.Position;

            // 도착했는가?
            if (NewHandle.Velocity.Length() < Limit.Radius)
            {
                LogManager.GetSingletone.WriteLog($"도착 업데이트 시작 {Data.Position}");
                NewHandle.Velocity = Vector3.Zero;
                NewHandle.Rotation = 0;
                lock (_lock)
                {
                    Data.Position = Destination;
                    Destination = Vector3.Zero;
                }
                LogManager.GetSingletone.WriteLog($"도착 업데이트 완료 {Data.Position}");
            }
            else
            {
                //NewHandle.Velocity = NewHandle.Velocity / TimeToTarget;
                if (NewHandle.Velocity.Length() > Limit.MaxSpeed)
                {
                    NewHandle.Velocity = Vector3.Normalize(NewHandle.Velocity) * Limit.MaxSpeed;
                }
                NewHandle.Rotation = 0;
            }

            lock (_lock)
            {
                Data.Orientation = NewOrientation(Data.Orientation, NewHandle.Velocity);
                Handle = NewHandle;
            }
        }

        // 랜덤한 방향으로 왔다리 갔다리 하도록하는 코드
        public void Wandering()
        {
            KinematicHandle NewHandle = new KinematicHandle();
            NewHandle.Velocity = Limit.MaxSpeed * ConvertMathUtility.RadianToVector3(Data.Orientation);
            NewHandle.Rotation = ConvertMathUtility.RandomBinomial() * Limit.MaxRotation;
            lock(_lock)
            {
                Handle = NewHandle;
            }
        }
    }
}
