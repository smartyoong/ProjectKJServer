using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using CoreUtility.GlobalVariable;
using CoreUtility.Utility;

namespace GameServer.Resource
{
    internal class ResourceLoader
    {
        private static Lazy<ResourceLoader> instance = new Lazy<ResourceLoader>(() => new ResourceLoader());
        public static ResourceLoader GetSingletone { get { return instance.Value; } }
        private ResourceLoader()
        {
        }
        public void LoadMapData(ref Dictionary<int, MapData> MapDataDictionary)
        {
            LogManager.GetSingletone.WriteLog("맵 정보를 로드합니다.");
            List<MapDataForResourceLoader> MapResourceList = new List<MapDataForResourceLoader>();
            foreach (string JsonFile in Directory.GetFiles(Path.Combine(GameServerSettings.Default.ResourceDicrectory, "MapFiles"), "*.json"))
            {
                string Json = File.ReadAllText(JsonFile);
                MapDataForResourceLoader? MapData;
                try
                {
                    MapData = JsonSerializer.Deserialize<MapDataForResourceLoader>(Json);
                }
                catch (Exception e)
                {
                    LogManager.GetSingletone.WriteLog($"맵 정보를 로드하는데 실패했습니다. 파일명 : {JsonFile}");
                    LogManager.GetSingletone.WriteLog(e.Message);
                    continue;
                }

                if (MapData != null)
                    MapResourceList.Add(MapData);
                else
                    LogManager.GetSingletone.WriteLog($"맵 정보를 로드하는데 실패했습니다. 파일명 : {JsonFile}");
            }
            // 이제 mapData에는 MapID, MapName, 그리고 모든 장애물의 위치, 크기, 메시의 크기, 그리고 메시의 이름 정보가 들어있습니다.

            LogManager.GetSingletone.WriteLog("포탈 정보를 로드합니다.");
            List<MapPortalData> MapPortalResourceList = new List<MapPortalData>();
            foreach (string JsonFile in Directory.GetFiles(Path.Combine(GameServerSettings.Default.ResourceDicrectory, "MapLinkFiles"), "*.json"))
            {
                MapPortalData? PortalData;
                string Json = File.ReadAllText(JsonFile);
                try
                {
                    PortalData = JsonSerializer.Deserialize<MapPortalData>(Json);
                }
                catch (Exception e)
                {
                    LogManager.GetSingletone.WriteLog($"포탈 정보를 로드하는데 실패했습니다. 파일명 : {JsonFile}");
                    LogManager.GetSingletone.WriteLog(e.Message);
                    continue;
                }
                if (PortalData != null)
                    MapPortalResourceList.Add(PortalData);
                else
                    LogManager.GetSingletone.WriteLog($"포탈 정보를 로드하는데 실패했습니다. 파일명 : {JsonFile}");
            }

            // 이제 MapPortalResourceList에는 MapID, MapName, LinkToMapID, Location, Bound(BoxSize) 정보가 들어있습니다.
            for (int i = 0; i < MapResourceList.Count; ++i)
            {
                MapResourceList[i].Obstacles = MapResourceList[i].Obstacles.AsParallel()
                               .OrderByDescending(o => o.Scale.X * o.MeshSize.X + o.Scale.Y * o.MeshSize.Y + o.Scale.Z * o.MeshSize.Z)
                               .ToList();
            }

            // 이제 변환하자
            LogManager.GetSingletone.WriteLog("맵 정보를 변환합니다.");

            foreach (MapDataForResourceLoader Data in MapResourceList)
            {
                MapDataDictionary.Add(Data.MapID, new MapData(Data.MapID, Data.MapName, Data.Obstacles, MapPortalResourceList.Where(p => p.MapID == Data.MapID).ToList()
                    , Data.Obstacles[0].MeshSize.X * Data.Obstacles[0].Scale.X
                    , Data.Obstacles[0].MeshSize.Y * Data.Obstacles[0].Scale.Y
                    , Data.Obstacles[0].MeshSize.Z * Data.Obstacles[0].Scale.Z));
            }
        }
    }
}
