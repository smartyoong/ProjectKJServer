using KYCUIEventManager;
using KYCLog;
using KYCSQL;

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
            LoginServerStatusTextBox.BackColor = Color.Red;
            GameServerStatusTextBox.BackColor = Color.Red;
            LoginServerStatusTextBox.ReadOnly = true;
            LoginServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
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
            UIEvent.GetSingletone.SubscribeLoginServerStatusEvent(IsConnected =>
            {
                ServerStatusTextBox.Invoke((() =>
                {
                    if (IsConnected)
                    {
                        LoginServerStatusTextBox.BackColor = Color.Blue;
                    }
                    else
                    {
                        LoginServerStatusTextBox.BackColor = Color.Red;
                    }
                }));
            }
            );
            UIEvent.GetSingletone.SubscribeLogErrorEvent(log => MessageBox.Show(log));
        }

        private async void ServerStartButton_Click(object sender, EventArgs e)
        {
            ServerStartButton.Enabled = false;
            ServerStopButton.Enabled = true;
            await LogManager.GetSingletone.WriteLog("������ �����մϴ�.").ConfigureAwait(false);
            await GameSQLManager.GetSingletone.ConnectToSQL().ConfigureAwait(false);
            await LogManager.GetSingletone.WriteLog("�α��� ������ ������ ����մϴ�.").ConfigureAwait(false);
            GameServerAcceptor.GetSingletone.Start();
        }

        private async void ServerStopButton_Click(object sender, EventArgs e)
        {
            ServerStopButton.Enabled = false;
            await LogManager.GetSingletone.WriteLog("������ �����մϴ�.").ConfigureAwait(false);
            await GameSQLManager.GetSingletone.StopSQL().ConfigureAwait(false);
            await LogManager.GetSingletone.WriteLog("SQL ������ ������ �ߴ��߽��ϴ�.").ConfigureAwait(false);
            await GameServerAcceptor.GetSingletone.Stop().ConfigureAwait(false);
            await LogManager.GetSingletone.WriteLog("�α��� �������� ������ �ߴ��߽��ϴ�.").ConfigureAwait(false);
            await LogManager.GetSingletone.WriteLog("������ �����߽��ϴ�. ����� ����˴ϴ�.").ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            LogManager.GetSingletone.Close();
            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            Environment.Exit(0);
        }
    }
}
