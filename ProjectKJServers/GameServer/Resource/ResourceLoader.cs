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
        public ResourceLoader()
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
                {
                    MapResourceList.Add(MapData);
                }
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

            // 이제 변환하자
            LogManager.GetSingletone.WriteLog("맵 정보를 변환합니다.");

            foreach (MapDataForResourceLoader Data in MapResourceList)
            {
                var FloorObstacle = Data.Obstacles.FirstOrDefault(o => o.MeshName == "SM_Floor");
                if (FloorObstacle == null)
                {
                    LogManager.GetSingletone.WriteLog($"맵 ID {Data.MapID}에 바닥이 없습니다.");
                    continue;
                }
                List<ConvertObstacles> ConvertObstacles = new List<ConvertObstacles>();
                foreach (var Obs in Data.Obstacles)
                {
                    switch (Obs.MeshName)
                    {
                        case "SM_Floor":
                            break;
                        case "SM_Cylinder":
                            ConvertObstacles.Add(ConvertMathUtility.CalculateCylinderVertex(Obs, Obs.MeshName));
                            break;
                        case "SM_Sphere":
                            ConvertObstacles.Add(ConvertMathUtility.CalculateSphereVertex(Obs, Obs.MeshName));
                            break;
                        default:
                            ConvertObstacles.Add(ConvertMathUtility.CalculateSquareVertex(Obs, Obs.MeshName));
                            break;
                    }
                }
                MapDataDictionary.Add(Data.MapID, new MapData(Data.MapID, Data.MapName, ConvertObstacles, MapPortalResourceList.Where(p => p.MapID == Data.MapID).ToList()
                    , FloorObstacle.MeshSize.X * FloorObstacle.Scale.X
                    , FloorObstacle.MeshSize.Y * FloorObstacle.Scale.Y
                    , FloorObstacle.MeshSize.Z * FloorObstacle.Scale.Z));
            }
        }

        public void LoadCharacterPreset(ref Dictionary<int, CharacterPresetData> CharPrestDataDictionary)
        {
            LogManager.GetSingletone.WriteLog("캐릭터 외형 정보를 로드합니다.");
            foreach (string JsonFile in Directory.GetFiles(Path.Combine(GameServerSettings.Default.ResourceDicrectory, "CharacterPreset"), "*.json"))
            {
                string Json = File.ReadAllText(JsonFile);
                CharacterPresetData? Data;
                try
                {
                    Data = JsonSerializer.Deserialize<CharacterPresetData>(Json);
                }
                catch (Exception e)
                {
                    LogManager.GetSingletone.WriteLog($"캐릭터 외형 정보를 로드하는데 실패했습니다. 파일명 : {JsonFile}");
                    LogManager.GetSingletone.WriteLog(e.Message);
                    continue;
                }

                if (Data != null)
                    CharPrestDataDictionary.Add(Data.PresetID, Data);
                else
                    LogManager.GetSingletone.WriteLog($"캐릭터 외형 정보를 로드하는데 실패했습니다. 파일명 : {JsonFile}");
            }
        }

        public void MakeMapGraph(ref Dictionary<int, Graph> MapGraphDictionary, ref readonly Dictionary<int, MapData> MapDataDictionary)
        {
            LogManager.GetSingletone.WriteLog("맵 그래프를 생성합니다.");
            MapGraph GraphMaker = new MapGraph();
            foreach (var Data in MapDataDictionary)
            {
                Graph MapGraph = GraphMaker.MakeGraph(Data.Value, 100); // 100으로 하니까 너무 많다
                MapGraphDictionary.Add(Data.Key, MapGraph);
            }
        }
    }
}
