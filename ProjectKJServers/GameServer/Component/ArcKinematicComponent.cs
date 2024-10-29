using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using CoreUtility.Utility;

namespace GameServer.Component
{
    internal class ArcKinematicComponent
    {
        public Vector3 StartPosition { get; private set; }
        public Vector3 TargetPosition { get; private set; }
        public float InitialSpeed { get; private set; }
        public Vector3 Gravity { get; private set; } = new Vector3(0, 0, -980f);

        private Vector3 Velocity;
        private Vector3 CurrentPosition;
        private float ElapsedTime;
        private bool IsLaunched = false;
        public Action? WhenArrived;

        public ArcKinematicComponent(Vector3 Start, Vector3 Target, float Speed)
        {
            StartPosition = Start;
            TargetPosition = Target;
            if(Speed > 0)
                InitialSpeed = Speed;
            else
                InitialSpeed = CalculateInitialSpeed(Start, Target, 45, Gravity);
            CurrentPosition = Start;
        }

        // 궤적을 계산해서 발사가 가능한지 확인
        public bool CalculateTrajectory()
        {
            Vector3 ToTarget = TargetPosition - StartPosition; // 거리 벡터
            float HorizontalDistance = new Vector3(ToTarget.X, ToTarget.Y, 0).Length(); // 수평 거리
            float Height = ToTarget.Z; // 높이

            if(HorizontalDistance == 0)
            {
                LogManager.GetSingletone.WriteLog("Target is at the same position as the start");
                return false;
            }

            // 발사각 계산
            float Angle = CalculateLaunchAngle(HorizontalDistance, Height, InitialSpeed, Math.Abs(Gravity.Z));
            if (!float.IsNaN(Angle))
            {
                LogManager.GetSingletone.WriteLog($"Launch angle: {Angle * 180 / Math.PI:F2} degrees");
                LogManager.GetSingletone.WriteLog($"Initial speed: {InitialSpeed:F2} m/s");
                // 유효하면 발사
                SetVelocity(Angle);
                return true;
            }

            return false;
        }

        //주어진 각도로 계산하기 위한 속도 계산 (45도 발사는 가장 빠름)
        private float CalculateInitialSpeed(Vector3 Start, Vector3 Target, float Angle, Vector3 Gravity)
        {
            Vector3 ToTarget = Target - Start; // 거리 벡터
            float HorizontalDistance = new Vector3(ToTarget.X, ToTarget.Y, 0).Length(); // 수평 거리
            float Height = ToTarget.Z; // 높이

            float AngleRad = Angle * (float)Math.PI / 180.0f; // 각도를 라디안으로 변환
            float CosAngle = (float)Math.Cos(AngleRad); // 코사인 X
            float SinAngle = (float)Math.Sin(AngleRad); // 사인 Y
            float TanAngle = (float)Math.Tan(AngleRad); // 탄젠트 Z

            // 발사체 운동 공식 속도^2 = (중력 * 수평거리^2) / (2 * 코사인 * 코사인 * (높이 - 수평거리 * 탄젠트))
            float SpeedSquared = (Gravity.Z * HorizontalDistance * HorizontalDistance) / (2 * CosAngle * CosAngle * (Height - HorizontalDistance * TanAngle));
            return (float)Math.Sqrt(Math.Abs(SpeedSquared)); // 속도의 절대값을 사용하여 음수 방지
        }

        // 주어진 속도 높이 등을 기반으로 발사 각도 계산
        private float CalculateLaunchAngle(float HorizontalDistance, float Height, float Speed, float Gravity)
        {
            float SpeedSquared = Speed * Speed; // 속도 제곱
            // 루트값 계산
            float Root = (float)Math.Sqrt(SpeedSquared * SpeedSquared - Gravity * (Gravity * HorizontalDistance * HorizontalDistance + 2 * Height * SpeedSquared));
            // 첫번째 항
            float Term1 = SpeedSquared + Root;
            // 두번째 항
            float Term2 = Gravity * HorizontalDistance;
            // 발사각도 = 아크탄젠트(첫번째항 / 두번째항)
            return (float)Math.Atan2(Term1, Term2);
        }
        // 최종적으로 발사체 속도를 지정한다. (발사된다)
        private void SetVelocity(float Angle)
        {
            Vector3 ToTarget = TargetPosition - StartPosition; // 거리 벡터
            Vector3 Horizontal = Vector3.Normalize(new Vector3(ToTarget.X, ToTarget.Y, 0)); // 수평 거리
            Velocity = Horizontal * InitialSpeed * (float)Math.Cos(Angle); // 수평 속도
            Velocity.Z = InitialSpeed * (float)Math.Sin(Angle); // 수직 속도
            IsLaunched = true;
        }

        public void Update(float DeltaTime)
        {
            if (!IsLaunched) 
                return;

            // 밀리초를 초로 변환
            DeltaTime /= 1000f;
            ElapsedTime += DeltaTime;

            // 현재 위치 계산 (이동거리 = 속도 * 시간 + 0.5 * 중력 * 시간^2) 운동방정식
            Vector3 Displacement = Velocity * ElapsedTime + 0.5f * Gravity * ElapsedTime * ElapsedTime;
            CurrentPosition = StartPosition + Displacement;
            // 로깅용 현재 속도 계산
            //Vector3 CurrentVelocity = Velocity + Gravity * ElapsedTime;

            // 로깅 (필요에 따라 주석 처리 또는 제거 가능)
            //LogManager.GetSingletone.WriteLog($"Time: {ElapsedTime:F2}s, Position: {CurrentPosition}, Velocity: {CurrentVelocity}");

            if (Displacement.Z <= 0 && ElapsedTime > 0.1f)
            {
                float t = (-Velocity.Z - (float)Math.Sqrt(Velocity.Z * Velocity.Z - 2 * Gravity.Z * (StartPosition.Z - TargetPosition.Z))) / Gravity.Z;
                CurrentPosition = StartPosition + Velocity * t + 0.5f * Gravity * t * t;
                LogManager.GetSingletone.WriteLog($"Target reached at: {CurrentPosition}");
                IsLaunched = false;
                //도착했으니 도착 델리게이트를 활성화 시킨다.
                WhenArrived?.Invoke();
            }
        }

        public Vector3 GetCurrentPosition()
        {
            return CurrentPosition;
        }

        public bool HasReachedTarget()
        {
            return !IsLaunched;
        }
    }
}
