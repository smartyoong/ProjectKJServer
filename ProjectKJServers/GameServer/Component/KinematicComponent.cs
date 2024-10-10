using CoreUtility.Utility;
using GameServer.MainUI;
using GameServer.Mehtod;
using System.Collections.Concurrent;
using System.Numerics;
using Windows.ApplicationModel.Background;
namespace GameServer.Component
{
    interface Behaviors
    {
         SteeringHandle? GetSteeringHandle(float Ratio ,Kinematic Character, Kinematic Target,float MaxSpeed,
            float MaxAccelerate, float MaxRotate, float MaxAngular, float TargetRadius, float SlowRadius, float TimeToTarget);
    }

    [Flags]
    enum MoveType : int
    {
        None = 0, // 0
        Chase = 1 << 0, // 1
        RunAway = 1 << 1, // 2
        Move = 1 << 2, // 4
        VelocityStop = 1 << 3, // 8
        EqaulVelocityMove = 1 << 4, // 16
        Brake = 1 << 5, // 32
        RotateStop = 1 << 6, // 64
        Align = 1 << 7, // 128
        VelocityMatch = 1 << 8, // 256
        Pursue = 1 << 9, // 512
        LockOn = 1 << 10, // 1024
        LookAtToMove = 1 << 11, // 2048
        EqualVelocityWander = 1 << 12, // 4096
        Wander = 1 << 13, // 8192
        FollwPath = 1 << 14, // 16384
        Sperate = 1 << 15, // 32768
        CollsionAvoidance = 1 << 16, // 65536
        ObstacleAvoidance = 1 << 17, // 131072
        EqualVelocityChase = 1 << 18, // 262144
        EqualVelocityRunAway = 1 << 19, // 524288
        OreintationChange = 1 << 20, // 1048576
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
        private const float TIME_TO_TARGET = 0.1f;
        private const float INVALID_RADIAN = -361;
        private const float SlowRadius = 400f;
        private int MoveFlag = 0;
        public float MaxSpeed { get; private set; }
        public float MaxRotation { get; private set; }
        public float Radius { get; private set; }
        public float MaxAccelerate { get; private set; }
        public float MaxAngular { get; private set; }

        private Kinematic CharacterData;
        public Kinematic CharcaterStaticData { get => CharacterData; }
        private Kinematic Target;
        public Kinematic TargetStaticData { get => Target; }
        private PathComponent? Path;

        private Vector3 CollisionPosition;
        private Vector3 CollisionNormal;

        public KinematicComponent(Vector3 Position, float MaxSpeed, float MaxAccelerate, float MaxRotation, float Radius, PathComponent? Path)
        {
            CharacterData = new Kinematic(Position, Vector3.Zero, 0, 0);
            this.MaxSpeed = MaxSpeed;
            this.MaxAccelerate = MaxAccelerate;
            this.MaxRotation = MaxRotation;
            this.Radius = Radius;
            MaxAngular = 30;
            Target = new Kinematic(Vector3.Zero, Vector3.Zero, INVALID_RADIAN, INVALID_RADIAN);
            MoveFlag = (int)MoveType.None;
            this.Path = Path;
        }

        private void AddMoveFlag(MoveType Flag)
        {
            int InitialValue, NewValue;
            do
            {
                InitialValue = MoveFlag;
                NewValue = InitialValue | (int)Flag;
            } while (Interlocked.CompareExchange(ref MoveFlag, NewValue, InitialValue) != InitialValue);
        }

        private void RemoveMoveFlag(MoveType Flag)
        {
            int InitialValue, NewValue;
            do
            {
                InitialValue = MoveFlag;
                NewValue = InitialValue & ~(int)Flag;
            } while (Interlocked.CompareExchange(ref MoveFlag, NewValue, InitialValue) != InitialValue);
        }

        private bool HasFlag(MoveType Flag)
        {
            return (MoveFlag & (int)Flag) == (int)Flag;
        }
        // 내생각엔 욕심이 너무 과했다. 가속도 Move까지만 있어도 될거 같긴한데
        public void Update(float DeltaTime)
        {
            DeltaTime /= 1000;

            // 위치 업데이트
            CharacterData.Position += CharacterData.Velocity * DeltaTime;
            // 방위 업데이트 (기본적인 회전 속도)
            CharacterData.Orientation += CharacterData.Rotation * DeltaTime;

            SteeringHandle TempHandle = new SteeringHandle(Vector3.Zero, 0);

            // 우선 순위 기반 행동 조합이다.

            if (HasFlag(MoveType.Move))
            {
                MoveMethod Move = new MoveMethod();
                var Result = Move.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                if (Result != null)
                {
                    TempHandle += Result.Value;
                }
                else
                {
                    RemoveMoveFlag(MoveType.Move);
                    AddMoveFlag(MoveType.Brake);
                }
            }

            if(HasFlag(MoveType.Pursue))
            {
                PursueMethod Pursue = new PursueMethod();
                var Result = Pursue.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                if (Result != null)
                {
                    TempHandle += Result.Value;
                }
                else
                {
                    RemoveMoveFlag(MoveType.Pursue);
                }
            }

            if (HasFlag(MoveType.VelocityMatch))
            {
                VelocityMatchMethod VelocityMatch = new VelocityMatchMethod();
                var Result = VelocityMatch.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                if (Result != null)
                {
                    TempHandle += Result.Value;
                }
                else
                {
                    RemoveMoveFlag(MoveType.VelocityMatch);
                }
            }

            if (HasFlag(MoveType.Chase))
            {
                ChaseMethod Chase = new ChaseMethod();
                var Result = Chase.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                if (Result != null)
                {
                    TempHandle += Result.Value;
                    RemoveMoveFlag(MoveType.Chase);
                }
            }

            if (HasFlag(MoveType.RunAway))
            {
                RunAwayMethod RunAway = new RunAwayMethod();
                SteeringHandle? Result = RunAway.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                if (Result != null)
                {
                    TempHandle += Result.Value;
                    RemoveMoveFlag(MoveType.RunAway);
                }
            }

            if (HasFlag(MoveType.Brake))
            {
                BrakeMethod Brake = new BrakeMethod();
                SteeringHandle? Result = Brake.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, DeltaTime);
                if (Result != null)
                {
                    TempHandle += Result.Value;
                }
                else
                {
                    RemoveMoveFlag(MoveType.Brake);
                    AddMoveFlag(MoveType.VelocityStop);
                }
            }

            if(HasFlag(MoveType.Wander))
            {
                WanderMethod Wander = new WanderMethod();
                SteeringHandle? Result = Wander.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                // 한번만 가속을 추가하고 끝낸다.
                // 매우 느리겠지만 어울릴 수도 있다.
                // 만약 매우 느리다면 Velocity쪽애 직접 추가하는 것을 고려해보자
                if (Result != null)
                {
                    TempHandle += Result.Value;
                    RemoveMoveFlag(MoveType.Wander);
                }
            }

            if (HasFlag(MoveType.FollwPath))
            {
                //경로가 없으면 행동을 안한다.
                if(Path == null)
                {
                    RemoveMoveFlag(MoveType.FollwPath);
                    return;
                }

                FollowPathMethod FollowPath = new FollowPathMethod(ref Path);
                SteeringHandle? Result = FollowPath.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                if (Result != null)
                {
                    TempHandle += Result.Value;
                }
                else
                {
                    RemoveMoveFlag(MoveType.FollwPath);
                }
            }

            if (HasFlag(MoveType.Sperate))
            {
                // 추후에 이걸 실질적으로 사용할때는 여기 List를 채우도록 구현한다.
                List<Kinematic> TargetList = new List<Kinematic>();
                SeperateMethod Sperate = new SeperateMethod(TargetList);
                SteeringHandle? Result = Sperate.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                if (Result != null)
                {
                    TempHandle += Result.Value;
                }
            }

            if (HasFlag(MoveType.CollsionAvoidance))
            {
                // 추후에 이걸 실질적으로 사용할때는 여기 List를 채우도록 구현한다.
                List<Kinematic> Targets = new List<Kinematic>();
                CollsionAvoidanceMethod CollsionAvoidance = new CollsionAvoidanceMethod(Targets);
                //여기서 Radius 대신 Collision Component의 Radius를 넣어주어야 한다.
                SteeringHandle? Result = CollsionAvoidance.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                if (Result != null)
                {
                    TempHandle += Result.Value;
                }
            }

            if (HasFlag(MoveType.ObstacleAvoidance))
            {
                //주의 충돌 판정 로직이 구현이 안되어 있음
                //충돌 판정 로직이 구현되면 해당 로직을 사용할것
                //Collision Component를 만들자
                ObstacleAvoidanceMethod ObstacleAvoidance = new ObstacleAvoidanceMethod(CollisionPosition,CollisionNormal);
                SteeringHandle? Result = ObstacleAvoidance.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                if (Result != null)
                {
                    TempHandle += Result.Value;
                }
                else
                {
                    RemoveMoveFlag(MoveType.ObstacleAvoidance);
                }
            }

            if (HasFlag(MoveType.EqaulVelocityMove))
            {
                EqualVelocityMoveMethod EqualVelocityMove = new EqualVelocityMoveMethod();
                SteeringHandle? Result = EqualVelocityMove.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                if (Result != null)
                {
                    CharacterData.Velocity += Result.Value.Linear;
                    AddMoveFlag(MoveType.OreintationChange);
                }
                else
                {
                    RemoveMoveFlag(MoveType.EqaulVelocityMove);
                    AddMoveFlag(MoveType.VelocityStop);
                }
            }

            if (HasFlag(MoveType.EqualVelocityWander))
            {
                EqualVelocityWanderMethod EqualVelocityWander = new EqualVelocityWanderMethod();
                SteeringHandle? Result = EqualVelocityWander.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                if (Result != null)
                {
                    // 배회 방향이랑 속도가 정해지면 플래그를 끈다.
                    CharacterData.Velocity += Result.Value.Linear;
                    CharacterData.Rotation += Result.Value.Angular;
                    RemoveMoveFlag(MoveType.EqualVelocityWander);
                    AddMoveFlag(MoveType.OreintationChange);
                }
            }

            if (HasFlag(MoveType.EqualVelocityChase))
            {
                EqualVelocityChaseMethod EqualVelocityChase = new EqualVelocityChaseMethod();
                SteeringHandle? Result = EqualVelocityChase.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                if (Result != null)
                {
                    CharacterData.Velocity += Result.Value.Linear;
                    CharacterData.Rotation += Result.Value.Angular;
                    RemoveMoveFlag(MoveType.EqualVelocityChase);
                    AddMoveFlag(MoveType.OreintationChange);
                }
            }

            if (HasFlag(MoveType.EqualVelocityRunAway))
            {
                EqualVelocityRunAwayMethod EqaulVelocityRunAway = new EqualVelocityRunAwayMethod();
                SteeringHandle? Result = EqaulVelocityRunAway.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                if (Result != null)
                {
                    CharacterData.Velocity += Result.Value.Linear;
                    CharacterData.Rotation += Result.Value.Angular;
                    RemoveMoveFlag(MoveType.EqualVelocityRunAway);
                    AddMoveFlag(MoveType.OreintationChange);
                }
            }

                // 속력을 완전 0으로 만들어주는 플래그 (강제 멈춤, 위치 강제 조정)
            if (HasFlag(MoveType.VelocityStop))
            {
                CharacterData.Velocity = Vector3.Zero;
                TempHandle.Linear = Vector3.Zero;
                CharacterData.Position = Target.Position;
                RemoveMoveFlag(MoveType.VelocityStop);
                LogManager.GetSingletone.WriteLog($"강제로 멈춤 {CharacterData.Position}");
            }

            // 속도 업데이트
            CharacterData.Velocity += TempHandle.Linear * DeltaTime;

            // 속도 제한
            if (CharacterData.Velocity.Length() > MaxSpeed)
            {
                CharacterData.Velocity = Vector3.Normalize(CharacterData.Velocity) * MaxSpeed;
            }


            if (HasFlag(MoveType.LookAtToMove))
            {
                LookAtToMoveMethod LookAtToMove = new LookAtToMoveMethod();
                var Result = LookAtToMove.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                if (Result != null)
                {
                    TempHandle += Result.Value;
                }
                else
                {
                    RemoveMoveFlag(MoveType.LookAtToMove);
                    AddMoveFlag(MoveType.RotateStop);
                }
            }

            if (HasFlag(MoveType.LockOn))
            {
                LockOnMethod LockOn = new LockOnMethod();
                var Result = LockOn.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                if (Result != null)
                {
                    TempHandle += Result.Value;
                }
                else
                {
                    RemoveMoveFlag(MoveType.LockOn);
                    AddMoveFlag(MoveType.RotateStop);
                }
            }

            if (HasFlag(MoveType.Align))
            {
                AlignMethod Align = new AlignMethod();
                var Result = Align.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, 2f, 10f, TIME_TO_TARGET);
                if (Result != null)
                {
                    TempHandle += Result.Value;
                }
                else
                {
                    RemoveMoveFlag(MoveType.Align);
                    AddMoveFlag(MoveType.RotateStop);
                }
            }

            if(HasFlag(MoveType.OreintationChange))
            {
                // 방향을 현재 속력에 따라 바꾼는 플래그
                CharacterData.Orientation = ConvertMathUtility.GetNewOrientationByVelocity(CharacterData.Orientation, CharacterData.Velocity);
                RemoveMoveFlag(MoveType.OreintationChange);
            }

            //회전을 강제로 멈추는 플래그
            if (HasFlag(MoveType.RotateStop))
            {
                CharacterData.Rotation = 0;
                TempHandle.Angular = 0;
                RemoveMoveFlag(MoveType.RotateStop);
            }

            // 회전 속도 업데이트
            CharacterData.Orientation += TempHandle.Angular * DeltaTime;

            // 회전 속도 제한

            if (CharacterData.Rotation > MaxRotation)
            {
                CharacterData.Rotation = MaxRotation;
            }
        }

        private void ClearAllFlag()
        {
            MoveFlag = (int)MoveType.None;
        }


        private bool IsMovingNow()
        {
            return Math.Abs(CharacterData.Rotation) > 1f || CharacterData.Velocity.Length() > 3f;
        }

        public bool MoveToLocation(int MapID, Vector3 Target)
        {
            // 추후 CanMove로 가능 여부 체크해보자 이때 같은 맵에 있는 다른 유저에게도 이동 패킷 보내야 할 듯
            if (!MainProxy.GetSingletone.CanMove(MapID, Target))
            {
                return false;
            }

            this.Target.Position = Target;
            AddMoveFlag(MoveType.EqaulVelocityMove);
            //작동은 하는데 등가속도, 등가각속도 이 2개가 체감이 별로다. 특정 상황에서는 사용 가능할 듯 (빙판길 같은곳)
            //AddMoveFlag(MoveType.Move);
            //AddMoveFlag(MoveType.LookAtToMove);
            return true;
        }

        public void AvoidObstacle(Vector3 Position, Vector3 Normal)
        {
            CollisionPosition = Position;
            CollisionNormal = Normal;
            AddMoveFlag(MoveType.ObstacleAvoidance);
        }

        public void StopMove()
        {
            //일단은 강제로 멈추게 해보자 그리고 이동관련 로직을 좀 쳐내자
            ClearAllFlag();
            Target.Position = CharacterData.Position;
            AddMoveFlag(MoveType.VelocityStop);
        }
    }

}
