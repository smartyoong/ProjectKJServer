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

        // UI�� �۵��ϴ� �������̱� ������ ConfigureAwait(false)�� ������� �ʽ��ϴ�.
        private async void ServerStartButton_Click(object sender, EventArgs e)
        {

            ServerStartButton.Enabled = false;
            ServerStopButton.Enabled = true;
            WriteFileLog("������ �����մϴ�.");
            CheckProcessMonitor();
            await MainProxy.GetSingletone.ConnectToSQLServer(SQLReadyEvent);
            await SQLReadyEvent.Task;
            WriteFileLog("SQL ������ ����Ǿ����ϴ�.");
            WriteFileLog("���� ������ ������ ����մϴ�.");
            MainProxy.GetSingletone.StartAcceptGameServer(GameServerReadyEvent);
            await GameServerReadyEvent.Task;
            WriteFileLog("���� ������ ����Ǿ����ϴ�.");
        }

        private async void ServerStopButton_Click(object sender, EventArgs e)
        {
            ServerStopButton.Enabled = false;
            WriteFileLog("������ �����մϴ�.");
            await MainProxy.GetSingletone.DisconnectSQLServer();
            WriteFileLog("SQL ������ ������ �ߴ��߽��ϴ�.");
            await Task.Delay(TimeSpan.FromSeconds(2));
            await MainProxy.GetSingletone.StopAcceptGameServer();
            WriteFileLog("���� �������� ������ �ߴ��߽��ϴ�.");
            await SocketManager.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("���� ���� �۽� ������������ �ߴ��մϴ�");
            MainProxy.GetSingletone.StopGameServerSendPacketPipeline();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("���� ���� ���� ������������ �ߴ��մϴ�.");
            MainProxy.GetSingletone.StopGameServerRecvPacketPipeline();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("��� ������ ���� ����Ǿ����ϴ� �α� �Ŵ��� ������ ���α׷� ����˴ϴ�.");
            await Task.Delay(TimeSpan.FromSeconds(2));
            ProcessManagerToken.Cancel();
            WriteFileLog("���μ��� ����͸��� �ߴ��մϴ�.");
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
                    while(!ProcessManagerToken.Token.IsCancellationRequested)
                        ProcessManager.Update();
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    WriteErrorLog(ex);
                }
            },ProcessManagerToken.Token);
        }
    }
}
