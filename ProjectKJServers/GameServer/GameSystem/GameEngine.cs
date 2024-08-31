using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using GameServer.Component;
using GameServer.MainUI;
using GameServer.Object;
using GameServer.PacketList;
using GameServer.Resource;
using System.Collections.Concurrent;

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
        UniformVelocityMovementSystem UniformVelocityMovementSystem;
        ResourceLoader ResourceLoader;

        public GameEngine()
        {
            MapSystem = new MapSystem();
            UniformVelocityMovementSystem = new UniformVelocityMovementSystem();
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
            ResourceLoader.LoadCharacterPreset(ref ChracterPresetDictionary);
        }

        private void Run()
        {
            while (!GameEngineCancleToken.IsCancellationRequested)
            {
                try
                {
                    MapSystem.Update();
                    UniformVelocityMovementSystem.Update();
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

        public void AddUniformVelocityMovementComponentToSystem(UniformVelocityMovementComponent Component)
        {
            UniformVelocityMovementSystem.AddComponent(Component);
        }

        public bool CanMove(int MapID, Vector3 Position)
        {
            return MapSystem.CanMove(MapID, Position);
        }

        public void AddNickName(string AccountID, string NickName)
        {
            NickNameMap.TryAdd(AccountID, NickName);
        }

        public void RemoveNickName(string AccountID, out string? NickName)
        {
            if (NickNameMap.TryRemove(AccountID, out NickName))
            {
                LogManager.GetSingletone.WriteLog($"계정 {AccountID}의 닉네임 {NickName}을 제거했습니다.");
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"계정 {AccountID}의 닉네임을 찾을 수 없습니다.");
            }
        }

        public void CreateCharacter(ResponseDBCharBaseInfoPacket Info)
        {

            PlayerCharacter NewCharacter = new PlayerCharacter();
            NewCharacter.AccountInfo.AccountID = Info.AccountID;
            NewCharacter.AccountInfo.NickName = Info.NickName;
            NewCharacter.CurrentPosition.MapID = Info.MapID;
            NewCharacter.CurrentPosition.Position = new System.Numerics.Vector3(Info.X, Info.Y, 0);
            NewCharacter.JobInfo.Job = Info.Job;
            NewCharacter.JobInfo.Level = Info.JobLevel;
            NewCharacter.LevelInfo.Level = Info.Level;
            NewCharacter.LevelInfo.CurrentExp = Info.EXP;
            NewCharacter.AppearanceInfo.PresetNumber = Info.PresetNumber;
            NewCharacter.AppearanceInfo.Gender = Info.Gender;
            if (OnlineCharacterDictionary.TryAdd(Info.AccountID, NewCharacter))
                LogManager.GetSingletone.WriteLog($"계정 {Info.AccountID} {Info.NickName}의 캐릭터를 생성했습니다.");
            MainProxy.GetSingletone.AddNickName(Info.AccountID, Info.NickName);
            NewCharacter.SetMovement(300, new System.Numerics.Vector3(Info.X, Info.Y, 0));

        }

        public void RemoveCharacter(string AccountID)
        {
            if (OnlineCharacterDictionary.TryRemove(AccountID, out _))
            {
                LogManager.GetSingletone.WriteLog($"계정 {AccountID}의 캐릭터를 제거했습니다.");
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"계정 {AccountID}의 캐릭터를 찾을 수 없습니다.");
            }
        }
    }
}
