namespace TOP_Messenger
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListBox lstMessages;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Button btnStartServer;
        private System.Windows.Forms.TextBox txtIP;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnChangeName;



        private void InitializeComponent()
        {
            this.lstMessages = new System.Windows.Forms.ListBox();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.btnStartServer = new System.Windows.Forms.Button();
            this.txtIP = new System.Windows.Forms.TextBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.btnChangeName = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // lstMessages
            this.lstMessages.FormattingEnabled = true;
            this.lstMessages.ItemHeight = 16;
            this.lstMessages.Location = new System.Drawing.Point(12, 41);
            this.lstMessages.Name = "lstMessages";
            this.lstMessages.Size = new System.Drawing.Size(560, 340);
            this.lstMessages.TabIndex = 0;

            // txtMessage
            this.txtMessage.Location = new System.Drawing.Point(12, 390);
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.Size = new System.Drawing.Size(460, 22);
            this.txtMessage.TabIndex = 1;
            this.txtMessage.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtMessage_KeyPress);

            // btnSend
            this.btnSend.Location = new System.Drawing.Point(478, 389);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(94, 25);
            this.btnSend.TabIndex = 2;
            this.btnSend.Text = "Отправить";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);

            // btnStartServer
            this.btnStartServer.Location = new System.Drawing.Point(12, 12);
            this.btnStartServer.Name = "btnStartServer";
            this.btnStartServer.Size = new System.Drawing.Size(120, 23);
            this.btnStartServer.TabIndex = 3;
            this.btnStartServer.Text = "Запустить сервер";
            this.btnStartServer.UseVisualStyleBackColor = true;
            this.btnStartServer.Click += new System.EventHandler(this.btnStartServer_Click);

            // txtIP
            this.txtIP.Location = new System.Drawing.Point(138, 13);
            this.txtIP.Name = "txtIP";
            this.txtIP.Size = new System.Drawing.Size(150, 22);
            this.txtIP.TabIndex = 4;
            this.txtIP.Text = "127.0.0.1";

            // btnConnect
            this.btnConnect.Location = new System.Drawing.Point(294, 12);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(94, 23);
            this.btnConnect.TabIndex = 5;
            this.btnConnect.Text = "Подключиться";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);

            // label1
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(394, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(178, 16);
            this.label1.TabIndex = 6;
            this.label1.Text = "(IP другого пользователя)";

            // btnChangeName
            this.btnChangeName.Location = new System.Drawing.Point(478, 12);
            this.btnChangeName.Name = "btnChangeName";
            this.btnChangeName.Size = new System.Drawing.Size(94, 23);
            this.btnChangeName.TabIndex = 7;
            this.btnChangeName.Text = "Сменить имя";
            this.btnChangeName.UseVisualStyleBackColor = true;
            this.btnChangeName.Click += new System.EventHandler(this.btnChangeName_Click);

            // Form1
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 426);
            this.Controls.Add(this.btnChangeName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.txtIP);
            this.Controls.Add(this.btnStartServer);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.txtMessage);
            this.Controls.Add(this.lstMessages);
            this.Name = "Form1";
            this.Text = "Чат";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}