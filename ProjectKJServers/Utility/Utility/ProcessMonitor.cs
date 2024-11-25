using CoreUtility.GlobalVariable;
using System.Diagnostics;
using System.Management;
using System.Runtime.Versioning;

namespace CoreUtility.Utility
{
    [SupportedOSPlatform("windows")]
    public class ProcessMonitor : IDisposable
    {
        private PerformanceCounter CpuCounter;
        private PerformanceCounter MemoryCounter;
        private PerformanceCounter ThreadCounter;
        private PerformanceCounter DiskCounter;
        private PerformanceCounter NetCounter;
        private PerformanceCounter PageFileCounter;
        private PerformanceCounter FileIOCounter;
        private bool IsAlreadyDisposed = false;
        private long LastTickCount = 0;

        public ProcessMonitor()
        {
            //여기 손보자
            CpuCounter = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
            MemoryCounter = new PerformanceCounter("Process", "Working Set - Private", Process.GetCurrentProcess().ProcessName);
            ThreadCounter = new PerformanceCounter("Process", "Thread Count", Process.GetCurrentProcess().ProcessName);
            DiskCounter = new PerformanceCounter("Process", "IO Read Bytes/sec", Process.GetCurrentProcess().ProcessName);
            NetCounter = new PerformanceCounter("Network Interface", "Bytes Total/sec", "Realtek Gaming 2.5GbE Family Controller");
            PageFileCounter = new PerformanceCounter("Process", "Page File Bytes", Process.GetCurrentProcess().ProcessName);
            FileIOCounter = new PerformanceCounter("Process", "IO Write Bytes/sec", Process.GetCurrentProcess().ProcessName);
        }

        public void Update()
        {
            long CurrentTickCount = Environment.TickCount;
            if (CurrentTickCount - LastTickCount < 2 * TimeSpan.MillisecondsPerMinute)
            {
                return;
            }
            UpdateAllInfo();
            LastTickCount = CurrentTickCount;
        }

        private void UpdateAllInfo()
        {
            float CpuUsage = GetCpuUsage();
            float MemoryUsage = GetMemoryUsage();
            float ThreadCount = GetThreadCount();
            float DiskIO = GetDiskIO();
            float NetworkUsage = GetNetworkUsage();
            float PageFileUsage = GetPageFileUsage();
            float FileIO = GetFileIO();
            long GarbageCollectionCount = GetGarbageCollectionCount();
            float CpuTemperature = GetCpuTemperature();
            List<string> SystemEvents = GetSystemEvents();
            UIEvent.GetSingletone.UpdateCPUUsage(CpuUsage);
            UIEvent.GetSingletone.UpdateMemoryUsage(MemoryUsage);
            UIEvent.GetSingletone.UpdateThreadUsage(ThreadCount);
            UIEvent.GetSingletone.UpdateDiskIO(DiskIO);
            UIEvent.GetSingletone.UpdateNetworkUsage(NetworkUsage);
            UIEvent.GetSingletone.UpdatePageUsage(PageFileUsage);
            UIEvent.GetSingletone.UpdateFileIO(FileIO);
            UIEvent.GetSingletone.UpdateGarbageCollection(GarbageCollectionCount);
            UIEvent.GetSingletone.UpdateCPUTemperature(CpuTemperature);
            foreach (string EventLog in SystemEvents)
            {
                UIEvent.GetSingletone.UpdateSystemLog(EventLog);
            }
            LogManager.GetSingletone.WriteLog("\n\n///////////////////////////////////////");
            LogManager.GetSingletone.WriteLog("Process Monitor Update");
            LogManager.GetSingletone.WriteLog("///////////////////////////////////////\n\n");
            LogManager.GetSingletone.WriteLog($"CPU Usage: {CpuUsage}");
            LogManager.GetSingletone.WriteLog($"Memory Usage: {MemoryUsage}");
            LogManager.GetSingletone.WriteLog($"Thread Count: {ThreadCount}");
            LogManager.GetSingletone.WriteLog($"Disk IO: {DiskIO}");
            LogManager.GetSingletone.WriteLog($"Network Usage: {NetworkUsage}");
            LogManager.GetSingletone.WriteLog($"Page File Usage: {PageFileUsage}");
            LogManager.GetSingletone.WriteLog($"File IO: {FileIO}");
            LogManager.GetSingletone.WriteLog($"Garbage Collection Count: {GarbageCollectionCount}");
            LogManager.GetSingletone.WriteLog($"CPU Temperature: {CpuTemperature}");
            foreach (string EventLog in SystemEvents)
            {
                LogManager.GetSingletone.WriteLog($"System Event: {EventLog}");
            }
            LogManager.GetSingletone.WriteLog("///////////////////////////////////////\n\n");

        }
        private float GetCpuUsage()
        {
            return CpuCounter.NextValue();
        }

        private float GetMemoryUsage()
        {
            return MemoryCounter.NextValue();
        }

        private float GetThreadCount()
        {
            return ThreadCounter.NextValue();
        }

        private float GetDiskIO()
        {
            return DiskCounter.NextValue();
        }

        private float GetNetworkUsage()
        {
            return NetCounter.NextValue();
        }

        private float GetPageFileUsage()
        {
            return PageFileCounter.NextValue();
        }

        private float GetFileIO()
        {

            return FileIOCounter.NextValue();
        }

        private long GetGarbageCollectionCount()
        {
            return GC.CollectionCount(2);
        }

        private float GetCpuTemperature()
        {
            // 현재 프로세스 관리자 권한으로 실행시키도록 해야함
            float Temperature = 0.0f;
            ManagementObjectSearcher Searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature");

            foreach (ManagementObject obj in Searcher.Get())
            {
                // 온도는 Kelvin 단위로 반환되므로, 섭씨로 변환합니다.
                Temperature = Convert.ToSingle(obj["CurrentTemperature"].ToString());
                Temperature = (Temperature - 2732) / 10.0f;
            }

            return Temperature;
        }

        private List<string> GetSystemEvents()
        {
            List<string> EventLogs = new List<string>();
            using (EventLog EventLog = new EventLog("System"))
            {
                foreach (EventLogEntry entry in EventLog.Entries)
                {
                    // 현재 프로세스와 관련된 치명적인 에러 로그만 필터링
                    if (entry.Source == Process.GetCurrentProcess().ProcessName && entry.EntryType == EventLogEntryType.Error)
                    {
                        EventLogs.Add($"Entry Type: {entry.EntryType}, Message: {entry.Message}");
                    }
                }
            }
            return EventLogs;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsAlreadyDisposed)
            {
                return;
            }

            if (disposing)
            {
                CpuCounter.Dispose();
                MemoryCounter.Dispose();
                ThreadCounter.Dispose();
                DiskCounter.Dispose();
                NetCounter.Dispose();
                PageFileCounter.Dispose();
                FileIOCounter.Dispose();
            }
            IsAlreadyDisposed = true;
        }

        public void Dispose()
        {
            if (IsAlreadyDisposed)
            {
                return;
            }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ProcessMonitor()
        {
            Dispose(false);
        }

    }
}
