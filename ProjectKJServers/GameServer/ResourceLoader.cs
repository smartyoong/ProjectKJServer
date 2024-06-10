using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KYCCoreDataStruct;
using System.Text.Json;
using KYCLog;

namespace GameServer
{
    internal class ResourceLoader
    {
        public void LoadMapData(ref List<MapData> MapDataList)
        {
            LogManager.GetSingletone.WriteLog("맵 정보를 로드합니다.");
            foreach(string JsonFile in Directory.GetFiles(Path.Combine(GameServerSettings.Default.ResourceDicrectory,"MapFiles"),"*.json"))
            {
                string Json = File.ReadAllText(JsonFile);
                MapData MapData = JsonSerializer.Deserialize<MapData>(Json);
                MapDataList.Add(MapData);
            }
            // 이제 mapData에는 MapID, MapName, 그리고 모든 장애물의 위치, 크기, 메시의 크기, 그리고 메시의 이름 정보가 들어있습니다.
        }
    }
}
