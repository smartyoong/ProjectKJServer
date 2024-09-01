using CoreUtility.GlobalVariable;
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

        public static ConvertObstacles CalculateSquareVertex(Obstacle Obs, string MeshName)
        {
            Vector2 Location = new Vector2(Obs.Location.X, Obs.Location.Y);
            Vector2 Scale = new Vector2(Obs.Scale.X, Obs.Scale.Y);
            Vector2 MeshSize = new Vector2(Obs.MeshSize.X, Obs.MeshSize.Y);
            // 2D 회전을 위해 Yaw만 사용 (라디안으로 변환)
            float Rotation = ToRadian(Obs.Rotation.Yaw);

            Vector2[] Vertices =
            [
                new Vector2(MeshSize.X / 2, MeshSize.Y / 2),
                new Vector2(-MeshSize.X / 2, MeshSize.Y / 2),
                new Vector2(MeshSize.X / 2, -MeshSize.Y / 2),
                new Vector2(-MeshSize.X / 2, -MeshSize.Y / 2),
            ];
            ConvertObstacles ConvertObstacles = new ConvertObstacles(0, new List<Vector3>(), MeshName);
            for (int i = 0; i < Vertices.Length; i++)
            {
                // 스케일 적용
                Vector2 ScaledVertex = Vector2.Multiply(Vertices[i], Scale);

                // 2D 회전 적용
                Vector2 RotatedVertex = new Vector2(
                    ScaledVertex.X * (float)Math.Cos(Rotation) - ScaledVertex.Y * (float)Math.Sin(Rotation),
                    ScaledVertex.X * (float)Math.Sin(Rotation) + ScaledVertex.Y * (float)Math.Cos(Rotation)
                );

                // 위치 적용
                Vector2 FinalVertex = RotatedVertex + Location;

                // 결과를 3D Vector로 변환 (Z = 0)
                ConvertObstacles.Points.Add(new Vector3(FinalVertex.X, FinalVertex.Y, 0));

                LogManager.GetSingletone.WriteLog($" 변환중 {FinalVertex} {ScaledVertex} {RotatedVertex} {Location}");
            }
            return ConvertObstacles;
        }
    }
}
