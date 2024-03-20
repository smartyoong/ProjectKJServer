using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace LoginServer
{
    internal class LogManager : IDisposable
    {
        private bool IsAlreadyDisposed = false;
        private readonly string LogDirectory = Properties.Settings.Default.LogDirectory;
        private readonly StreamWriter LogFile;
        private readonly Channel<string> LogChannel = Channel.CreateUnbounded<string>();
        private readonly SemaphoreSlim LogSemaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource LogCancellationTokenSource = new CancellationTokenSource();
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
        public void Start()
        {
            Task.Run(async () =>
            {
                while (!LogCancellationTokenSource.IsCancellationRequested)
                {
                    await PopLogAsync();
                }
            }, LogCancellationTokenSource.Token);
        }
        public void Close()
        {
            LogChannel.Writer.Complete();
            LogCancellationTokenSource.Cancel();
            Dispose();
        }
        protected virtual void Dispose(bool Disposing)
        {
            if (IsAlreadyDisposed)
                return;
            if (Disposing)
            {
                
            }
            LogFile.Dispose();
            LogSemaphoreSlim.Dispose();
            LogCancellationTokenSource.Dispose();
            IsAlreadyDisposed = true;
        }

        public async void WriteLog(string Log)
        {
            if(Directory.Exists(LogDirectory) == false)
            {
                Directory.CreateDirectory(LogDirectory);
            }
            await PushLogAsync(Log);
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
        private async Task PushLogAsync(string Log)
        {
            await LogChannel.Writer.WriteAsync(Log);
        }
        private async Task<string> PopLogAsync()
        {
            string Log = await LogChannel.Reader.ReadAsync(LogCancellationTokenSource.Token);
            await LogSemaphoreSlim.WaitAsync();
            try
            {
                LogFile.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + Log);
            }
            finally
            {
                LogSemaphoreSlim.Release();
            }
            return Log;
        }
    }
}
