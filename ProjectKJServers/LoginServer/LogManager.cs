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
        private readonly string LogFilePath = Properties.Settings.Default.LogDirectory;
        private readonly StreamWriter LogFile;
        private readonly Channel<string> LogChannel = Channel.CreateUnbounded<string>();
        private readonly SemaphoreSlim LogSemaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource LogCancellationTokenSource = new CancellationTokenSource();
        private readonly StringBuilder LogStringBuilder = new StringBuilder();

        private static readonly Lazy<LogManager> Lazy = new Lazy<LogManager>(() => new LogManager());
        // 지연 생성 및 싱글톤 패턴 구현
        public static LogManager GetSingletone { get { return Lazy.Value; } }

        // ListBox 등 UI에 표현하기 위해 이벤트 사용
        public event Action<string>? LogEvent;
        private LogManager()
        {
            // 디렉토리를 설정한다 여기서는 그냥 stringbuilder 사용 안함
            if(string.IsNullOrEmpty(LogFilePath))
            {
                LogFilePath = Environment.CurrentDirectory + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            }
            else
            {
                LogFilePath = LogFilePath + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            }
            string? DirectoryPath = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(DirectoryPath))
            {
                if(DirectoryPath != null)
                {
                    Directory.CreateDirectory(DirectoryPath);
                }
                else
                {
                    MessageBox.Show("로그 디렉토리를 생성하지 못했습니다. 프로그램을 종료합니다.");
                    Environment.Exit(0);
                }
            }
            if (!File.Exists(LogFilePath))
            {
                File.Create(LogFilePath).Close();
            }
            LogFile = new StreamWriter(LogFilePath, true);
            Start();
        }
        ~LogManager()
        {
            Dispose(false);
        }
        // Dispose 패턴 구현
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        // Channel을 사용하여 로그를 비동기로 처리한다.
        private void Start()
        {
            Task.Run(async () =>
            {
                while (!LogCancellationTokenSource.IsCancellationRequested)
                {
                    await PopLogAsync().ConfigureAwait(false);
                }
            }, LogCancellationTokenSource.Token);
        }
        // 관련 리소스를 정리한다 반드시 불려야한다
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
                LogStringBuilder.Clear();
            }
            LogFile.Dispose();
            LogSemaphoreSlim.Dispose();
            LogCancellationTokenSource.Dispose();
            IsAlreadyDisposed = true;
        }
        // 현재 파일이 없다면 생성하고 Channel을 통해 로그를 쓴다.
        public async void WriteLog(string Log)
        {
            await PushLogAsync(Log);
        }
        // 에러 로그를 쓴다. StringBuilder를 사용하여 여러줄을 쓴다.
        public void WriteErrorLog(Exception ex)
        {
            LogStringBuilder.Append("[EXCEPTION] : ");
            LogStringBuilder.Append(ex.Message);
            WriteLog(LogStringBuilder.ToString());
            LogStringBuilder.Clear();
            if(ex.StackTrace != null)
            {
                foreach (string StackTrace in ex.StackTrace.Split('\n'))
                {
                    LogStringBuilder.Append("[EXCEPTION] StackTrace :");
                    LogStringBuilder.Append(StackTrace);
                    WriteLog(LogStringBuilder.ToString());
                    LogStringBuilder.Clear();
                }
            }
            if(ex.HelpLink != null)
            {
                LogStringBuilder.Append("[EXCEPTION] HelpLink :");
                LogStringBuilder.Append(ex.HelpLink);
                WriteLog(LogStringBuilder.ToString());
                LogStringBuilder.Clear();
            }
            if(ex.Source != null)
            {
                LogStringBuilder.Append("[EXCEPTION] Source :");
                LogStringBuilder.Append(ex.Source);
                WriteLog(LogStringBuilder.ToString());
                LogStringBuilder.Clear();
            }
            if(ex.TargetSite != null)
            {
                LogStringBuilder.Append("[EXCEPTION] TargetSite :");
                LogStringBuilder.Append(ex.TargetSite);
                WriteLog(LogStringBuilder.ToString());
                LogStringBuilder.Clear();
            }
            if(ex.Data != null)
            {
                LogStringBuilder.Append("[EXCEPTION] Data :");
                LogStringBuilder.Append(ex.Data);
                WriteLog(LogStringBuilder.ToString());
                LogStringBuilder.Clear();
            }
            if(ex.InnerException != null)
            {
                LogStringBuilder.Append("[EXCEPTION] InnerException :");
                LogStringBuilder.Append(ex.InnerException);
                WriteLog(LogStringBuilder.ToString());
                LogStringBuilder.Clear();
                if(ex.InnerException.StackTrace != null)
                {
                    foreach (string InnerStackTrace in ex.InnerException.StackTrace.Split('\n'))
                    {
                        LogStringBuilder.Append("[EXCEPTION] InnerStackTrace :");
                        LogStringBuilder.Append(InnerStackTrace);
                        WriteLog(LogStringBuilder.ToString());
                        LogStringBuilder.Clear();
                    }
                }
            }
        }
        // Channel에 로그를 넣는다
        private async Task PushLogAsync(string Log)
        {
            await LogChannel.Writer.WriteAsync(Log).ConfigureAwait(false);
        }
        // Channel에서 로그를 빼서 파일에 쓴다.
        private async Task PopLogAsync()
        {
            string Log = await LogChannel.Reader.ReadAsync(LogCancellationTokenSource.Token).ConfigureAwait(false);
            await LogSemaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                LogStringBuilder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                LogStringBuilder.Append(" : ");
                LogStringBuilder.Append(Log);
                LogFile.WriteLine(LogStringBuilder.ToString());
                LogEvent?.Invoke(LogStringBuilder.ToString());
                LogFile.Flush();
                LogStringBuilder.Clear();
            }
            finally
            {
                LogSemaphoreSlim.Release();
            }
        }
    }
}
