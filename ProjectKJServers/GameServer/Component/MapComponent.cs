using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Component
{
    internal class MapComponent
    {
        private object _lock = new object();
        private int MapID;
        private int MapBufferID;
        private int MapDebuffID;
        private string AccountID;

        public MapComponent(int MapID, string UserID)
        {
            this.MapID = MapID;
            MapBufferID = 0;
            MapDebuffID = 0;
            AccountID = UserID;
        }

        public void Update()
        {
            // 맵 관련 업데이트가 필요할 경우 여기서 진행함
            MapBufferID = 0;
            MapDebuffID = 0;
            if (MapBufferID == 0)
            {
                if (MapDebuffID == 0)
                {
                    return;  // 경고 출력 안되게 일단 막음 안쓰일것 같아서
                }
            }
        }

        public void MoveToAnotherMap(int MapID)
        {
            // 맵 이동 관련 처리가 필요할 경우 여기서 진행함
            lock (_lock)
            {
                this.MapID = MapID;
            }
        }

        public int GetCurrentMapID()
        {
            return MapID;
        }

        public string GetAccountID()
        {
            return AccountID;
        }
    }
}
