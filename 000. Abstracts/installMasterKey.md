# Master 키를 레지스트리에 저장하는 툴

```code
using System;
using Microsoft.Win32;

namespace InstallMasterKey
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== InstallMasterKey Utility ===");

            if (args.Length < 1)
            {
                Console.WriteLine("Usage: InstallMasterKey.exe <Base64MasterKey>");
                return;
            }

            string base64MasterKey = args[0];

            try
            {
                byte[] masterKey = Convert.FromBase64String(base64MasterKey);

                // 레지스트리 경로
                const string registryPath = @"Software\YourCompany\YourApp";

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(registryPath, true))
                {
                    if (key == null)
                    {
                        Console.WriteLine("Failed to open or create registry key.");
                        return;
                    }

                    // 바이너리 값으로 저장
                    key.SetValue("MasterKey", masterKey, RegistryValueKind.Binary);
                }

                Console.WriteLine("MasterKey successfully saved to registry:");
                Console.WriteLine(@"HKEY_LOCAL_MACHINE\" + registryPath + @"\MasterKey");
            }
            catch (FormatException)
            {
                Console.WriteLine("Error: Invalid Base64 string for Master Key.");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Error: Access denied. Please run as administrator.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}

```
