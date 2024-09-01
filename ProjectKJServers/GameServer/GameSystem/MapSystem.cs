using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Resource;
using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using Windows.ApplicationModel.VoiceCommands;
using System.Numerics;

namespace GameServer.GameSystem
{
    using Vector3 = System.Numerics.Vector3;
    using CustomVector3 = CoreUtility.GlobalVariable.Vector3;
    internal class MapSystem : IComponentSystem
    {
        private Dictionary<int, MapData> MapDataDictionary = new Dictionary<int, MapData>();

        public MapSystem()
        {
        }

        public void SetMapData(ref Dictionary<int, MapData> MapDataDictionary)
        {
            this.MapDataDictionary = MapDataDictionary;
        }

        public void Update()
        {
            // 만약 장애물 관련 업데이트가 필요할 경우 여기서 진행함
        }

        public bool CanMove(int MapID, CustomVector3 Position)
        {
            if(MapDataDictionary == null)
            {
                LogManager.GetSingletone.WriteLog("맵 데이터가 없습니다.");
                return false;
            }

            if (!MapDataDictionary.ContainsKey(MapID))
            {
                LogManager.GetSingletone.WriteLog($"맵 ID {MapID}에 해당하는 맵 정보가 없습니다.");
                return false;
            }
            MapData Data = MapDataDictionary[MapID];
            if (Position.X < 0 || Position.X > Data.MapBoundX)
            {
                LogManager.GetSingletone.WriteLog($"맵 ID {MapID}의 X축 경계를 벗어났습니다. {Position.X}");
                return false;
            }
            if (Position.Y < 0 || Position.Y > Data.MapBoundY)
            {
                LogManager.GetSingletone.WriteLog($"맵 ID {MapID}의 Y축 경계를 벗어났습니다. {Position.Y}");
                return false;
            }

            // Z축은 사용하지 않는다고 가정
            foreach (ConvertObstacles ObstacleData in Data.Obstacles)
            {
                if (ObstacleData.MeshName == "SM_Floor")
                {
                    continue;
                }
                //사각형이라면,,
                float MinX = ObstacleData.Points.Min(x => x.X);
                float MaxX = ObstacleData.Points.Max(x => x.X);
                float MinY = ObstacleData.Points.Min(x => x.Y);
                float MaxY = ObstacleData.Points.Max(x => x.Y);
                // 충돌 체크 범위가 올바른지 확인
                if (Position.X >= MinX && Position.X <= MaxX &&
                    Position.Y >= MinY && Position.Y <= MaxY)
                {
                    LogManager.GetSingletone.WriteLog($"맵 ID {MapID}의 장애물에 부딪혔습니다.{MinX} {MinY} {MaxX} {MaxY} {ObstacleData.MeshName}");
                    return false;
                }
            }
            return true;
        }
    }
}
