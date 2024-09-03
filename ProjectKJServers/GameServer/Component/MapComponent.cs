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
        private bool NeedUpdate = false;
        MapComponent(int MapID)
        {
            this.MapID = MapID;
        }

        public void Update()
        {
            // 맵 관련 업데이트가 필요할 경우 여기서 진행함
            if (NeedUpdate)
            {
                NeedUpdate = false;
            }
        }

        public void MoveToAnotherMap(int MapID)
        {
            // 맵 이동 관련 처리가 필요할 경우 여기서 진행함
            lock (_lock)
            {
                this.MapID = MapID;
                NeedUpdate = true;
            }
        }
    }
}
