using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;

namespace EncryptionTesting
{
    [TestClass]
    public class EncryptionTests
    {
        private const string TEST_FILE_PATH = "encrypted_messages.txt";

        [TestInitialize]
        public void TestInitialize()
        {
            // Очищаем файл перед каждым тестом
            if (File.Exists(TEST_FILE_PATH))
            {
                File.Delete(TEST_FILE_PATH);
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Очищаем файл после каждого теста
            if (File.Exists(TEST_FILE_PATH))
            {
                File.Delete(TEST_FILE_PATH);
            }
        }

        [TestMethod]
        public void Encrypt_EmptyString_ReturnsEmptyString()
        {
            string input = "";

            string result = Encryption.Encrypt(input);

            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void Encrypt_NullString_ReturnsNull()
        {
            string input = null;

            string result = Encryption.Encrypt(input);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void Encrypt_LowercaseLetters_ShiftedByThree()
        {
            string input = "abcdefghijklmnopqrstuvwxyz";
            string expected = "defghijklmnopqrstuvwxyzabc";

            string result = Encryption.Encrypt(input);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Encrypt_UppercaseLetters_ShiftedByThree()
        {
            string input = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string expected = "DEFGHIJKLMNOPQRSTUVWXYZABC";

            string result = Encryption.Encrypt(input);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Encrypt_MixedCaseLetters_CorrectlyEncrypted()
        {
            string input = "Hello World";
            string expected = "Khoor Zruog";

            string result = Encryption.Encrypt(input);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Encrypt_WithNumbersAndSymbols_OnlyLettersEncrypted()
        {
            string input = "Hello123!@# World456";
            string expected = "Khoor123!@# Zruog456";

            string result = Encryption.Encrypt(input);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Encrypt_CreatesLogFile()
        {
            string input = "Test message";

            string result = Encryption.Encrypt(input);

            Assert.IsTrue(File.Exists(TEST_FILE_PATH));
        }

        [TestMethod]
        public void Encrypt_LogsMessageToFile()
        {
            string input = "Test message";
            string encrypted = Encryption.Encrypt(input);

            string logContent = File.ReadAllText(TEST_FILE_PATH, Encoding.UTF8);

            Assert.IsTrue(logContent.Contains(input));
            Assert.IsTrue(logContent.Contains(encrypted));
        }

        [TestMethod]
        public void Decrypt_EmptyString_ReturnsEmptyString()
        {
            string input = "";

            string result = Encryption.Decrypt(input);

            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void Decrypt_NullString_ReturnsNull()
        {
            string input = null;

            string result = Encryption.Decrypt(input);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void Decrypt_EncryptedString_ReturnsOriginal()
        {
            string original = "Hello World123!@#";
            string encrypted = Encryption.Encrypt(original);

            string decrypted = Encryption.Decrypt(encrypted);

            Assert.AreEqual(original, decrypted);
        }

        [TestMethod]
        public void Decrypt_LowercaseLetters_ShiftedBackByThree()
        {
            string input = "defghijklmnopqrstuvwxyzabc";
            string expected = "abcdefghijklmnopqrstuvwxyz";

            string result = Encryption.Decrypt(input);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Decrypt_UppercaseLetters_ShiftedBackByThree()
        {
            string input = "DEFGHIJKLMNOPQRSTUVWXYZABC";
            string expected = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            string result = Encryption.Decrypt(input);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Decrypt_MixedCase_CorrectlyDecrypted()
        {
            string input = "Khoor Zruog";
            string expected = "Hello World";

            string result = Encryption.Decrypt(input);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void GetEncryptedMessagesLog_FileNotExists_ReturnsErrorMessage()
        {
            string result = Encryption.GetEncryptedMessagesLog();

            Assert.AreEqual("Файл с зашифрованными сообщениями не найден.", result);
        }

        [TestMethod]
        public void GetEncryptedMessagesLog_FileExists_ReturnsContent()
        {
            string testMessage = "Test";
            Encryption.Encrypt(testMessage);

            string result = Encryption.GetEncryptedMessagesLog();

            Assert.IsTrue(result.Contains("Test"));
            Assert.IsTrue(result.Contains("Khw"));
        }

        [TestMethod]
        public void ClearEncryptedMessagesLog_FileExists_DeletesFile()
        {
            Encryption.Encrypt("Test message");

            Assert.IsTrue(File.Exists(TEST_FILE_PATH));

            Encryption.ClearEncryptedMessagesLog();

            Assert.IsFalse(File.Exists(TEST_FILE_PATH));
        }

        [TestMethod]
        public void ClearEncryptedMessagesLog_FileNotExists_DoesNothing()
        {
            Assert.IsFalse(File.Exists(TEST_FILE_PATH));

            try
            {
                Encryption.ClearEncryptedMessagesLog();
                // Если не произошло исключения - тест пройден
                Assert.IsTrue(true);
            }
            catch
            {
                Assert.Fail("Метод не должен бросать исключение при отсутствии файла");
            }
        }

        [TestMethod]
        public void EncryptDecrypt_RoundTrip_ReturnsOriginal()
        {
            string[] testCases =
            {
                "",
                "Hello",
                "HELLO WORLD",
                "hello world",
                "Test123!@#",
                "Абвгд", // Кириллица (не будет зашифрована, т.к. проверка только на латинские буквы)
                "   ", // Только пробелы
                "a",
                "Z",
                "The quick brown fox jumps over the lazy dog"
            };

            foreach (string testCase in testCases)
            {
                string encrypted = Encryption.Encrypt(testCase);
                string decrypted = Encryption.Decrypt(encrypted);

                Assert.AreEqual(testCase, decrypted, $"Failed for: {testCase}");
            }
        }

        [TestMethod]
        public void LogEncryptedMessage_ExceptionHandled_DoesNotThrow()
        {
            // Создадим директорию с файлом, чтобы сделать файл недоступным для записи
            string lockedFilePath = "encrypted_messages.txt";

            // Создаем файл и сразу его открываем для исключительного доступа
            using (var fs = new FileStream(lockedFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                try
                {
                    // Попытка записи в заблокированный файл
                    string result = Encryption.Encrypt("Test message");

                    // Проверяем что результат корректен
                    Assert.AreEqual("Khw", result.Substring(0, 3));
                }
                catch
                {
                    Assert.Fail("Метод должен обрабатывать исключения при записи в файл");
                }
            }
        }
    }
}