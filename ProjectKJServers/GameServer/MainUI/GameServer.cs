using CoreUtility.GlobalVariable;
using CoreUtility.SocketCore;
using CoreUtility.Utility;
using GameServer.Component;
using GameServer.GameSystem;
using GameServer.MainUI;
using GameServer.Object;
using GameServer.PacketPipeLine;
using GameServer.SocketConnect;
using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Principal;

namespace GameServer
{
    public partial class GameServer : Form
    {
        private int CurrentUserCount = 0;
        private TaskCompletionSource<bool> LoginServerReadyEvent = new TaskCompletionSource<bool>();
        private TaskCompletionSource<bool> DBServerReadyEvent = new TaskCompletionSource<bool>();

        private delegate void DelegateWriteLog(string Log);
        private delegate void DelegateWriteErrorLog(Exception ex);
        private DelegateWriteLog WriteFileLog;
        private DelegateWriteErrorLog WriteErrorLog;
        private ProcessMonitor ProcessMonitorSystem;
        private CancellationTokenSource ProcessManagerToken;

        public GameServer()
        {
            InitializeComponent();
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            LogManager.SetLogPath(GameServerSettings.Default.LogDirectory);
            ProcessMonitorSystem = new ProcessMonitor();
            ProcessManagerToken = new CancellationTokenSource();
            UIEvent.GetSingletone.SubscribeLogErrorEvent(log => MessageBox.Show(log));
            UIEvent.GetSingletone.SubscribeLogEvent(
            Log =>
            {
                LogListBox.Invoke(() =>
                {
                    LogListBox.Items.Add(Log);
                    LogListBox.TopIndex = LogListBox.Items.Count - 1;
                });
            }
            );
            UIEvent.GetSingletone.SubscribeDBServerStatusEvent(
                IsConnected =>
                {
                    DBServerStatusTextBox.Invoke(() =>
                    {
                        if (IsConnected)
                        {
                            DBServerStatusTextBox.BackColor = Color.Blue;
                        }
                        else
                        {
                            DBServerStatusTextBox.BackColor = Color.Red;
                        }
                    }
                    );
                }
            );
            UIEvent.GetSingletone.SubscribeGameServerStatusEvent(
                IsConnected =>
                {
                    ServerStatusTextBox.Invoke(() =>
                    {
                        if (IsConnected)
                        {
                            ServerStatusTextBox.BackColor = Color.Blue;

                        }
                        else
                        {
                            ServerStatusTextBox.BackColor = Color.Red;
                        }
                    });
                }
            );
            UIEvent.GetSingletone.SubscribeLoginServerStatusEvent(
                IsConnected =>
                {
                    LoginServerStatusTextBox.Invoke(() =>
                    {
                        if (IsConnected)
                        {
                            LoginServerStatusTextBox.BackColor = Color.Blue;

                        }
                        else
                        {
                            LoginServerStatusTextBox.BackColor = Color.Red;
                        }
                    });
                }
             );
            UIEvent.GetSingletone.SubscribeUserCountEvent(
                IsIncrease =>
                {
                    ServerStatusTextBox.Invoke(() =>
                    {
                        if (IsIncrease)
                        {
                            CurrentUserCount++;
                            CurrentUserCountTextBox.Text = CurrentUserCount.ToString();
                        }
                        else
                        {
                            CurrentUserCount--;
                            CurrentUserCountTextBox.Text = CurrentUserCount.ToString();
                        }
                    });
                }
             );
            UIEvent.GetSingletone.SubscribeCPUUsageEvent(UpdateCPUUsage);
            UIEvent.GetSingletone.SubscribeMemoryUsageEvent(UpdateMemoryUsage);
            UIEvent.GetSingletone.SubscribeFileIOEvent(UpdateFileIO);
            UIEvent.GetSingletone.SubscribeGarbageCollectionEvent(UpdateGarbageCollection);
            UIEvent.GetSingletone.SubscribeCPUTemperatureEvent(UpdateCPUTemperature);
            UIEvent.GetSingletone.SubscribeSystemLogEvent(UpdateSystemLog);
            UIEvent.GetSingletone.SubscribeNetworkUsageEvent(UpdateNetworkUsage);
            UIEvent.GetSingletone.SubscribePageUsageEvent(UpdatePageUsage);
            UIEvent.GetSingletone.SubscribeDiskIOEvent(UpdateDiskIO);
            UIEvent.GetSingletone.SubscribeThreadUsageEvent(UpdateThreadUsage);
            ServerStopButton.Enabled = false;
            LoginServerStatusTextBox.BackColor = Color.Red;
            DBServerStatusTextBox.BackColor = Color.Red;
            ServerStatusTextBox.BackColor = Color.Red;
            DBServerStatusTextBox.ReadOnly = true;
            DBServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            ServerStatusTextBox.ReadOnly = true;
            ServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            LoginServerStatusTextBox.ReadOnly = true;
            LoginServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            CurrentUserCountTextBox.ReadOnly = true;
            CurrentUserCountTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            CurrentUserCountTextBox.Text = CurrentUserCount.ToString();
            WriteFileLog = LogManager.GetSingletone.WriteLog;
            WriteErrorLog = LogManager.GetSingletone.WriteLog;
            CPUUsageTextBox.Text = "0";
            MemoryUsageTextBox.Text = "0";
            FileIOTextBox.Text = "0";
            GarbageCollectionTextBox.Text = "0";
            CPUTemperatureTextBox.Text = "0";
            NetworkIOTextBox.Text = "0";
            PageUsageTextBox.Text = "0";
            DiskIOTextBox.Text = "0";
            ThreadUsageTextBox.Text = "0";
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show((e.ExceptionObject as Exception)?.Message, "GameServer Domain Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            MessageBox.Show((e.ExceptionObject as Exception)?.StackTrace, "GameServer Domain Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "GameServer Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            MessageBox.Show(e.Exception.StackTrace, "GameServer Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async void ServerStartButton_Click(object sender, EventArgs e)
        {
            WriteFileLog("게임 서버를 시작합니다.");
            ServerStartButton.Enabled = false;
            ServerStopButton.Enabled = true;
            CheckProcessMonitor();
            WriteFileLog("게임 엔진을 시작합니다.");
            MainProxy.GetSingletone.StartGameEngine();
            WriteFileLog("로그인 서버의 연결을 대기합니다.");
            MainProxy.GetSingletone.StartAcceptLoginServer(LoginServerReadyEvent);
            await LoginServerReadyEvent.Task;
            WriteFileLog("로그인 서버와 연결되었습니다.");
            WriteFileLog("DB 서버의 연결을 대기합니다.");
            MainProxy.GetSingletone.ConnectToDBServer(DBServerReadyEvent);
            await DBServerReadyEvent.Task;
            WriteFileLog("DB 서버와 연결되었습니다.");
            WriteFileLog("클라이언트의 연결을 받겠습니다..");
            MainProxy.GetSingletone.StartAcceptClient();
        }

        private async void ServerStopButton_Click(object sender, EventArgs e)
        {
            WriteFileLog("게임 서버를 중단합니다.");
            MainProxy.GetSingletone.StopGameEngine();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("로그인 서버와 연결을 중단합니다.");
            await MainProxy.GetSingletone.StopAcceptLoginServer();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("로그인 서버 송신 파이프라인을 중단합니다.");
            MainProxy.GetSingletone.StopLoginServerSendPacketPipeline();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("로그인 서버 수신 파이프라인을 중단합니다.");
            MainProxy.GetSingletone.StopLoginServerRecvPacketPipeline();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("DB 서버와 연결을 중단합니다.");
            await MainProxy.GetSingletone.StopConnectToDBServer();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("DB 서버 송신 파이프라인을 중단합니다.");
            MainProxy.GetSingletone.StopDBServerSendPacketPipeline();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("DB 서버 수신 파이프라인을 중단합니다.");
            MainProxy.GetSingletone.StopDBServerRecvPacketPipeline();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("클라이언트와 연결을 중단합니다.");
            await MainProxy.GetSingletone.StopAcceptClient();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("클라이언트 송신 파이프라인을 중단합니다.");
            MainProxy.GetSingletone.StopClientSendPacketPipeline();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("클라이언트 수신 파이프라인을 중단합니다.");
            MainProxy.GetSingletone.StopClientRecvPacketPipeline();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("연결중인 모든 소켓을 중단합니다.");
            await SocketManager.GetSingletone.Cancel();
            WriteFileLog("프로세스 모니터를 종료합니다.");
            ProcessManagerToken.Cancel();
            WriteFileLog("잠시후 로그 매니저를 종료하고 게임 서버를 종료합니다.");
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.Close();
            await Task.Delay(TimeSpan.FromSeconds(2));
            Application.Exit();
        }

        private void UpdateCPUUsage(float CPUUsage)
        {
            CPUUsageTextBox.Text = CPUUsage.ToString();
        }

        private void UpdateMemoryUsage(float MemoryUsage)
        {
            MemoryUsageTextBox.Text = MemoryUsage.ToString();
        }

        private void UpdateFileIO(float FileIO)
        {
            FileIOTextBox.Text = FileIO.ToString();
        }

        private void UpdateGarbageCollection(long GarbageCollection)
        {
            GarbageCollectionTextBox.Text = GarbageCollection.ToString();
        }

        private void UpdateCPUTemperature(float CPUTemperature)
        {
            CPUTemperatureTextBox.Text = CPUTemperature.ToString();
        }

        private void UpdateSystemLog(string Log)
        {
            SystemLogBox.Items.Add(Log);
            SystemLogBox.TopIndex = SystemLogBox.Items.Count - 1;
        }

        private void UpdateNetworkUsage(float NetworkUsage)
        {
            NetworkIOTextBox.Text = NetworkUsage.ToString();
        }

        private void UpdatePageUsage(float PageUsage)
        {
            PageUsageTextBox.Text = PageUsage.ToString();
        }

        private void UpdateDiskIO(float DiskIO)
        {
            DiskIOTextBox.Text = DiskIO.ToString();
        }

        private void UpdateThreadUsage(float ThreadUsage)
        {
            ThreadUsageTextBox.Text = ThreadUsage.ToString();
        }

        private void MoniotorCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
                Invoke(new Action(() => MoniotorCheckBox_CheckedChanged(sender, e)));
            else
                ProcessMonitorSystem.Activate(MoniotorCheckBox.Checked);
        }
        private void CheckProcessMonitor()
        {
            Task.Run(() =>
            {
                try
                {
                    while (!ProcessManagerToken.Token.IsCancellationRequested)
                        ProcessMonitorSystem.Update();
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    WriteErrorLog(ex);
                }
            }, ProcessManagerToken.Token);
        }
    }
}
