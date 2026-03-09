using System;
using System.IO;
using System.Management;
using System.Diagnostics;
using System.Net.NetworkInformation; // Adicionado para checagem de Rede

namespace EasyCleanAgent
{
    public static class HardwareMonitor
    {
        private static PerformanceCounter cpuCounter;

        static HardwareMonitor()
        {
            try
            {
                // Tenta inicializar usando nomes em Inglês (Padrão)
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuCounter.NextValue();
            }
            catch
            {
                try
                {
                    // Tenta usar IDs numéricos ou nomes em Português caso falhe (Sistemas PT-BR)
                    cpuCounter = new PerformanceCounter("Processador", "% de tempo do processador", "_Total");
                    cpuCounter.NextValue();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Falha fatal ao iniciar contador de CPU: {ex.Message}");
                }
            }
        }
        public static HardwareMetrics GetMetrics()
        {
            var metrics = new HardwareMetrics();
            
            try
            {
                metrics.CpuTemperature = GetCpuTemperature();
                metrics.CpuUsage = GetCpuUsage();
                metrics.RamUsage = GetRamUsage();
                metrics.SystemDriveFreeSpace = GetSystemDriveFreeSpace();
                metrics.SystemUptime = GetSystemUptime();
                metrics.IsNetworkConnected = NetworkInterface.GetIsNetworkAvailable();
                metrics.MacAddress = GetMacAddress();
                metrics.CpuModel = GetCpuModel();
                metrics.MachineModel = GetMachineModel();
                metrics.Manufacturer = GetManufacturer();
                metrics.TotalRam = GetTotalRam();
                metrics.Hostname = Environment.MachineName;
                metrics.LoggedUser = Environment.UserName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro lendo hardware: {ex.Message}");
            }

            return metrics;
        }

        private static string GetCpuTemperature()
        {
            try
            {
                // Estratégia 1: MSAcpi_ThermalZoneTemperature (WMI root/wmi)
                using (var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        double temp = Convert.ToDouble(obj["CurrentTemperature"]);
                        // Kelvin para Celsius: (K - 273.15) / 10 ou direto se o driver já der em décimos de Kelvin
                        temp = (temp - 2731.5) / 10.0;
                        if (temp > 0 && temp < 120) return $"{temp:F1}°C";
                    }
                }

                // Estratégia 2: Win32_TemperatureProbe (WMI root/cimv2)
                using (var searcher = new ManagementObjectSearcher("SELECT CurrentReading FROM Win32_TemperatureProbe"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var reading = obj["CurrentReading"];
                        if (reading != null) return $"{reading}°C";
                    }
                }
            }
            catch { }

            return "N/A";
        }

        private static string GetCpuUsage()
        {
            try
            {
                if (cpuCounter != null)
                {
                    float usage = cpuCounter.NextValue();
                    return $"{Math.Round(usage)}%";
                }

                using (var searcher = new ManagementObjectSearcher("select LoadPercentage from Win32_Processor"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return $"{obj["LoadPercentage"]}%";
                    }
                }
                return "0%";
            }
            catch { return "N/A"; }
        }

        private static string GetRamUsage()
        {
            try
            {
                using (var mc = new ManagementClass("Win32_OperatingSystem"))
                using (var moc = mc.GetInstances())
                {
                    foreach (ManagementObject item in moc)
                    {
                        double total = Convert.ToDouble(item["TotalVisibleMemorySize"]);
                        double free = Convert.ToDouble(item["FreePhysicalMemory"]);
                        double used = total - free;
                        double percent = (used / total) * 100;
                        return $"{percent:F1}%";
                    }
                }
            }
            catch { }
            return "N/A";
        }

        private static string GetSystemDriveFreeSpace()
        {
            try
            {
                string systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
                string driveLetter = Path.GetPathRoot(systemPath)!;
                var driveInfo = new DriveInfo(driveLetter);

                double freeGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                double totalGB = driveInfo.TotalSize / (1024.0 * 1024 * 1024);
                double percentFree = (freeGB / totalGB) * 100;

                return $"{percentFree:F1}% ({freeGB:F1} GB Lívres)";
            }
            catch { return "N/A"; }
        }

        // Recupera o tempo em que a máquina está ligada (Uptime), método muito rápido nativo
        private static string GetSystemUptime()
        {
            try
            {
                TimeSpan uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
                return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
            }
            catch { return "N/A"; }
        }

        // Pega o endereço MAC da primeira placa de rede ativa (Identificador Único)
        private static string GetMacAddress()
        {
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    // Ignora adaptadores de loopback ou túneis
                    if (nic.NetworkInterfaceType != NetworkInterfaceType.Loopback && 
                        nic.OperationalStatus == OperationalStatus.Up)
                    {
                        return nic.GetPhysicalAddress().ToString(); 
                    }
                }
            }
            catch { }
            return Guid.NewGuid().ToString(); // Fallback caso não ache MAC
        }

        private static string GetCpuModel()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("select Name from Win32_Processor"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        var name = obj["Name"]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(name)) return name;
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Erro CPU: {ex.Message}"); }
            return "Desconhecido";
        }

        private static string GetMachineModel()
        {
            try
            {
                // Tenta ComputerSystem primeiro
                using (var searcher = new ManagementObjectSearcher("select Model from Win32_ComputerSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        var model = obj["Model"]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(model) && model != "To be filled by O.E.M.") return model;
                    }
                }

                // Fallback para BaseBoard (Placa Mãe)
                using (var searcher = new ManagementObjectSearcher("select Product from Win32_BaseBoard"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return obj["Product"]?.ToString()?.Trim() ?? "Desconhecido";
                    }
                }
            }
            catch { }
            return "Desconhecido";
        }

        private static string GetManufacturer()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("select Manufacturer from Win32_ComputerSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        var m = obj["Manufacturer"]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(m) && m != "To be filled by O.E.M.") return m;
                    }
                }
                
                using (var searcher = new ManagementObjectSearcher("select Manufacturer from Win32_BaseBoard"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return obj["Manufacturer"]?.ToString()?.Trim() ?? "Desconhecido";
                    }
                }
            }
            catch { }
            return "Desconhecido";
        }

        private static string GetTotalRam()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("select TotalPhysicalMemory from Win32_ComputerSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        double totalBytes = Convert.ToDouble(obj["TotalPhysicalMemory"]);
                        double totalGB = totalBytes / (1024.0 * 1024 * 1024);
                        return $"{Math.Round(totalGB)} GB";
                    }
                }
            }
            catch { }
            return "Desconhecida";
        }
    }

    public class HardwareMetrics
    {
        public string CpuTemperature { get; set; } = "Desconhecida";
        public string CpuUsage { get; set; } = "0%";
        public string RamUsage { get; set; } = "0%";
        public string SystemDriveFreeSpace { get; set; } = "N/A";
        public string SystemUptime { get; set; } = "N/A";
        public bool IsNetworkConnected { get; set; } = false;
        public string MacAddress { get; set; } = "";
        public string CpuModel { get; set; } = "Desconhecido";
        public string MachineModel { get; set; } = "Desconhecido";
        public string SystemModel { get; set; } = "Desconhecido"; // Alias usado no Form
        public string Manufacturer { get; set; } = "Desconhecido";
        public string TotalRam { get; set; } = "Desconhecida";
        public string Hostname { get; set; } = "Desconhecido";
        public string LoggedUser { get; set; } = "Desconhecido";
    }
}
