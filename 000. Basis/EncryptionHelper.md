# 암/복호화 Helper


## RSA

## RSAEncryptionPadding 요약 정리

### 개요
- `RSAEncryptionPadding`은 RSA 암호화에서 사용할 **패딩 알고리즘**을 지정하는 데 사용됩니다.
- 패딩은 보안성 확보를 위해 필수적이며, 적절한 알고리즘 선택이 중요합니다.

---

### 주요 패딩 종류

| 패딩 | 설명 | 권장 여부 |
|------|------|-----------|
| `Pkcs1` | 오래된 표준 (PKCS#1 v1.5). 보안 취약점 존재 | 비권장 |
| `OaepSHA1` | SHA-1 기반 OAEP. SHA-1의 보안성 문제로 사용 자제 | 비권장 |
| `OaepSHA256` | SHA-256 기반 OAEP. **현재 가장 권장되는 방식** | **권장** |
| `OaepSHA384` / `OaepSHA512` | 더 강력한 해시 기반 OAEP. 고보안 환경에 적합 | 선택적 권장 |

---

### 선택 가이드

| 환경 | 추천 패딩 |
|------|-----------|
| 일반적인 현대 시스템 | `OaepSHA256` |
| 고보안 시스템 (금융, 공공기관 등) | `OaepSHA384` 또는 `OaepSHA512` |
| 구형 시스템과의 호환 필요 | `Pkcs1` (보안 위험 감수 필요) |

---

### 결론
- **가능한 한 `OaepSHA256` 이상을 사용하는 것이 보안상 안전**합니다.
- `Pkcs1`은 호환성 외에는 사용할 이유가 거의 없습니다.

 
### 예제 코드

``` code
using System;
using System.Security.Cryptography;
using System.Text;

public static class RsaEncryptionHelper
{
    // XML 문자열 방식 (기존)
    public static (string publicKeyXml, string privateKeyXml) GenerateKeyPairXml()
    {
        using (var rsa = RSA.Create(2048))
        {
            string publicKeyXml = rsa.ToXmlString(false); // 공개키
            string privateKeyXml = rsa.ToXmlString(true); // 비공개키
            return (publicKeyXml, privateKeyXml);
        }
    }

    public static byte[] EncryptWithXml(string plainText, string publicKeyXml)
    {
        using (var rsa = RSA.Create())
        {
            rsa.FromXmlString(publicKeyXml);
            byte[] data = Encoding.UTF8.GetBytes(plainText);
            return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        }
    }

    public static string DecryptWithXml(byte[] cipherBytes, string privateKeyXml)
    {
        using (var rsa = RSA.Create())
        {
            rsa.FromXmlString(privateKeyXml);
            byte[] decrypted = rsa.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA256);
            return Encoding.UTF8.GetString(decrypted);
        }
    }

    // -----------------------------------
    // byte[] 방식 (추천)
    // -----------------------------------

    public static (byte[] publicKeyBytes, byte[] privateKeyBytes) GenerateKeyPairBytes()
    {
        using (var rsa = RSA.Create(2048))
        {
            // 공개키: X.509 SubjectPublicKeyInfo 형식
            byte[] publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();

            // 개인키: PKCS#8 PrivateKeyInfo 형식
            byte[] privateKeyBytes = rsa.ExportPkcs8PrivateKey();

            return (publicKeyBytes, privateKeyBytes);
        }
    }

    public static byte[] EncryptWithBytes(string plainText, byte[] publicKeyBytes)
    {
        using (var rsa = RSA.Create())
        {
            // 공개키 복원
            rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            byte[] data = Encoding.UTF8.GetBytes(plainText);
            return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        }
    }

    public static string DecryptWithBytes(byte[] cipherBytes, byte[] privateKeyBytes)
    {
        using (var rsa = RSA.Create())
        {
            // 개인키 복원
            rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);

            byte[] decrypted = rsa.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA256);
            return Encoding.UTF8.GetString(decrypted);
        }
    }
}

```



## 2-2. AES

# CipherMode 및 PaddingMode 요약

## CipherMode 열거형 요약

| 모드     | 설명                                                    | 특징                                                   |
|----------|---------------------------------------------------------|--------------------------------------------------------|
| CBC      | Cipher Block Chaining                                   | 각 블록이 이전 암호문 블록과 연결되어 암호화됨. IV 필요. 보안적으로 널리 사용됨. |
| ECB      | Electronic Codebook                                     | 각 블록을 독립적으로 암호화. 패턴 노출 위험 있음. 사용 비추천. |
| CFB      | Cipher Feedback                                          | 블록 암호를 스트림 암호처럼 사용. 작은 단위 암호화 가능.       |
| OFB      | Output Feedback                                          | 암호화 출력 값을 다음 입력으로 사용. 오류 전파 없음. 스트림 암호와 유사. |
| CTS      | Cipher Text Stealing                                    | 마지막 블록이 블록 크기보다 작을 경우 처리. CBC와 함께 사용.  |

---

## PaddingMode 열거형 요약

| 모드      | 설명                                                     | 특징                                                       |
|-----------|----------------------------------------------------------|------------------------------------------------------------|
| None      | 패딩 없음                                                | 입력이 블록 크기의 배수여야 함. 직접 처리 필요.                  |
| PKCS7     | 부족한 블록에 패딩 바이트 수로 채움                     | 가장 일반적으로 사용됨. 대부분의 시스템과 호환됨.                |
| Zeros     | 부족한 부분을 0x00 바이트로 채움                        | 텍스트 데이터에는 부적합할 수 있음.                              |
| ANSIX923  | 마지막 바이트에 패딩 길이를 쓰고, 나머지는 0x00으로 채움 | 보안적으로 PKCS7보다 약간 덜 일반적이나 사용 가능.               |
| ISO10126  | 마지막 바이트에 패딩 길이를 쓰고, 나머지는 임의 값으로 채움 | 무작위성 포함. 예측 어려움. 현재는 잘 사용되지 않음.              |


### 예제 코드
```code
using System;
using System.Security.Cryptography;
using System.Text;

public static class AesEncryptionHelper
{
    // AES 키와 IV를 생성하여 반환 (키: 32바이트(256bit), IV: 16바이트(128bit))
    public static (byte[] key, byte[] iv) GenerateKeyIv()
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        aes.GenerateIV();
        return (aes.Key, aes.IV);
    }

    // 평문을 AES-CBC + PKCS7 패딩으로 암호화 (키, IV 필요)
    public static byte[] Encrypt(string plainText, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var encryptor = aes.CreateEncryptor();

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
    }

    // 암호문을 AES-CBC + PKCS7 패딩으로 복호화 (키, IV 필요)
    public static string Decrypt(byte[] cipherBytes, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var decryptor = aes.CreateDecryptor();

        byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}

```

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
