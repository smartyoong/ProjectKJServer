﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using GameServer.Resource;

namespace GameServer.GameSystem
{
    internal class GameEngine : IDisposable
    {
        const int MINIMUM_UPDATE_INTERVAL = 1000 / 60;
        private int LastTickCount = 0;
        private static Lazy<GameEngine> instance = new Lazy<GameEngine>(() => new GameEngine());
        public static GameEngine GetSingletone { get { return instance.Value; } }

        private bool IsAlreadyDisposed = false;
        private CancellationTokenSource GameEngineCancleToken = new CancellationTokenSource();
        private Dictionary<int, MapData> MapDataDictionary = new Dictionary<int, MapData>();
        private Dictionary<int, CharacterPresetData> ChracterPresetDictionary = new Dictionary<int, CharacterPresetData>();

        private GameEngine()
        {
        }
        public void Start()
        {
            LoadResource();
            Task.Run(() => Run());
        }

        public void LoadResource()
        {
            LogManager.GetSingletone.WriteLog("리소스를 로드합니다.");
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
            ResourceLoader.GetSingletone.LoadCharacterPreset(ref ChracterPresetDictionary);
            foreach (var Data in ChracterPresetDictionary)
            {
                LogManager.GetSingletone.WriteLog($"캐릭터 ID : {Data.Key}, 캐릭터 이름 : {Data.Value.PresetName}");
                LogManager.GetSingletone.WriteLog($"캐릭터 성별 : {Data.Value.Gender}");
                LogManager.GetSingletone.WriteLog($"BP : {Data.Value.BlueprintName}");
                LogManager.GetSingletone.WriteLog($"캐릭터 메시 : {Data.Value.PlayerCharacterName}");
            }
        }

        public void Run()
        {
            while (!GameEngineCancleToken.IsCancellationRequested)
            {
                MapSystem.GetSingletone.Update();
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

    }
}
