using CoreUtility.Utility;
using GameServer.MainUI;
using GameServer.Object;
using System.Numerics;

namespace GameServer.Component
{

    // 조종할때 사용
    internal struct SteeringHandle
    {
        public Vector3 Linear; // 직선 가속도 -> 이걸 통해서 속도를 증가시키자
        public float Angular; // 각속도 -> 이걸 통해서 회전 속도를 증가시키자
    }

    internal struct KinematicStatic
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
        private KinematicStatic Data;
        private KinematicHandle Handle;
        private LimitData Limit;
        private SteeringHandle SteeringHandle;
        private Vector3 Destination;

        private PlayerCharacter? Player = null;

        public KinematicComponent(float MaxSpeed, float MaxRotation, Vector3 Position, PlayerCharacter? Onwer = null)
        {
            Data = new KinematicStatic();
            Data.Position = Position;
            Data.Orientation = 0;
            Limit = new LimitData();
            Limit.MaxRotation = MaxRotation;
            Limit.MaxSpeed = MaxSpeed;
            //클라랑 서버는 반경이 다르다 (클라가 먼저 선 이동하니까 동기화 목적)
            Limit.Radius = 10f;
            Handle = new KinematicHandle();
            Handle.Rotation = 0;
            Handle.Velocity = Vector3.Zero;
            SteeringHandle = new SteeringHandle();
            SteeringHandle.Angular = 0;
            SteeringHandle.Linear = Vector3.Zero;
            Destination = ConvertMathUtility.MinusOneVector3;
            Player = Onwer;
        }

        public KinematicHandle GetKinematicHandle()
        {
            return Handle;
        }

        public SteeringHandle GetSteeringHandle()
        {
            return SteeringHandle;
        }

        public Vector3 GetCurrentPosition()
        {
            return Data.Position;
        }

        public float GetCurrentOrientation()
        {
            return Data.Orientation;
        }

        public Vector3 GetDestination()
        {
            return Destination;
        }

        private void SetDestination(Vector3 Target)
        {
            lock (_lock)
            {
                Destination = Target;
            }
        }

        private void SetKinematicHandle(KinematicHandle NewHandle)
        {
            lock (_lock)
            {
                Handle = NewHandle;
            }
        }

        private void SetOrientation(float NewOrientation)
        {
            lock (_lock)
            {
                Data.Orientation = NewOrientation;
            }
        }

        private void SetPosition(Vector3 NewPosition)
        {
            lock (_lock)
            {
                Data.Position = NewPosition;
            }
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
            }

            if (Destination != ConvertMathUtility.MinusOneVector3)
            {
                Arrive();
            }

            lock (_lock)
            {
                // 속도 업데이트
                Handle.Velocity += SteeringHandle.Linear * DeltaTime;
                // 회전 속도 업데이트
                Data.Orientation += SteeringHandle.Angular * DeltaTime;
            }
        }

        float NewOrientation(float Current, Vector3 Velocity)
        {
            if (Velocity.Length() > 0)
            {
                return (float)Math.Atan2(Velocity.X, Velocity.X);
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
            NewHandle.Velocity = Target - GetCurrentPosition();
            NewHandle.Velocity = Vector3.Normalize(NewHandle.Velocity) * Limit.MaxSpeed;
            NewHandle.Rotation = 0;
            SetDestination(Target);
            SetKinematicHandle(NewHandle);
            SetOrientation(NewOrientation(GetCurrentOrientation(), NewHandle.Velocity));
            return true;
        }

        // 도망 가기
        public void RunFromTarget(Vector3 Target)
        {
            KinematicHandle NewHandle = new KinematicHandle();
            NewHandle.Velocity = GetCurrentPosition() - Target;
            NewHandle.Velocity = Vector3.Normalize(NewHandle.Velocity) * Limit.MaxSpeed;
            NewHandle.Rotation = 0;
            SetKinematicHandle(NewHandle);
            SetOrientation(NewOrientation(GetCurrentOrientation(), NewHandle.Velocity));
        }

        public void Arrive()
        {
            // 고정시간으로 목표지점까지 이동 시킬거면 필요함 주석 해제하면됨
            const float TimeToTarget = 0.25f;
            KinematicHandle NewHandle = new KinematicHandle();
            NewHandle.Velocity = GetDestination() - GetCurrentPosition();

            // 도착했는가?
            if (NewHandle.Velocity.Length() < Limit.Radius)
            {
                NewHandle.Velocity = Vector3.Zero;
                NewHandle.Rotation = 0;
                SetPosition(Destination);
                SetDestination(ConvertMathUtility.MinusOneVector3);
                LogManager.GetSingletone.WriteLog($"도착 업데이트 완료 {GetCurrentPosition()}");
            }
            else
            {
                NewHandle.Velocity = NewHandle.Velocity / TimeToTarget;
                if (NewHandle.Velocity.Length() > Limit.MaxSpeed)
                {
                    NewHandle.Velocity = Vector3.Normalize(NewHandle.Velocity) * Limit.MaxSpeed;
                }
                NewHandle.Rotation = 0;
            }

            SetKinematicHandle(NewHandle);
            SetOrientation(NewOrientation(GetCurrentOrientation(), NewHandle.Velocity));
        }

        // 랜덤한 방향으로 왔다리 갔다리 하도록하는 코드
        public void Wandering()
        {
            KinematicHandle NewHandle = new KinematicHandle();
            NewHandle.Velocity = Limit.MaxSpeed * ConvertMathUtility.RadianToVector3(GetCurrentOrientation());
            NewHandle.Rotation = ConvertMathUtility.RandomBinomial() * Limit.MaxRotation;
            SetKinematicHandle(NewHandle);
        }
    }
}
