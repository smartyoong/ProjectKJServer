using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Diagnostics;
using System.Runtime.Versioning;
using CoreUtility.GlobalVariable;

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
        private PerformanceCounter HandleCounter;
        private PerformanceCounter FileIOCounter;
        private PerformanceCounter CpuTemperatureCounter;
        private bool IsAlreadyDisposed = false;
        private long LastTickCount = 0;

        public ProcessMonitor()
        {
            CpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            MemoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            ThreadCounter = new PerformanceCounter("Process", "Thread Count", Process.GetCurrentProcess().ProcessName);
            DiskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            NetCounter = new PerformanceCounter("Network Interface", "Bytes Total/sec", "YourNetworkInterfaceName");
            PageFileCounter = new PerformanceCounter("Paging File", "% Usage", "_Total");
            HandleCounter = new PerformanceCounter("Process", "Handle Count", Process.GetCurrentProcess().ProcessName);
            FileIOCounter = new PerformanceCounter("System", "File Read Bytes/sec");
            CpuTemperatureCounter = new PerformanceCounter("Thermal Zone Information", "Temperature", "ACPI\\ThermalZone\\THM0");
        }

        public void Update()
        {
            long CurrentTickCount = Environment.TickCount;
            if (CurrentTickCount - LastTickCount < 2 * TimeSpan.MillisecondsPerMinute)
            {
                return;
            }
            LastTickCount = CurrentTickCount;
            float CpuUsage = GetCpuUsage();
            float MemoryUsage = GetMemoryUsage();
            float ThreadCount = GetThreadCount();
            float DiskIO = GetDiskIO();
            float NetworkUsage = GetNetworkUsage();
            float PageFileUsage = GetPageFileUsage();
            float HandleCount = GetHandleCount();
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
            UIEvent.GetSingletone.UpdateDiskIO(HandleCount);
            UIEvent.GetSingletone.UpdateFileIO(FileIO);
            UIEvent.GetSingletone.UpdateGarbageCollection(GarbageCollectionCount);
            UIEvent.GetSingletone.UpdateCPUTemperature(CpuTemperature);
            foreach (string EventLog in SystemEvents)
            {
                UIEvent.GetSingletone.UpdateSystemLog(EventLog);
            }

            LogManager.GetSingletone.WriteLog($"CPU Usage: {CpuUsage}");
            LogManager.GetSingletone.WriteLog($"Memory Usage: {MemoryUsage}");
            LogManager.GetSingletone.WriteLog($"Thread Count: {ThreadCount}");
            LogManager.GetSingletone.WriteLog($"Disk IO: {DiskIO}");
            LogManager.GetSingletone.WriteLog($"Network Usage: {NetworkUsage}");
            LogManager.GetSingletone.WriteLog($"Page File Usage: {PageFileUsage}");
            LogManager.GetSingletone.WriteLog($"Handle Count: {HandleCount}");
            LogManager.GetSingletone.WriteLog($"File IO: {FileIO}");
            LogManager.GetSingletone.WriteLog($"Garbage Collection Count: {GarbageCollectionCount}");
            LogManager.GetSingletone.WriteLog($"CPU Temperature: {CpuTemperature}");
            foreach (string EventLog in SystemEvents)
            {
                LogManager.GetSingletone.WriteLog($"System Event: {EventLog}");
            }
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

        private float GetHandleCount()
        {
            return HandleCounter.NextValue();
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
            float Temperature = CpuTemperatureCounter.NextValue();
            // 온도는 Kelvin 단위로 반환되므로, 섭씨로 변환합니다.
            Temperature = (Temperature - 2732) / 10.0f;
            return Temperature;
        }

        private List<string> GetSystemEvents()
        {
            List<string> EventLogs = new List<string>();
            using (EventLog eventLog = new EventLog("System"))
            {
                foreach (EventLogEntry entry in eventLog.Entries)
                {
                    EventLogs.Add($"Entry Type: {entry.EntryType}, Message: {entry.Message}");
                }
            }
            return EventLogs;
        }

        protected virtual void Dispose(bool disposing)
        {
            if(IsAlreadyDisposed)
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
                HandleCounter.Dispose();
                FileIOCounter.Dispose();
                CpuTemperatureCounter.Dispose();
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
