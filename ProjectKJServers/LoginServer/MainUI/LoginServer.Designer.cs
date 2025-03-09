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
            SQLStatusTextBox = new TextBox();
            GameServerStatusTextBox = new TextBox();
            ServerStatusTextBox = new TextBox();
            CurrentUserCountTextBox = new TextBox();
            UserCountLabel = new Label();
            label9 = new Label();
            label8 = new Label();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            SystemLogBox = new ListBox();
            GarbageCollectionTextBox = new TextBox();
            GarbageCollectionLabel = new Label();
            FileIOTextBox = new TextBox();
            FileIOLabel = new Label();
            PageUsageTextBox = new TextBox();
            PageUsageLabel = new Label();
            NetworkIOTextBox = new TextBox();
            NetworkIOLabel = new Label();
            DiskIOTextBox = new TextBox();
            DiskIOLabel = new Label();
            ThreadUsageTextBox = new TextBox();
            ThreadUsageLabel = new Label();
            MemoryUsageTextBox = new TextBox();
            MemoryUsageLabel = new Label();
            CPUTemperatureTextBox = new TextBox();
            CPUTemperatureLabel = new Label();
            CPUUsageTextBox = new TextBox();
            CPUUsageLabel = new Label();
            MoniotorCheckBox = new CheckBox();
            SuspendLayout();
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
            ServerStopButton.Click += ServerStopButton_Click;
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
            // SQLStatusTextBox
            // 
            SQLStatusTextBox.Font = new Font("맑은 고딕", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 129);
            SQLStatusTextBox.Location = new Point(625, 12);
            SQLStatusTextBox.Name = "SQLStatusTextBox";
            SQLStatusTextBox.ReadOnly = true;
            SQLStatusTextBox.Size = new Size(163, 35);
            SQLStatusTextBox.TabIndex = 3;
            SQLStatusTextBox.Text = "SQL";
            SQLStatusTextBox.TextAlign = HorizontalAlignment.Center;
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
            // CurrentUserCountTextBox
            // 
            CurrentUserCountTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            CurrentUserCountTextBox.Location = new Point(693, 135);
            CurrentUserCountTextBox.Name = "CurrentUserCountTextBox";
            CurrentUserCountTextBox.ReadOnly = true;
            CurrentUserCountTextBox.Size = new Size(95, 21);
            CurrentUserCountTextBox.TabIndex = 6;
            CurrentUserCountTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // UserCountLabel
            // 
            UserCountLabel.AutoSize = true;
            UserCountLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            UserCountLabel.Location = new Point(625, 138);
            UserCountLabel.Name = "UserCountLabel";
            UserCountLabel.Size = new Size(61, 13);
            UserCountLabel.TabIndex = 7;
            UserCountLabel.Text = "동접자 수 :";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(999, 226);
            label9.Name = "label9";
            label9.Size = new Size(19, 15);
            label9.TabIndex = 94;
            label9.Text = "개";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(999, 199);
            label8.Name = "label8";
            label8.Size = new Size(35, 15);
            label8.TabIndex = 93;
            label8.Text = "Bytes";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(999, 172);
            label7.Name = "label7";
            label7.Size = new Size(35, 15);
            label7.TabIndex = 92;
            label7.Text = "Bytes";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(999, 146);
            label6.Name = "label6";
            label6.Size = new Size(35, 15);
            label6.TabIndex = 91;
            label6.Text = "Bytes";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(999, 119);
            label5.Name = "label5";
            label5.Size = new Size(35, 15);
            label5.TabIndex = 90;
            label5.Text = "Bytes";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(999, 97);
            label4.Name = "label4";
            label4.Size = new Size(19, 15);
            label4.TabIndex = 89;
            label4.Text = "개";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(999, 66);
            label3.Name = "label3";
            label3.Size = new Size(35, 15);
            label3.TabIndex = 88;
            label3.Text = "Bytes";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(999, 36);
            label2.Name = "label2";
            label2.Size = new Size(15, 15);
            label2.TabIndex = 87;
            label2.Text = "C";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(999, 10);
            label1.Name = "label1";
            label1.Size = new Size(17, 15);
            label1.TabIndex = 86;
            label1.Text = "%";
            // 
            // SystemLogBox
            // 
            SystemLogBox.FormattingEnabled = true;
            SystemLogBox.HorizontalScrollbar = true;
            SystemLogBox.Location = new Point(806, 253);
            SystemLogBox.Name = "SystemLogBox";
            SystemLogBox.ScrollAlwaysVisible = true;
            SystemLogBox.SelectionMode = SelectionMode.None;
            SystemLogBox.Size = new Size(250, 169);
            SystemLogBox.TabIndex = 85;
            // 
            // GarbageCollectionTextBox
            // 
            GarbageCollectionTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            GarbageCollectionTextBox.Location = new Point(898, 226);
            GarbageCollectionTextBox.Name = "GarbageCollectionTextBox";
            GarbageCollectionTextBox.ReadOnly = true;
            GarbageCollectionTextBox.Size = new Size(95, 21);
            GarbageCollectionTextBox.TabIndex = 84;
            GarbageCollectionTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // GarbageCollectionLabel
            // 
            GarbageCollectionLabel.AutoSize = true;
            GarbageCollectionLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            GarbageCollectionLabel.Location = new Point(806, 234);
            GarbageCollectionLabel.Name = "GarbageCollectionLabel";
            GarbageCollectionLabel.Size = new Size(82, 13);
            GarbageCollectionLabel.TabIndex = 83;
            GarbageCollectionLabel.Text = "2세대 가비지 : ";
            // 
            // FileIOTextBox
            // 
            FileIOTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            FileIOTextBox.Location = new Point(898, 199);
            FileIOTextBox.Name = "FileIOTextBox";
            FileIOTextBox.ReadOnly = true;
            FileIOTextBox.Size = new Size(95, 21);
            FileIOTextBox.TabIndex = 82;
            FileIOTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // FileIOLabel
            // 
            FileIOLabel.AutoSize = true;
            FileIOLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            FileIOLabel.Location = new Point(806, 207);
            FileIOLabel.Name = "FileIOLabel";
            FileIOLabel.Size = new Size(74, 13);
            FileIOLabel.TabIndex = 81;
            FileIOLabel.Text = "파일 Write : ";
            // 
            // PageUsageTextBox
            // 
            PageUsageTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            PageUsageTextBox.Location = new Point(898, 172);
            PageUsageTextBox.Name = "PageUsageTextBox";
            PageUsageTextBox.ReadOnly = true;
            PageUsageTextBox.Size = new Size(95, 21);
            PageUsageTextBox.TabIndex = 80;
            PageUsageTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // PageUsageLabel
            // 
            PageUsageLabel.AutoSize = true;
            PageUsageLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            PageUsageLabel.Location = new Point(806, 180);
            PageUsageLabel.Name = "PageUsageLabel";
            PageUsageLabel.Size = new Size(86, 13);
            PageUsageLabel.TabIndex = 79;
            PageUsageLabel.Text = "페이지 사용량 : ";
            // 
            // NetworkIOTextBox
            // 
            NetworkIOTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            NetworkIOTextBox.Location = new Point(898, 145);
            NetworkIOTextBox.Name = "NetworkIOTextBox";
            NetworkIOTextBox.ReadOnly = true;
            NetworkIOTextBox.Size = new Size(95, 21);
            NetworkIOTextBox.TabIndex = 78;
            NetworkIOTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // NetworkIOLabel
            // 
            NetworkIOLabel.AutoSize = true;
            NetworkIOLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            NetworkIOLabel.Location = new Point(806, 153);
            NetworkIOLabel.Name = "NetworkIOLabel";
            NetworkIOLabel.Size = new Size(81, 13);
            NetworkIOLabel.TabIndex = 77;
            NetworkIOLabel.Text = "네트워크 I/O : ";
            // 
            // DiskIOTextBox
            // 
            DiskIOTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            DiskIOTextBox.Location = new Point(898, 118);
            DiskIOTextBox.Name = "DiskIOTextBox";
            DiskIOTextBox.ReadOnly = true;
            DiskIOTextBox.Size = new Size(95, 21);
            DiskIOTextBox.TabIndex = 76;
            DiskIOTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // DiskIOLabel
            // 
            DiskIOLabel.AutoSize = true;
            DiskIOLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            DiskIOLabel.Location = new Point(806, 126);
            DiskIOLabel.Name = "DiskIOLabel";
            DiskIOLabel.Size = new Size(71, 13);
            DiskIOLabel.TabIndex = 75;
            DiskIOLabel.Text = "파일 Read : ";
            // 
            // ThreadUsageTextBox
            // 
            ThreadUsageTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            ThreadUsageTextBox.Location = new Point(898, 91);
            ThreadUsageTextBox.Name = "ThreadUsageTextBox";
            ThreadUsageTextBox.ReadOnly = true;
            ThreadUsageTextBox.Size = new Size(95, 21);
            ThreadUsageTextBox.TabIndex = 74;
            ThreadUsageTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // ThreadUsageLabel
            // 
            ThreadUsageLabel.AutoSize = true;
            ThreadUsageLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            ThreadUsageLabel.Location = new Point(806, 99);
            ThreadUsageLabel.Name = "ThreadUsageLabel";
            ThreadUsageLabel.Size = new Size(86, 13);
            ThreadUsageLabel.TabIndex = 73;
            ThreadUsageLabel.Text = "스레드 사용량 : ";
            // 
            // MemoryUsageTextBox
            // 
            MemoryUsageTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            MemoryUsageTextBox.Location = new Point(898, 64);
            MemoryUsageTextBox.Name = "MemoryUsageTextBox";
            MemoryUsageTextBox.ReadOnly = true;
            MemoryUsageTextBox.Size = new Size(95, 21);
            MemoryUsageTextBox.TabIndex = 72;
            MemoryUsageTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // MemoryUsageLabel
            // 
            MemoryUsageLabel.AutoSize = true;
            MemoryUsageLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            MemoryUsageLabel.Location = new Point(806, 72);
            MemoryUsageLabel.Name = "MemoryUsageLabel";
            MemoryUsageLabel.Size = new Size(86, 13);
            MemoryUsageLabel.TabIndex = 71;
            MemoryUsageLabel.Text = "메모리 사용량 : ";
            // 
            // CPUTemperatureTextBox
            // 
            CPUTemperatureTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            CPUTemperatureTextBox.Location = new Point(898, 36);
            CPUTemperatureTextBox.Name = "CPUTemperatureTextBox";
            CPUTemperatureTextBox.ReadOnly = true;
            CPUTemperatureTextBox.Size = new Size(95, 21);
            CPUTemperatureTextBox.TabIndex = 70;
            CPUTemperatureTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // CPUTemperatureLabel
            // 
            CPUTemperatureLabel.AutoSize = true;
            CPUTemperatureLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            CPUTemperatureLabel.Location = new Point(806, 44);
            CPUTemperatureLabel.Name = "CPUTemperatureLabel";
            CPUTemperatureLabel.Size = new Size(65, 13);
            CPUTemperatureLabel.TabIndex = 69;
            CPUTemperatureLabel.Text = "CPU 온도 : ";
            // 
            // CPUUsageTextBox
            // 
            CPUUsageTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            CPUUsageTextBox.Location = new Point(898, 9);
            CPUUsageTextBox.Name = "CPUUsageTextBox";
            CPUUsageTextBox.ReadOnly = true;
            CPUUsageTextBox.Size = new Size(95, 21);
            CPUUsageTextBox.TabIndex = 68;
            CPUUsageTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // CPUUsageLabel
            // 
            CPUUsageLabel.AutoSize = true;
            CPUUsageLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            CPUUsageLabel.Location = new Point(806, 17);
            CPUUsageLabel.Name = "CPUUsageLabel";
            CPUUsageLabel.Size = new Size(76, 13);
            CPUUsageLabel.TabIndex = 67;
            CPUUsageLabel.Text = "CPU 사용량 : ";
            // 
            // MoniotorCheckBox
            // 
            MoniotorCheckBox.AutoSize = true;
            MoniotorCheckBox.Location = new Point(625, 168);
            MoniotorCheckBox.Name = "MoniotorCheckBox";
            MoniotorCheckBox.Size = new Size(102, 19);
            MoniotorCheckBox.TabIndex = 95;
            MoniotorCheckBox.Text = "프로세스 감시";
            MoniotorCheckBox.UseVisualStyleBackColor = true;
            MoniotorCheckBox.CheckedChanged += MoniotorCheckBox_CheckedChanged;
            // 
            // LoginServer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1072, 450);
            Controls.Add(MoniotorCheckBox);
            Controls.Add(label9);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(SystemLogBox);
            Controls.Add(GarbageCollectionTextBox);
            Controls.Add(GarbageCollectionLabel);
            Controls.Add(FileIOTextBox);
            Controls.Add(FileIOLabel);
            Controls.Add(PageUsageTextBox);
            Controls.Add(PageUsageLabel);
            Controls.Add(NetworkIOTextBox);
            Controls.Add(NetworkIOLabel);
            Controls.Add(DiskIOTextBox);
            Controls.Add(DiskIOLabel);
            Controls.Add(ThreadUsageTextBox);
            Controls.Add(ThreadUsageLabel);
            Controls.Add(MemoryUsageTextBox);
            Controls.Add(MemoryUsageLabel);
            Controls.Add(CPUTemperatureTextBox);
            Controls.Add(CPUTemperatureLabel);
            Controls.Add(CPUUsageTextBox);
            Controls.Add(CPUUsageLabel);
            Controls.Add(UserCountLabel);
            Controls.Add(CurrentUserCountTextBox);
            Controls.Add(ServerStatusTextBox);
            Controls.Add(GameServerStatusTextBox);
            Controls.Add(SQLStatusTextBox);
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
        private TextBox SQLStatusTextBox;
        private TextBox GameServerStatusTextBox;
        private TextBox ServerStatusTextBox;
        private TextBox CurrentUserCountTextBox;
        private Label UserCountLabel;
        private Label label9;
        private Label label8;
        private Label label7;
        private Label label6;
        private Label label5;
        private Label label4;
        private Label label3;
        private Label label2;
        private Label label1;
        private ListBox SystemLogBox;
        private TextBox GarbageCollectionTextBox;
        private Label GarbageCollectionLabel;
        private TextBox FileIOTextBox;
        private Label FileIOLabel;
        private TextBox PageUsageTextBox;
        private Label PageUsageLabel;
        private TextBox NetworkIOTextBox;
        private Label NetworkIOLabel;
        private TextBox DiskIOTextBox;
        private Label DiskIOLabel;
        private TextBox ThreadUsageTextBox;
        private Label ThreadUsageLabel;
        private TextBox MemoryUsageTextBox;
        private Label MemoryUsageLabel;
        private TextBox CPUTemperatureTextBox;
        private Label CPUTemperatureLabel;
        private TextBox CPUUsageTextBox;
        private Label CPUUsageLabel;
        private CheckBox MoniotorCheckBox;
    }
}
