﻿namespace LoginServer
{
    /// <summary>
    /// LoginServer 클래스 입니다.
    /// MainForm역할을 하며, UI 및 사용자와의 상호작용을 담당합니다.
    /// </summary>
    public partial class LoginServer : Form
    {
        /// <summary>
        /// LoginServer 생성자입니다.
        /// 모든 LoginServer의 클래스의 초기화 작업을 진행합니다.
        /// LogManager 클래스의 내부 생성 작업을 진행합니다.
        /// LogManager 클래스의 LogEvent 이벤트를 구독합니다.
        /// LogManager 클래스 생성중 에러가 발생하면 프로그램이 종료됩니다.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">LogManager 생성시 디렉토리 생성에 실패하면 프로그램이 종료됩니다.</exception>
        /// <see cref="LogManager()"/>
        public LoginServer()
        {
            InitializeComponent();
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
                    if (IsConnected)
                    {
                        DBServerStatusTextBox.BackColor = Color.Green;

                    }
                    else
                    {
                        DBServerStatusTextBox.BackColor = Color.Red;
                    }
                }
            );
            ServerStopButton.Enabled = false;
            ServerStatusTextBox.BackColor = Color.Red;
            DBServerStatusTextBox.BackColor = Color.Red;
            GameServerStatusTextBox.BackColor = Color.Red;
            DBServerStatusTextBox.ReadOnly = true;
            DBServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            GameServerStatusTextBox.ReadOnly = true;
            GameServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            ServerStatusTextBox.ReadOnly = true;
            ServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            CurrentUserCountTextBox.ReadOnly = true;
            CurrentUserCountTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
        }

        private async void ServerStartButton_Click(object sender, EventArgs e)
        {
            await LogManager.GetSingletone.WriteLog("서버를 시작합니다.");
            ServerStartButton.Enabled = false;
            ServerStopButton.Enabled = true;
            DBServerConnector.GetSingletone.Start();
        }

        private async void ServerStopButton_Click(object sender, EventArgs e)
        {
            await LogManager.GetSingletone.WriteLog("서버를 종료완료, 몇 초 대기후 프로그램을 종료합니다");
            await DBServerConnector.GetSingletone.Stop();
            await Task.Delay(5000);
            LogManager.GetSingletone.Close();
            Environment.Exit(0);
        }
    }
}
