using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KYCCoreDataStruct;
using System.Text.Json;

namespace GameServer
{
    internal class ResourceLoader
    {
        public void LoadMapData()
        {
            string json = File.ReadAllText("MapData.json");
           
            JsonSerializer.Deserialize<MapData>(json);

            // 이제 mapData에는 MapID, MapName, 그리고 모든 장애물의 위치, 크기, 메시의 크기, 그리고 메시의 이름 정보가 들어있습니다.
        }
    }
}
