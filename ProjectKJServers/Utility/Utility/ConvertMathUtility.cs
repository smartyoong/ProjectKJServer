﻿using CoreUtility.GlobalVariable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CoreUtility.Utility
{
    using CustomVector3 = CoreUtility.GlobalVariable.Vector3;
    using Vector3 = System.Numerics.Vector3;
    public class ConvertMathUtility
    {
        public static Vector3 ConvertToSystemVector3(CustomVector3 Vector3)
        {
            return new Vector3(Vector3.X, Vector3.Y, Vector3.Z);
        }
        public static float ToRadian(float degrees)
        {
            return (float)(degrees * (Math.PI / 180.0));
        }

        public static Quaternion ConvertToSystemQuaternion(CoreUtility.GlobalVariable.Rotation Rotation)
        {
            return Quaternion.CreateFromYawPitchRoll(ToRadian(Rotation.Yaw), ToRadian(Rotation.Pitch), ToRadian(Rotation.Roll));
        }
        private static Vector2 RotateVector(Vector2 vector, float angle)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            return new Vector2(
                vector.X * cos - vector.Y * sin,
                vector.X * sin + vector.Y * cos
            );
        }

        public static ConvertObstacles CalculateSquareVertex(Obstacle Obs, string MeshName)
        {
            const int SQUARE = 0;
            Vector2 Location = new Vector2(Obs.Location.X, Obs.Location.Y);
            Vector2 Scale = new Vector2(Obs.Scale.X, Obs.Scale.Y);
            Vector2 MeshSize = new Vector2(Obs.MeshSize.X, Obs.MeshSize.Y);
            // 2D 회전을 위해 Yaw만 사용 (라디안으로 변환)
            float Rotation = ToRadian(Obs.Rotation.Yaw);

            Vector2[] Vertices =
            [
                new Vector2(0, 0),
                new Vector2(MeshSize.X, 0),
                new Vector2(0, MeshSize.Y),
                new Vector2(MeshSize.X, MeshSize.Y),
            ];
            ConvertObstacles ConvertObstacles = new ConvertObstacles(SQUARE, new List<Vector3>(), MeshName,0,0,0);
            for (int i = 0; i < Vertices.Length; i++)
            {
                // 스케일 적용
                Vector2 ScaledVertex = Vector2.Multiply(Vertices[i], Scale);
                Vector2 WorldVertex = ScaledVertex + Location;
                // 2D 회전 적용
                Vector2 RotatedVertex = RotateVector(WorldVertex - Location, Rotation) + Location;

                // 결과를 3D Vector로 변환 (Z = 0)
                ConvertObstacles.Points.Add(new Vector3(RotatedVertex.X, RotatedVertex.Y, 0));

                //LogManager.GetSingletone.WriteLog($" 변환중 {WorldVertex} {ScaledVertex} {RotatedVertex} {Location}");
            }
            return ConvertObstacles;
        }
        public static ConvertObstacles CalculateSphereVertex(Obstacle Obs, string MeshName)
        {
            const int Sphere = 1;
            Vector2 Location = new Vector2(Obs.Location.X, Obs.Location.Y);
            Vector2 Scale = new Vector2(Obs.Scale.X, Obs.Scale.Y);
            Vector2 MeshSize = new Vector2(Obs.MeshSize.X, Obs.MeshSize.Y);
            float RotationAngle = ToRadian(Obs.Rotation.Yaw);

            LogManager.GetSingletone.WriteLog($" 변환중 {Location} {Obs.SphereRadius}");
            Vector2[] Vertices =
            [
                new Vector2(Location.X - Obs.SphereRadius, Location.Y - Obs.SphereRadius),
                new Vector2(Location.X + Obs.SphereRadius, Location.Y - Obs.SphereRadius),
                new Vector2(Location.X - Obs.SphereRadius, Location.Y + Obs.SphereRadius),
                new Vector2(Location.X + Obs.SphereRadius, Location.Y + Obs.SphereRadius),
            ];

            ConvertObstacles ConvertObstacles = new ConvertObstacles(Sphere, new List<Vector3>(), MeshName, Obs.CylinderRadius, Obs.CylinderHeight, Obs.SphereRadius);
            for (int i = 0; i < Vertices.Length; i++)
            {
                LogManager.GetSingletone.WriteLog($" 변환전 {Vertices[i]}");
                // 스케일 적용
                Vector2 ScaledVertex = Vector2.Multiply(Vertices[i], Scale);
                Vector2 WorldVertex = ScaledVertex + Location;
                // 2D 회전 적용
                Vector2 RotatedVertex = RotateVector(WorldVertex - Location, RotationAngle) + Location;

                // 결과를 3D Vector로 변환 (Z = 0)
                ConvertObstacles.Points.Add(new Vector3(RotatedVertex.X, RotatedVertex.Y, 0));

                LogManager.GetSingletone.WriteLog($" 변환후 {RotatedVertex}");
            }
            return ConvertObstacles;
        }

        public static ConvertObstacles CalculateCylinderVertex(Obstacle Obs, string MeshName)
        {
            const int Cylinder = 2;
            Vector2 Location = new Vector2(Obs.Location.X, Obs.Location.Y);
            Vector2 Scale = new Vector2(Obs.Scale.X, Obs.Scale.Y);
            Vector2 MeshSize = new Vector2(Obs.MeshSize.X, Obs.MeshSize.Y);
            float RotationAngle = ToRadian(Obs.Rotation.Yaw);

            // Cylinder의 꼭지점 좌표 계산 (Z축 무시이므로 사실상 X,Y좌표에 반지름 +-해주는것)
            LogManager.GetSingletone.WriteLog($" 변환중 {Location} {Obs.CylinderRadius}");
            Vector2[] Vertices =
            [
                new Vector2(Location.X - Obs.CylinderRadius, Location.Y - Obs.CylinderRadius),
                new Vector2(Location.X + Obs.CylinderRadius, Location.Y - Obs.CylinderRadius),
                new Vector2(Location.X - Obs.CylinderRadius, Location.Y + Obs.CylinderRadius),
                new Vector2(Location.X + Obs.CylinderRadius, Location.Y + Obs.CylinderRadius),
            ];

            ConvertObstacles ConvertObstacles = new ConvertObstacles(Cylinder, new List<Vector3>(), MeshName, Obs.CylinderRadius, Obs.CylinderHeight, Obs.SphereRadius);
            for (int i = 0; i < Vertices.Length; i++)
            {
                LogManager.GetSingletone.WriteLog($" 변환전 {Vertices[i]}");
                // 스케일 적용
                Vector2 ScaledVertex = Vector2.Multiply(Vertices[i], Scale);
                Vector2 WorldVertex = ScaledVertex + Location;
                // 2D 회전 적용
                Vector2 RotatedVertex = RotateVector(WorldVertex - Location, RotationAngle) + Location;

                // 결과를 3D Vector로 변환 (Z = 0)
                ConvertObstacles.Points.Add(new Vector3(RotatedVertex.X, RotatedVertex.Y, 0));

                LogManager.GetSingletone.WriteLog($" 변환후 {RotatedVertex}");
            }
            return ConvertObstacles;
        }
    }
}