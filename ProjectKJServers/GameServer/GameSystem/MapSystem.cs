using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Resource;
using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using Windows.ApplicationModel.VoiceCommands;
//using System.Numerics;

namespace GameServer.GameSystem
{
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

        public bool CanMove(int MapID, Vector3 Position)
        {
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
            if (Position.Z < 0 || Position.Z > Data.MapBoundZ)
            {
                LogManager.GetSingletone.WriteLog($"맵 ID {MapID}의 Z축 경계를 벗어났습니다. {Position.Z}");
                return false;
            }

            // Z축은 사용하지 않는다고 가정
            foreach (Obstacle ObstacleData in Data.Obstacles)
            {
                if (Position.X >= ObstacleData.Location.X && Position.X <= ObstacleData.Location.X + ObstacleData.Scale.X * ObstacleData.MeshSize.X)
                {
                    if (Position.Y >= ObstacleData.Location.Y && Position.Y <= ObstacleData.Location.Y + ObstacleData.Scale.Y * ObstacleData.MeshSize.Y)
                    {
                        LogManager.GetSingletone.WriteLog($"맵 ID {MapID}의 장애물에 부딪혔습니다. {Position} {ObstacleData}");
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
