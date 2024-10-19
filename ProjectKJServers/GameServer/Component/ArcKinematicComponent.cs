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
        private Vector3 CurrentPosition;
        private Vector3 Gravity;
        private float ElapsedTime;
        private Vector3 TargetPosition;
        private float Threshold = 0.3f;
        private bool IsAlreadyArrived = false;
        private Vector3? Velocity = Vector3.Zero;
        private float Speed;

        public ArcKinematicComponent(Vector3 Start, Vector3 End, float MuzzleV)
        {
            TargetPosition = End;
            Gravity = new Vector3(0,0,-9.8f);
            ElapsedTime = 0;
            CurrentPosition = Start;
            Speed = MuzzleV;
        }

        public static bool CheckCanShoot(Vector3 Start, Vector3 End, float MuzzleV, Vector3 Gravity)
        {
            Vector3? Solution = CalculateFiringSolution(Start, End, MuzzleV, Gravity);
            if (Solution == null)
            {
                return false;
            }
            return true;
        }

        private static Vector3? CalculateFiringSolution(Vector3 Start, Vector3 End, float MuzzleV, Vector3 Gravity)
        {
            Vector3 Delta = End - Start;
            float a = Gravity.LengthSquared();
            float b = -4 * (Vector3.Dot(Gravity * Delta, Gravity * Delta) + MuzzleV * MuzzleV);
            float c = Delta.LengthSquared();

            float Discriminant = b * b - 4 * a * c;
            if (Discriminant < 0)
            {
                return null;
            }

            float Time0 = (float)Math.Sqrt((-b + Math.Sqrt(Discriminant)) / (2 * a));
            float Time1 = (float)Math.Sqrt((-b - Math.Sqrt(Discriminant)) / (2 * a));

            float ttt = 0;
            if (Time0 < 0)
            {
                if (Time1 < 0)
                    return null;
                else
                    ttt = Time1;
            }
            else
            {
                if (Time1 < 0)
                {
                    ttt = Time0;
                }
                else
                {
                    ttt = Math.Min(Time0, Time1); // Max를 리턴하면 높은 궤적을 선택하게됨
                }
            }

            return (Delta * 2 - Gravity * (ttt * ttt)) / (2 * MuzzleV * ttt);
        }

        public void Update(float DeltaTime)
        {
            if (IsAlreadyArrived)
                return;

            DeltaTime /= 1000; // DeltaTime을 초 단위로 변환

            Velocity = CalculateFiringSolution(CurrentPosition, TargetPosition, Speed, Gravity);

            if (Velocity == null)
            {
                LogManager.GetSingletone.WriteLog($"Cannot shoot the target! {CurrentPosition} {TargetPosition}");
                IsAlreadyArrived = true;
                return;
            }

            // 위치 = 속도 * 시간
            CurrentPosition += Velocity.Value * DeltaTime;

            // 로그를 통해 현재 위치를 출력
            LogManager.GetSingletone.WriteLog($"Current Position: {CurrentPosition}");

            // 목표 위치에 도달했는지 확인
            if (Vector3.Distance(CurrentPosition, TargetPosition) <= Threshold)
            {
                LogManager.GetSingletone.WriteLog("Target reached!");
                IsAlreadyArrived = true;
            }
        }

        public Vector3 GetCurrentPosition()
        {
            return CurrentPosition;
        }

        public bool IsArrived()
        {
            return IsAlreadyArrived;
        }
    }
}
