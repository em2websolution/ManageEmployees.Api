using ManageEmployees.Domain;
using ManageEmployees.Domain.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;

namespace ManageEmployees.Services.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _keyBytes;
        private const int ExpectedPartsLength = 2;

        public EncryptionService()
        {
            _keyBytes = Encoding.UTF8.GetBytes(Auth.DecryptKey);
            if (_keyBytes.Length != 16)
            {
                throw new ArgumentException(Auth.DecriptKeyError);
            }
        }

        public string Decrypt(string encryptedData)
        {
            ValidateEncryptedDataFormat(encryptedData);

            (byte[] iv, byte[] cipherTextBytes) = ExtractIvAndCipherText(encryptedData);

            return DecryptCipherText(cipherTextBytes, iv);
        }

        private static void ValidateEncryptedDataFormat(string encryptedData)
        {
            if (string.IsNullOrWhiteSpace(encryptedData) || encryptedData.Split(':').Length != ExpectedPartsLength)
            {
                throw new ArgumentException(Auth.DecriptStringError);
            }
        }

        private static (byte[] iv, byte[] cipherTextBytes) ExtractIvAndCipherText(string encryptedData)
        {
            string[] parts = encryptedData.Split(':');
            byte[] iv = ConvertHexStringToByteArray(parts[0]);
            byte[] cipherTextBytes = Convert.FromBase64String(parts[1]);

            return (iv, cipherTextBytes);
        }

        private static byte[] ConvertHexStringToByteArray(string hexString)
        {
            return Enumerable.Range(0, hexString.Length / 2)
                             .Select(i => Convert.ToByte(hexString.Substring(i * 2, 2), 16))
                             .ToArray();
        }

        private string DecryptCipherText(byte[] cipherTextBytes, byte[] iv)
        {
            using Aes aesAlg = Aes.Create();
            aesAlg.Key = _keyBytes;
            aesAlg.IV = iv;
            aesAlg.Padding = PaddingMode.PKCS7;

            using var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            using var msDecrypt = new MemoryStream(cipherTextBytes);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            return srDecrypt.ReadToEnd();
        }
    }
}
