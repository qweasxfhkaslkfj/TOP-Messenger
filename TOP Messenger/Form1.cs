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
        private string serverIP = "192.168.88.128";
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

                string localIP = "192.168.88.128";
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
        public void AddFileToHistory(string originalFileName, long fileSize = 0, string serverFileName = null)
        {
            try
            {
                if (panelHistoryFiles.InvokeRequired)
                {
                    panelHistoryFiles.Invoke(new Action<string, long, string>(AddFileToHistory),
                        originalFileName, fileSize, serverFileName);
                    return;
                }

                // Используем serverFileName если есть, иначе originalFileName
                string displayName = !string.IsNullOrEmpty(serverFileName) ?
                    Path.GetFileName(serverFileName) :
                    Path.GetFileName(originalFileName);

                string fileKey = !string.IsNullOrEmpty(serverFileName) ?
                    serverFileName : originalFileName;

                // Проверяем, не добавлен ли уже файл
                bool alreadyAdded = false;
                foreach (Control control in panelHistoryFiles.Controls)
                {
                    if (control is Panel panel && panel.Tag != null &&
                        panel.Tag.ToString() == fileKey)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (alreadyAdded)
                {
                    Console.WriteLine($"Файл уже отображается: {displayName}");
                    return;
                }

                // Добавляем файл в список
                if (!chatFiles.Contains(fileKey))
                {
                    chatFiles.Add(fileKey);
                    Console.WriteLine($"Добавлен файл в список: {displayName} (ключ: {fileKey})");
                }

                // Ширина с учетом скроллбара
                int panelWidth = panelHistoryFiles.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 10;

                // Создаем простую панель для файла
                Panel filePanel = new Panel
                {
                    Width = Math.Max(panelWidth, 100),
                    Height = 55,
                    Margin = new Padding(5, 5, 5, 5),
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand,
                    Tag = fileKey // Сохраняем ключ файла в Tag
                };

                // Укорачиваем имя файла для отображения
                string shortDisplayName = displayName.Length > 25 ?
                    displayName.Substring(0, 22) + "..." : displayName;

                // Оригинальное имя файла
                Label fileNameLabel = new Label
                {
                    Text = $"📎 {Path.GetFileName(originalFileName)}",
                    AutoSize = false,
                    Width = filePanel.Width - 90,
                    Height = 20,
                    Left = 10,
                    Top = 5,
                    Font = new Font("Arial", 9, FontStyle.Bold),
                    ForeColor = Color.Black,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Tag = fileKey,
                    Cursor = Cursors.Hand
                };

                // Информация о файле
                string fileInfo = $"Отправитель: {Registration.GetCurrentLogin()}";
                if (fileSize > 0)
                {
                    fileInfo += $" | Размер: {FormatFileSize(fileSize)}";
                }

                // Добавляем информацию о серверном файле
                if (!string.IsNullOrEmpty(serverFileName))
                {
                    fileInfo += " | 📍 На сервере";
                }

                Label fileInfoLabel = new Label
                {
                    Text = fileInfo,
                    AutoSize = false,
                    Width = filePanel.Width - 90,
                    Height = 30,
                    Left = 10,
                    Top = 25,
                    Font = new Font("Arial", 8),
                    ForeColor = Color.DarkGray,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Tag = fileKey,
                    Cursor = Cursors.Hand
                };

                // Кнопка "Скачать"
                Button downloadBtn = new Button
                {
                    Text = "Скачать",
                    Width = 70,
                    Height = 40,
                    Left = filePanel.Width - 75,
                    Top = 7,
                    Tag = fileKey,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.LightGreen,
                    Font = new Font("Arial", 8),
                    Cursor = Cursors.Hand
                };

                // Обработчики событий
                downloadBtn.Click += (sender, e) =>
                {
                    string fileToDownload = (string)((Button)sender).Tag;
                    DownloadFile(fileToDownload, originalFileName);
                };

                fileNameLabel.Click += (sender, e) =>
                {
                    string fileToDownload = (string)((Label)sender).Tag;
                    DownloadFile(fileToDownload, originalFileName);
                };

                fileInfoLabel.Click += (sender, e) =>
                {
                    string fileToDownload = (string)((Label)sender).Tag;
                    DownloadFile(fileToDownload, originalFileName);
                };

                filePanel.Click += (sender, e) =>
                {
                    string fileToDownload = (string)((Panel)sender).Tag;
                    DownloadFile(fileToDownload, originalFileName);
                };

                // Добавляем элементы на панель
                filePanel.Controls.Add(fileNameLabel);
                filePanel.Controls.Add(fileInfoLabel);
                filePanel.Controls.Add(downloadBtn);

                // Добавляем панель в панель истории файлов
                panelHistoryFiles.Controls.Add(filePanel);

                // Обновляем расположение всех панелей
                ArrangeFilePanels();

                // Принудительно обновляем отображение и скроллбар
                panelHistoryFiles.PerformLayout();
                panelHistoryFiles.Refresh();

                Console.WriteLine($"Файловая панель добавлена: {originalFileName}. Ключ: {fileKey}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в AddFileToHistory: {ex.Message}");
            }
        }
        private void DownloadFile(string fileKey, string originalFileName)
        {
            try
            {
                Console.WriteLine($"Попытка скачать файл. Ключ: {fileKey}, Оригинал: {originalFileName}");

                // Извлекаем serverFileName из fileKey
                string serverFileName = null;

                if (fileKey.Contains("[SERVER_FILE:"))
                {
                    int start = fileKey.IndexOf("[SERVER_FILE:") + 13;
                    int end = fileKey.IndexOf("]", start);
                    if (start > 0 && end > start)
                    {
                        serverFileName = fileKey.Substring(start, end - start).Trim();
                    }
                }

                // Если не нашли в fileKey, ищем оригинальное имя
                if (string.IsNullOrEmpty(serverFileName))
                {
                    serverFileName = originalFileName;
                }

                Console.WriteLine($"Ищем файл на сервере: {serverFileName}");

                // Показываем диалог сохранения
                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.FileName = Path.GetFileName(originalFileName);
                    saveDialog.Filter = "Все файлы (*.*)|*.*";
                    saveDialog.Title = "Скачать файл с сервера";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Проверяем, существует ли файл на сервере
                        bool fileExists = FileTransfer.FileExistsOnServer(serverFileName);
                        Console.WriteLine($"Файл существует на сервере: {fileExists}");

                        if (!fileExists)
                        {
                            // Пытаемся найти файл по другому имени
                            Console.WriteLine("Пытаюсь найти файл другим способом...");

                            // Получаем все файлы на сервере
                            var allFiles = FileTransfer.GetAllServerFiles();
                            Console.WriteLine($"Всего файлов на сервере: {allFiles.Count}");

                            foreach (var file in allFiles)
                            {
                                Console.WriteLine($"  - {file}");

                                // Ищем файл, который содержит оригинальное имя
                                string fileWithoutExt = Path.GetFileNameWithoutExtension(file);
                                string origWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);

                                if (file.Contains(origWithoutExt) ||
                                    fileWithoutExt.Contains(origWithoutExt) ||
                                    origWithoutExt.Contains(fileWithoutExt))
                                {
                                    Console.WriteLine($"Нашел возможный файл: {file}");
                                    serverFileName = file;
                                    fileExists = true;
                                    break;
                                }
                            }
                        }

                        if (fileExists)
                        {
                            try
                            {
                                // Скачиваем файл
                                string downloadedPath = FileTransfer.DownloadFileFromServer(serverFileName, saveDialog.FileName);

                                MessageBox.Show($"Файл успешно скачан!\n{Path.GetFileName(downloadedPath)}",
                                    "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                AddChatMessage($"Файл '{Path.GetFileName(originalFileName)}' скачан");
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Ошибка при скачивании: {ex.Message}",
                                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Файл не найден на сервере.\n\n" +
                                          $"Искали файл: {serverFileName}\n" +
                                          $"Оригинальное имя: {originalFileName}",
                                "Файл не найден", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        if (textPart.Contains("[ФАЙЛ от") || textPart.Contains("[SERVER_FILE:"))
                        {
                            // Извлекаем информацию о серверном файле
                            ExtractFileInfoFromMessage(textPart);
                        }

                        AddChatMessage(textPart);
                    }
                }
            }
            else if (message.Contains("[SERVER_FILE:") || message.Contains("[ФАЙЛ от"))
            {
                // Сообщение с информацией о серверном файле
                ExtractFileInfoFromMessage(message);
                AddChatMessage(message);
            }
            else
            {
                // Обычное сообщение
                AddChatMessage(message);
            }
        }
        private void ExtractFileInfoFromMessage(string message)
        {
            try
            {
                Console.WriteLine($"Извлекаю информацию о файле из сообщения: {message}");

                // Упрощаем логику: просто ищем [SERVER_FILE:...]
                if (message.Contains("[SERVER_FILE:") && message.Contains("]"))
                {
                    int serverFileStart = message.IndexOf("[SERVER_FILE:") + 13;
                    int serverFileEnd = message.IndexOf("]", serverFileStart);

                    if (serverFileStart > 0 && serverFileEnd > serverFileStart)
                    {
                        string serverFileName = message.Substring(serverFileStart, serverFileEnd - serverFileStart).Trim();
                        string originalFileName = "";
                        long fileSize = 0;

                        // Пытаемся найти оригинальное имя файла
                        // Формат: [ФАЙЛ от user]: filename (size) [SERVER_FILE:...]
                        if (message.Contains("]: "))
                        {
                            int nameStart = message.IndexOf("]: ") + 3;
                            int nameEnd = message.IndexOf(" (");

                            if (nameEnd > nameStart)
                            {
                                originalFileName = message.Substring(nameStart, nameEnd - nameStart).Trim();
                            }
                            else
                            {
                                // Если нет скобок с размером, берем все до [SERVER_FILE:
                                int serverTagPos = message.IndexOf("[SERVER_FILE:");
                                if (serverTagPos > nameStart)
                                {
                                    originalFileName = message.Substring(nameStart, serverTagPos - nameStart).Trim();
                                }
                                else
                                {
                                    // Последний вариант: берем все после "]: "
                                    originalFileName = message.Substring(nameStart).Trim();
                                }
                            }
                        }

                        // Пытаемся извлечь размер
                        if (message.Contains("(") && message.Contains(")"))
                        {
                            int sizeStart = message.IndexOf("(") + 1;
                            int sizeEnd = message.IndexOf(")", sizeStart);

                            if (sizeStart > 0 && sizeEnd > sizeStart)
                            {
                                string sizeStr = message.Substring(sizeStart, sizeEnd - sizeStart);

                                // Убираем "Б", "КБ" и т.д.
                                string[] parts = sizeStr.Split(new[] { ' ', 'Б', 'К', 'М', 'Г' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length > 0 && double.TryParse(parts[0], out double parsedSize))
                                {
                                    if (sizeStr.Contains("КБ") || sizeStr.Contains("кб"))
                                        fileSize = (long)(parsedSize * 1024);
                                    else if (sizeStr.Contains("МБ") || sizeStr.Contains("мб"))
                                        fileSize = (long)(parsedSize * 1024 * 1024);
                                    else
                                        fileSize = (long)parsedSize;
                                }
                            }
                        }

                        // Если не удалось извлечь оригинальное имя, используем серверное
                        if (string.IsNullOrEmpty(originalFileName))
                        {
                            originalFileName = serverFileName;
                        }

                        Console.WriteLine($"Файл найден: оригинал='{originalFileName}', сервер='{serverFileName}', размер={fileSize}");

                        // Создаем ключ для файла
                        string fileKey = $"{originalFileName} [SERVER_FILE:{serverFileName}]";

                        // Добавляем в историю
                        AddFileToHistory(originalFileName, fileSize, fileKey);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка извлечения информации о файле: {ex.Message}");
            }
        }
        // Проверяет, зашифровано ли сообщение (по наличию зашифрованных символов)
        private bool IsEncryptedMessage(string message)
        {
            // Проверяем наличие зашифрованных русских букв
            // В шифре Цезаря со сдвигом 3: 
            // 'а' → 'г', 'б' → 'д', 'в' → 'е', и т.д.
            // Проверяем наличие характерных зашифрованных последовательностей

            if (message.Contains("гд") || message.Contains("де") ||
                message.Contains("еж") || message.Contains("жз"))
            {
                return true;
            }

            // Дополнительная проверка: если в сообщении есть русские буквы, 
            // но они не образуют осмысленных слов
            int russianLetterCount = 0;
            int meaningfulWords = 0;

            foreach (char c in message)
            {
                if (c >= 'а' && c <= 'я' || c >= 'А' && c <= 'Я')
                {
                    russianLetterCount++;
                }
            }

            // Если много русских букв, но сообщение не содержит пробелов 
            // и выглядит как случайный набор букв - вероятно зашифровано
            if (russianLetterCount > 5 && !message.Contains(" ") &&
                !message.Contains("подключился") && !message.Contains("покинул"))
            {
                return true;
            }

            return false;
        }

        // Дешифровка сообщения из истории
        private string DecryptMessageFromHistory(string encryptedLine)
        {
            try
            {
                // Формат строки: [timestamp] [sender] encrypted_message
                // Или: [timestamp] encrypted_message (для системных)

                if (encryptedLine.Contains("] ["))
                {
                    // Сообщение от пользователя
                    int timestampEnd = encryptedLine.IndexOf("] ") + 2;
                    int senderStart = encryptedLine.IndexOf("[", timestampEnd);
                    int senderEnd = encryptedLine.IndexOf("]", senderStart) + 2;

                    string timestampAndSender = encryptedLine.Substring(0, senderEnd);
                    string encryptedMessage = encryptedLine.Substring(senderEnd).Trim();

                    // Дешифруем сообщение
                    string decryptedMessage = Encryption.Decrypt(encryptedMessage);

                    return $"{timestampAndSender}{decryptedMessage}";
                }
                else if (encryptedLine.Contains("] "))
                {
                    // Системное сообщение
                    int timestampEnd = encryptedLine.IndexOf("] ") + 2;
                    string timestamp = encryptedLine.Substring(0, timestampEnd);
                    string encryptedMessage = encryptedLine.Substring(timestampEnd).Trim();

                    // Дешифруем сообщение
                    string decryptedMessage = Encryption.Decrypt(encryptedMessage);

                    return $"{timestamp}{decryptedMessage}";
                }
                else
                {
                    // Пробуем дешифровать всю строку
                    return Encryption.Decrypt(encryptedLine);
                }
            }
            catch (Exception)
            {
                return encryptedLine; // В случае ошибки возвращаем оригинал
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
            // Отладочная информация
            Console.WriteLine($"Текущий пользователь: {Registration.GetCurrentLogin()}");
            Console.WriteLine($"Путь ServerFiles: {FileTransfer.ServerFilesDirectory}");

            // Проверить существование директории
            if (!Directory.Exists(FileTransfer.ServerFilesDirectory))
            {
                Console.WriteLine($"ВНИМАНИЕ: Директория ServerFiles не существует!");
                Console.WriteLine($"Создаю директорию...");
                Directory.CreateDirectory(FileTransfer.ServerFilesDirectory);
            }

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
                    var history = chatServer.LoadChatHistoryFromFile(100);

                    Console.WriteLine($"Загружено {history.Count} сообщений из истории");

                    foreach (var line in history)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            Console.WriteLine($"Добавляю в чат: {line}");
                            AddChatMessage(line);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки истории сервера: {ex.Message}");
                AddChatMessage($"Ошибка загрузки истории: {ex.Message}");
            }
        }


        // Новый метод для дешифровки строки истории
        private string DecryptMessageLine(string encryptedLine)
        {
            try
            {
                if (encryptedLine.Contains("] ["))
                {
                    int timestampEnd = encryptedLine.IndexOf("] ") + 2;
                    int senderStart = encryptedLine.IndexOf("[", timestampEnd);
                    int senderEnd = encryptedLine.IndexOf("]", senderStart) + 2;

                    string timestampAndSender = encryptedLine.Substring(0, senderEnd);
                    string encryptedMessage = encryptedLine.Substring(senderEnd).Trim();

                    string decryptedMessage = Encryption.Decrypt(encryptedMessage);

                    return $"{timestampAndSender}{decryptedMessage}";
                }
                else if (encryptedLine.Contains("] "))
                {
                    int timestampEnd = encryptedLine.IndexOf("] ") + 2;
                    string timestamp = encryptedLine.Substring(0, timestampEnd);
                    string encryptedMessage = encryptedLine.Substring(timestampEnd).Trim();

                    string decryptedMessage = Encryption.Decrypt(encryptedMessage);

                    return $"{timestamp}{decryptedMessage}";
                }
                else
                {
                    return encryptedLine;
                }
            }
            catch (Exception)
            {
                return encryptedLine;
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

                // Показываем сообщение о начале отправки
                AddChatMessage($"Отправляю файл: {fileName} ({FormatFileSize(fileSize)})...");

                // Отправляем файл на сервер
                bool fileSent = false;
                string serverFileName = "";

                if (isConnected && tcpClient != null && tcpClient.Connected)
                {
                    // Создаем отдельное соединение для передачи файла
                    using (TcpClient fileClient = new TcpClient())
                    {
                        try
                        {
                            // Подключаемся к специальному порту для файлов (например, 8889)
                            fileClient.Connect(serverIP, 8889);

                            using (NetworkStream stream = fileClient.GetStream())
                            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode))
                            {
                                // Отправляем сигнал, что это файл
                                writer.Write("FILE_TRANSFER");

                                // Отправляем логин отправителя
                                writer.Write(Registration.GetCurrentLogin());

                                // Отправляем имя файла
                                writer.Write(fileName);

                                // Отправляем размер файла
                                writer.Write(fileSize);

                                // Отправляем содержимое файла
                                using (FileStream fileStream = File.OpenRead(filePath))
                                {
                                    byte[] buffer = new byte[8192];
                                    int bytesRead;
                                    long totalBytesSent = 0;

                                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        stream.Write(buffer, 0, bytesRead);
                                        totalBytesSent += bytesRead;

                                        // Обновляем прогресс (опционально)
                                        int progress = (int)((totalBytesSent * 100) / fileSize);
                                        // Можно обновлять UI с прогрессом
                                    }
                                }

                                // Читаем ответ от сервера с именем сохраненного файла
                                using (BinaryReader reader = new BinaryReader(stream, Encoding.Unicode))
                                {
                                    serverFileName = reader.ReadString();
                                }

                                fileSent = true;
                                Console.WriteLine($"Файл успешно отправлен на сервер: {serverFileName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка передачи файла на сервер: {ex.Message}");
                            // Пробуем локальное сохранение
                        }
                    }
                }

                // Если не удалось отправить через сеть, сохраняем локально
                if (!fileSent)
                {
                    try
                    {
                        string serverFilePath = FileTransfer.SaveFileOnServer(
                            filePath,
                            Registration.GetCurrentLogin()
                        );
                        serverFileName = Path.GetFileName(serverFilePath);
                        Console.WriteLine($"Файл сохранен локально: {serverFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка локального сохранения файла: {ex.Message}");
                        serverFileName = fileName;
                    }
                }

                // Имитация отправки
                await Task.Delay(300);

                // Уведомляем в чате об успешной отправке
                AddChatMessage($"Файл {fileName} отправлен!");

                // Отправляем сообщение в чат о файле (НЕ шифруется)
                string fileMessage = $"[ФАЙЛ от {Registration.GetCurrentLogin()}]: {fileName} ({FormatFileSize(fileSize)}) [SERVER_FILE:{serverFileName}]";

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

                // Добавляем файл в историю с указанием имени файла на сервере
                Console.WriteLine($"Добавляю файл в историю: {fileName} (серверное имя: {serverFileName})");
                AddFileToHistory(fileName, fileSize, serverFileName);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки файла {filePath}: {ex.Message}");
                AddChatMessage($"Ошибка отправки файла: {ex.Message}");

                // Показываем пользователю сообщение об ошибке
                MessageBox.Show($"Ошибка отправки файла: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void buttonPlayGame_Click(object sender, EventArgs e)
        {
            // Реализация игры
            MessageBox.Show("Функция игры в разработке", "Игра",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void listBoxChat_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxChat.SelectedItems != null)
            {
                string selectetText = listBoxChat.SelectedItem.ToString();
                Clipboard.SetText(selectetText);
            }
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

    }
}