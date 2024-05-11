namespace GameServer
{
    partial class GameServer
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            ServerStopButton = new Button();
            ServerStartButton = new Button();
            LogListBox = new ListBox();
            DBServerStatusTextBox = new TextBox();
            LoginServerStatusTextBox = new TextBox();
            UserCountLabel = new Label();
            CurrentUserCountTextBox = new TextBox();
            ServerStatusTextBox = new TextBox();
            SuspendLayout();
            // 
            // ServerStopButton
            // 
            ServerStopButton.Font = new Font("맑은 고딕", 18F, FontStyle.Regular, GraphicsUnit.Point, 129);
            ServerStopButton.ForeColor = Color.Red;
            ServerStopButton.Location = new Point(625, 367);
            ServerStopButton.Name = "ServerStopButton";
            ServerStopButton.Size = new Size(163, 54);
            ServerStopButton.TabIndex = 2;
            ServerStopButton.Text = "서버종료";
            ServerStopButton.UseVisualStyleBackColor = true;
            ServerStopButton.Click += ServerStopButton_Click;
            // 
            // ServerStartButton
            // 
            ServerStartButton.Font = new Font("맑은 고딕", 18F, FontStyle.Regular, GraphicsUnit.Point, 129);
            ServerStartButton.ForeColor = Color.Blue;
            ServerStartButton.Location = new Point(625, 307);
            ServerStartButton.Name = "ServerStartButton";
            ServerStartButton.Size = new Size(163, 54);
            ServerStartButton.TabIndex = 3;
            ServerStartButton.Text = "서버시작";
            ServerStartButton.UseVisualStyleBackColor = true;
            ServerStartButton.Click += ServerStartButton_Click;
            // 
            // LogListBox
            // 
            LogListBox.FormattingEnabled = true;
            LogListBox.HorizontalScrollbar = true;
            LogListBox.Location = new Point(12, 12);
            LogListBox.Name = "LogListBox";
            LogListBox.ScrollAlwaysVisible = true;
            LogListBox.SelectionMode = SelectionMode.None;
            LogListBox.Size = new Size(607, 409);
            LogListBox.TabIndex = 4;
            // 
            // DBServerStatusTextBox
            // 
            DBServerStatusTextBox.Font = new Font("맑은 고딕", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 129);
            DBServerStatusTextBox.Location = new Point(625, 12);
            DBServerStatusTextBox.Name = "DBServerStatusTextBox";
            DBServerStatusTextBox.ReadOnly = true;
            DBServerStatusTextBox.Size = new Size(163, 35);
            DBServerStatusTextBox.TabIndex = 5;
            DBServerStatusTextBox.Text = "DBServer";
            DBServerStatusTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // LoginServerStatusTextBox
            // 
            LoginServerStatusTextBox.Font = new Font("맑은 고딕", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 129);
            LoginServerStatusTextBox.Location = new Point(625, 53);
            LoginServerStatusTextBox.Name = "LoginServerStatusTextBox";
            LoginServerStatusTextBox.ReadOnly = true;
            LoginServerStatusTextBox.Size = new Size(163, 35);
            LoginServerStatusTextBox.TabIndex = 6;
            LoginServerStatusTextBox.Text = "LoginServer";
            LoginServerStatusTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // UserCountLabel
            // 
            UserCountLabel.AutoSize = true;
            UserCountLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            UserCountLabel.Location = new Point(626, 143);
            UserCountLabel.Name = "UserCountLabel";
            UserCountLabel.Size = new Size(61, 13);
            UserCountLabel.TabIndex = 8;
            UserCountLabel.Text = "동접자 수 :";
            // 
            // CurrentUserCountTextBox
            // 
            CurrentUserCountTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            CurrentUserCountTextBox.Location = new Point(693, 135);
            CurrentUserCountTextBox.Name = "CurrentUserCountTextBox";
            CurrentUserCountTextBox.ReadOnly = true;
            CurrentUserCountTextBox.Size = new Size(95, 21);
            CurrentUserCountTextBox.TabIndex = 9;
            CurrentUserCountTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // ServerStatusTextBox
            // 
            ServerStatusTextBox.Font = new Font("맑은 고딕", 15.75F);
            ServerStatusTextBox.Location = new Point(625, 94);
            ServerStatusTextBox.Name = "ServerStatusTextBox";
            ServerStatusTextBox.ReadOnly = true;
            ServerStatusTextBox.Size = new Size(163, 35);
            ServerStatusTextBox.TabIndex = 10;
            ServerStatusTextBox.Text = "Status";
            ServerStatusTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // GameServer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(ServerStatusTextBox);
            Controls.Add(CurrentUserCountTextBox);
            Controls.Add(UserCountLabel);
            Controls.Add(LoginServerStatusTextBox);
            Controls.Add(DBServerStatusTextBox);
            Controls.Add(LogListBox);
            Controls.Add(ServerStartButton);
            Controls.Add(ServerStopButton);
            Name = "GameServer";
            Text = "GameServer";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button ServerStopButton;
        private Button ServerStartButton;
        private ListBox LogListBox;
        private TextBox DBServerStatusTextBox;
        private TextBox LoginServerStatusTextBox;
        private Label UserCountLabel;
        private TextBox CurrentUserCountTextBox;
        private TextBox ServerStatusTextBox;
    }
}
