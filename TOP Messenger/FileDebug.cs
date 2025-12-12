using System;
using System.IO;
using System.Windows.Forms;

namespace TOP_Messenger
{
    internal static class FileDebug
    {
        public static void LogFileInfo(string message, string fileName, string serverFileName = null)
        {
            try
            {
                string logPath = Path.Combine(Application.StartupPath, "file_debug.log");
                string logMessage = $"[{DateTime.Now:HH:mm:ss}] {message}\n" +
                                  $"File: {fileName}\n" +
                                  $"Server File: {serverFileName}\n" +
                                  $"Exists: {FileTransfer.FileExistsOnServer(serverFileName ?? fileName)}\n" +
                                  $"---\n";

                File.AppendAllText(logPath, logMessage);
            }
            catch (Exception ex)
            {
                // В случае ошибки логирования, пишем в консоль
                Console.WriteLine($"Ошибка логирования файла: {ex.Message}");
            }
        }

        public static void CheckServerFiles()
        {
            try
            {
                string logPath = Path.Combine(Application.StartupPath, "server_files_check.log");
                var files = FileTransfer.GetAllServerFiles();

                string logMessage = $"[{DateTime.Now:HH:mm:ss}] Server files check\n" +
                                  $"Total files: {files.Count}\n" +
                                  $"Server directory: {FileTransfer.ServerFilesDirectory}\n\n";

                foreach (var file in files)
                {
                    try
                    {
                        string fullPath = Path.Combine(FileTransfer.ServerFilesDirectory, file);
                        long size = 0;

                        if (File.Exists(fullPath))
                        {
                            FileInfo fileInfo = new FileInfo(fullPath);
                            size = fileInfo.Length;
                        }

                        logMessage += $"- {file} ({size} bytes)\n";
                    }
                    catch (Exception ex)
                    {
                        logMessage += $"- {file} (Ошибка получения размера: {ex.Message})\n";
                    }
                }

                File.WriteAllText(logPath, logMessage);
                Console.WriteLine($"Проверка файлов сервера завершена. Лог сохранен в: {logPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки файлов сервера: {ex.Message}");
            }
        }

        /// <summary>
        /// Создает тестовый файл для проверки работы файловой системы
        /// </summary>
        public static void CreateTestFile()
        {
            try
            {
                string testFilePath = Path.Combine(Application.StartupPath, "test_file.txt");
                string content = $"Тестовый файл для проверки\n" +
                               $"Создан: {DateTime.Now}\n" +
                               $"Путь приложения: {Application.StartupPath}\n" +
                               $"Директория ServerFiles: {FileTransfer.ServerFilesDirectory}\n" +
                               $"Существует ли директория: {Directory.Exists(FileTransfer.ServerFilesDirectory)}";

                File.WriteAllText(testFilePath, content);
                Console.WriteLine($"Тестовый файл создан: {testFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания тестового файла: {ex.Message}");
            }
        }

        /// <summary>
        /// Логирует ошибку при скачивании файла
        /// </summary>
        public static void LogDownloadError(string fileKey, string originalFileName, string errorMessage)
        {
            try
            {
                string logPath = Path.Combine(Application.StartupPath, "download_errors.log");
                string logMessage = $"[{DateTime.Now:HH:mm:ss}] Ошибка скачивания\n" +
                                  $"File Key: {fileKey}\n" +
                                  $"Original File: {originalFileName}\n" +
                                  $"Error: {errorMessage}\n" +
                                  $"---\n";

                File.AppendAllText(logPath, logMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка логирования ошибки скачивания: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверяет и создает необходимые директории
        /// </summary>
        public static void EnsureDirectoriesExist()
        {
            try
            {
                // Проверяем и создаем директорию ServerFiles
                if (!Directory.Exists(FileTransfer.ServerFilesDirectory))
                {
                    Console.WriteLine($"Создаю директорию ServerFiles: {FileTransfer.ServerFilesDirectory}");
                    Directory.CreateDirectory(FileTransfer.ServerFilesDirectory);
                }

                // Проверяем и создаем другие необходимые директории
                string[] directories = {
                    Path.Combine(Application.StartupPath, "ChatData"),
                    Path.Combine(Application.StartupPath, "Logs")
                };

                foreach (string directory in directories)
                {
                    if (!Directory.Exists(directory))
                    {
                        Console.WriteLine($"Создаю директорию: {directory}");
                        Directory.CreateDirectory(directory);
                    }
                }

                Console.WriteLine("Все необходимые директории созданы/проверены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания директорий: {ex.Message}");
            }
        }
    }
}