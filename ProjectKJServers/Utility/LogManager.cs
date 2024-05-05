using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CoreUtility;
using KYCUIEventManager;

namespace KYCLog
{
    /// <summary>
    /// 로그관련 모든 기능을 담당하는 클래스입니다.
    /// 싱글톤 클래스이며, 지연 생성 및 Dispose 패턴을 구현하였습니다.
    /// 내부적으로 비동기 큐를 사용하며, UI에 표현하기 위해 이벤트를 사용합니다.
    /// 모든 파일 입출력에 대해 비동기 및 스레드에 안전하게 처리합니다.
    /// </summary>
    public class LogManager : IDisposable
    {
        private bool IsAlreadyDisposed = false;
        private static string LogFilePath = CoreSettings.Default.LogDirectory;
        /// <value>StreamWriter는 내부적으로 버퍼링을 사용하기에 동기화 코드를 제거했습니다.</value>
        private readonly StreamWriter LogFile;
        private readonly Channel<string> LogChannel = Channel.CreateUnbounded<string>();
        private readonly CancellationTokenSource LogCancellationTokenSource = new CancellationTokenSource();
        private readonly StringBuilder LogStringBuilder = new StringBuilder();

        /// <value>지연 생성 및 싱글톤 패턴을 위해 Lazy를 사용합니다.</value>
        private static readonly Lazy<LogManager> Lazy = new Lazy<LogManager>(() => new LogManager());
        public static LogManager GetSingletone { get { return Lazy.Value; } }



        /// <summary>
        /// LoginManager 생성자입니다.
        /// 클래스 생성중 파일 디렉토리 관련 에러가 발생하면 프로그램이 종료됩니다.
        /// 자동으로 파일 생성 및 디렉토리를 생성하며, 생성과 동시에 비동기로 로그를 처리합니다.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">LogManager 생성시 디렉토리 생성에 실패하면 프로그램이 종료됩니다.</exception>
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
                    // 생성자에서 에러가 날 경우 그냥 종료가 낫다
                    UIEvent.GetSingletone.ShowMessageBoxLogError("로그 디렉토리를 생성하지 못했습니다. 프로그램을 종료합니다.");
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

        /// <summary>
        /// 새로운 Lazy 인스턴스는 Value 속성에 처음 액세스할 때까지 인스턴스화하지 않기때문에
        /// Lazy가 인스턴스화 하기 전에 해당 변수값을 재설정하게 할 수 있다.
        /// </summary>
        /// <param name="Path"></param>
        static public void SetLogPath(string Path)
        {
            if(Lazy.IsValueCreated)
            {
                UIEvent.GetSingletone.ShowMessageBoxLogError("로그 디렉토리를 변경할 수 없습니다. \n이미 Lazy 객체가 생성되었습니다.");
                return;
            }
            LogFilePath = Path;
        }

        /// <summary>
        /// LoginManager 종결자입니다 최후의 방어수단으로 Dispose를 호출합니다.
        /// </summary>
        ~LogManager()
        {
            Dispose(false);
        }

        /// <summary>
        /// IDisposable 인터페이스를 구현한 Dispose 메서드입니다.
        /// 되도록이면, Close 메서드를 호출하여 리소스를 정리해주세요.
        /// </summary>
        public void Dispose()
        {
            if(IsAlreadyDisposed)
                return;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 비동기 큐 Recv 작업을 시작하는 메서드입니다.
        /// 취소 프로토콜을 지원합니다.
        /// 생성시에 자동으로 호출됩니다.
        /// </summary>
        /// <see cref="LogManager()"/>
        private void Start()
        {
            Task.Run(async () =>
            {
                while (!LogCancellationTokenSource.IsCancellationRequested)
                {
                    await PopLogAsync().ConfigureAwait(false);
                }
            }, LogCancellationTokenSource.Token).ContinueWith(LogTask =>
            {
                if (LogTask.IsFaulted)
                {
                    // 로깅 클래스에서 에러가 날 경우 로깅이 불가능하므로 메세지박스로 처리한다
                    UIEvent.GetSingletone.ShowMessageBoxLogError(LogTask.Exception?.Message);
                }
            });
        }

        /// <summary>
        /// LogManager 클래스를 종료하는 메서드입니다.
        /// 진행중인 작업을 모두 취소시키고, 관련 모든 리소스를 정리합니다.
        /// 내부적으로 Dispose를 호출하므로, 더이상 사용이 필요없을 경우 해당 메서드를 호출해주세요
        /// </summary>
        /// <exception cref="System.OperationCanceledException">
        /// 취소 프로토콜을 지원합니다. 취소가 요청되면 예외가 발생합니다.
        /// </exception>
        /// <see cref="LogManager.Dispose()"/>
        public void Close()
        {
            if(IsAlreadyDisposed)
                return;
            LogCancellationTokenSource.Cancel();
            LogChannel.Writer.Complete();
            Dispose();
        }

        /// <summary>
        /// IDisposable 인터페이스를 구현한 Dispose 메서드입니다.
        /// 되도록이면, Close 메서드를 호출하여 리소스를 정리해주세요.
        /// </summary>
        protected virtual void Dispose(bool Disposing)
        {
            if (IsAlreadyDisposed)
                return;
            if (Disposing)
            {
                LogStringBuilder.Clear();
            }
            LogFile.Dispose();
            LogCancellationTokenSource.Dispose();
            IsAlreadyDisposed = true;
        }

        /// <summary>
        /// 로그를 쓰는 메서드입니다.
        /// 내부적으로는 비동기 큐에 아이템을 삽입합니다.
        /// </summary>
        /// <param name="Log">
        /// 작성할 로그입니다.
        /// </param>
        /// <returns>
        /// 반환 값은 없습니다.
        /// </returns>
        /// <exception cref="System.OperationCanceledException">
        /// 취소 프로토콜을 지원합니다. 취소가 요청되면 예외가 발생합니다.
        /// 또한 LogTask에서 예외가 발생하면 메시지박스로 출력합니다.
        /// </exception>
        public void WriteLog(string Log)
        {
            if(IsAlreadyDisposed)
                return;

            PushLog(Log);
        }


        /// <summary>
        /// 에러 로그를 쓰는 메서드입니다.
        /// 내부적으로는 StringBuilder를 사용하여 로그를 작성합니다.
        /// 또한 WriteLog 메서드를 호출하여 로그를 작성합니다.
        /// </summary>
        /// <param name="ex">
        /// 작성할 로그입니다.
        /// </param>
        /// <returns>
        /// 반환 값은 없습니다.
        /// </returns>
        /// <exception cref="System.OperationCanceledException">
        /// 취소 프로토콜을 지원합니다. 취소가 요청되면 예외가 발생합니다.
        /// 또한 LogTask에서 예외가 발생하면 메시지박스로 출력합니다.
        /// </exception>
        /// <see cref="LogManager.WriteLog(string)"/>
        public void WriteLog(Exception ex)
        {
            if(IsAlreadyDisposed)
                return;

            LogStringBuilder.Append("[EXCEPTION] : ")
            .Append(ex.Message);
            WriteLog(LogStringBuilder.ToString());
            LogStringBuilder.Clear();
            if(ex.StackTrace != null)
            {
                foreach (string StackTrace in ex.StackTrace.Split('\n'))
                {
                    LogStringBuilder.Append("[EXCEPTION] StackTrace :")
                    .Append(StackTrace);
                    WriteLog(LogStringBuilder.ToString());
                    LogStringBuilder.Clear();
                }
            }
            if(ex.HelpLink != null)
            {
                LogStringBuilder.Append("[EXCEPTION] HelpLink :")
                .Append(ex.HelpLink);
                WriteLog(LogStringBuilder.ToString());
                LogStringBuilder.Clear();
            }
            if(ex.Source != null)
            {
                LogStringBuilder.Append("[EXCEPTION] Source :")
                .Append(ex.Source);
                WriteLog(LogStringBuilder.ToString());
                LogStringBuilder.Clear();
            }
            if(ex.Data != null)
            {
                LogStringBuilder.Append("[EXCEPTION] Data :")
                .Append(ex.Data);
                WriteLog(LogStringBuilder.ToString());
                LogStringBuilder.Clear();
            }
            if(ex.InnerException != null)
            {
                LogStringBuilder.Append("[EXCEPTION] InnerException :")
                .Append(ex.InnerException);
                WriteLog(LogStringBuilder.ToString());
                LogStringBuilder.Clear();
                if(ex.InnerException.StackTrace != null)
                {
                    foreach (string InnerStackTrace in ex.InnerException.StackTrace.Split('\n'))
                    {
                        LogStringBuilder.Append("[EXCEPTION] InnerStackTrace :")
                        .Append(InnerStackTrace);
                        WriteLog(LogStringBuilder.ToString());
                        LogStringBuilder.Clear();
                    }
                }
            }
        }
        /// <summary>
        /// 비동기 큐에 로그를 넣는 메서드입니다.
        /// TryWrite를 사용해서 백그라운드 메서드에서 Wait같은 불필요 대기를 줄입니다
        /// </summary>
        private void PushLog(string Log)
        {
            try
            {
                if(!LogChannel.Writer.TryWrite(Log))
                {
                    UIEvent.GetSingletone.ShowMessageBoxLogError($"PushLogAsync : LogChannel.Writer.TryWrite 실패 {Log}");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                UIEvent.GetSingletone.ShowMessageBoxLogError($"PushLogAsync : {ex.Message}");
            }
        }
        /// <summary>
        /// 비동기 큐에서 로그를 빼는 메서드입니다.
        /// </summary>
        private async Task PopLogAsync()
        {
            try
            {
                string Log = await LogChannel.Reader.ReadAsync(LogCancellationTokenSource.Token).ConfigureAwait(false);
                await LogFile.WriteLineAsync(LogStringBuilder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Append(" : ")
                .Append(Log)
                , LogCancellationTokenSource.Token);
                LogFile.Flush();
            }
            catch (AggregateException ae)
            {
                ae = ae.Flatten(); // 중첩된 AggregateException을 평탄화합니다.
                foreach (var ex in ae.InnerExceptions)
                {
                    if (ex is not OperationCanceledException)
                    {
                        UIEvent.GetSingletone.ShowMessageBoxLogError($"PopLogAsync Aggregate : {ex.Message}");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                UIEvent.GetSingletone.ShowMessageBoxLogError($"PopLogAsync : {ex.Message}");
            }
            UIEvent.GetSingletone.AddLogToUI(LogStringBuilder.ToString());
            LogStringBuilder.Clear();
        }
    }
}
