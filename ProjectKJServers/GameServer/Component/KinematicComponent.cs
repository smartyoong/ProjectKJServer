﻿using CoreUtility.Utility;
using GameServer.MainUI;
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


        public KinematicComponent(Vector3 Position, float MaxSpeed, float MaxAccelerate, float MaxRotation, float Radius)
        {
            CharacterData = new Kinematic(Position, Vector3.Zero, 0, 0);
            this.MaxSpeed = MaxSpeed;
            this.MaxAccelerate = MaxAccelerate;
            this.MaxRotation = MaxRotation;
            this.Radius = Radius;
            MaxAngular = 30;
            Target = new Kinematic(Vector3.Zero, Vector3.Zero, INVALID_RADIAN, INVALID_RADIAN);
            MoveFlag = (int)MoveType.None;
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

            if (HasFlag(MoveType.EqaulVelocityMove))
            {
                EqualVelocityMoveMethod EqualVelocityMove = new EqualVelocityMoveMethod();
                SteeringHandle? Result = EqualVelocityMove.GetSteeringHandle(1, CharacterData, Target, MaxSpeed, MaxAccelerate, MaxRotation, MaxAngular, Radius, SlowRadius, TIME_TO_TARGET);
                if (Result != null)
                {
                    CharacterData.Velocity += Result.Value.Linear;
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
                }
            }

             // 속력을 완전 0으로 만들어주는 플래그 (강제 멈춤, 위치 강제 조정)
            if (HasFlag(MoveType.VelocityStop))
            {
                CharacterData.Velocity = Vector3.Zero;
                TempHandle.Linear = Vector3.Zero;
                CharacterData.Position = Target.Position;
                RemoveMoveFlag(MoveType.VelocityStop);
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
            CharacterData.Orientation = ConvertMathUtility.GetNewOrientationByVelocity(CharacterData.Orientation, Target - CharacterData.Position);
            AddMoveFlag(MoveType.EqaulVelocityMove);
            //작동은 하는데 등가속도, 등가각속도 이 2개가 체감이 별로다. 특정 상황에서는 사용 가능할 듯 (빙판길 같은곳)
            //AddMoveFlag(MoveType.Move);
            //AddMoveFlag(MoveType.LookAtToMove);
            return true;
        }
    }

}
