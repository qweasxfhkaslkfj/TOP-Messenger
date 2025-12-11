using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DecryptionTests
{
    [TestClass]
    public class Decryption1Tests
    {
        private const string TestEncryptionKey = "MySecretKey123!";
        private string _tempTestFilePath;
        private string _tempEncryptedFilePath;

        [TestInitialize]
        public void TestInitialize()
        {
            // Создаем временные файлы для тестов
            _tempTestFilePath = Path.GetTempFileName();
            _tempEncryptedFilePath = Path.GetTempFileName();

            // Создаем тестовые данные
            var testMessages = new List<string>
            {
                "Hello, World!",
                "This is a test message.",
                "Привет, мир!",
                "Тестовое сообщение на русском",
                "Message with special characters: !@#$%^&*()",
                ""
            };

            // Сохраняем тестовые данные в файл
            File.WriteAllLines(_tempTestFilePath, testMessages);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Удаляем временные файлы
            if (File.Exists(_tempTestFilePath))
                File.Delete(_tempTestFilePath);

            if (File.Exists(_tempEncryptedFilePath))
                File.Delete(_tempEncryptedFilePath);
        }

        [TestMethod]
        public void DecryptFile_WithValidKey_ReturnsDecryptedText()
        {
            // Arrange
            string testContent = "This is a secret message!";
            byte[] key = GenerateTestKey(TestEncryptionKey);
            byte[] iv = GenerateRandomIV();
            byte[] encryptedData = EncryptDataForTest(testContent, key, iv);
            File.WriteAllBytes(_tempEncryptedFilePath, encryptedData);

            // Act
            string result = DecryptFile(_tempEncryptedFilePath, TestEncryptionKey);

            // Assert
            Assert.AreEqual(testContent, result);
        }

        private static string DecryptFile(string filePath, string encryptionKey)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            // Реализация метода дешифровки файла
            byte[] key = ConvertKeyToByteArray(encryptionKey);
            byte[] encryptedData = File.ReadAllBytes(filePath);

            if (encryptedData.Length < 16)
                throw new CryptographicException("File is too small to contain valid encrypted data");

            byte[] iv = new byte[16];
            Array.Copy(encryptedData, 0, iv, 0, 16);

            byte[] cipherText = new byte[encryptedData.Length - 16];
            Array.Copy(encryptedData, 16, cipherText, 0, cipherText.Length);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream(cipherText))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void DecryptFile_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            // Act
            DecryptFile("non_existent_file.txt", TestEncryptionKey);
        }

        [TestMethod]
        public void DecryptData_WithValidData_ReturnsOriginalBytes()
        {
            // Arrange
            string originalText = "Test data for decryption";
            byte[] originalBytes = Encoding.UTF8.GetBytes(originalText);
            byte[] key = GenerateTestKey(TestEncryptionKey);
            byte[] iv = GenerateRandomIV();
            byte[] encryptedData = EncryptDataForTest(originalText, key, iv);

            // Act
            byte[] result = DecryptData(encryptedData, key);

            // Assert
            CollectionAssert.AreEqual(originalBytes, result);
        }

        private static byte[] DecryptData(byte[] encryptedData, byte[] key)
        {
            if (encryptedData.Length < 16)
                throw new CryptographicException("Data is too small to contain valid encrypted data");

            byte[] iv = new byte[16];
            Array.Copy(encryptedData, 0, iv, 0, 16);

            byte[] cipherText = new byte[encryptedData.Length - 16];
            Array.Copy(encryptedData, 16, cipherText, 0, cipherText.Length);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream(cipherText))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var msDecrypted = new MemoryStream())
                {
                    cs.CopyTo(msDecrypted);
                    return msDecrypted.ToArray();
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(CryptographicException))]
        public void DecryptData_WithInvalidKey_ThrowsCryptographicException()
        {
            // Arrange
            string originalText = "Test data";
            byte[] correctKey = GenerateTestKey(TestEncryptionKey);
            byte[] wrongKey = GenerateTestKey("WrongKey123!");
            byte[] iv = GenerateRandomIV();
            byte[] encryptedData = EncryptDataForTest(originalText, correctKey, iv);

            // Act
            DecryptData(encryptedData, wrongKey);
        }

        [TestMethod]
        public void ConvertKeyToByteArray_ReturnsConsistentHash()
        {
            // Arrange
            string key1 = "MyKey";
            string key2 = "MyKey";
            string key3 = "DifferentKey";

            // Act
            byte[] hash1 = ConvertKeyToByteArray(key1);
            byte[] hash2 = ConvertKeyToByteArray(key2);
            byte[] hash3 = ConvertKeyToByteArray(key3);

            // Assert
            CollectionAssert.AreEqual(hash1, hash2); // Одинаковые ключи дают одинаковый хэш
            CollectionAssert.AreNotEqual(hash1, hash3); // Разные ключи дают разные хэши
            Assert.AreEqual(32, hash1.Length); // SHA256 produces 32 bytes
        }

        private static byte[] ConvertKeyToByteArray(string encryptionKey)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);
                return sha256.ComputeHash(keyBytes);
            }
        }

        [TestMethod]
        public void LoadDecryptedMessages_WithMultipleMessages_ReturnsCorrectList()
        {
            // Arrange
            List<string> expectedMessages = new List<string>
            {
                "Message 1",
                "Message 2",
                "Message 3",
                "Message 4"
            };

            string content = string.Join(Environment.NewLine, expectedMessages);
            byte[] key = GenerateTestKey(TestEncryptionKey);
            byte[] iv = GenerateRandomIV();
            byte[] encryptedData = EncryptDataForTest(content, key, iv);
            File.WriteAllBytes(_tempEncryptedFilePath, encryptedData);

            // Act
            List<string> result = LoadDecryptedMessages(_tempEncryptedFilePath, TestEncryptionKey);

            // Assert
            CollectionAssert.AreEqual(expectedMessages, result);
        }

        private static List<string> LoadDecryptedMessages(string filePath, string encryptionKey)
        {
            string decryptedContent = DecryptFile(filePath, encryptionKey);

            if (string.IsNullOrEmpty(decryptedContent))
                return new List<string>();

            return decryptedContent
                .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                .ToList();
        }

        [TestMethod]
        public void LoadDecryptedMessages_WithEmptyFile_ReturnsEmptyList()
        {
            // Arrange
            byte[] key = GenerateTestKey(TestEncryptionKey);
            byte[] iv = GenerateRandomIV();
            byte[] encryptedData = EncryptDataForTest("", key, iv);
            File.WriteAllBytes(_tempEncryptedFilePath, encryptedData);

            // Act
            List<string> result = LoadDecryptedMessages(_tempEncryptedFilePath, TestEncryptionKey);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(CryptographicException))]
        public void LoadDecryptedMessages_WithWrongKey_ThrowsException()
        {
            // Arrange
            string content = "Test message";
            byte[] correctKey = GenerateTestKey(TestEncryptionKey);
            byte[] iv = GenerateRandomIV();
            byte[] encryptedData = EncryptDataForTest(content, correctKey, iv);
            File.WriteAllBytes(_tempEncryptedFilePath, encryptedData);

            // Act
            LoadDecryptedMessages(_tempEncryptedFilePath, "WrongKey!");
        }

        [TestMethod]
        public void CombineMessagesToText_WithValidMessages_ReturnsCorrectText()
        {
            // Arrange
            List<string> messages = new List<string>
            {
                "First line",
                "Second line",
                "Third line"
            };

            string expected = $"First line{Environment.NewLine}Second line{Environment.NewLine}Third line{Environment.NewLine}";

            // Act
            string result = CombineMessagesToText(messages);

            // Assert
            Assert.AreEqual(expected, result);
        }

        private static string CombineMessagesToText(List<string> messages)
        {
            if (messages == null || messages.Count == 0)
                return string.Empty;

            return string.Join(Environment.NewLine, messages) + Environment.NewLine;
        }

        [TestMethod]
        public void CombineMessagesToText_WithEmptyList_ReturnsEmptyString()
        {
            // Arrange
            List<string> messages = new List<string>();

            // Act
            string result = CombineMessagesToText(messages);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void CombineMessagesToText_WithNullList_ReturnsEmptyString()
        {
            // Act
            string result = CombineMessagesToText(null);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void ValidateEncryptionKey_WithValidKey_ReturnsTrue()
        {
            // Arrange
            string content = "Test content";
            byte[] key = GenerateTestKey(TestEncryptionKey);
            byte[] iv = GenerateRandomIV();
            byte[] encryptedData = EncryptDataForTest(content, key, iv);
            File.WriteAllBytes(_tempEncryptedFilePath, encryptedData);

            // Act
            bool isValid = ValidateEncryptionKey(_tempEncryptedFilePath, TestEncryptionKey);

            // Assert
            Assert.IsTrue(isValid);
        }

        private static bool ValidateEncryptionKey(string filePath, string encryptionKey)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                byte[] fileData = File.ReadAllBytes(filePath);
                if (fileData.Length < 16)
                    return false;

                // Пытаемся расшифровать файл
                DecryptFile(filePath, encryptionKey);
                return true;
            }
            catch
            {
                return false;
            }
        }

        [TestMethod]
        public void ValidateEncryptionKey_WithInvalidKey_ReturnsFalse()
        {
            // Arrange
            string content = "Test content";
            byte[] correctKey = GenerateTestKey(TestEncryptionKey);
            byte[] iv = GenerateRandomIV();
            byte[] encryptedData = EncryptDataForTest(content, correctKey, iv);
            File.WriteAllBytes(_tempEncryptedFilePath, encryptedData);

            // Act
            bool isValid = ValidateEncryptionKey(_tempEncryptedFilePath, "WrongKey!");

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void ValidateEncryptionKey_WithNonExistentFile_ReturnsFalse()
        {
            // Act
            bool isValid = ValidateEncryptionKey("non_existent_file.txt", TestEncryptionKey);

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void ValidateEncryptionKey_WithSmallFile_ReturnsFalse()
        {
            // Arrange
            File.WriteAllBytes(_tempEncryptedFilePath, new byte[] { 1, 2, 3 }); // File less than 16 bytes

            // Act
            bool isValid = ValidateEncryptionKey(_tempEncryptedFilePath, TestEncryptionKey);

            // Assert
            Assert.IsFalse(isValid);
        }

        // Вспомогательные методы для создания тестовых зашифрованных данных
        private static byte[] GenerateTestKey(string encryptionKey)
        {
            return ConvertKeyToByteArray(encryptionKey);
        }

        private static byte[] GenerateRandomIV()
        {
            using (var aes = Aes.Create())
            {
                aes.GenerateIV();
                return aes.IV;
            }
        }

        private static byte[] EncryptDataForTest(string data, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (var ms = new MemoryStream())
                {
                    // Записываем IV в начало потока
                    ms.Write(iv, 0, iv.Length);

                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs, Encoding.UTF8))
                    {
                        sw.Write(data);
                    }

                    return ms.ToArray();
                }
            }
        }

        [TestMethod]
        public void IntegrationTest_FullDecryptionWorkflow()
        {
            // Arrange
            List<string> originalMessages = new List<string>
            {
                "Первое сообщение",
                "Второе сообщение",
                "",
                "Сообщение с переносом\nстроки",
                "Последнее сообщение"
            };

            // Сохраняем в файл
            File.WriteAllLines(_tempTestFilePath, originalMessages);

            // Читаем все содержимое файла (включая пустые строки)
            string content = File.ReadAllText(_tempTestFilePath);

            // Шифруем
            byte[] key = GenerateTestKey(TestEncryptionKey);
            byte[] iv = GenerateRandomIV();
            byte[] encryptedData = EncryptDataForTest(content, key, iv);
            File.WriteAllBytes(_tempEncryptedFilePath, encryptedData);

            // Act
            List<string> decryptedMessages = LoadDecryptedMessages(_tempEncryptedFilePath, TestEncryptionKey);
            string combinedText = CombineMessagesToText(decryptedMessages);

            // Assert
            // В LoadDecryptedMessages мы не фильтруем пустые строки
            Assert.AreEqual(5, decryptedMessages.Count);
            Assert.IsTrue(combinedText.Contains("Первое сообщение"));
            Assert.IsTrue(combinedText.Contains("Последнее сообщение"));
            Assert.IsTrue(combinedText.Contains("Сообщение с переносом\nстроки"));
        }

        [TestMethod]
        public void Test_WithSpecialCharactersAndUnicode()
        {
            // Arrange
            string testContent = "Unicode: ✅ ✔ ✘ ❌\nEmoji: 😀 🚀 🌍\nSpecial: \t\n\r\"'";
            byte[] key = GenerateTestKey(TestEncryptionKey);
            byte[] iv = GenerateRandomIV();
            byte[] encryptedData = EncryptDataForTest(testContent, key, iv);
            File.WriteAllBytes(_tempEncryptedFilePath, encryptedData);

            // Act
            string result = DecryptFile(_tempEncryptedFilePath, TestEncryptionKey);

            // Assert
            Assert.AreEqual(testContent, result);
        }
    }
}