using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.IO;

namespace MoonAPI
{
    public static class Api
    {
        private static Moon moon = new Moon();

        static Api()
        {
            StartMonitoring();
        }

        private static async void StartMonitoring()
        {
            while (true)
            {
                if (!IsRobloxOpen() && moon.IsInjected())
                {
                    moon.Deject();
                }
                await Task.Delay(1000);
            }
        }

        public static void Inject() => Inject();

        public static void KillRoblox() => moon?.KillRoblox();

        public static bool IsInjected() => moon?.IsInjected() ?? false;

        public static bool IsRobloxOpen() => Process.GetProcessesByName("RobloxPlayerBeta").Length > 0;

        public static void ExecuteScript(string script) => moon?.ExecuteScript(script);

        public static void SetAutoInject(bool value) => moon?.AutoInject(value);
    }

    public class Moon
    {
        public static string APIversion = "1";
        private static string executorName = "MoonAPI";
        private bool isInjected;
        private bool autoinject;
        private bool isUpdating = false;

        [DllImport("Injector.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void StartClient();

        [DllImport("Injector.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void ExecuteSC(byte[] scriptSource);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        public Moon()
        {
            StartAutoInjectionLoop();
        }

        private async void StartAutoInjectionLoop()
        {
            while (true)
            {
                if (Api.IsRobloxOpen() && autoinject && !isInjected)
                {
                    Inject();
                }
                await Task.Delay(1000);
            }
        }

        public void KillRoblox()
        {
            var processes = Process.GetProcessesByName("RobloxPlayerBeta");
            foreach (var process in processes)
            {
                TryKillProcess(process);
            }
        }

        private static void TryKillProcess(Process process)
        {
            try
            {
                process.Kill();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error killing process: {ex.Message}");
            }
        }

        private async Task IsLatestVersion()
        {
            isUpdating = true;
            string latestVersionUrl = "https://raw.githubusercontent.com/trewzy/web/refs/heads/main/types/files/version.txt";
            string InjectorURL = "https://github.com/trewzy/web/raw/refs/heads/main/types/files/Injector.dll";
            string ApiURL = "https://github.com/trewzy/web/raw/refs/heads/main/types/files/MoonAPI.dll";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string latestVersion = await client.GetStringAsync(latestVersionUrl);
                    latestVersion = latestVersion.Trim();

                    if (!APIversion.Equals(latestVersion, StringComparison.OrdinalIgnoreCase))
                    {
                        string startupPath = Application.StartupPath;

                        ReplaceFile(Path.Combine(startupPath, "Injector.dll"), ".old");
                        ReplaceFile(Path.Combine(startupPath, "MoonAPI.dll"), ".old");

                        await DownloadFile(client, InjectorURL, Path.Combine(startupPath, "Injector.dll"));
                        await DownloadFile(client, ApiURL, Path.Combine(startupPath, "MoonAPI.dll"));

                        MessageBox.Show("Updated. Please reopen your executor.", "MoonAPI Update");

                        await Task.Delay(1000);
                        Environment.Exit(0);
                    }
                }
                catch (HttpRequestException e)
                {
                    MessageBox.Show("Error checking latest version: " + e.Message, "MoonAPI Error");
                }
                catch (IOException ioEx)
                {
                    MessageBox.Show("File operation error: " + ioEx.Message, "MoonAPI Error");
                }
                finally
                {
                    isUpdating = false;
                }
            }
        }

        private static async Task UpdateFiles(HttpClient client, string InjectorURL, string ApiURL)
        {
            string startupPath = Application.StartupPath;
            ReplaceFile(Path.Combine(startupPath, "Injector.dll"), "Injector.old");
            ReplaceFile(Path.Combine(startupPath, "MoonAPI.dll"), "MoonAPIOld.old");

            await DownloadFile(client, InjectorURL, Path.Combine(startupPath, "Injector.dll"));
            await DownloadFile(client, ApiURL, Path.Combine(startupPath, "MoonAPI.dll"));

            MessageBox.Show("Updated, please reopen your executor", "MoonAPI Update");
            Environment.Exit(0);
        }

        private static void ReplaceFile(string filePath, string backupExtension)
        {
            string backupPath = Path.ChangeExtension(filePath, backupExtension);

            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }

            if (File.Exists(filePath))
            {
                File.Move(filePath, backupPath);
            }
        }

        private static async Task DownloadFile(HttpClient client, string url, string destinationPath)
        {
            byte[] data = await client.GetByteArrayAsync(url);

            using (FileStream fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await fs.WriteAsync(data, 0, data.Length);
            }
        }

        public void AutoInject(bool value) => autoinject = value;

        public bool IsInjected() => isInjected;

        private void CleanUpOldFiles()
        {
            string startupPath = Application.StartupPath;

            string APIoldpath = Path.Combine(startupPath, "MoonAPI.old");
            string injectorOldPath = Path.Combine(startupPath, "Injector.old");

            if (File.Exists(APIoldpath)) File.Delete(APIoldpath);
            if (File.Exists(injectorOldPath)) File.Delete(injectorOldPath);
        }

        public async void Inject()
        {
            if (isUpdating) return;

            await IsLatestVersion();

            string startupPath = Application.StartupPath;
            string InjectorPath = Path.Combine(startupPath, "Injector.dll");

            if (!File.Exists(InjectorPath))
            {
                await DownloadFile(new HttpClient(), "https://github.com/trewzy/web/raw/refs/heads/main/types/files/Injector.dll", InjectorPath);
            }

            if (!isUpdating && Api.IsRobloxOpen())
            {
                CleanUpOldFiles();

                try
                {
                    StartClient();
                    isInjected = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to attach MoonAPI: {ex.Message}", "Attaching Error");
                    isInjected = false;
                }
            }
        }

        public void Deject()
        {
            isInjected = false;
            IntPtr hModule = GetModuleHandle("Injector.dll");
            if (hModule != IntPtr.Zero)
            {
                FreeLibrary(hModule);
            }
        }

        public void ExecuteScript(string script)
        {
            if (IsInjected() && Api.IsRobloxOpen())
            {
                if (script == "print(identifyexecutor())")
                {
                    script = $"print('{executorName}')";
                    ExecuteSC(Encoding.UTF8.GetBytes(script));
                    return;
                }
                else
                {
                    ExecuteSC(Encoding.UTF8.GetBytes(script));
                    return;
                }
            }
        }
    }
}
