using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer
{
    internal class LogManager : IDisposable
    {
        bool IsAlreadyDisposed = false;
        readonly string LogDirectory = Properties.Settings.Default.LogDirectory;
        StreamWriter LogFile;
        public LogManager()
        {
            if(string.IsNullOrEmpty(LogDirectory))
            {
                LogDirectory = Environment.CurrentDirectory;
            }
            string LogFileName = LogDirectory + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            LogFile = new StreamWriter(LogFileName, true);
        }
        public LogManager(string LogDirectory)
        {
            this.LogDirectory = LogDirectory;
            Properties.Settings.Default.LogDirectory = LogDirectory;
            Properties.Settings.Default.Save();
            string LogFileName = LogDirectory + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            LogFile = new StreamWriter(LogFileName, true);
        }
        ~LogManager()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool Disposing)
        {
            if (IsAlreadyDisposed)
                return;
            if (Disposing)
            {

            }
            LogFile.Dispose();
            IsAlreadyDisposed = true;
        }

        public void WriteLog(string Log)
        {
            if(Directory.Exists(LogDirectory) == false)
            {
                Directory.CreateDirectory(LogDirectory);
            }
            LogFile.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + Log);
        }
        public void WriteErrorLog(Exception ex)
        {
            WriteLog("[EXCEPTION] : " + ex.Message);
            WriteLog("[EXCEPTION] Source :" + ex.Source);
            if(ex.StackTrace != null)
            {
                foreach (string StackTrace in ex.StackTrace.Split('\n'))
                {
                    WriteLog("[EXCEPTION] StackTrace :" + StackTrace);
                }
            }
            WriteLog("[EXCEPTION] TargetSite :" + ex.TargetSite);
            WriteLog("[EXCEPTION] Data :" + ex.Data);
            if(ex.InnerException != null)
            {
                WriteLog("[EXCEPTION] InnerException :" + ex.InnerException);
                if(ex.InnerException.StackTrace != null)
                {
                    foreach (string InnerStackTrace in ex.InnerException.StackTrace.Split('\n'))
                    {
                        WriteLog("[EXCEPTION] InnerStackTrace :" + InnerStackTrace);
                    }
                }
            }
        }
    }
}
