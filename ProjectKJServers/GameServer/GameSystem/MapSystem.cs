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

        public void LoadMapResource()
        {
            ResourceLoader.GetSingletone.LoadMapData(ref MapDataDictionary);
            foreach (var Data in MapDataDictionary)
            {
                LogManager.GetSingletone.WriteLog($"맵 ID : {Data.Key}, 맵 이름 : {Data.Value.MapName}");
                foreach (Obstacle ObstacleData in Data.Value.Obstacles)
                {
                    LogManager.GetSingletone.WriteLog($"장애물 이름 : {ObstacleData.MeshName}");
                    LogManager.GetSingletone.WriteLog($"장애물 위치 : {ObstacleData.Location.X} {ObstacleData.Location.Y} {ObstacleData.Location.Z}");
                    LogManager.GetSingletone.WriteLog($"장애물 크기 : {ObstacleData.Scale.X} {ObstacleData.Scale.Y} {ObstacleData.Scale.Z}");
                    LogManager.GetSingletone.WriteLog($"메시 크기 : {ObstacleData.MeshSize.X}   {ObstacleData.MeshSize.Y}   {ObstacleData.MeshSize.Z}");
                }
                LogManager.GetSingletone.WriteLog($"맵 바운드 : {Data.Value.MapBoundX} {Data.Value.MapBoundY} {Data.Value.MapBoundZ}");
                foreach (MapPortalData PortalData in Data.Value.Portals)
                {
                    LogManager.GetSingletone.WriteLog($"포탈 이름 : {PortalData.MapName}");
                    LogManager.GetSingletone.WriteLog($"포탈 위치 : {PortalData.Portals[0].Location.X} {PortalData.Portals[0].Location.Y} {PortalData.Portals[0].Location.Z}");
                    LogManager.GetSingletone.WriteLog($"포탈 크기 : {PortalData.Portals[0].Scale.X} {PortalData.Portals[0].Scale.Y} {PortalData.Portals[0].Scale.Z}");
                    LogManager.GetSingletone.WriteLog($"포탈 바운드 : {PortalData.Portals[0].BoxSize.X} {PortalData.Portals[0].BoxSize.Y} {PortalData.Portals[0].BoxSize.Z}");
                    LogManager.GetSingletone.WriteLog($"포탈 이동 맵 ID : {PortalData.Portals[0].LinkMapID}");
                }

            }
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
