using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using System.IO;

namespace EasyCleanAgent
{
    public class UpdateManager
    {
        public const string CurrentVersion = "1.0.5";
        // Substitua 'usuario' e 'repositorio' pelos seus dados reais do GitHub
        private const string GitHubRepo = "Jota-P3-dev/EasyClean";
        private const string UpdateApiUrl = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";

        public static async Task CheckForUpdates()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // A API do GitHub exige um User-Agent
                    client.DefaultRequestHeaders.Add("User-Agent", "EasyCleanAgent-Updater");
                    
                    var response = await client.GetStringAsync(UpdateApiUrl);
                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        string latestVersion = doc.RootElement.GetProperty("tag_name").GetString().Replace("v", "");
                        
                        if (IsNewerVersion(latestVersion, CurrentVersion))
                        {
                            // Pega o primeiro asset que termina com .exe
                            var assets = doc.RootElement.GetProperty("assets");
                            foreach (var asset in assets.EnumerateArray())
                            {
                                string downloadUrl = asset.GetProperty("browser_download_url").GetString();
                                if (downloadUrl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                {
                                    await DownloadAndApplyUpdate(downloadUrl);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao verificar atualizações no GitHub: {ex.Message}");
            }
        }

        private static bool IsNewerVersion(string latest, string current)
        {
            try {
                Version vLatest = new Version(latest);
                Version vCurrent = new Version(current);
                return vLatest > vCurrent;
            } catch { return false; }
        }

        private static async Task DownloadAndApplyUpdate(string url)
        {
            try {
                // 1. Define caminhos
                string tempPath = Path.Combine(Path.GetTempPath(), "EasyCleanUpdate.exe");
                string batPath = Path.Combine(Path.GetTempPath(), "update.bat");
                string currentExe = Process.GetCurrentProcess().MainModule.FileName;

                // 2. Baixa o novo executável
                using (var client = new HttpClient()) {
                    var data = await client.GetByteArrayAsync(url);
                    File.WriteAllBytes(tempPath, data);
                }

                // 3. Cria o script .bat para fazer a substituição "a frio"
                string batContent = $@"
@echo off
timeout /t 2 /nobreak > nul
:loop
del /f /q ""{currentExe}""
if exist ""{currentExe}"" (
    timeout /t 1 /nobreak > nul
    goto loop
)
move /y ""{tempPath}"" ""{currentExe}""
start """" ""{currentExe}""
del ""%~f0""
";
                File.WriteAllText(batPath, batContent);

                // 4. Executa o bat e fecha o app atual
                Process.Start(new ProcessStartInfo {
                    FileName = batPath,
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                
                Environment.Exit(0);
            } catch (Exception ex) {
                Debug.WriteLine("Falha no processo de update: " + ex.Message);
            }
        }
    }

    public class UpdateResponse
    {
        public bool update_available { get; set; }
        public string latest_version { get; set; }
        public string download_url { get; set; }
        public string changelog { get; set; }
    }
}
