using FluentAssertions;
using ManageEmployees.Domain;
using ManageEmployees.Services.Services;
using System.Security.Cryptography;
using System.Text;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class EncryptionServiceTests
{
    private EncryptionService _encryptionService;

    [SetUp]
    public void Setup()
    {
        _encryptionService = new EncryptionService();
    }

    private string Encrypt(string plainText, string key)
    {
        // Certifique-se de que a chave tem 16 bytes
        var keyBytes = Encoding.UTF8.GetBytes(key);
        if (keyBytes.Length != 16)
        {
            throw new ArgumentException("Key must be exactly 16 bytes long.");
        }

        // Gerar IV aleatório
        using var aesAlg = Aes.Create();
        aesAlg.Key = keyBytes;
        aesAlg.GenerateIV();
        aesAlg.Padding = PaddingMode.PKCS7;

        var iv = aesAlg.IV;

        using var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, iv);
        using var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        var cipherTextBytes = msEncrypt.ToArray();
        var ivHex = BitConverter.ToString(iv).Replace("-", string.Empty);
        var cipherTextBase64 = Convert.ToBase64String(cipherTextBytes);

        return $"{ivHex}:{cipherTextBase64}";
    }


    [Test]
    public void Constructor_ShouldNotThrowException_WhenKeyIsValid()
    {
        // Act
        Func<EncryptionService> act = () => new EncryptionService();

        // Assert
        act.Should().NotThrow();
    }

    [Test]
    public void Decrypt_ShouldThrowException_WhenEncryptedDataIsEmpty()
    {
        // Act
        Func<string> act = () => _encryptionService.Decrypt("");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage(Auth.DecriptStringError);
    }

    [Test]
    public void Decrypt_ShouldThrowException_WhenEncryptedDataHasInvalidFormat()
    {
        // Arrange
        var invalidEncryptedData = "InvalidFormat";

        // Act
        Func<string> act = () => _encryptionService.Decrypt(invalidEncryptedData);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage(Auth.DecriptStringError);
    }

    [Test]
    public void Decrypt_ShouldThrowException_WhenIvIsInvalid()
    {
        // Arrange
        var encryptedDataWithInvalidIv = "InvalidIV:VGhpcyBpcyBhIHRlc3QgZW5jcnlwdGVkIHN0cmluZw==";

        // Act
        Func<string> act = () => _encryptionService.Decrypt(encryptedDataWithInvalidIv);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void Decrypt_ShouldReturnDecryptedString_WhenDataIsValid()
    {
        // Arrange
        var expectedDecryptedString = "This is a test encrypted string";
        var encryptedData = Encrypt(expectedDecryptedString, "@my-secret-key-@");

        // Act
        var result = _encryptionService.Decrypt(encryptedData);

        // Assert
        result.Should().Be(expectedDecryptedString);
    }


    [Test]
    public void Decrypt_ShouldThrowException_WhenCryptoFails()
    {
        // Arrange
        var validIv = "0A1B2C3D4E5F6071"; // 16 bytes em Hex
        var invalidCipherText = Convert.ToBase64String(new byte[] { 0x00, 0x01 }); // Dados inválidos
        var encryptedData = $"{validIv}:{invalidCipherText}";

        // Act
        Func<string> act = () => _encryptionService.Decrypt(encryptedData);

        // Assert
        act.Should().Throw<CryptographicException>();
    }
}



