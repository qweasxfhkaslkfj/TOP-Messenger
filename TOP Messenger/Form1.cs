using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace TOP_Messenger
{
    public partial class FormClient : Form
    {
        private ChatServer chatServer;
        private TcpClient tcpClient;
        private StreamReader reader;
        private StreamWriter writer;
        private Thread receiveThread;
        private bool isConnected = false;
        private string serverIP = "192.168.88.145";
        private int serverPort = 8888;
        private Color currentUserColor = Color.Black;

        // Новые поля для работы с файлами
        private List<string> selectedFilePaths = new List<string>();
        private List<string> chatFiles = new List<string>();
        private Dictionary<string, Color> userColors = new Dictionary<string, Color>();

        public FormClient()
        {
            InitializeComponent();

            listBoxChat.DrawMode = DrawMode.OwnerDrawVariable;
            listBoxChat.DrawItem += ListBoxChat_DrawItem;
            listBoxChat.MeasureItem += ListBoxChat_MeasureItem;

            this.Resize += FormClient_Resize;
            this.ResizeBegin += (sender, e) => { panelHistoryFiles.SuspendLayout(); };
            this.ResizeEnd += (sender, e) => {
                panelHistoryFiles.ResumeLayout();
                ArrangeFilePanels();
            };

            listBoxChat.IntegralHeight = false;
            listBoxChat.ScrollAlwaysVisible = true;

            // Инициализация панели файлов
            InitializeFilePanel();

            // Инициализация цветов пользователей по умолчанию
            InitializeDefaultUserColors();

            SetupInterfaceByRole();

            // Обработчик изменения размера для панели файлов
            panelHistoryFiles.Resize += (sender, e) =>
            {
                ArrangeFilePanels();
            };

            // Проверка на сервер
            if (Registration.IsCurrentUserServer())
            {
                chatServer = new ChatServer();
                chatServer.MessageLogged += (message) =>
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action<string>(msg => AddServerLog(msg)), message);
                    }
                    else
                    {
                        AddServerLog(message);
                    }
                };

                string localIP = "192.168.88.145";
                serverIP = localIP;
                chatServer.Start(localIP, serverPort);
                AddServerLog($"Сервер запущен на {localIP}:{serverPort}");

                ConnectToServer();
            }
        }

        // Инициализация цветов пользователей по умолчанию
        private void InitializeDefaultUserColors()
        {
            userColors.Clear();

            // Устанавливаем цвета для всех пользователей
            userColors.Add("krs333", Color.DarkOrange);
            userColors.Add("Pagan821", Color.Pink);
            userColors.Add("denden", Color.DarkGreen);
            userColors.Add("cat_noir", Color.Black);
            userColors.Add("lady_bug", Color.DarkRed);
            userColors.Add("tabeer", Color.Brown);
            userColors.Add("lushPush", Color.DarkViolet);
            userColors.Add("Siles", Color.DarkSlateBlue);
            userColors.Add("USF055", Color.MidnightBlue);
            userColors.Add("vld666", Color.Maroon);
            userColors.Add("ananas", Color.Purple);
            userColors.Add("server", Color.Black);

            // Добавляем цвета для гостей
            for (int i = 1; i <= 10; i++)
            {
                userColors.Add($"Guest#{i}", GetGuestColor(i));
            }
        }

        // Получение цвета для гостя
        private Color GetGuestColor(int guestNumber)
        {
            Color[] guestColors = new Color[]
            {
                Color.DarkRed,
                Color.DarkBlue,
                Color.DarkGreen,
                Color.DarkMagenta,
                Color.DarkCyan,
                Color.DarkOrange,
                Color.DarkViolet,
                Color.DarkSlateBlue,
                Color.MidnightBlue,
                Color.Maroon
            };

            return guestColors[(guestNumber - 1) % guestColors.Length];
        }

        private void InitializeFilePanel()
        {
            // Простая инициализация для обычного Panel
            panelHistoryFiles.AutoScroll = true;
            panelHistoryFiles.BackColor = Color.WhiteSmoke;
            panelHistoryFiles.BorderStyle = BorderStyle.FixedSingle;

            // Устанавливаем правильные свойства для ScrollBar
            panelHistoryFiles.HorizontalScroll.Enabled = false;
            panelHistoryFiles.HorizontalScroll.Visible = false;
            panelHistoryFiles.HorizontalScroll.Maximum = 0;
            panelHistoryFiles.AutoScroll = true;
            panelHistoryFiles.VerticalScroll.Visible = true;
            panelHistoryFiles.VerticalScroll.Enabled = true;

            // Включаем автоматическую прокрутку
            panelHistoryFiles.AutoScroll = true;
            panelHistoryFiles.AutoScrollMinSize = new Size(0, 0);

            // Очищаем панель при инициализации
            panelHistoryFiles.Controls.Clear();
        }

        // Метод для упорядочивания панелей файлов
        private void ArrangeFilePanels()
        {
            try
            {
                if (panelHistoryFiles.InvokeRequired)
                {
                    panelHistoryFiles.Invoke(new Action(ArrangeFilePanels));
                    return;
                }

                int yPos = 5;
                // Ширина панели с учетом полосы прокрутки
                int panelWidth = panelHistoryFiles.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 5;

                foreach (Control control in panelHistoryFiles.Controls)
                {
                    if (control is Panel filePanel)
                    {
                        filePanel.Top = yPos;
                        filePanel.Width = Math.Max(panelWidth, 100); // Минимальная ширина
                        yPos += filePanel.Height + filePanel.Margin.Top + filePanel.Margin.Bottom;
                    }
                }

                int totalHeight = yPos + 10;
                panelHistoryFiles.AutoScrollMinSize = new Size(0, totalHeight);

                panelHistoryFiles.PerformLayout();
                panelHistoryFiles.Refresh();

                if (totalHeight > panelHistoryFiles.ClientSize.Height)
                {
                    panelHistoryFiles.VerticalScroll.Visible = true;
                    panelHistoryFiles.VerticalScroll.Enabled = true;
                    Console.WriteLine($"Прокрутка включена. Высота контента: {totalHeight}, высота панели: {panelHistoryFiles.ClientSize.Height}");
                }
                else
                {
                    Console.WriteLine($"Прокрутка не нужна. Высота контента: {totalHeight}, высота панели: {panelHistoryFiles.ClientSize.Height}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в ArrangeFilePanels: {ex.Message}");
            }
        }
        //  обработчик для изменения размера формы
        private void FormClient_Resize(object sender, EventArgs e)
        {
            listBoxChat.BeginUpdate();
            for (int i = 0; i < listBoxChat.Items.Count; i++)
            {
                listBoxChat.Invalidate(listBoxChat.GetItemRectangle(i));
            }
            listBoxChat.EndUpdate();
            listBoxChat.Refresh();

            // обновляем расположение панелей файлов при изменении размера
            ArrangeFilePanels();

            // обновляем скроллбар
            panelHistoryFiles.PerformLayout();
            panelHistoryFiles.Refresh();
        }

        private void AddServerLog(string message)
        {
            // Вместо пустого метода добавляем сообщение в чат
            if (InvokeRequired)
            {
                Invoke(new Action<string>(AddServerLog), message);
                return;
            }

            // Добавляем системное сообщение в чат
            AddChatMessage($"[Сервер]: {message}");
        }

        private void AddChatMessage(string message)
        {
            listBoxChat.Items.Add(message);
            listBoxChat.TopIndex = listBoxChat.Items.Count - 1;
            listBoxChat.Invalidate();
        }

        private void AddColoredChatMessage(string message, Color color)
        {
            listBoxChat.Items.Add(message);
            listBoxChat.TopIndex = listBoxChat.Items.Count - 1;
        }

        private void AddOwnMessage(string message)
        {
            string ownMessage = $"[Вы]: {message}";
            listBoxChat.Items.Add(ownMessage);
            listBoxChat.TopIndex = listBoxChat.Items.Count - 1;
            listBoxChat.Invalidate();
        }

        // метод для добавления файла в историю
        public void AddFileToHistory(string fileName, long fileSize = 0)
        {
            try
            {
                if (panelHistoryFiles.InvokeRequired)
                {
                    panelHistoryFiles.Invoke(new Action<string, long>(AddFileToHistory), fileName, fileSize);
                    return;
                }

                // Проверяем, не добавлен ли уже файл
                bool alreadyAdded = false;
                foreach (Control control in panelHistoryFiles.Controls)
                {
                    if (control is Panel panel && panel.Tag != null && panel.Tag.ToString() == fileName)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (alreadyAdded)
                {
                    Console.WriteLine($"Файл уже отображается: {fileName}");
                    return;
                }

                // Добавляем файл в список
                if (!chatFiles.Contains(fileName))
                {
                    chatFiles.Add(fileName);
                    Console.WriteLine($"Добавлен файл в список: {fileName}");
                }

                // Ширина с учетом скроллбара
                int panelWidth = panelHistoryFiles.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 10;

                // Создаем простую панель для файла
                Panel filePanel = new Panel
                {
                    Width = Math.Max(panelWidth, 100), // Минимальная ширина
                    Height = 45,
                    Margin = new Padding(5, 5, 5, 5),
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand,
                    Tag = fileName // Сохраняем имя файла в Tag
                };

                // Укорачиваем имя файла для отображения
                string displayName = Path.GetFileName(fileName);
                if (displayName.Length > 25)
                {
                    displayName = displayName.Substring(0, 22) + "...";
                }

                // Название файла
                Label fileNameLabel = new Label
                {
                    Text = displayName,
                    AutoSize = false,
                    Width = filePanel.Width - 90,
                    Height = 20,
                    Left = 10,
                    Top = 5,
                    Font = new Font("Arial", 9, FontStyle.Bold),
                    ForeColor = Color.Black,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Tag = fileName,
                    Cursor = Cursors.Hand
                };

                // Информация о файле
                string fileInfo = "";
                if (fileSize > 0)
                {
                    fileInfo = FormatFileSize(fileSize);
                }
                else
                {
                    fileInfo = "Размер неизвестен";
                }

                Label fileInfoLabel = new Label
                {
                    Text = fileInfo,
                    AutoSize = false,
                    Width = filePanel.Width - 90,
                    Height = 15,
                    Left = 10,
                    Top = 25,
                    Font = new Font("Arial", 8),
                    ForeColor = Color.DarkGray,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Tag = fileName,
                    Cursor = Cursors.Hand
                };

                // Кнопка "Сохранить"
                Button saveBtn = new Button
                {
                    Text = "Сохранить",
                    Width = 70,
                    Height = 30,
                    Left = filePanel.Width - 75,
                    Top = 7,
                    Tag = fileName,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.LightBlue,
                    Font = new Font("Arial", 8),
                    Cursor = Cursors.Hand
                };

                // Обработчики событий
                saveBtn.Click += (sender, e) =>
                {
                    string fileToSave = (string)((Button)sender).Tag;
                    SaveFile(fileToSave);
                };

                fileNameLabel.Click += (sender, e) =>
                {
                    string fileToSave = (string)((Label)sender).Tag;
                    SaveFile(fileToSave);
                };

                fileInfoLabel.Click += (sender, e) =>
                {
                    string fileToSave = (string)((Label)sender).Tag;
                    SaveFile(fileToSave);
                };

                filePanel.Click += (sender, e) =>
                {
                    string fileToSave = (string)((Panel)sender).Tag;
                    SaveFile(fileToSave);
                };

                // Добавляем элементы на панель
                filePanel.Controls.Add(fileNameLabel);
                filePanel.Controls.Add(fileInfoLabel);
                filePanel.Controls.Add(saveBtn);

                // Добавляем панель в панель истории файлов
                panelHistoryFiles.Controls.Add(filePanel);

                // Обновляем расположение всех панелей
                ArrangeFilePanels();

                // Принудительно обновляем отображение и скроллбар
                panelHistoryFiles.PerformLayout();
                panelHistoryFiles.Refresh();

                Console.WriteLine($"Файловая панель добавлена: {fileName}. Всего панелей: {panelHistoryFiles.Controls.Count}. Ширина панели: {filePanel.Width}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в AddFileToHistory: {ex.Message}");
            }
        }
        private void SaveFile(string fileName)
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.FileName = Path.GetFileName(fileName);
                saveDialog.Filter = "Все файлы (*.*)|*.*";
                saveDialog.Title = "Сохранить файл";
                saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Создаем тестовый файл с информацией
                        File.WriteAllText(saveDialog.FileName,
                            $"Файл из чата TOP Messenger\n" +
                            $"Название: {fileName}\n" +
                            $"Отправитель: {Registration.GetCurrentLogin()}\n" +
                            $"Время сохранения: {DateTime.Now}\n\n" +
                            $"Это демонстрационный файл. В реальном приложении\n" +
                            $"здесь будет содержимое исходного файла.");

                        MessageBox.Show($"Файл сохранен как: {saveDialog.FileName}",
                            "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        AddChatMessage($"Файл '{Path.GetFileName(fileName)}' сохранен");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения файла: {ex.Message}",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
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
        }

        private void ConnectToServer()
        {
            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(serverIP, serverPort);

                NetworkStream stream = tcpClient.GetStream();
                reader = new StreamReader(stream, Encoding.Unicode);
                writer = new StreamWriter(stream, Encoding.Unicode) { AutoFlush = true };

                // Отправляем имя пользователя серверу
                writer.WriteLine(Registration.GetCurrentLogin());

                isConnected = true;

                // Запускаем поток для получения сообщений
                receiveThread = new Thread(new ThreadStart(ReceiveMessages));
                receiveThread.IsBackground = true;
                receiveThread.Start();

                AddChatMessage("Подключено к серверу чата");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
                isConnected = false;
            }
        }

        private void ReceiveMessages()
        {
            try
            {
                while (isConnected)
                {
                    string message = reader.ReadLine();
                    if (message == null) break;

                    if (InvokeRequired)
                    {
                        Invoke(new Action<string>(ProcessMessage), message);
                    }
                    else
                    {
                        ProcessMessage(message);
                    }
                }
            }
            catch (IOException)
            {
                if (isConnected)
                {
                    Invoke(new Action(() => AddChatMessage("Соединение с сервером потеряно")));
                }
            }
            catch (Exception ex)
            {
                if (isConnected)
                {
                    Invoke(new Action(() => AddChatMessage($"Ошибка получения: {ex.Message}")));
                }
            }
            finally
            {
                isConnected = false;
            }
        }

        private void ProcessMessage(string message)
        {
            // Обработка сообщений от сервера
            if (message.StartsWith("COLOR:"))
            {
                // Разбираем цветное сообщение
                int pipeIndex = message.IndexOf('|');
                if (pipeIndex > 0)
                {
                    string colorPart = message.Substring(6, pipeIndex - 6);
                    string textPart = message.Substring(pipeIndex + 1);

                    if (int.TryParse(colorPart, out int argb))
                    {
                        Color messageColor = Color.FromArgb(argb);
                        SaveUserColorFromMessage(textPart, messageColor);

                        // Проверяем, является ли сообщение о файле
                        if (textPart.Contains("[ФАЙЛ от"))
                        {
                            ExtractFileNameFromMessage(textPart);
                        }

                        AddChatMessage(textPart);
                    }
                }
            }
            else if (message.StartsWith("YOUR_COLOR:"))
            {
                // Сообщение с цветом текущего пользователя
                string colorPart = message.Substring(11);
                if (int.TryParse(colorPart, out int argb))
                {
                    currentUserColor = Color.FromArgb(argb);
                    string login = Registration.GetCurrentLogin();
                    SaveUserColor(login, currentUserColor);
                }
            }
            else if (message.StartsWith("HISTORY:"))
            {
                // Получаем историю с сервера
                string historyMessage = message.Substring(8);
                AddChatMessageFromHistory(historyMessage);
            }
            else if (message.Contains("[ФАЙЛ от"))
            {
                // Сообщение о файле без цвета
                ExtractFileNameFromMessage(message);
                AddChatMessage(message);
            }
            else
            {
                // Обычное сообщение без цвета
                AddChatMessage(message);
            }
        }

        // Метод для добавления сообщений из истории сервера
        private void AddChatMessageFromHistory(string historyLine)
        {
            // Парсим строку истории формата: "[yyyy-MM-dd HH:mm] [sender] message"
            try
            {
                if (historyLine.Contains("["))
                {
                    // Извлекаем время
                    int timeStart = historyLine.IndexOf('[');
                    int timeEnd = historyLine.IndexOf(']');

                    if (timeStart >= 0 && timeEnd > timeStart)
                    {
                        string time = historyLine.Substring(timeStart + 1, timeEnd - timeStart - 1);

                        // Проверяем, есть ли отправитель
                        int senderStart = historyLine.IndexOf('[', timeEnd + 1);
                        int senderEnd = historyLine.IndexOf(']', senderStart);

                        if (senderStart > 0 && senderEnd > senderStart)
                        {
                            // Есть отправитель
                            string sender = historyLine.Substring(senderStart + 1, senderEnd - senderStart - 1);
                            string message = historyLine.Substring(senderEnd + 1).Trim();

                            string displayMessage = $"[{sender}]: {message}";
                            AddChatMessage(displayMessage);
                        }
                        else
                        {
                            // Нет отправителя (системное сообщение)
                            string message = historyLine.Substring(timeEnd + 1).Trim();
                            AddChatMessage(message);
                        }
                    }
                }
                else
                {
                    AddChatMessage(historyLine);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка парсинга истории: {ex.Message}");
                AddChatMessage(historyLine);
            }
        }

        private void ExtractFileNameFromMessage(string message)
        {
            try
            {
                Console.WriteLine($"Извлекаю имя файла из сообщения: {message}");

                // Паттерн: [ФАЙЛ от пользователь]: имя_файла (размер)
                if (message.Contains("]: "))
                {
                    int start = message.IndexOf("]: ") + 3;
                    int end = message.IndexOf(" (");

                    string fileName = "";
                    long fileSize = 0;

                    if (end > start && start > 0)
                    {
                        // Извлекаем имя файла
                        fileName = message.Substring(start, end - start).Trim();

                        // Извлекаем размер файла
                        int sizeStart = message.IndexOf(" (");
                        int sizeEnd = message.IndexOf(")");
                        if (sizeStart > 0 && sizeEnd > sizeStart)
                        {
                            string sizeStr = message.Substring(sizeStart + 2, sizeEnd - sizeStart - 2);
                            Console.WriteLine($"Размер файла в строке: {sizeStr}");

                            // Убираем единицы измерения и пробелы
                            sizeStr = sizeStr.Replace(" ", "").Replace("Б", "").Replace("КБ", "").Replace("МБ", "").Trim();

                            if (long.TryParse(sizeStr, out long parsedSize))
                            {
                                fileSize = parsedSize;
                                Console.WriteLine($"Размер файла: {fileSize}");
                            }
                        }
                    }
                    else if (start > 0)
                    {
                        // Если нет размера, просто берем все после "]: "
                        fileName = message.Substring(start).Trim();
                    }

                    if (!string.IsNullOrEmpty(fileName))
                    {
                        Console.WriteLine($"Найден файл: {fileName}");
                        // Добавляем файл в историю
                        AddFileToHistory(fileName, fileSize);
                    }
                }
                else if (message.Contains("Отправил файл:"))
                {
                    // Обработка сообщения "Отправил файл: filename"
                    int start = message.IndexOf("Отправил файл:") + 14;
                    if (start > 0)
                    {
                        string fileName = message.Substring(start).Trim();
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            Console.WriteLine($"Найден файл из собственного сообщения: {fileName}");
                            AddFileToHistory(fileName, 0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка извлечения имени файла: {ex.Message}");
            }
        }

        private void SendMessageToServer()
        {
            if (!isConnected || writer == null)
            {
                MessageBox.Show("Нет подключения к серверу", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string message = textBoxMessage.Text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            try
            {
                AddOwnMessage(message);

                writer.WriteLine(message);

                textBoxMessage.Clear();
                textBoxMessage.Focus();
            }
            catch (Exception ex)
            {
                AddChatMessage($"Ошибка отправки: {ex.Message}");
                isConnected = false;
            }

        }

        private void UpdateUserColorsInListBox()
        {
            if (listBoxChat.Items.Count > 0)
            {
                listBoxChat.BeginUpdate();
                for (int i = 0; i < listBoxChat.Items.Count; i++)
                {
                    listBoxChat.Invalidate(listBoxChat.GetItemRectangle(i));
                }
                listBoxChat.EndUpdate();
            }
        }

        private void SaveUserColor(string username, Color color)
        {
            if (!userColors.ContainsKey(username))
            {
                userColors.Add(username, color);
            }
            else
            {
                userColors[username] = color;
            }

            UpdateUserColorsInListBox();
        }

        private string ExtractUsernameFromMessage(string message)
        {
            // Извлекаем имя пользователя из сообщения
            if (message.Contains("["))
            {
                int start = message.IndexOf('[') + 1;
                int end = message.IndexOf(']');
                if (start > 0 && end > start)
                {
                    return message.Substring(start, end - start);
                }
            }
            else if (message.Contains("подключился к чату"))
            {
                int end = message.IndexOf(" подключился к чату");
                if (end > 0)
                {
                    return message.Substring(0, end);
                }
            }
            else if (message.Contains("покинул чат"))
            {
                int end = message.IndexOf(" покинул чат");
                if (end > 0)
                {
                    return message.Substring(0, end);
                }
            }

            return string.Empty;
        }

         private void SaveUserColorFromMessage(string message, Color color)
        {
            string username = ExtractUsernameFromMessage(message);
            if (!string.IsNullOrEmpty(username))
            {
                SaveUserColor(username, color);
            }
        }

        private void ListBoxChat_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= listBoxChat.Items.Count)
                return;

            string text = listBoxChat.Items[e.Index].ToString();

            SizeF textSize = e.Graphics.MeasureString(
                text,
                listBoxChat.Font,
                listBoxChat.Width - 20,
                StringFormat.GenericTypographic
            );

            e.ItemHeight = (int)textSize.Height + 4;
            e.ItemWidth = listBoxChat.Width;
        }

        private void ListBoxChat_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= listBoxChat.Items.Count)
                return;

            e.DrawBackground();

            string text = listBoxChat.Items[e.Index].ToString();
            Color color = Color.Black;
            bool isFileMessage = false;

            // Проверка на сообщения о файлах
            if (text.Contains("[ФАЙЛ от"))
            {
                color = Color.DarkGreen;
                isFileMessage = true;
                e.Graphics.FillRectangle(new SolidBrush(Color.LightYellow), e.Bounds);
            }
            else if (text.Contains("Выбран файл:") || text.Contains("Отправляю файл:") || text.Contains("Файл отправлен"))
            {
                color = Color.DarkBlue;
            }
            else if (text.Contains("Server"))
            {
                color = Color.DarkRed;
            }
            else if (text.Contains("Ваш цвет"))
            {
                color = Color.Green;
            }
            else if (text.Contains("Подключено"))
            {
                color = Color.Blue;
            }
            else if (text.StartsWith("[Вы]:"))
            {
                color = currentUserColor;
            }
            else if (text.Contains("подключился к чату") || text.Contains("покинул чат"))
            {
                string username = ExtractUsernameFromMessage(text);
                if (!string.IsNullOrEmpty(username))
                {
                    if (userColors.ContainsKey(username))
                    {
                        color = userColors[username];
                    }
                    else
                    {
                        color = Color.DarkGray;
                    }
                }
                else
                {
                    color = Color.DarkGray;
                }
            }
            else if (text.StartsWith("["))
            {
                string username = ExtractUsernameFromMessage(text);
                if (!string.IsNullOrEmpty(username))
                {
                    if (userColors.ContainsKey(username))
                    {
                        color = userColors[username];
                    }
                    else
                    {
                        // Проверяем известных пользователей
                        if (username.Contains("krs333") || username.Contains("Pagan821"))
                            color = Color.DarkRed;
                        else if (username.Contains("cat_noir"))
                            color = Color.Black;
                        else if (username.Contains("denden"))
                            color = Color.DarkGreen;
                        else if (username.Contains("lady_bug"))
                            color = Color.DarkCyan;
                        else if (username.Contains("tabeer"))
                            color = Color.DarkOrange;
                        else if (username.Contains("lushPush"))
                            color = Color.DarkViolet;
                        else if (username.Contains("Siles"))
                            color = Color.DarkSlateBlue;
                        else if (username.Contains("USF055"))
                            color = Color.MidnightBlue;
                        else if (username.Contains("vld666"))
                            color = Color.Maroon;
                        else if (username.Contains("ananas"))
                            color = Color.Purple;
                        else
                            color = Color.DarkSlateGray;
                    }
                }
                else
                {
                    color = Color.DarkSlateGray;
                }
            }

            // Создаем формат с переносом слов
            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Near;
            format.LineAlignment = StringAlignment.Near;
            format.FormatFlags = StringFormatFlags.LineLimit;
            format.Trimming = StringTrimming.Word;

            // Создаем прямоугольник для текста с отступами
            Rectangle textRect = new Rectangle(
                e.Bounds.X + 2,
                e.Bounds.Y + 2,
                e.Bounds.Width - 4,
                e.Bounds.Height - 4
            );

            using (SolidBrush brush = new SolidBrush(color))
            {
                e.Graphics.DrawString(text, e.Font, brush, textRect, format);
            }

            e.DrawFocusRectangle();
        }

        private void FormClient_Load(object sender, EventArgs e)
        {
            if (Registration.IsCurrentUserServer())
            {
                LoadServerChatHistory();
            }

            if (!Registration.IsCurrentUserServer())
            {
                ConnectToServer();
            }
        }

        private void LoadServerChatHistory()
        {
            try
            {
                // Только сервер имеет локальный файл истории
                if (Registration.IsCurrentUserServer() && chatServer != null)
                {
                    string historyFile = chatServer.GetLogFilePath();

                    if (File.Exists(historyFile))
                    {
                        var lines = File.ReadAllLines(historyFile);

                        foreach (var line in lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line) &&
                                !line.Contains("=== Начало лога") &&
                                !line.Contains("=== Сессия"))
                            {
                                AddChatMessageFromHistory(line);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки истории сервера: {ex.Message}");
            }
        }


        private void LoadChatHistory()
        {
            try
            {
                var history = SavingMessage.LoadChatHistory(100000);

                foreach (var message in history)
                {
                    // Парсим строку истории для отображения
                    if (message.Contains("[") && message.Contains("]"))
                    {
                        // Извлекаем само сообщение (после второго ']')
                        int secondBracket = message.IndexOf(']', message.IndexOf(']') + 1);
                        if (secondBracket > 0)
                        {
                            string messageText = message.Substring(secondBracket + 1).Trim();

                            // Извлекаем отправителя
                            int start = message.IndexOf('[', message.IndexOf('[') + 1) + 1;
                            int end = message.IndexOf(']', start);
                            string sender = "";

                            if (start > 0 && end > start)
                            {
                                sender = message.Substring(start, end - start);
                            }

                            // Форматируем для отображения
                            string displayMessage = $"[{sender}]: {messageText}";
                            AddChatMessage(displayMessage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки истории: {ex.Message}");
            }
        }

        private void FormClient_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (isConnected)
            {
                try
                {
                    if (writer != null)
                        writer.WriteLine("EXIT");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }

                isConnected = false;
                reader?.Close();
                writer?.Close();
                tcpClient?.Close();
            }

            if (chatServer != null)
                chatServer.Stop();

            Application.Exit();
        }

        private void textBoxMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;

                // Если есть выбранные файлы - обрабатываем их
                if (selectedFilePaths.Count > 0)
                {
                    _ = SendAllSelectedFiles();
                }
                else if (!string.IsNullOrEmpty(textBoxMessage.Text.Trim()))
                {
                    SendMessageToServer();
                }
            }
        }

        // Кнопка "Файл"
        private void buttonFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Выберите файлы для отправки";
                openFileDialog.Filter = "Все файлы (*.*)|*.*|" +
                                       "Текстовые файлы (*.txt)|*.txt|" +
                                       "Изображения (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp|" +
                                       "Документы (*.pdf;*.doc;*.docx;*.xls;*.xlsx)|*.pdf;*.doc;*.docx;*.xls;*.xlsx|" +
                                       "Архивы (*.zip;*.rar;*.7z)|*.zip;*.rar;*.7z";
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = true; // Разрешаем выбор нескольких файлов

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Очищаем список выбранных файлов
                    selectedFilePaths.Clear();

                    // Добавляем все выбранные файлы
                    selectedFilePaths.AddRange(openFileDialog.FileNames);

                    // Показываем информацию о первом файле
                    if (selectedFilePaths.Count > 0)
                    {
                        string firstFileName = Path.GetFileName(selectedFilePaths[0]);
                        long firstFileSize = new FileInfo(selectedFilePaths[0]).Length;

                        textBoxMessage.Text = $"[ФАЙЛЫ]: {selectedFilePaths.Count} файлов выбранно";
                        textBoxMessage.Focus();

                        AddChatMessage($"Выбрано {selectedFilePaths.Count} файлов для отправки");
                    }
                }
            }
        }

        // Кнопка "Отправить"
        private async void buttonSend_Click(object sender, EventArgs e)
        {
            // Если есть выбранные файлы - обрабатываем их
            if (selectedFilePaths.Count > 0)
            {
                await SendAllSelectedFiles();
            }

            // Если есть текст в поле сообщения - отправляем его
            if (!string.IsNullOrEmpty(textBoxMessage.Text.Trim()))
            {
                SendMessageToServer();
            }
        }

        // Отправка всех выбранных файлов
        private async Task SendAllSelectedFiles()
        {
            try
            {
                AddChatMessage($"Начинаю отправку {selectedFilePaths.Count} файлов...");

                foreach (string filePath in selectedFilePaths)
                {
                    if (File.Exists(filePath))
                    {
                        await SendSingleFile(filePath);
                        await Task.Delay(200); // Небольшая задержка между файлами
                    }
                }

                // Очищаем список после отправки
                selectedFilePaths.Clear();
                textBoxMessage.Clear();

                AddChatMessage("Все файлы отправлены!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке файлов: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                AddChatMessage($"Ошибка отправки файлов: {ex.Message}");
            }
        }

        // Отправка одного файла
        private async Task SendSingleFile(string filePath)
        {
            try
            {
                string fileName = Path.GetFileName(filePath);
                long fileSize = new FileInfo(filePath).Length;

                Console.WriteLine($"Отправка файла: {fileName}, размер: {fileSize} байт");

                // Сохраняем информацию о файле в истории
                SavingMessage.SaveFileMessage(Registration.GetCurrentLogin(), fileName, fileSize);

                // Показываем сообщение о начале отправки
                AddChatMessage($"Отправляю файл: {fileName} ({FormatFileSize(fileSize)})...");

                // Имитация отправки
                await Task.Delay(300);

                // Уведомляем в чате об успешной отправке
                AddChatMessage($"Файл {fileName} отправлен!");

                // Отправляем сообщение в чат о файле
                string fileMessage = $"[ФАЙЛ от {Registration.GetCurrentLogin()}]: {fileName} ({FormatFileSize(fileSize)})";

                // Если подключены к серверу, отправляем сообщение о файле
                if (isConnected && writer != null)
                {
                    writer.WriteLine(fileMessage);
                    AddOwnMessage($"Отправил файл: {fileName}");
                }
                else
                {
                    // Локальное отображение, если нет подключения
                    AddOwnMessage($"Файл: {fileName} ({FormatFileSize(fileSize)})");
                }

                // Добавляем файл в историю
                Console.WriteLine($"Добавляю файл в историю: {fileName}");
                AddFileToHistory(fileName, fileSize);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки файла {filePath}: {ex.Message}");
                AddChatMessage($"Ошибка отправки файла: {ex.Message}");
            }
        }

        // Кнопка "Играть"
        private void buttonPlayGame_Click(object sender, EventArgs e)
        {
           FormGameSelection formGameSelection = new FormGameSelection();
            formGameSelection.ShowDialog();
            this.Hide();
        }

        // Обработчики кнопок пользователей
        private void btnUserKrs_Click(object sender, EventArgs e) { }
        private void btnUserPagan_Click(object sender, EventArgs e) { }
        private void btnUserDenden_Click(object sender, EventArgs e) { }
        private void btnUserCat_Noir_Click(object sender, EventArgs e) { }
        private void btnUserLady_Bug_Click(object sender, EventArgs e) { }
        private void btnUserTabeer_Click(object sender, EventArgs e) { }
        private void btnUserLushPush_Click(object sender, EventArgs e) { }
        private void btnUserSiles_Click(object sender, EventArgs e) { }
        private void btnUserUSF_Click(object sender, EventArgs e) { }
        private void btnUserVld_Click(object sender, EventArgs e) { }
        private void btnUserAnanas_Click(object sender, EventArgs e) { }

        private void listBoxChat_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxChat.SelectedItems != null)
            {
                string selectedText = listBoxChat.SelectedItem.ToString();

                Clipboard.SetText(selectedText);

            }
        }
    }
}