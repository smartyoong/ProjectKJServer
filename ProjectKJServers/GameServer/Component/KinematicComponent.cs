using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using GameServer.MainUI;
using GameServer.Mehtod;
using GameServer.Object;
using System.Numerics;
namespace GameServer.Component
{
    using Vector3 = System.Numerics.Vector3;
    interface Behaviors
    {
        SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed,
           float MaxAccelerate, float MaxRotate, float MaxAngular, float TargetRadius, float SlowRadius, float TimeToTarget);
    }

    [Flags]
    enum MoveType : int
    {
        None = 0, // 0
        Move = 1 << 0,
        VelocityStop = 1 << 1,
        EqaulVelocityMove = 1 << 2,
        RotateStop = 1 << 3,
        LockOn = 1 << 4,
        LookAtToMove = 1 << 5,
        EqualVelocityWander = 1 << 6,
        FollwPath = 1 << 7,
        EqualVelocityChase = 1 << 8,
        EqualVelocityRunAway = 1 << 9,
        OreintationChange = 1 << 10,
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
        private Kinematic PreviusCharacterData;
        public Kinematic CharcaterStaticData { get => CharacterData; }
        public Kinematic PreviusCharacterStaticData { get => PreviusCharacterData; }
        private Kinematic Target;
        public Kinematic TargetStaticData { get => Target; }
        private Pawn Owner;
        private float BlockRadius;


        public KinematicComponent(Pawn Owner, Vector3 Position, float MaxSpeed, float MaxAccelerate, float MaxRotation, float Radius, float CollisionRadius)
        {
            this.Owner = Owner;
            CharacterData = new Kinematic(Position, Vector3.Zero, 0, 0);
            PreviusCharacterData = new Kinematic(Position, Vector3.Zero, 0, 0);
            this.MaxSpeed = MaxSpeed;
            this.MaxAccelerate = MaxAccelerate;
            this.MaxRotation = MaxRotation;
            this.Radius = Radius;
            MaxAngular = 30;
            Target = new Kinematic(Position, Vector3.Zero, INVALID_RADIAN, INVALID_RADIAN);
            MoveFlag = (int)MoveType.None;
            BlockRadius = CollisionRadius;
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

            // 이전 위치 업데이트
            PreviusCharacterData = CharacterData;

            Vector3 NextPosition = CharacterData.Position + CharacterData.Velocity * DeltaTime;

            List<ConvertObstacles> HitObstacles = new List<ConvertObstacles>();
            List<Vector2> HitPoints = new List<Vector2>();
            List<Vector2> HitNormals = new List<Vector2>();
            if (Owner.GetCollisionComponent.CheckPositionBlockByWall(Owner.GetCurrentMapID, NextPosition, BlockRadius, ref HitObstacles, ref HitNormals, ref HitPoints))
            {
                // 캐릭터의 현재 위치를 가져옵니다.
                Vector2 NewPosition = new Vector2(NextPosition.X, NextPosition.Y);
                Vector2 CurrentPosition = new Vector2(PreviusCharacterStaticData.Position.X, PreviusCharacterStaticData.Position.Y);

                // 충돌 지점에서의 법선 벡터를 이용하여 슬라이딩 벡터 계산
                // 벡터를 평면에 투영하는 것은 벡터와 법선 벡터의 내적을 이용하여 계산
                Vector2 Direction = NewPosition - CurrentPosition;
                Vector2 SlideDirection = Vector2.Zero;
                int DebugCount = 0;
                for (int i = 0; i < HitNormals.Count; i++)
                {
                    //최초 1개만 찾고 끝내자
                    if (HitNormals[i] != Vector2.Zero)
                    {
                        SlideDirection = Direction - Vector2.Dot(Direction, HitNormals[i]) * HitNormals[i];
                        break;
                    }
                    DebugCount++;
                }

                // AdjustPosition을 CurrentPosition에서 SlideDirection을 더한 값으로 설정
                Vector2 AdjustPosition = CurrentPosition + SlideDirection;

                //LogManager.GetSingletone.WriteLog($"캐릭터 {Owner.GetAccountID()}이 원에 막혀 위치가 조정됩니다. {AdjustPosition}");
                //LogManager.GetSingletone.WriteLog($"캐릭터 {Owner.GetAccountID()}의 슬라이드 방향 및 조정전 위치. {SlideDirection}, {CurrentPosition}");
                //LogManager.GetSingletone.WriteLog($"캐릭터 {Owner.GetAccountID()}의 Next랑 HitNormal. {NewPosition}, {HitNormals[DebugCount]} {DebugCount}");

                // 조정된 위치로 업데이트
                CharacterData.Position = new Vector3(AdjustPosition.X, AdjustPosition.Y, 0);
                // 방위 업데이트 (기본적인 회전 속도)
                CharacterData.Orientation += CharacterData.Rotation * DeltaTime;
            }
            else // 충돌하지 않았다면 그냥 업데이트 시킨다.
            {
                // 위치 업데이트
                CharacterData.Position += CharacterData.Velocity * DeltaTime;
                // 방위 업데이트 (기본적인 회전 속도)
                CharacterData.Orientation += CharacterData.Rotation * DeltaTime;
            }

            SteeringHandle TempHandle = new SteeringHandle(Vector3.Zero, 0);

            // 우선 순위 기반 행동 조합이다.

            // 가속도 기반 이동이다.
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
                    AddMoveFlag(MoveType.VelocityStop);
                }
            }

            // 등속도 기반 경로 추적 이동
            if (HasFlag(MoveType.FollwPath))
            {
                //경로가 없으면 행동을 안한다.
                if (Owner.GetPathComponent == null)
                {
                    RemoveMoveFlag(MoveType.FollwPath);
                    return;
                }
                PathComponent Path = Owner.GetPathComponent;
                FollowPathMethod FollowPath = new FollowPathMethod(ref Path);
                SteeringHandle? Result = FollowPath.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                if (Result != null)
                {
                    CharacterData.Velocity += Result.Value.Linear;
                }
                else
                {
                    RemoveMoveFlag(MoveType.FollwPath);
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

            // 일정시간 뒤에는 멈추도록 해야한다.
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

            // 일정시간 뒤에는 멈추도록 해야한다.
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

            // 일정시간 뒤에는 멈추도록 해야한다.
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
                LogManager.GetSingletone.WriteLog($"도착! {CharacterData.Position}");
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

            if (HasFlag(MoveType.OreintationChange))
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
    }

}
