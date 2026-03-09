using System;
using Microsoft.Win32;

namespace EasyCleanAgent
{
    public class MachineService
    {
        private System.Threading.Timer monitoringTimer;
        
        public MachineService()
        {
            SetAutoStart();
        }

        // Configura para rodar com o Windows (Startup Registry)
        private void SetAutoStart()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? System.AppContext.BaseDirectory;

                    key?.SetValue("EasyCleanAgent", $"\"{exePath}\"");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Falha no AutoStart: {ex.Message}");
            }
        }

        // Inicia o timer que executa a cada 30 segundos (30000 ms)
        public void StartMonitoringTask(Action periodicAction)
        {
            // Delay inicial 5s, depois a cada 30 segundos para checagem ONLINE/OFFLINE no portal
            monitoringTimer = new System.Threading.Timer(e => periodicAction(), null, 5000, 30000); 
        }

        public void StopMonitoringTask()
        {
            monitoringTimer?.Dispose();
        }
    }
}
