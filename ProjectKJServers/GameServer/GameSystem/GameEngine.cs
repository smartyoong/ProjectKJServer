using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using GameServer.Component;
using GameServer.MainUI;
using GameServer.Object;
using GameServer.PacketList;
using GameServer.Resource;
using System.Collections.Concurrent;
using Windows.ApplicationModel.VoiceCommands;

namespace GameServer.GameSystem
{
    internal class GameEngine : IDisposable
    {
        public static readonly int UPDATE_INTERVAL_20PERSEC = 50;

        private bool IsAlreadyDisposed = false;
        private CancellationTokenSource GameEngineCancleToken = new CancellationTokenSource();
        private Dictionary<int, MapData> MapDataDictionary = new Dictionary<int, MapData>();
        private Dictionary<int, CharacterPresetData> ChracterPresetDictionary = new Dictionary<int, CharacterPresetData>();
        private ConcurrentDictionary<string, PlayerCharacter> OnlineCharacterDictionary = new ConcurrentDictionary<string, PlayerCharacter>();
        private ConcurrentDictionary<string, string> NickNameMap = new ConcurrentDictionary<string, string>();

        MapSystem MapSystem;
        KinematicMoveSystem KinematicMovementSystem;
        ResourceLoader ResourceLoader;

        public GameEngine()
        {
            MapSystem = new MapSystem();
            KinematicMovementSystem = new KinematicMoveSystem();
            ResourceLoader = new ResourceLoader();
        }

        public void Start()
        {
            LoadResource();
            Task.Run(() => Run());
        }

        private void LoadResource()
        {
            LogManager.GetSingletone.WriteLog("리소스를 로드합니다.");
            ResourceLoader.LoadMapData(ref MapDataDictionary);
            MapSystem.SetMapData(ref MapDataDictionary);
            ResourceLoader.LoadCharacterPreset(ref ChracterPresetDictionary);
        }

        private void Run()
        {
            while (!GameEngineCancleToken.IsCancellationRequested)
            {
                try
                {
                    MapSystem.Update();
                    KinematicMovementSystem.Update();
                }
                catch (Exception e)
                {
                    LogManager.GetSingletone.WriteLog(e.Message);
                }
            }
        }

        public void Stop()
        {
            LogManager.GetSingletone.WriteLog("게임 엔진을 중단합니다.");
            GameEngineCancleToken.Cancel();
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool Disposing)
        {
            if (IsAlreadyDisposed)
                return;
            if (Disposing)
            {
                GameEngineCancleToken.Dispose();
            }
            IsAlreadyDisposed = true;
        }

        public void AddKinematicComponentToSystem(KinematicComponent Component)
        {
            KinematicMovementSystem.AddComponent(Component);
        }

        public void RemoveKinematicComponentFromSystem(KinematicComponent Component, int Count)
        {
            KinematicMovementSystem.RemoveComponent(Component, Count);
        }

        public void AddUserToMap(MapComponent Component)
        {
            MapSystem.AddUser(Component);
        }

        public void RemoveUserFromMap(MapComponent Component)
        {
            MapSystem.RemoveUser(Component);
        }

        public bool CanMove(int MapID, Vector3 Position)
        {
            return MapSystem.CanMove(MapID, Position);
        }

        public void AddNickName(string AccountID, string NickName)
        {
            NickNameMap.TryAdd(AccountID, NickName);
        }

        public string GetNickName(string AccountID)
        {
            if (NickNameMap.TryGetValue(AccountID, out string? NickName))
            {
                return NickName;
            }
            else
            {
                return string.Empty;
            }
        }

        public void RemoveNickName(string AccountID)
        {
            string? NickName;
            if (NickNameMap.TryRemove(AccountID, out NickName))
            {
                LogManager.GetSingletone.WriteLog($"계정 {AccountID}의 닉네임 {NickName}을 제거했습니다.");
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"계정 {AccountID}의 닉네임을 찾을 수 없습니다.");
            }
        }

        public void SendToSameMap<T>(int MapID, GamePacketListID PacketID, T Packet) where T : struct
        {
            MapSystem.SendPacketToSameMapUsers(MapID, PacketID, Packet);
        }

        public List<MapComponent>? GetMapUsers(int MapID)
        {
            return MapSystem.GetMapUsers(MapID);
        }

        public void CreateCharacter(ResponseDBCharBaseInfoPacket Info)
        {

            PlayerCharacter NewCharacter = new PlayerCharacter(Info.AccountID, Info.NickName, Info.MapID, Info.Job, 
                Info.JobLevel, Info.Level, Info.EXP, Info.PresetNumber, Info.Gender, new System.Numerics.Vector3(Info.X, Info.Y, 0));

            if (OnlineCharacterDictionary.TryAdd(Info.AccountID, NewCharacter))
                LogManager.GetSingletone.WriteLog($"계정 {Info.AccountID} {Info.NickName}의 캐릭터를 생성했습니다.");

            MainProxy.GetSingletone.AddNickName(Info.AccountID, Info.NickName);

        }

        public PlayerCharacter? GetCharacter(string AccountID)
        {
            if(OnlineCharacterDictionary.TryGetValue(AccountID, out PlayerCharacter? Character))
                return Character;
            return null;
        }

        public PlayerCharacter? GetCharacterByAccountID(string AccountID)
        {
            if(OnlineCharacterDictionary.TryGetValue(AccountID, out PlayerCharacter? Character))
            {
                return Character;
            }
            else
            {
                return null;
            }
        }

        public void RemoveCharacter(string AccountID)
        {
            PlayerCharacter? TempChar;
            RemoveNickName(AccountID);
            if (OnlineCharacterDictionary.TryRemove(AccountID, out TempChar))
            {
                if(TempChar != null)
                {
                    TempChar.RemoveCharacter();
                }
                LogManager.GetSingletone.WriteLog($"계정 {AccountID}의 캐릭터를 제거했습니다.");
            }
            else
            {
                //캐릭터 생성안하고 종료하면 이렇게될 수도
                LogManager.GetSingletone.WriteLog($"계정 {AccountID}의 캐릭터를 찾을 수 없습니다.");
            }
        }
    }
}
