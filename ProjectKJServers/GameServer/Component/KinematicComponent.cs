using CoreUtility.Utility;
using GameServer.MainUI;
using System.Collections.Concurrent;
using System.Numerics;
using Windows.ApplicationModel.Background;

namespace GameServer.Component
{
    interface Behaviors
    {
        SteeringHandle? GetSteeringHandle(Kinematic Character, Kinematic Target,float MaxSpeed,
            float MaxAccelerate, float MaxRotate, float MaxAngular, float TargetRadius, float SlowRadius, float TimeToTarget);
    }

    enum MoveType
    {
        Stop,
        Chase,
        RunAway,
        Move
    }

    // 조종할때 사용
    internal struct SteeringHandle
    {
        public Vector3 Linear { get; set; } // 직선 가속도 -> 이걸 통해서 속도를 증가시키자
        public float Angular { get; set; } // 각속도 -> 이걸 통해서 회전 속도를 증가시키자

        // 생성자
        public SteeringHandle(Vector3 linear, float angular)
        {
            Linear = linear;
            Angular = angular;
        }

        // 연산자 오버로딩
        public static SteeringHandle operator +(SteeringHandle A, SteeringHandle B)
        {
            return new SteeringHandle(A.Linear + B.Linear, A.Angular + B.Angular);
        }
    }

    internal struct Kinematic(Vector3 Position, Vector3 Velocity, float Orientation, float Rotation)
    {
        public Vector3 Position { get; set; } = Position;
        public Vector3 Velocity { get; set; } = Velocity;
        public float Orientation { get; set; } = Orientation;
        public float Rotation { get; set; } = Rotation;
    }


    internal class KinematicComponent
    {
        // 고정시간으로 목표지점까지 이동 시킬거면 필요함 주석 해제하면됨
        private const float TIME_TO_TARGET = 0.25f;
        private const float INVALID_RADIAN = -361;
        private const float SlowRadius = 300f;
        public float MaxSpeed { get; private set; }
        public float MaxRotation { get; private set; }
        public float Radius { get; private set; }
        public float MaxAccelerate { get; private set; }
        public float MaxAngular { get; private set; }
        private ConcurrentQueue<Behaviors> BehaviorList;
        private Kinematic CharacterData;
        public Kinematic CharcaterStaticData { get => CharacterData; }
        private Kinematic Target;
        public Kinematic TargetStaticData { get => Target; }

        public MoveType CurrentMoveType { get; private set; }

        public KinematicComponent(Vector3 Position, float MaxSpeed, float MaxAccelerate, float MaxRotation, float Radius)
        {
            BehaviorList = new ConcurrentQueue<Behaviors>();
            CharacterData = new Kinematic(Position, Vector3.Zero, 0, 0);
            this.MaxSpeed = MaxSpeed;
            this.MaxAccelerate = MaxAccelerate;
            this.MaxRotation = MaxRotation;
            this.Radius = Radius;
            MaxAngular = 1;
            Target = new Kinematic(Vector3.Zero, Vector3.Zero, INVALID_RADIAN, INVALID_RADIAN);
            CurrentMoveType = MoveType.Stop;
        }

        public void Update(float DeltaTime)
        {
            DeltaTime /= 1000;

            // 위치 업데이트
            CharacterData.Position += CharacterData.Velocity * DeltaTime;
            // 방위 업데이트 (기본적인 회전 속도)
            CharacterData.Orientation += CharacterData.Rotation * DeltaTime;

            SteeringHandle TempHandle = new SteeringHandle(Vector3.Zero, 0);
            // 정확히 현재 큐에 있는 만큼만 반복한다. 추가로 삽입된건 다음턴에 한다.
            int CurrentCount = BehaviorList.Count;
            for (int i = 0; i < CurrentCount; ++i)
            {
                if (BehaviorList.IsEmpty)
                    break;
                BehaviorList.TryDequeue(out Behaviors? TempBehavior);
                if (TempBehavior == null)
                    continue;
                SteeringHandle? BehaviorResult;
                BehaviorResult = TempBehavior.GetSteeringHandle
                    (CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                // 아무런 동작을 할 필요가 없다.
                if (BehaviorResult == null)
                {
                    LogManager.GetSingletone.WriteLog($"도착 진짜 위치 : {CharacterData.Position}");
                    StopMove();
                    LogManager.GetSingletone.WriteLog($"도착 위치 : {CharacterData.Position}");
                    LogManager.GetSingletone.WriteLog($"도착 속력 : {CharacterData.Velocity}");
                    continue;
                }
                TempHandle += (SteeringHandle)BehaviorResult;
                BehaviorList.Enqueue(TempBehavior);
            }

            // 속도 업데이트
            CharacterData.Velocity += TempHandle.Linear * DeltaTime;
            // 회전 속도 업데이트
            CharacterData.Orientation += TempHandle.Angular * DeltaTime;

            // 속도 제한
            if (CharacterData.Velocity.Length() > MaxSpeed)
            {
                CharacterData.Velocity = Vector3.Normalize(CharacterData.Velocity) * MaxSpeed;
            }

            // 회전 속도 제한

            if (CharacterData.Rotation > MaxRotation)
            {
                CharacterData.Rotation = MaxRotation;
            }
        }

        private bool IsMovingNow()
        {
            return Math.Abs(CharacterData.Rotation) > 1f || CharacterData.Velocity.Length() > 3f;
        }

        public void StopMove()
        {
            BehaviorList.Clear();
            CharacterData.Velocity = Vector3.Zero;
            CharacterData.Rotation = 0;
            CurrentMoveType = MoveType.Stop;
            CharacterData.Position = Target.Position;
            CharacterData.Orientation = Target.Orientation;
        }

        public bool MoveToLocation(int MapID, Vector3 Target)
        {
            // 추후 CanMove로 가능 여부 체크해보자 이때 같은 맵에 있는 다른 유저에게도 이동 패킷 보내야 할 듯
            if (!MainProxy.GetSingletone.CanMove(MapID, Target))
            {
                return false;
            }

            this.Target.Position = Target;
            MoveMethod Move = new MoveMethod();
            BehaviorList.Enqueue(Move);
            CurrentMoveType = MoveType.Move;
            return true;
        }
    }

}
