﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace TOP_Messenger
{
    internal class FileTransfer
    {
        public static string ServerFilesDirectory
        {
            get
            {
                string appDirectory = Application.StartupPath;
                string filesDirectory = Path.Combine(appDirectory, "ServerFiles");

                if (!Directory.Exists(filesDirectory))
                {
                    Directory.CreateDirectory(filesDirectory);
                }

                return filesDirectory;
            }
        }

        public static string SaveFileOnServer(string sourceFilePath, string senderLogin)
        {
            try
            {
                if (!File.Exists(sourceFilePath))
                {
                    throw new FileNotFoundException("Исходный файл не найден", sourceFilePath);
                }

                // Создаем уникальное имя файла на сервере
                string originalFileName = Path.GetFileName(sourceFilePath);
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
                string extension = Path.GetExtension(originalFileName);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
                string uniqueFileName = $"{fileNameWithoutExt}_{senderLogin}_{timestamp}{extension}";

                // Путь для сохранения на сервере
                string serverFilePath = Path.Combine(ServerFilesDirectory, uniqueFileName);

                // Копируем файл на сервер
                File.Copy(sourceFilePath, serverFilePath, true);

                // Сохраняем информацию о файле (отправитель, оригинальное имя, путь на сервере)
                SaveFileInfo(senderLogin, originalFileName, serverFilePath);

                return serverFilePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения файла на сервере: {ex.Message}", ex);
            }
        }

        // НОВЫЙ МЕТОД: передача файла на сервер через сокет
        public static bool SendFileToServer(string filePath, string serverIP, int serverPort)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    client.Connect(serverIP, serverPort);

                    using (NetworkStream stream = client.GetStream())
                    using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode))
                    {
                        // Отправляем сигнал, что это файл
                        writer.Write("FILE_TRANSFER");

                        // Отправляем имя файла
                        string fileName = Path.GetFileName(filePath);
                        writer.Write(fileName);

                        // Отправляем размер файла
                        long fileSize = new FileInfo(filePath).Length;
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

                                // Можно добавить прогресс бар здесь
                            }
                        }

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка передачи файла на сервер: {ex.Message}");
                return false;
            }
        }

        // НОВЫЙ МЕТОД: сохранение файла, полученного от клиента
        public static string SaveFileFromClient(NetworkStream stream, string senderLogin)
        {
            try
            {
                using (BinaryReader reader = new BinaryReader(stream, Encoding.Unicode))
                {
                    // Читаем информацию о файле
                    string fileName = reader.ReadString();
                    long fileSize = reader.ReadInt64();

                    // Создаем уникальное имя для файла
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    string extension = Path.GetExtension(fileName);
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
                    string uniqueFileName = $"{fileNameWithoutExt}_{senderLogin}_{timestamp}{extension}";
                    string serverFilePath = Path.Combine(ServerFilesDirectory, uniqueFileName);

                    // Сохраняем файл
                    using (FileStream fileStream = File.Create(serverFilePath))
                    {
                        byte[] buffer = new byte[8192];
                        long totalBytesRead = 0;
                        int bytesRead;

                        while (totalBytesRead < fileSize)
                        {
                            int bytesToRead = (int)Math.Min(buffer.Length, fileSize - totalBytesRead);
                            bytesRead = stream.Read(buffer, 0, bytesToRead);

                            if (bytesRead == 0)
                                break;

                            fileStream.Write(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;
                        }
                    }

                    // Сохраняем информацию о файле
                    SaveFileInfo(senderLogin, fileName, serverFilePath);

                    return uniqueFileName;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения файла от клиента: {ex.Message}", ex);
            }
        }

        private static void SaveFileInfo(string senderLogin, string originalFileName, string serverFilePath)
        {
            try
            {
                string infoFilePath = Path.Combine(ServerFilesDirectory, "files_info.txt");

                FileInfo fileInfo = new FileInfo(serverFilePath);
                string infoLine = $"{DateTime.Now:yyyy-MM-dd HH:mm}|{senderLogin}|{originalFileName}|{serverFilePath}|{fileInfo.Length}";

                File.AppendAllText(infoFilePath, infoLine + Environment.NewLine);
            }
            catch
            {
                // Игнорируем ошибки записи информации
            }
        }

        public static string DownloadFileFromServer(string serverFileName, string savePath)
        {
            try
            {
                Console.WriteLine($"Попытка скачать файл: {serverFileName}");
                Console.WriteLine($"Путь для сохранения: {savePath}");

                string serverFilePath = Path.Combine(ServerFilesDirectory, serverFileName);
                Console.WriteLine($"Полный путь к файлу: {serverFilePath}");

                if (!File.Exists(serverFilePath))
                {
                    Console.WriteLine($"Файл не найден. Ищу альтернативы...");

                    // Ищем файлы в директории
                    if (Directory.Exists(ServerFilesDirectory))
                    {
                        var allFiles = Directory.GetFiles(ServerFilesDirectory, "*.*");
                        Console.WriteLine($"Всего файлов в директории: {allFiles.Length}");

                        foreach (var file in allFiles)
                        {
                            string fileName = Path.GetFileName(file);
                            Console.WriteLine($"  Проверяю: {fileName}");

                            // Пытаемся найти похожий файл
                            if (fileName.ToLower().Contains(serverFileName.ToLower()) ||
                                serverFileName.ToLower().Contains(fileName.ToLower()))
                            {
                                Console.WriteLine($"Нашел похожий файл: {fileName}");
                                serverFilePath = file;
                                break;
                            }
                        }
                    }
                }

                if (!File.Exists(serverFilePath))
                {
                    throw new FileNotFoundException($"Файл '{serverFileName}' не найден на сервере. Директория: {ServerFilesDirectory}");
                }

                Console.WriteLine($"Копирую файл: {serverFilePath} -> {savePath}");
                File.Copy(serverFilePath, savePath, true);

                Console.WriteLine($"Файл успешно скопирован");
                return savePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в DownloadFileFromServer: {ex.Message}");
                throw;
            }
        }

        public static bool FileExistsOnServer(string serverFileName)
        {
            if (string.IsNullOrEmpty(serverFileName))
                return false;

            string serverFilePath = Path.Combine(ServerFilesDirectory, serverFileName);
            bool exists = File.Exists(serverFilePath);

            Console.WriteLine($"FileExistsOnServer: {serverFileName} -> {exists}");

            if (!exists)
            {
                // Покажем список файлов для отладки
                if (Directory.Exists(ServerFilesDirectory))
                {
                    var files = Directory.GetFiles(ServerFilesDirectory);
                    Console.WriteLine($"Файлы в директории ServerFiles:");
                    foreach (var file in files)
                    {
                        Console.WriteLine($"  - {Path.GetFileName(file)}");
                    }
                }
                else
                {
                    Console.WriteLine($"Директория ServerFiles не существует: {ServerFilesDirectory}");
                }
            }

            return exists;
        }

        public static string GetOriginalFileName(string serverFileName)
        {
            try
            {
                string infoFilePath = Path.Combine(ServerFilesDirectory, "files_info.txt");

                if (File.Exists(infoFilePath))
                {
                    var lines = File.ReadAllLines(infoFilePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 4 && parts[3].Contains(serverFileName))
                        {
                            return parts[2]; // Оригинальное имя файла
                        }
                    }
                }

                // Если информация не найдена, возвращаем имя без уникального суффикса
                string fileName = serverFileName;
                int lastUnderscore = fileName.LastIndexOf('_');
                if (lastUnderscore > 0)
                {
                    int secondUnderscore = fileName.LastIndexOf('_', lastUnderscore - 1);
                    if (secondUnderscore > 0)
                    {
                        string extension = Path.GetExtension(fileName);
                        return fileName.Substring(0, secondUnderscore) + extension;
                    }
                }

                return fileName;
            }
            catch
            {
                return serverFileName;
            }
        }

        public static long GetFileSize(string serverFileName)
        {
            try
            {
                string serverFilePath = Path.Combine(ServerFilesDirectory, serverFileName);
                if (File.Exists(serverFilePath))
                {
                    return new FileInfo(serverFilePath).Length;
                }
            }
            catch { }

            return 0;
        }


        /// <summary>
        /// Находит серверный файл по оригинальному имени и отправителю
        /// </summary>
        public static string FindServerFileName(string originalFileName, string sender = "")
        {
            try
            {
                // Сначала проверяем файл mapping
                string mappingFilePath = Path.Combine(ServerFilesDirectory, "files_mapping.txt");
                if (File.Exists(mappingFilePath))
                {
                    var lines = File.ReadAllLines(mappingFilePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 4)
                        {
                            string lineSender = parts[1];
                            string lineOriginalName = parts[2];
                            string lineServerName = Path.GetFileName(parts[3]); // Берем только имя файла

                            // Ищем точное совпадение по оригинальному имени
                            if (lineOriginalName.Equals(originalFileName, StringComparison.OrdinalIgnoreCase))
                            {
                                // Если указан отправитель, проверяем совпадение
                                if (string.IsNullOrEmpty(sender) ||
                                    lineSender.Equals(sender, StringComparison.OrdinalIgnoreCase))
                                {
                                    return lineServerName;
                                }
                            }
                            // Также проверяем частичное совпадение
                            else if (originalFileName.Contains(lineOriginalName) ||
                                    lineOriginalName.Contains(originalFileName))
                            {
                                if (string.IsNullOrEmpty(sender) ||
                                    lineSender.Equals(sender, StringComparison.OrdinalIgnoreCase))
                                {
                                    return lineServerName;
                                }
                            }
                        }
                    }
                }

                // Если не нашли в mapping, ищем по структуре имени файла
                string[] serverFiles = Directory.GetFiles(ServerFilesDirectory, "*.*");

                foreach (string serverFile in serverFiles)
                {
                    string serverFileName = Path.GetFileName(serverFile);

                    // Пропускаем служебные файлы
                    if (serverFileName.EndsWith("files_mapping.txt") ||
                        serverFileName.EndsWith("files_info.txt"))
                        continue;

                    // Проверяем различные варианты имени
                    string serverFileWithoutExt = Path.GetFileNameWithoutExtension(serverFileName);
                    string originalFileWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);

                    // Вариант 1: Файл содержит оригинальное имя
                    if (serverFileWithoutExt.Contains(originalFileWithoutExt))
                    {
                        return serverFileName;
                    }

                    // Вариант 2: Оригинальное имя содержит часть имени серверного файла
                    if (originalFileWithoutExt.Contains(serverFileWithoutExt))
                    {
                        return serverFileName;
                    }

                    // Вариант 3: Ищем файл с таким же расширением и похожим именем
                    if (Path.GetExtension(serverFileName).Equals(Path.GetExtension(originalFileName),
                        StringComparison.OrdinalIgnoreCase))
                    {
                        // Проверяем, содержит ли имя отправителя
                        if (!string.IsNullOrEmpty(sender) && serverFileWithoutExt.Contains(sender))
                        {
                            return serverFileName;
                        }
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
        /// <summary>
        /// Получает список всех файлов на сервере
        /// </summary>
        public static List<string> GetAllServerFiles()
        {
            List<string> files = new List<string>();
            try
            {
                if (Directory.Exists(ServerFilesDirectory))
                {
                    string[] allFiles = Directory.GetFiles(ServerFilesDirectory, "*.*");
                    foreach (string file in allFiles)
                    {
                        // Исключаем файлы с информацией
                        string fileName = Path.GetFileName(file);
                        if (!fileName.EndsWith("files_mapping.txt") &&
                            !fileName.EndsWith("files_info.txt"))
                        {
                            files.Add(fileName);
                        }
                    }
                }
            }
            catch { }
            return files;
        }
    }
}