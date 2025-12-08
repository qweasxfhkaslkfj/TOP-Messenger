using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TOP_Messenger
{
    internal class Encryption
    {
        // Сдвиг для шифра Цезаря
        private const int SHIFT = 3;

        // Путь к файлу для записи зашифрованных сообщений
        private static readonly string logFilePath = "encrypted_messages.txt";

        /// Шифрует сообщение с использованием шифра Цезаря
        /// <param name="message">Исходное сообщение</param>

        public static string Encrypt(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            StringBuilder encrypted = new StringBuilder();

            foreach (char c in message)
            {
                // Шифруем только буквы, цифры и пробелы оставляем как есть
                if (char.IsLetter(c))
                {
                    char offset = char.IsUpper(c) ? 'A' : 'a';
                    char encryptedChar = (char)(((c + SHIFT - offset) % 26) + offset);
                    encrypted.Append(encryptedChar);
                }
                else
                {
                    // Не буквы оставляем без изменений
                    encrypted.Append(c);
                }
            }

            string result = encrypted.ToString();

            // Записываем в файл
            LogEncryptedMessage(message, result);

            return result;
        }

        /// Расшифровывает сообщение, зашифрованное шифром Цезаря
        /// <param name="encryptedMessage">Зашифрованное сообщение</param>
        public static string Decrypt(string encryptedMessage)
        {
            if (string.IsNullOrEmpty(encryptedMessage))
                return encryptedMessage;

            StringBuilder decrypted = new StringBuilder();

            foreach (char c in encryptedMessage)
            {
                if (char.IsLetter(c))
                {
                    char offset = char.IsUpper(c) ? 'A' : 'a';
                    // Для дешифровки используется обратный сдвиг 
                    char decryptedChar = (char)(((c - SHIFT - offset + 26) % 26) + offset);
                    decrypted.Append(decryptedChar);
                }
                else
                {
                    decrypted.Append(c);
                }
            }

            return decrypted.ToString();
        }

        /// Записывает зашифрованное сообщение в файл
        /// <param name="originalMessage">Исходное сообщение</param>
        /// <param name="encryptedMessage">Зашифрованное сообщение</param>
        private static void LogEncryptedMessage(string originalMessage, string encryptedMessage)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(logFilePath, true, Encoding.UTF8))
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    sw.WriteLine($"[{timestamp}]");
                    sw.WriteLine($"Оригинал: {originalMessage}");
                    sw.WriteLine($"Зашифровано: {encryptedMessage}");
                    sw.WriteLine(new string('-', 50));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при записи в файл: {ex.Message}");
            }
        }

        /// Показывает все зашифрованные сообщения из файла
        public static string GetEncryptedMessagesLog()
        {
            try
            {
                if (File.Exists(logFilePath))
                {
                    return File.ReadAllText(logFilePath, Encoding.Unicode);
                }
                return "Файл с зашифрованными сообщениями не найден.";
            }
            catch (Exception ex)
            {
                return $"Ошибка при чтении файла: {ex.Message}";
            }
        }

        /// Очищает файл с зашифрованными сообщениями
        public static void ClearEncryptedMessagesLog()
        {
            try
            {
                if (File.Exists(logFilePath))
                {
                    File.Delete(logFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении файла: {ex.Message}");
            }
        }
    }
}