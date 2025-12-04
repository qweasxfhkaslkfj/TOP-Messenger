using System;
using System.Windows.Forms;

namespace TOP_Messenger
{
   
    public partial class FormClient : Form
    {
        public FormClient()
        {
            InitializeComponent();
            SetupInterfaceByRole();
        }
        public void AddFileToHistory(string fileName)
        {
            FlowLayoutPanel filePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 5)
            };

            Label fileLabel = new Label
            {
                Text = fileName,
                AutoSize = true,
                Margin = new Padding(5, 0, 5, 0),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            Button downloadButton = new Button
            {
                Text = "Скачать",
                Tag = fileName, 
                Margin = new Padding(5, 0, 5, 0),
                Padding = new Padding(5)
            };

            downloadButton.Click += (sender, e) =>
            {
                string fileToDownload = (string)((Button)sender).Tag;

                MessageBox.Show($"Скачивание файла: {fileToDownload}");
            };

            filePanel.Controls.Add(fileLabel);
            filePanel.Controls.Add(downloadButton);

            panelHistoryFiles.Controls.Add(filePanel);
        }

        private void SetupInterfaceByRole()
        {
            bool isServer = Registration.IsCurrentUserServer();
            bool isGuest = Registration.IsCurrentUserGuest();
            string login = Registration.GetCurrentLogin();

            buttonPlayGame.Enabled = !isGuest && !isServer;
            buttonFile.Enabled = !isGuest;

            // Проверка на сервер или гостя или логин
            btnUserAnanas.Enabled = !isServer && !isGuest && login != "ananas";
            btnUserCat_Noir.Enabled = !isServer && !isGuest && login != "cat_noir";
            btnUserDenden.Enabled = !isServer && !isGuest && login != "denden";
            btnUserKrs.Enabled = !isServer && !isGuest && login != "krs333";
            btnUserLady_Bug.Enabled = !isServer && !isGuest && login != "lady_bug";
            btnUserLushPush.Enabled = !isServer && !isGuest && login != "lushPush";
            btnUserPagan.Enabled = !isServer && !isGuest && login != "Pagan821";
            btnUserSiles.Enabled = !isServer && !isGuest && login != "Siles";
            btnUserTabeer.Enabled = !isServer && !isGuest && login != "tabeer";
            btnUserVld.Enabled = !isServer && !isGuest && login != "vld666";
            buttonUserUSF.Enabled = !isServer && !isGuest && login != "USF055";


            /*//Блокировка для сервера и для сервера
            if (isServer)
            {
                buttonPlayGame.Enabled = !isServer;

                btnUserAnanas.Enabled = !isServer;
                btnUserCat_Noir.Enabled = !isServer;
                btnUserDenden.Enabled = !isServer;
                btnUserKrs.Enabled = !isServer;
                btnUserLady_Bug.Enabled = !isServer;
                btnUserLushPush.Enabled = !isServer;
                btnUserPagan.Enabled = !isServer;
                btnUserSiles.Enabled = !isServer;
                btnUserTabeer.Enabled = !isServer;
                btnUserVld.Enabled = !isServer;
                buttonUserUSF.Enabled = !isServer;
            }
            //Блокировка для гостя
            else if (isGuest)
            {
                buttonPlayGame.Enabled = !isGuest;
                buttonFile.Enabled = !isGuest;

                btnUserAnanas.Enabled = !isGuest;
                btnUserCat_Noir.Enabled = !isGuest;
                btnUserDenden.Enabled = !isGuest;
                btnUserKrs.Enabled = !isGuest;
                btnUserLady_Bug.Enabled = !isGuest;
                btnUserLushPush.Enabled = !isGuest;
                btnUserPagan.Enabled = !isGuest;
                btnUserSiles.Enabled = !isGuest;
                btnUserTabeer.Enabled = !isGuest;
                btnUserVld.Enabled = !isGuest;
                buttonUserUSF.Enabled = !isGuest;
            }
            else if (login == 
            /*else
            {
                buttonPlayGame.Enabled = true;
                buttonFile.Enabled = true;
                btnUserAnanas.Enabled = true;
                btnUserCat_Noir.Enabled = true;
                btnUserDenden.Enabled = true;
                btnUserKrs.Enabled = true;
                btnUserLady_Bug.Enabled = true;
                btnUserLushPush.Enabled = true;
                btnUserPagan.Enabled = true;
                btnUserSiles.Enabled = true;
                btnUserTabeer.Enabled = true;
                btnUserVld.Enabled = true;
                buttonUserUSF.Enabled = true;
            }*/
            
            
        }

        private void FormClient_Load(object sender, EventArgs e)
        {
            // Временно добавьте для проверки
            MessageBox.Show(
                $"Текущий пользователь:\n" +
                $"Логин: {Registration.CurrentLogin}\n" +
                $"Гость: {Registration.IsGuest}\n" +
                $"Сервер: {Registration.IsServer}\n" +
                $"Роль: {Registration.CurrentRole}",
                "Отладка");
        }
    }
}
