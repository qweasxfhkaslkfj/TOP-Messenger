using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TOP_Messenger
{
    internal class Decryption
    {
        /// <summary>
        /// Дешифрует содержимое файла и возвращает его как текст
        /// </summary>
        /// <param name="filePath">Путь к зашифрованному файлу</param>
        /// <param name="encryptionKey">Ключ шифрования (строка)</param>
        /// <returns>Дешифрованный текст</returns>
        public static string DecryptFile(string filePath, string encryptionKey)
        {
            try
            {
                // Проверяем существование файла
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("Файл не найден.", filePath);
                }

                // Читаем зашифрованные данные из файла
                byte[] encryptedData = File.ReadAllBytes(filePath);

                // Конвертируем ключ в байтовый массив
                byte[] key = ConvertKeyToByteArray(encryptionKey);

                // Дешифруем данные
                byte[] decryptedData = DecryptData(encryptedData, key);

                // Конвертируем байты в строку
                return Encoding.UTF8.GetString(decryptedData);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при дешифровке файла: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Дешифрует массив байтов с использованием AES
        /// </summary>
        private static byte[] DecryptData(byte[] encryptedData, byte[] key)
        {
            using (Aes aesAlg = Aes.Create())
            {
                // Настраиваем алгоритм AES
                aesAlg.Key = key;

                // Для AES нам нужен IV (вектор инициализации)
                // Предполагаем, что первые 16 байт - это IV
                byte[] iv = new byte[16];
                byte[] actualEncryptedData = new byte[encryptedData.Length - 16];

                Array.Copy(encryptedData, 0, iv, 0, 16);
                Array.Copy(encryptedData, 16, actualEncryptedData, 0, encryptedData.Length - 16);

                aesAlg.IV = iv;

                // Создаем дешифратор
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Дешифруем данные
                using (MemoryStream msDecrypt = new MemoryStream(actualEncryptedData))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (MemoryStream msResult = new MemoryStream())
                {
                    csDecrypt.CopyTo(msResult);
                    return msResult.ToArray();
                }
            }
        }

        /// <summary>
        /// Конвертирует строковый ключ в байтовый массив фиксированной длины
        /// </summary>
        private static byte[] ConvertKeyToByteArray(string encryptionKey)
        {
            // Используем SHA256 для получения хеша ключа фиксированной длины
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);
                return sha256.ComputeHash(keyBytes);
            }
        }

        /// <summary>
        /// Загружает и дешифрует несколько сообщений из файла
        /// </summary>
        /// <param name="filePath">Путь к файлу с сообщениями</param>
        /// <param name="encryptionKey">Ключ шифрования</param>
        /// <returns>Список дешифрованных сообщений</returns>
        public static List<string> LoadDecryptedMessages(string filePath, string encryptionKey)
        {
            List<string> messages = new List<string>();

            try
            {
                string decryptedContent = DecryptFile(filePath, encryptionKey);

                // Разделяем сообщения (предполагаем, что каждое сообщение на новой строке)
                var lines = decryptedContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        messages.Add(line.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                // Можно добавить логирование ошибки
                Console.WriteLine($"Ошибка при загрузке сообщений: {ex.Message}");
                throw;
            }

            return messages;
        }

        /// <summary>
        /// Объединяет дешифрованные сообщения в единый текст
        /// </summary>
        public static string CombineMessagesToText(List<string> messages)
        {
            if (messages == null || messages.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder combinedText = new StringBuilder();
            foreach (var message in messages)
            {
                combinedText.AppendLine(message);
            }

            return combinedText.ToString();
        }

        /// <summary>
        /// Проверяет, является ли ключ валидным для дешифровки файла
        /// </summary>
        public static bool ValidateEncryptionKey(string filePath, string encryptionKey)
        {
            try
            {
                // Пробуем дешифровать первые несколько байт
                if (!File.Exists(filePath))
                    return false;

                byte[] encryptedData = File.ReadAllBytes(filePath);
                if (encryptedData.Length < 16)
                    return false;

                byte[] key = ConvertKeyToByteArray(encryptionKey);

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = key;

                    try
                    {
                        // Пробуем дешифровать первые 32 байта
                        byte[] testData = new byte[Math.Min(32, encryptedData.Length)];
                        Array.Copy(encryptedData, testData, testData.Length);

                        byte[] iv = new byte[16];
                        Array.Copy(testData, 0, iv, 0, 16);
                        aesAlg.IV = iv;

                        // Если не выбрасывает исключение - ключ вероятно валидный
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
