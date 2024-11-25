namespace DBServer
{
    partial class DBServer
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
            ServerStatusTextBox = new TextBox();
            GameServerStatusTextBox = new TextBox();
            ServerStartButton = new Button();
            ServerStopButton = new Button();
            LogListBox = new ListBox();
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
            SuspendLayout();
            // 
            // ServerStatusTextBox
            // 
            ServerStatusTextBox.Font = new Font("맑은 고딕", 15.75F);
            ServerStatusTextBox.Location = new Point(625, 62);
            ServerStatusTextBox.Name = "ServerStatusTextBox";
            ServerStatusTextBox.ReadOnly = true;
            ServerStatusTextBox.Size = new Size(163, 35);
            ServerStatusTextBox.TabIndex = 13;
            ServerStatusTextBox.Text = "Status";
            ServerStatusTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // GameServerStatusTextBox
            // 
            GameServerStatusTextBox.Font = new Font("맑은 고딕", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 129);
            GameServerStatusTextBox.Location = new Point(625, 21);
            GameServerStatusTextBox.Name = "GameServerStatusTextBox";
            GameServerStatusTextBox.ReadOnly = true;
            GameServerStatusTextBox.Size = new Size(163, 35);
            GameServerStatusTextBox.TabIndex = 12;
            GameServerStatusTextBox.Text = "GameServer";
            GameServerStatusTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // ServerStartButton
            // 
            ServerStartButton.Font = new Font("맑은 고딕", 18F, FontStyle.Regular, GraphicsUnit.Point, 129);
            ServerStartButton.ForeColor = Color.Blue;
            ServerStartButton.Location = new Point(625, 316);
            ServerStartButton.Name = "ServerStartButton";
            ServerStartButton.Size = new Size(163, 54);
            ServerStartButton.TabIndex = 10;
            ServerStartButton.Text = "서버시작";
            ServerStartButton.UseVisualStyleBackColor = true;
            ServerStartButton.Click += ServerStartButton_Click;
            // 
            // ServerStopButton
            // 
            ServerStopButton.Font = new Font("맑은 고딕", 18F, FontStyle.Regular, GraphicsUnit.Point, 129);
            ServerStopButton.ForeColor = Color.Red;
            ServerStopButton.Location = new Point(625, 376);
            ServerStopButton.Name = "ServerStopButton";
            ServerStopButton.Size = new Size(163, 54);
            ServerStopButton.TabIndex = 9;
            ServerStopButton.Text = "서버종료";
            ServerStopButton.UseVisualStyleBackColor = true;
            ServerStopButton.Click += ServerStopButton_Click;
            // 
            // LogListBox
            // 
            LogListBox.FormattingEnabled = true;
            LogListBox.HorizontalScrollbar = true;
            LogListBox.Location = new Point(12, 21);
            LogListBox.Name = "LogListBox";
            LogListBox.ScrollAlwaysVisible = true;
            LogListBox.SelectionMode = SelectionMode.None;
            LogListBox.Size = new Size(607, 409);
            LogListBox.TabIndex = 8;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(998, 232);
            label9.Name = "label9";
            label9.Size = new Size(19, 15);
            label9.TabIndex = 66;
            label9.Text = "개";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(998, 205);
            label8.Name = "label8";
            label8.Size = new Size(35, 15);
            label8.TabIndex = 65;
            label8.Text = "Bytes";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(998, 178);
            label7.Name = "label7";
            label7.Size = new Size(35, 15);
            label7.TabIndex = 64;
            label7.Text = "Bytes";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(998, 152);
            label6.Name = "label6";
            label6.Size = new Size(35, 15);
            label6.TabIndex = 63;
            label6.Text = "Bytes";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(998, 125);
            label5.Name = "label5";
            label5.Size = new Size(35, 15);
            label5.TabIndex = 62;
            label5.Text = "Bytes";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(998, 103);
            label4.Name = "label4";
            label4.Size = new Size(19, 15);
            label4.TabIndex = 61;
            label4.Text = "개";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(998, 72);
            label3.Name = "label3";
            label3.Size = new Size(35, 15);
            label3.TabIndex = 60;
            label3.Text = "Bytes";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(998, 42);
            label2.Name = "label2";
            label2.Size = new Size(15, 15);
            label2.TabIndex = 59;
            label2.Text = "C";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(998, 16);
            label1.Name = "label1";
            label1.Size = new Size(17, 15);
            label1.TabIndex = 58;
            label1.Text = "%";
            // 
            // SystemLogBox
            // 
            SystemLogBox.FormattingEnabled = true;
            SystemLogBox.HorizontalScrollbar = true;
            SystemLogBox.Location = new Point(805, 259);
            SystemLogBox.Name = "SystemLogBox";
            SystemLogBox.ScrollAlwaysVisible = true;
            SystemLogBox.SelectionMode = SelectionMode.None;
            SystemLogBox.Size = new Size(250, 169);
            SystemLogBox.TabIndex = 57;
            // 
            // GarbageCollectionTextBox
            // 
            GarbageCollectionTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            GarbageCollectionTextBox.Location = new Point(897, 232);
            GarbageCollectionTextBox.Name = "GarbageCollectionTextBox";
            GarbageCollectionTextBox.ReadOnly = true;
            GarbageCollectionTextBox.Size = new Size(95, 21);
            GarbageCollectionTextBox.TabIndex = 56;
            GarbageCollectionTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // GarbageCollectionLabel
            // 
            GarbageCollectionLabel.AutoSize = true;
            GarbageCollectionLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            GarbageCollectionLabel.Location = new Point(805, 240);
            GarbageCollectionLabel.Name = "GarbageCollectionLabel";
            GarbageCollectionLabel.Size = new Size(82, 13);
            GarbageCollectionLabel.TabIndex = 55;
            GarbageCollectionLabel.Text = "2세대 가비지 : ";
            // 
            // FileIOTextBox
            // 
            FileIOTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            FileIOTextBox.Location = new Point(897, 205);
            FileIOTextBox.Name = "FileIOTextBox";
            FileIOTextBox.ReadOnly = true;
            FileIOTextBox.Size = new Size(95, 21);
            FileIOTextBox.TabIndex = 54;
            FileIOTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // FileIOLabel
            // 
            FileIOLabel.AutoSize = true;
            FileIOLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            FileIOLabel.Location = new Point(805, 213);
            FileIOLabel.Name = "FileIOLabel";
            FileIOLabel.Size = new Size(74, 13);
            FileIOLabel.TabIndex = 53;
            FileIOLabel.Text = "파일 Write : ";
            // 
            // PageUsageTextBox
            // 
            PageUsageTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            PageUsageTextBox.Location = new Point(897, 178);
            PageUsageTextBox.Name = "PageUsageTextBox";
            PageUsageTextBox.ReadOnly = true;
            PageUsageTextBox.Size = new Size(95, 21);
            PageUsageTextBox.TabIndex = 52;
            PageUsageTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // PageUsageLabel
            // 
            PageUsageLabel.AutoSize = true;
            PageUsageLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            PageUsageLabel.Location = new Point(805, 186);
            PageUsageLabel.Name = "PageUsageLabel";
            PageUsageLabel.Size = new Size(86, 13);
            PageUsageLabel.TabIndex = 51;
            PageUsageLabel.Text = "페이지 사용량 : ";
            // 
            // NetworkIOTextBox
            // 
            NetworkIOTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            NetworkIOTextBox.Location = new Point(897, 151);
            NetworkIOTextBox.Name = "NetworkIOTextBox";
            NetworkIOTextBox.ReadOnly = true;
            NetworkIOTextBox.Size = new Size(95, 21);
            NetworkIOTextBox.TabIndex = 50;
            NetworkIOTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // NetworkIOLabel
            // 
            NetworkIOLabel.AutoSize = true;
            NetworkIOLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            NetworkIOLabel.Location = new Point(805, 159);
            NetworkIOLabel.Name = "NetworkIOLabel";
            NetworkIOLabel.Size = new Size(81, 13);
            NetworkIOLabel.TabIndex = 49;
            NetworkIOLabel.Text = "네트워크 I/O : ";
            // 
            // DiskIOTextBox
            // 
            DiskIOTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            DiskIOTextBox.Location = new Point(897, 124);
            DiskIOTextBox.Name = "DiskIOTextBox";
            DiskIOTextBox.ReadOnly = true;
            DiskIOTextBox.Size = new Size(95, 21);
            DiskIOTextBox.TabIndex = 48;
            DiskIOTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // DiskIOLabel
            // 
            DiskIOLabel.AutoSize = true;
            DiskIOLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            DiskIOLabel.Location = new Point(805, 132);
            DiskIOLabel.Name = "DiskIOLabel";
            DiskIOLabel.Size = new Size(71, 13);
            DiskIOLabel.TabIndex = 47;
            DiskIOLabel.Text = "파일 Read : ";
            // 
            // ThreadUsageTextBox
            // 
            ThreadUsageTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            ThreadUsageTextBox.Location = new Point(897, 97);
            ThreadUsageTextBox.Name = "ThreadUsageTextBox";
            ThreadUsageTextBox.ReadOnly = true;
            ThreadUsageTextBox.Size = new Size(95, 21);
            ThreadUsageTextBox.TabIndex = 46;
            ThreadUsageTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // ThreadUsageLabel
            // 
            ThreadUsageLabel.AutoSize = true;
            ThreadUsageLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            ThreadUsageLabel.Location = new Point(805, 105);
            ThreadUsageLabel.Name = "ThreadUsageLabel";
            ThreadUsageLabel.Size = new Size(86, 13);
            ThreadUsageLabel.TabIndex = 45;
            ThreadUsageLabel.Text = "스레드 사용량 : ";
            // 
            // MemoryUsageTextBox
            // 
            MemoryUsageTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            MemoryUsageTextBox.Location = new Point(897, 70);
            MemoryUsageTextBox.Name = "MemoryUsageTextBox";
            MemoryUsageTextBox.ReadOnly = true;
            MemoryUsageTextBox.Size = new Size(95, 21);
            MemoryUsageTextBox.TabIndex = 44;
            MemoryUsageTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // MemoryUsageLabel
            // 
            MemoryUsageLabel.AutoSize = true;
            MemoryUsageLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            MemoryUsageLabel.Location = new Point(805, 78);
            MemoryUsageLabel.Name = "MemoryUsageLabel";
            MemoryUsageLabel.Size = new Size(86, 13);
            MemoryUsageLabel.TabIndex = 43;
            MemoryUsageLabel.Text = "메모리 사용량 : ";
            // 
            // CPUTemperatureTextBox
            // 
            CPUTemperatureTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            CPUTemperatureTextBox.Location = new Point(897, 42);
            CPUTemperatureTextBox.Name = "CPUTemperatureTextBox";
            CPUTemperatureTextBox.ReadOnly = true;
            CPUTemperatureTextBox.Size = new Size(95, 21);
            CPUTemperatureTextBox.TabIndex = 42;
            CPUTemperatureTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // CPUTemperatureLabel
            // 
            CPUTemperatureLabel.AutoSize = true;
            CPUTemperatureLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            CPUTemperatureLabel.Location = new Point(805, 50);
            CPUTemperatureLabel.Name = "CPUTemperatureLabel";
            CPUTemperatureLabel.Size = new Size(65, 13);
            CPUTemperatureLabel.TabIndex = 41;
            CPUTemperatureLabel.Text = "CPU 온도 : ";
            // 
            // CPUUsageTextBox
            // 
            CPUUsageTextBox.Font = new Font("나눔스퀘어라운드 Regular", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            CPUUsageTextBox.Location = new Point(897, 15);
            CPUUsageTextBox.Name = "CPUUsageTextBox";
            CPUUsageTextBox.ReadOnly = true;
            CPUUsageTextBox.Size = new Size(95, 21);
            CPUUsageTextBox.TabIndex = 40;
            CPUUsageTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // CPUUsageLabel
            // 
            CPUUsageLabel.AutoSize = true;
            CPUUsageLabel.Font = new Font("나눔스퀘어라운드 Bold", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            CPUUsageLabel.Location = new Point(805, 23);
            CPUUsageLabel.Name = "CPUUsageLabel";
            CPUUsageLabel.Size = new Size(76, 13);
            CPUUsageLabel.TabIndex = 39;
            CPUUsageLabel.Text = "CPU 사용량 : ";
            // 
            // DBServer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1072, 450);
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
            Controls.Add(ServerStatusTextBox);
            Controls.Add(GameServerStatusTextBox);
            Controls.Add(ServerStartButton);
            Controls.Add(ServerStopButton);
            Controls.Add(LogListBox);
            Name = "DBServer";
            Text = "DBServer";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TextBox ServerStatusTextBox;
        private TextBox GameServerStatusTextBox;
        private Button ServerStartButton;
        private Button ServerStopButton;
        private ListBox LogListBox;
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
    }
}
