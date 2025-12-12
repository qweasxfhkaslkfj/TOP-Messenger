using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TOP_Messenger
{
    public class SavingMessage
    {
        private static string _chatHistoryFilePath;
        private static readonly object _fileLock = new object();
        private const int MAX_HISTORY_LINES = 100000;

        // Инициализация пути к файлу истории
        static SavingMessage()
        {
            InitializeFilePath();
        }

        private static void InitializeFilePath()
        {
            try
            {
                // Получаем путь к папке приложения
                string appDirectory = Application.StartupPath;

                // Создаем подпапку для данных приложения, если её нет
                string dataDirectory = Path.Combine(appDirectory, "ChatData");
                if (!Directory.Exists(dataDirectory))
                {
                    Directory.CreateDirectory(dataDirectory);
                }

                // Устанавливаем путь к файлу истории
                _chatHistoryFilePath = Path.Combine(dataDirectory, "chat_history.txt");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации пути к файлу истории: {ex.Message}");
                // Используем путь по умолчанию в папке приложения
                _chatHistoryFilePath = Path.Combine(Application.StartupPath, "chat_history.txt");
            }
        }

        // Сохранение сообщения в файл (теперь не используется, оставлено для совместимости)
        public static void SaveMessage(string message, string sender = null)
        {
            // Пустая реализация - сохранение теперь делается в ChatServer
        }

        // Обрезка файла истории, если он слишком большой
        private static void TrimHistoryFile()
        {
            // Пустая реализация
        }

        // Загрузка истории чата из файла
        public static List<string> LoadChatHistory(int maxLines = 50)
        {
            return new List<string>(); // Больше не используется, так как загрузка через ChatServer
        }

        // Очистка истории чата (только для сервера)
        public static void ClearChatHistory()
        {
            try
            {
                // Теперь очистка должна выполняться через ChatServer
                // Поэтому этот метод не делает ничего
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка очистки истории: {ex.Message}");
            }
        }

        // Получение пути к файлу истории
        public static string GetHistoryFilePath()
        {
            if (Registration.IsCurrentUserServer())
            {
                try
                {
                    string appDirectory = Application.StartupPath;
                    string dataDirectory = Path.Combine(appDirectory, "ChatData");

                    if (!Directory.Exists(dataDirectory))
                    {
                        Directory.CreateDirectory(dataDirectory);
                    }

                    return Path.Combine(dataDirectory, "chat_history.txt");
                }
                catch (Exception)
                {
                    return "chat_history.txt";
                }
            }

            // Для обычных пользователей и гостей - файл истории не создается
            return null;
        }

        // Сохранение системного сообщения
        public static void SaveSystemMessage(string message)
        {
            // Пустая реализация
        }

        // Сохранение сообщения о подключении/отключении пользователя
        public static void SaveConnectionMessage(string userName, bool isConnected)
        {
            // Пустая реализация
        }

        // Сохранение информации о файле
        public static void SaveFileMessage(string userName, string fileName, long fileSize)
        {
            // Пустая реализация - файлы теперь сохраняются на сервере
        }

        // Форматирование размера файла
        private static string FormatFileSize(long bytes)
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

        // Проверка существования файла истории
        public static bool HistoryFileExists()
        {
            return File.Exists(_chatHistoryFilePath);
        }

        // Получение информации о файле истории
        public static string GetHistoryFileInfo()
        {
            if (File.Exists(_chatHistoryFilePath))
            {
                FileInfo fileInfo = new FileInfo(_chatHistoryFilePath);
                int lineCount = 0;

                try
                {
                    lineCount = File.ReadLines(_chatHistoryFilePath).Count();
                }
                catch { }

                return $"Файл: {fileInfo.Name}\n" +
                       $"Размер: {FormatFileSize(fileInfo.Length)}\n" +
                       $"Создан: {fileInfo.CreationTime}\n" +
                       $"Изменен: {fileInfo.LastWriteTime}\n" +
                       $"Сообщений: {lineCount}";
            }

            return "Файл истории не найден";
        }
    }
}