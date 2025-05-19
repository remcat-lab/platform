# Api Gateway

``` mermaid
sequenceDiagram
    participant Client
    participant APIGateway
    participant Credential
    participant Service

    Note over Client: Session 기반 요청<br/>Payload는 AES로 암호화됨
    Client->>APIGateway: SessionId + EncryptedPayload

    Note over APIGateway: 세션 검증 요청
    APIGateway->>Credential: ValidateSession(SessionId)
    Credential-->>APIGateway: UserId, AESKey

    Note over APIGateway: AESKey로 복호화
    APIGateway->>APIGateway: Decrypt(EncryptedPayload, AESKey)

    Note over APIGateway: 평문 전달
    APIGateway->>Service: UserId + DecryptedPayload

    Service->>Service: Handle DecryptedPayload

    Note over Client: 비세션 요청 (예: 로그인)
    Client->>APIGateway: PlainPayload
    APIGateway->>Service: PlainPayload
    Service->>Service: Handle PlainPayload
```

## 1. 기능 설명
 ### Ciphers
 - Api Gateway와 Client간의 암호화 처리를 위한 기능으로 버전별 RSA 키를 보관, 관리한다.
 ### Services 
 - 서비스의 라우팅을 처리하는 것으로 생성되는 각 서비스의 name과 endpoint, access여부를 관리한다.

## 2. Cipher
### 2-1. RSA

 #### 키 생성
``` code
using System.Security.Cryptography;
using System.Text;

public class RsaKeyGenerator
{
    public static (byte[] privateKeyBlob, string publicKeyXml) GenerateKeys()
    {
        using (var rsa = RSA.Create(2048))
        {
            // Private Key - Export as PKCS#8 (Binary blob)
            var privateKeyBlob = rsa.ExportPkcs8PrivateKey();

            // Public Key - Export as XML string (you could also use PEM format)
            var publicKeyXml = rsa.ToXmlString(false); // false = public only

            return (privateKeyBlob, publicKeyXml);
        }
    }
}
```

#### private 복호화
``` code
using System.Security.Cryptography;

public class RsaCrypto
{
    private readonly RSA _rsa;

    public RsaCrypto(byte[] privateKeyBlob)
    {
        _rsa = RSA.Create();
        _rsa.ImportPkcs8PrivateKey(privateKeyBlob, out _);
    }

    public byte[] Decrypt(byte[] encryptedData)
    {
        return _rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
    }
}
```


#### Client 암호화
``` code
using System.Security.Cryptography;
using System.Text;

public static class RsaClientEncryptor
{
    // publicKeyXml은 서버에서 생성한 XML 문자열을 복사해 넣기
    public const string PublicKeyXml = @"<RSAKeyValue><Modulus>...</Modulus><Exponent>...</Exponent></RSAKeyValue>";

    public static byte[] Encrypt(string plainText)
    {
        using (var rsa = RSA.Create())
        {
            rsa.FromXmlString(PublicKeyXml);
            var bytesToEncrypt = Encoding.UTF8.GetBytes(plainText);
            return rsa.Encrypt(bytesToEncrypt, RSAEncryptionPadding.OaepSHA256);
        }
    }
}
```

#### SQL DDL
```code
CREATE TABLE rsa_keys (
    seq int AUTO_INCREMENT PRIMARY KEY,
    status tinyint unsigned DEFAULT 0,
    unix_millis bigint NOT NULL,
    version VARCHAR(16) NOT NULL,
    public_key blob NOT NULL,
    private_key blob NOT NULL
);
```


### 2-2. AES

#### 키 생성, 암/복호화, DPAPI 사용으로 파일 저장
``` code
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class AesWithDpapi
{
    const string AesKeyFile = "aes_key.bin";
    const string AesIvFile = "aes_iv.bin";

    public static void Main()
    {
        string plainText = "userId=testUser;timestamp=20250519120000;sequence=12345";

        // 키 생성 및 저장 (최초 1회)
        if (!File.Exists(AesKeyFile) || !File.Exists(AesIvFile))
        {
            SaveEncryptedAesKey();
        }

        // 키 불러오기
        byte[] key = LoadEncryptedAesKey();
        byte[] iv = File.ReadAllBytes(AesIvFile);

        // 암호화
        byte[] encrypted = EncryptAes(key, iv, plainText);

        // 복호화
        string decrypted = DecryptAes(key, iv, encrypted);

        Console.WriteLine($"AES Key (Base64): {Convert.ToBase64String(key)}");
        Console.WriteLine($"IV (Base64): {Convert.ToBase64String(iv)}");
        Console.WriteLine($"Encrypted (Base64): {Convert.ToBase64String(encrypted)}");
        Console.WriteLine($"Decrypted: {decrypted}");
    }

    public static void SaveEncryptedAesKey()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32); // AES-256
        byte[] iv = RandomNumberGenerator.GetBytes(16);  // 128-bit IV

        byte[] protectedKey = ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser);

        File.WriteAllBytes(AesKeyFile, protectedKey);
        File.WriteAllBytes(AesIvFile, iv);
    }

    public static byte[] LoadEncryptedAesKey()
    {
        byte[] protectedKey = File.ReadAllBytes(AesKeyFile);
        return ProtectedData.Unprotect(protectedKey, null, DataProtectionScope.CurrentUser);
    }

    public static byte[] EncryptAes(byte[] key, byte[] iv, string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
    }

    public static string DecryptAes(byte[] key, byte[] iv, byte[] cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        byte[] decrypted = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
        return Encoding.UTF8.GetString(decrypted);
    }
}

```
