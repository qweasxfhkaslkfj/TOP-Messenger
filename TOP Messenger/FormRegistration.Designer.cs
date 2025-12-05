namespace TOP_Messenger
{
    partial class FormRegistration
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelProsba = new System.Windows.Forms.Label();
            this.textBoxLogin = new System.Windows.Forms.TextBox();
            this.labelLogin = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxPassword = new System.Windows.Forms.TextBox();
            this.buttonVhod = new System.Windows.Forms.Button();
            this.buttonZahodGosta = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // labelProsba
            // 
            this.labelProsba.AutoSize = true;
            this.labelProsba.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelProsba.Location = new System.Drawing.Point(136, 9);
            this.labelProsba.Name = "labelProsba";
            this.labelProsba.Size = new System.Drawing.Size(329, 24);
            this.labelProsba.TabIndex = 0;
            this.labelProsba.Text = "Пожалуйста, войдите в систему";
            // 
            // textBoxLogin
            // 
            this.textBoxLogin.Location = new System.Drawing.Point(210, 87);
            this.textBoxLogin.Name = "textBoxLogin";
            this.textBoxLogin.Size = new System.Drawing.Size(161, 20);
            this.textBoxLogin.TabIndex = 1;
            this.textBoxLogin.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxLogin_KeyDown);
            // 
            // labelLogin
            // 
            this.labelLogin.AutoSize = true;
            this.labelLogin.Location = new System.Drawing.Point(239, 71);
            this.labelLogin.Name = "labelLogin";
            this.labelLogin.Size = new System.Drawing.Size(108, 13);
            this.labelLogin.TabIndex = 2;
            this.labelLogin.Text = "Введите свой логин";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(239, 136);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(115, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Введите свой пароль";
            // 
            // textBoxPassword
            // 
            this.textBoxPassword.Location = new System.Drawing.Point(210, 152);
            this.textBoxPassword.Name = "textBoxPassword";
            this.textBoxPassword.Size = new System.Drawing.Size(161, 20);
            this.textBoxPassword.TabIndex = 4;
            this.textBoxPassword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxPassword_KeyDown);
            // 
            // buttonVhod
            // 
            this.buttonVhod.Location = new System.Drawing.Point(154, 196);
            this.buttonVhod.Name = "buttonVhod";
            this.buttonVhod.Size = new System.Drawing.Size(117, 32);
            this.buttonVhod.TabIndex = 5;
            this.buttonVhod.Text = "Войти";
            this.buttonVhod.UseVisualStyleBackColor = true;
            this.buttonVhod.Click += new System.EventHandler(this.buttonVhod_Click);
            // 
            // buttonZahodGosta
            // 
            this.buttonZahodGosta.Location = new System.Drawing.Point(316, 196);
            this.buttonZahodGosta.Name = "buttonZahodGosta";
            this.buttonZahodGosta.Size = new System.Drawing.Size(114, 32);
            this.buttonZahodGosta.TabIndex = 6;
            this.buttonZahodGosta.Text = "Войти как гость";
            this.buttonZahodGosta.UseVisualStyleBackColor = true;
            this.buttonZahodGosta.Click += new System.EventHandler(this.buttonZahodGosta_Click);
            // 
            // FormRegistration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 261);
            this.Controls.Add(this.buttonZahodGosta);
            this.Controls.Add(this.buttonVhod);
            this.Controls.Add(this.textBoxPassword);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.labelLogin);
            this.Controls.Add(this.textBoxLogin);
            this.Controls.Add(this.labelProsba);
            this.MaximumSize = new System.Drawing.Size(600, 300);
            this.MinimumSize = new System.Drawing.Size(600, 300);
            this.Name = "FormRegistration";
            this.Text = "Вход";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelProsba;
        private System.Windows.Forms.TextBox textBoxLogin;
        private System.Windows.Forms.Label labelLogin;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxPassword;
        private System.Windows.Forms.Button buttonVhod;
        private System.Windows.Forms.Button buttonZahodGosta;
    }
}