using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Resource;
using CoreUtility.GlobalVariable;
using CoreUtility.Utility;

namespace GameServer.GameSystem
{
    internal class MapSystem : IComponentSystem
    {
        private static Lazy<MapSystem> instance = new Lazy<MapSystem>(() => new MapSystem());
        public static MapSystem GetSingletone { get { return instance.Value; } }

        private Dictionary<int, MapData> MapDataDictionary = new Dictionary<int, MapData>();

        private MapSystem()
        {
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
                return false;
            if (Position.Y < 0 || Position.Y > Data.MapBoundY)
                return false;
            if (Position.Z < 0 || Position.Z > Data.MapBoundZ)
                return false;

            // Z축은 사용하지 않는다고 가정
            foreach (Obstacle ObstacleData in Data.Obstacles)
            {
                if (Position.X >= ObstacleData.Location.X && Position.X <= ObstacleData.Location.X + ObstacleData.Scale.X * ObstacleData.MeshSize.X)
                {
                    if (Position.Y >= ObstacleData.Location.Y && Position.Y <= ObstacleData.Location.Y + ObstacleData.Scale.Y * ObstacleData.MeshSize.Y)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
