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

### 스트림 처리
```code
using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

public class GatewayStreamProcessor
{
    // --- Credential 서비스 연동 시뮬레이션 ---
    // 실제 환경에서는 Credential 서비스의 API를 비동기 호출하여 세션 정보를 가져옵니다.
    // 여기서는 메모리 상의 ConcurrentDictionary로 스레드 안전한 가상 세션 정보를 저장해둡니다.
    private static ConcurrentDictionary<string, (byte[] aesKey, byte[] aesIV, string userId)> _sessionStore =
        new ConcurrentDictionary<string, (byte[] aesKey, byte[] aesIV, string userId)>();

    // 시뮬레이션: 초기 연결 성공 시 세션 정보를 저장하는 메서드 (서버 측에서 호출)
    public static void StoreSession(string sessionId, byte[] aesKey, byte[] aesIV, string userId)
    {
        _sessionStore[sessionId] = (aesKey, aesIV, userId);
        Console.WriteLine($"[SIM] 세션 정보 저장됨 (ID: {sessionId})");
    }

    // 시뮬레이션: 세션 ID로 AES 키, IV 및 사용자 정보를 비동기적으로 가져오는 메서드 (Gateway에서 호출)
    private static Task<(byte[] aesKey, byte[] aesIV, string userId)?> GetAesKeyAndIVForSessionAsync(string sessionId)
    {
        // 실제 Credential 서비스 API 호출을 시뮬레이션 (간단히 Task.FromResult 사용)
        if (_sessionStore.TryGetValue(sessionId, out var sessionInfo))
        {
            Console.WriteLine($"[SIM] 세션 정보 발견 (ID: {sessionId}, User: {sessionInfo.userId})");
            return Task.FromResult<(byte[] aesKey, byte[] aesIV, string userId)?>(sessionInfo);
        }
        Console.WriteLine($"[SIM] 세션 정보 찾을 수 없음 (ID: {sessionId})");
        return Task.FromResult<(byte[] aesKey, byte[] aesIV, string userId)?>(null); // 세션 정보 없음 (무효 세션)
    }
    // --- 시뮬레이션 끝 ---


    /// <summary>
    /// 암호화된 입력 스트림을 복호화하여 다른 출력 스트림으로 전달합니다.
    /// Stream.CopyToAsync를 사용하여 버퍼 관리를 자동으로 처리합니다.
    /// </summary>
    /// <param name="encryptedInputStream">클라이언트로부터 받은 암호화된 요청 본문 스트림</param>
    /// <param name="sessionId">클라이언트 요청에서 추출한 세션 ID</param>
    /// <param name="decryptedOutputStream">백엔드 서비스로 보낼 복호화된 요청 본문 스트림</param>
    /// <returns>처리 작업 Task. 복호화 성공 시 사용자 ID 반환, 실패 시 null 반환.</returns>
    public static async Task<string?> ProcessAndForwardEncryptedStreamAsync(
        Stream encryptedInputStream,
        string sessionId,
        Stream decryptedOutputStream)
    {
        // 1. 세션 ID로 해당 세션의 AES 키와 IV, 사용자 정보를 비동기적으로 가져옵니다.
        var sessionInfo = await GetAesKeyAndIVForSessionAsync(sessionId);

        if (sessionInfo == null)
        {
            Console.WriteLine($"[ERROR] 복호화 실패: 세션 ID '{sessionId}'가 유효하지 않거나 만료됨.");
            return null; // 유효하지 않거나 만료된 세션
        }

        byte[] sessionAesKey = sessionInfo.Value.aesKey;
        byte[] sessionAesIV = sessionInfo.Value.aesIV;
        string userId = sessionInfo.Value.userId;

        // 2. AES 객체 생성 (암호화 시 사용했던 것과 동일한 키, IV, 모드, 패딩)
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = sessionAesKey;
            aesAlg.IV = sessionAesIV;
            aesAlg.Mode = CipherMode.CBC; // 클라이언트 암호화와 일치해야 함
            aesAlg.Padding = PaddingMode.PKCS7; // 클라이언트 암호화와 일치해야 함

            // 3. 복호화 객체 생성
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            // 4. 암호화된 입력 스트림 위에 CryptoStream을 겹쳐서 생성
            // 이 스트림(csDecrypt)에서 데이터를 읽으면 자동으로 복호화됩니다.
            using (CryptoStream csDecrypt = new CryptoStream(encryptedInputStream, decryptor, CryptoStreamMode.Read))
            {
                Console.WriteLine($"[INFO] 복호화 및 스트리밍 시작 (Session ID: {sessionId})");

                try
                {
                    // 5. 복호화된 데이터를 CryptoStream에서 읽어 출력 스트림으로 복사합니다.
                    // CopyToAsync 메서드가 내부적으로 버퍼를 사용하여 효율적으로 처리합니다.
                    await csDecrypt.CopyToAsync(decryptedOutputStream);

                    Console.WriteLine($"[INFO] 스트림 복호화 및 전달 완료 (Session ID: {sessionId})");

                    // 출력 스트림의 버퍼를 비워 최종 데이터가 전송되도록 합니다.
                    await decryptedOutputStream.FlushAsync();

                    // 복호화 및 전달 성공 시 사용자 ID 반환
                    return userId;
                }
                catch (CryptographicException e)
                {
                    // 암호화 오류 발생 (잘못된 키/IV, 패딩 오류, 데이터 변조 등)
                    Console.WriteLine($"[ERROR] 스트림 복호화 중 Cryptographic 오류 발생 (Session ID: {sessionId}): {e.Message}");
                    // 보안상 연결을 종료하거나 오류 응답을 보내야 합니다.
                    return null;
                }
                catch (Exception e)
                {
                    // 기타 오류 발생 (네트워크 오류, 스트림 오류 등)
                    Console.WriteLine($"[ERROR] 스트림 처리 중 오류 발생 (Session ID: {sessionId}): {e.Message}");
                    // 오류 처리 로직
                    return null;
                }
            } // using (CryptoStream) 자동 Dispose
        } // using (Aes) 자동 Dispose
    } // method end
}
```
