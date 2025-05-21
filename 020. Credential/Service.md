# Credential Service

- 사용자 인증 처리와 사용자 정보 관리를 하는 서비스다.
- 실행시 args에 따라 master key를 레지스트리에 저장하는 기능도 포함한다.

```code
using Microsoft.Win32;
using System;

class Program
{
    private const string RegistryPath = @"SOFTWARE\YourCompany\YourApp";
    private const string RegistryValueName = "MasterKey";

    static void Main(string[] args)
    {
        if (args.Length == 2 && args[0].Equals("install-master-key", StringComparison.OrdinalIgnoreCase))
        {
            string base64Key = args[1];
            try
            {
                byte[] masterKey = Convert.FromBase64String(base64Key);
                bool success = WriteMasterKeyToRegistry(masterKey);

                if (success)
                    Console.WriteLine("Master Key installed successfully.");
                else
                    Console.WriteLine("Failed to install Master Key.");
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid Base64 string for Master Key.");
            }
        }
        else
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  InstallMasterKey.exe install-master-key <Base64MasterKey>");
        }
    }

    private static bool WriteMasterKeyToRegistry(byte[] masterKey)
    {
        try
        {
            using RegistryKey? baseKey = Registry.LocalMachine.CreateSubKey(RegistryPath);
            if (baseKey == null)
            {
                Console.WriteLine("Failed to open or create registry key.");
                return false;
            }

            baseKey.SetValue(RegistryValueName, masterKey, RegistryValueKind.Binary);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to registry: {ex.Message}");
            return false;
        }
    }
}

```
