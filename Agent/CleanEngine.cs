using System;
using System.Diagnostics;
using System.IO;

namespace EasyCleanAgent
{
    public static class CleanEngine
    {
        private static string EnsureEngineExtracted()
        {
            try
            {
                string programDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EasyClean");
                string tempEnginePath = Path.Combine(programDataPath, "Engine");
                
                if (!Directory.Exists(tempEnginePath))
                {
                    Directory.CreateDirectory(tempEnginePath);
                }

                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string[] resourceNames = assembly.GetManifestResourceNames();

                foreach (string resName in resourceNames)
                {
                    if (resName.Contains(".engine."))
                    {
                        // Ex: EasyCleanAgent.engine.DirectClean.ps1
                        string fileName = resName.Substring(resName.IndexOf(".engine.") + 8);
                        string filePath = Path.Combine(tempEnginePath, fileName);
                        
                        using (Stream stream = assembly.GetManifestResourceStream(resName))
                        {
                            if (stream != null)
                            {
                                using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    stream.CopyTo(fileStream);
                                }
                            }
                        }
                    }
                }
                return tempEnginePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erro ao extrair engine: " + ex.Message);
                return null;
            }
        }

        public static string RunScript(string scriptName, Action<string> onOutput = null)
        {
            try
            {
                // Extrai os scripts embutidos para uma pasta temporária
                string enginePath = EnsureEngineExtracted();
                
                if (string.IsNullOrEmpty(enginePath))
                {
                    string err = "Erro: Falha ao extrair arquivos do motor de limpeza interno.";
                    onOutput?.Invoke(err);
                    return err;
                }

                string scriptPath = Path.Combine(enginePath, scriptName);

                if (!File.Exists(scriptPath))
                {
                    string err = $"Erro: Script {scriptName} não encontrado em {scriptPath}";
                    onOutput?.Invoke(err);
                    return err;
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = scriptName.EndsWith(".bat") ? "cmd.exe" : "powershell.exe",
                    Arguments = scriptName.EndsWith(".bat") ? $"/c \"cd /d \"{enginePath}\" && \"{scriptName}\"\"" : $"-NoProfile -ExecutionPolicy Bypass -Command \"& '{scriptPath}'\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = enginePath // Importante para scripts que chamam outros na mesma pasta
                };

                System.Text.StringBuilder outputBuilder = new System.Text.StringBuilder();

                using (var process = new Process { StartInfo = processInfo })
                {
                    process.OutputDataReceived += (sender, args) => { 
                        if (!string.IsNullOrEmpty(args.Data)) {
                            onOutput?.Invoke(args.Data); 
                            outputBuilder.AppendLine(args.Data);
                        }
                    };
                    process.ErrorDataReceived += (sender, args) => { 
                        if (!string.IsNullOrEmpty(args.Data)) {
                            onOutput?.Invoke("ERRO: " + args.Data); 
                            outputBuilder.AppendLine("ERRO: " + args.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    
                    process.WaitForExit();
                    
                    string fullLog = outputBuilder.ToString().Trim();
                    if (string.IsNullOrWhiteSpace(fullLog)) fullLog = "Execução finalizada sem nenhum log de saída.";

                    if (process.ExitCode != 0)
                    {
                        return $"FALHA: O processo encerrou com código {process.ExitCode}\n\nDetalhes:\n{fullLog}";
                    }

                    return $"SUCESSO: Comando executado.\n\nDetalhes:\n{fullLog}";
                }
            }
            catch (Exception ex)
            {
                string errCritical = $"ERRO CRITÍCO: {ex.Message}";
                onOutput?.Invoke(errCritical);
                return errCritical;
            }
        }
    }
}
