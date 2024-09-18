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
using GameServer.Component;
using GameServer.PacketList;
using GameServer.MainUI;
using System.Net.Sockets;

namespace GameServer.GameSystem
{
    using Vector3 = System.Numerics.Vector3;
    using CustomVector3 = CoreUtility.GlobalVariable.Vector3;

    internal class MapSystem : IComponentSystem
    {
        private readonly object _lock = new object();
        private Dictionary<int, MapData> MapDataDictionary = new Dictionary<int, MapData>();
        //ConCurrentBag는 편의성 좋은 메서드가 하나도 없네, AddRemove때만 Lock을 잘 걸자
        private List<List<MapComponent>>? MapUserList;

        public MapSystem()
        {
        }

        public void SetMapData(ref Dictionary<int, MapData> MapDataDictionary)
        {
            this.MapDataDictionary = MapDataDictionary;
            MapUserList = new List<List<MapComponent>>(MapDataDictionary.Keys.Max()+1);
        }

        public void Update()
        {
            // 만약 장애물 관련 업데이트가 필요할 경우 여기서 진행함
        }

        private bool ValidComponentCheck(MapComponent Component)
        {
            if (MapUserList == null)
            {
                LogManager.GetSingletone.WriteLog("맵 유저 리스트가 초기화 되지 않았습니다.");
                return false;
            }
            int MapID = Component.GetCurrentMapID();
            if (MapUserList.Count <= MapID)
            {
                LogManager.GetSingletone.WriteLog($"맵 ID {MapID}에 해당하는 맵 정보가 없습니다.");
                return false;
            }
            return true;
        }

        public void AddUser(MapComponent Component)
        {
            if (!ValidComponentCheck(Component))
            {
                return;
            }
            int MapID = Component.GetCurrentMapID();
            lock (_lock)
            {
                MapUserList![MapID].Add(Component);
            }
        }

        public void RemoveUser(MapComponent Component)
        {
            if (!ValidComponentCheck(Component))
            {
                return;
            }
            int MapID = Component.GetCurrentMapID();

            if (MapUserList![MapID] == null)
            {
                LogManager.GetSingletone.WriteLog($"맵 ID {MapID}에는 유저가 존재하지 않습니다.");
                return;
            }

            lock (_lock)
            {
                MapUserList![MapID].Remove(Component);
            }
        }

        private bool ValidMapIDCheck(int MapID)
        {
            if (MapDataDictionary == null)
            {
                LogManager.GetSingletone.WriteLog("맵 데이터가 없습니다.");
                return false;
            }

            if (!MapDataDictionary.ContainsKey(MapID))
            {
                LogManager.GetSingletone.WriteLog($"맵 ID {MapID}에 해당하는 맵 정보가 없습니다.");
                return false;
            }
            return true;
        }

        public void SendPacketToSameMapUsers<T>(int MapID, GamePacketListID PacketID, T Packet) where T : struct
        {
            if(!ValidMapIDCheck(MapID))
            {
                return;
            }

            if (MapUserList == null)
            {
                LogManager.GetSingletone.WriteLog("맵 유저 리스트가 초기화 되지 않았습니다.");
                return;
            }
            if (MapUserList[MapID] == null)
            {
                LogManager.GetSingletone.WriteLog($"맵 ID {MapID}에는 유저가 존재하지 않습니다.");
                return;
            }

            //여기서 Lock을 걸면 오래걸릴것 같은데,, 근데 그렇다고 락을안 걸 수도 없고 음,,, 일단 걸어보자
            lock (_lock)
            {
                foreach (MapComponent User in MapUserList[MapID])
                {
                    Socket? Sock = MainProxy.GetSingletone.GetClientSocketByAccountID(User.GetAccountID());
                    if (Sock != null)
                        MainProxy.GetSingletone.SendToClient(PacketID,Packet,MainProxy.GetSingletone.GetClientID(Sock));
                }
            }
        }

        private bool BoundaryCheck(int MapID, ref readonly CustomVector3 Position)
        {
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
            return true;
        }

        private bool PositionAABBCheck(in int MapID, ref readonly MapData Data, ref readonly CustomVector3 Position)
        {
            // Z축은 사용하지 않는다고 가정
            foreach (ConvertObstacles ObstacleData in Data.Obstacles)
            {
                //바닥은 건너뛰자
                if (ObstacleData.MeshName == "SM_Floor")
                {
                    continue;
                }
                //사각형, 실린더, 구까지는 각 X,Y 꼭짓점을 구하도록 작업했음 그래서 영역체크는 이걸로 가능 (충돌 일때는 Radius 사용할 예정)
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

        public bool CanMove(int MapID, CustomVector3 Position)
        {
            if(!ValidMapIDCheck(MapID))
            {
                return false;
            }
            if (!BoundaryCheck(MapID, ref Position))
            {
                return false;
            }
            MapData Data = MapDataDictionary[MapID];
            if (!PositionAABBCheck(MapID, ref Data, ref Position))
            {
                return false;
            }
            return true;
        }
    }
}
