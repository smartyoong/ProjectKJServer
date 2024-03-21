namespace LoginServer
{
    partial class LoginServer
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
            LogListBox = new ListBox();
            ServerStopButton = new Button();
            ServerStartButton = new Button();
            DBServerStatusTextBox = new TextBox();
            GameServerStatusTextBox = new TextBox();
            ServerStatusTextBox = new TextBox();
            SuspendLayout();
            // 
            // LogListBox
            // 
            LogListBox.FormattingEnabled = true;
            LogListBox.HorizontalScrollbar = true;
            LogListBox.ItemHeight = 15;
            LogListBox.Location = new Point(12, 12);
            LogListBox.Name = "LogListBox";
            LogListBox.ScrollAlwaysVisible = true;
            LogListBox.SelectionMode = SelectionMode.None;
            LogListBox.Size = new Size(607, 409);
            LogListBox.TabIndex = 0;
            // 
            // ServerStopButton
            // 
            ServerStopButton.Font = new Font("맑은 고딕", 18F, FontStyle.Regular, GraphicsUnit.Point, 129);
            ServerStopButton.ForeColor = Color.Red;
            ServerStopButton.Location = new Point(625, 367);
            ServerStopButton.Name = "ServerStopButton";
            ServerStopButton.Size = new Size(163, 54);
            ServerStopButton.TabIndex = 1;
            ServerStopButton.Text = "서버종료";
            ServerStopButton.UseVisualStyleBackColor = true;
            // 
            // ServerStartButton
            // 
            ServerStartButton.Font = new Font("맑은 고딕", 18F, FontStyle.Regular, GraphicsUnit.Point, 129);
            ServerStartButton.ForeColor = Color.Blue;
            ServerStartButton.Location = new Point(625, 307);
            ServerStartButton.Name = "ServerStartButton";
            ServerStartButton.Size = new Size(163, 54);
            ServerStartButton.TabIndex = 2;
            ServerStartButton.Text = "서버시작";
            ServerStartButton.UseVisualStyleBackColor = true;
            ServerStartButton.Click += ServerStartButton_Click;
            // 
            // DBServerStatusTextBox
            // 
            DBServerStatusTextBox.Font = new Font("맑은 고딕", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 129);
            DBServerStatusTextBox.Location = new Point(625, 12);
            DBServerStatusTextBox.Name = "DBServerStatusTextBox";
            DBServerStatusTextBox.ReadOnly = true;
            DBServerStatusTextBox.Size = new Size(163, 35);
            DBServerStatusTextBox.TabIndex = 3;
            DBServerStatusTextBox.Text = "DBServer";
            DBServerStatusTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // GameServerStatusTextBox
            // 
            GameServerStatusTextBox.Font = new Font("맑은 고딕", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 129);
            GameServerStatusTextBox.Location = new Point(625, 53);
            GameServerStatusTextBox.Name = "GameServerStatusTextBox";
            GameServerStatusTextBox.ReadOnly = true;
            GameServerStatusTextBox.Size = new Size(163, 35);
            GameServerStatusTextBox.TabIndex = 4;
            GameServerStatusTextBox.Text = "GameServer";
            GameServerStatusTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // ServerStatusTextBox
            // 
            ServerStatusTextBox.Font = new Font("맑은 고딕", 15.75F);
            ServerStatusTextBox.Location = new Point(625, 94);
            ServerStatusTextBox.Name = "ServerStatusTextBox";
            ServerStatusTextBox.ReadOnly = true;
            ServerStatusTextBox.Size = new Size(163, 35);
            ServerStatusTextBox.TabIndex = 5;
            ServerStatusTextBox.Text = "Status";
            ServerStatusTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // LoginServer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(ServerStatusTextBox);
            Controls.Add(GameServerStatusTextBox);
            Controls.Add(DBServerStatusTextBox);
            Controls.Add(ServerStartButton);
            Controls.Add(ServerStopButton);
            Controls.Add(LogListBox);
            Name = "LoginServer";
            Text = "LoginServer";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListBox LogListBox;
        private Button ServerStopButton;
        private Button ServerStartButton;
        private TextBox DBServerStatusTextBox;
        private TextBox GameServerStatusTextBox;
        private TextBox ServerStatusTextBox;
    }
}
