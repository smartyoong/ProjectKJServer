using CoreUtility.SocketCore;
using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using DBServer.SocketConnect;
using DBServer.PacketPipeLine;
using DBServer.MainUI;

namespace DBServer
{
    public partial class DBServer : Form
    {
        private TaskCompletionSource<bool> GameServerReadyEvent = new TaskCompletionSource<bool>();
        private TaskCompletionSource<bool> SQLReadyEvent = new TaskCompletionSource<bool>();

        private delegate void DelegateWriteLog(string Log);
        private delegate void DelegateWriteErrorLog(Exception ex);
        private DelegateWriteLog WriteFileLog;
        private DelegateWriteErrorLog WriteErrorLog;
        private ProcessMonitor ProcessManager;
        private CancellationTokenSource ProcessManagerToken;

        public DBServer()
        {
            InitializeComponent();
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            LogManager.SetLogPath(DBServerSettings.Default.LogDirectory);
            ServerStopButton.Enabled = false;
            ServerStatusTextBox.BackColor = Color.Red;
            GameServerStatusTextBox.BackColor = Color.Red;
            GameServerStatusTextBox.ReadOnly = true;
            GameServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            ServerStatusTextBox.ReadOnly = true;
            ServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            SubscribeAllEvent();
            WriteFileLog = LogManager.GetSingletone.WriteLog;
            WriteErrorLog = LogManager.GetSingletone.WriteLog;
            ProcessManager = new ProcessMonitor();
            ProcessManagerToken = new CancellationTokenSource();
        }

        private void SubscribeAllEvent()
        {
            UIEvent.GetSingletone.SubscribeLogEvent(Log =>
            {
                LogListBox.Invoke((() =>
                {
                    LogListBox.Items.Add(Log);
                    LogListBox.TopIndex = LogListBox.Items.Count - 1;
                }));
            }
);
            UIEvent.GetSingletone.SubscribeDBServerStatusEvent(IsConnected =>
            {
                ServerStatusTextBox.Invoke((() =>
                {
                    if (IsConnected)
                    {
                        ServerStatusTextBox.BackColor = Color.Blue;
                    }
                    else
                    {
                        ServerStatusTextBox.BackColor = Color.Red;
                    }
                }));
            }
            );
            UIEvent.GetSingletone.SubscribeGameServerStatusEvent(IsConnected =>
            {
                ServerStatusTextBox.Invoke((() =>
                {
                    if (IsConnected)
                    {
                        GameServerStatusTextBox.BackColor = Color.Blue;
                    }
                    else
                    {
                        GameServerStatusTextBox.BackColor = Color.Red;
                    }
                }));
            }
            );
            UIEvent.GetSingletone.SubscribeLogErrorEvent(log => MessageBox.Show(log));
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

        // UI와 작동하는 스레드이기 때문에 ConfigureAwait(false)를 사용하지 않습니다.
        private async void ServerStartButton_Click(object sender, EventArgs e)
        {

            ServerStartButton.Enabled = false;
            ServerStopButton.Enabled = true;
            WriteFileLog("서버를 가동합니다.");
            CheckProcessMonitor();
            await MainProxy.GetSingletone.ConnectToSQLServer(SQLReadyEvent);
            await SQLReadyEvent.Task;
            WriteFileLog("SQL 서버와 연결되었습니다.");
            WriteFileLog("게임 서버의 연결의 대기합니다.");
            MainProxy.GetSingletone.StartAcceptGameServer(GameServerReadyEvent);
            await GameServerReadyEvent.Task;
            WriteFileLog("게임 서버와 연결되었습니다.");
        }

        private async void ServerStopButton_Click(object sender, EventArgs e)
        {
            ServerStopButton.Enabled = false;
            WriteFileLog("서버를 중지합니다.");
            await MainProxy.GetSingletone.DisconnectSQLServer();
            WriteFileLog("SQL 서버와 연결을 중단했습니다.");
            await Task.Delay(TimeSpan.FromSeconds(2));
            await MainProxy.GetSingletone.StopAcceptGameServer();
            WriteFileLog("게임 서버와의 연결을 중단했습니다.");
            await SocketManager.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("게임 서버 송신 파이프라인을 중단합니다");
            MainProxy.GetSingletone.StopGameServerSendPacketPipeline();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("게임 서버 수신 파이프라인을 중단합니다.");
            MainProxy.GetSingletone.StopGameServerRecvPacketPipeline();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("모든 소켓이 연결 종료되었습니다 로그 매니저 차단후 프로그램 종료됩니다.");
            await Task.Delay(TimeSpan.FromSeconds(2));
            ProcessManagerToken.Cancel();
            WriteFileLog("프로세스 모니터링을 중단합니다.");
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
        private void CheckProcessMonitor()
        {
            Task.Run(() =>
            {
                try
                {
                    while (!ProcessManagerToken.Token.IsCancellationRequested)
                        ProcessManager.Update();
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    WriteErrorLog(ex);
                }
            }, ProcessManagerToken.Token);
        }

        private void MoniotorCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
                Invoke(new Action(() => MoniotorCheckBox_CheckedChanged(sender, e)));
            else
                ProcessManager.Activate(MoniotorCheckBox.Checked);
        }
    }
}
