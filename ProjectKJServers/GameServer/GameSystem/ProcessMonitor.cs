using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.GameSystem
{
    internal class ProcessMonitor
    {
        public static float GetCpuUsage()
        {
            using (PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
            {
                cpuCounter.NextValue();
                System.Threading.Thread.Sleep(1000);
                return cpuCounter.NextValue();
            }
        }

        public static float GetMemoryUsage()
        {
            using (PerformanceCounter memCounter = new PerformanceCounter("Memory", "Available MBytes"))
            {
                return memCounter.NextValue();
            }
        }

        public static int GetThreadCount()
        {
            using (PerformanceCounter threadCounter = new PerformanceCounter("Process", "Thread Count", Process.GetCurrentProcess().ProcessName))
            {
                return (int)threadCounter.NextValue();
            }
        }

        public static float GetDiskIO()
        {
            using (PerformanceCounter diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total"))
            {
                diskCounter.NextValue();
                System.Threading.Thread.Sleep(1000);
                return diskCounter.NextValue();
            }
        }

        public static float GetNetworkUsage()
        {
            using (PerformanceCounter netCounter = new PerformanceCounter("Network Interface", "Bytes Total/sec", "YourNetworkInterfaceName"))
            {
                netCounter.NextValue();
                System.Threading.Thread.Sleep(1000);
                return netCounter.NextValue();
            }
        }

        public static float GetPageFileUsage()
        {
            using (PerformanceCounter pageFileCounter = new PerformanceCounter("Paging File", "% Usage", "_Total"))
            {
                return pageFileCounter.NextValue();
            }
        }

        public static int GetHandleCount()
        {
            using (PerformanceCounter handleCounter = new PerformanceCounter("Process", "Handle Count", Process.GetCurrentProcess().ProcessName))
            {
                return (int)handleCounter.NextValue();
            }
        }

        public static float GetFileIO()
        {
            using (PerformanceCounter fileIOCounter = new PerformanceCounter("System", "File Read Bytes/sec"))
            {
                fileIOCounter.NextValue();
                System.Threading.Thread.Sleep(1000);
                return fileIOCounter.NextValue();
            }
        }

        public static long GetGarbageCollectionCount()
        {
            return GC.CollectionCount(0);
        }

        public static float GetCpuTemperature()
        {
            float temperature = 0;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature");

            foreach (ManagementObject obj in searcher.Get())
            {
                temperature = Convert.ToSingle(obj["CurrentTemperature"].ToString());
                temperature = (temperature - 2732) / 10.0f;
            }

            return temperature;
        }

        public static void GetSystemEvents()
        {
            EventLog eventLog = new EventLog("System");
            foreach (EventLogEntry entry in eventLog.Entries)
            {
                Console.WriteLine($"Entry Type: {entry.EntryType}, Message: {entry.Message}");
            }
        }

    }
}
