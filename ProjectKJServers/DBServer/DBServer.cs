using KYCUIEventManager;
using KYCLog;
using KYCSQL;
using KYCSocketCore;

namespace DBServer
{
    public partial class DBServer : Form
    {
        public DBServer()
        {
            InitializeComponent();
            LogManager.SetLogPath(DBServerSettings.Default.LogDirectory);
            ServerStopButton.Enabled = false;
            ServerStatusTextBox.BackColor = Color.Red;
            GameServerStatusTextBox.BackColor = Color.Red;
            GameServerStatusTextBox.ReadOnly = true;
            GameServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            ServerStatusTextBox.ReadOnly = true;
            ServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            CurrentUserCountTextBox.ReadOnly = true;
            CurrentUserCountTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            SubscribeAllEvent();
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
        }

        // UI와 작동하는 스레드이기 때문에 ConfigureAwait(false)를 사용하지 않습니다.
        private async void ServerStartButton_Click(object sender, EventArgs e)
        {
            ServerStartButton.Enabled = false;
            ServerStopButton.Enabled = true;
            await LogManager.GetSingletone.WriteLog("서버를 가동합니다.");
            await GameSQLManager.GetSingletone.ConnectToSQL();
            await LogManager.GetSingletone.WriteLog("로그인 서버의 연결의 대기합니다.");
            GameServerAcceptor.GetSingletone.Start();
        }

        private async void ServerStopButton_Click(object sender, EventArgs e)
        {
            ServerStopButton.Enabled = false;
            await LogManager.GetSingletone.WriteLog("서버를 중지합니다.");
            await GameSQLManager.GetSingletone.StopSQL();
            await LogManager.GetSingletone.WriteLog("SQL 서버와 연결을 중단했습니다.");
            await Task.Delay(TimeSpan.FromSeconds(2));
            await GameServerAcceptor.GetSingletone.Stop();
            await LogManager.GetSingletone.WriteLog("게임 서버와의 연결을 중단했습니다.");
            await SocketManager.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            await LogManager.GetSingletone.WriteLog("모든 소켓이 연결 종료되었습니다 로그 매니저 차단후 프로그램 종료됩니다.");
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.Close();
            await Task.Delay(TimeSpan.FromSeconds(2));
            Environment.Exit(0);
        }
    }
}
