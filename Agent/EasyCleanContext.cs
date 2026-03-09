using System;
using System.Drawing;
using System.Windows.Forms;
using Supabase;

namespace EasyCleanAgent
{
    public class EasyCleanContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private Client supabaseClient;
        private MachineService machineService;
        private MainForm mainForm;

        public EasyCleanContext()
        {
            trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Information,
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true,
                Text = "EasyClean Agent"
            };

            // --- Menu de Contexto Refinado ---
            var openItem = new ToolStripMenuItem("Abrir Painel Corporativo");
            openItem.Font = new Font(openItem.Font, FontStyle.Bold);
            openItem.Click += OpenPanel;

            trayIcon.ContextMenuStrip.Items.Add(openItem);
            trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            trayIcon.ContextMenuStrip.Items.Add("Verificar Status do Hardware", null, ShowHwStatus);
            trayIcon.ContextMenuStrip.Items.Add("Sincronizar com Portal", null, SyncNow);
            trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            trayIcon.ContextMenuStrip.Items.Add("Sair do Agente", null, Exit);

            trayIcon.DoubleClick += OpenPanel;

            machineService = new MachineService();
            mainForm = new MainForm(this); // Inicializa escondido, o Windows cuida disso
            
            InitializeAgent();
        }

        private void ShowHwStatus(object? sender, EventArgs e)
        {
            var metrics = HardwareMonitor.GetMetrics();
            MessageBox.Show($"Máquina: {metrics.MachineModel}\nProcessador: {metrics.CpuModel}\nMemória Total: {metrics.TotalRam}\n" +
                          $"CPU Temp: {metrics.CpuTemperature}\nArmazenamento Livre: {metrics.SystemDriveFreeSpace}\n\n" +
                          $"Status de Conexão: {(metrics.IsNetworkConnected ? "ONLINE" : "OFFLINE")}", 
                          "Status do Hardware Escaneado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OpenPanel(object? sender, EventArgs e)
        {
            if (mainForm.IsDisposed)
            {
                mainForm = new MainForm(this);
            }
            mainForm.Show();
            mainForm.WindowState = FormWindowState.Maximized;
            mainForm.BringToFront();
        }

        private async void InitializeAgent()
        {
            try
            {
                var url = "https://vbnuqgyekcqxmfqiihjw.supabase.co";
                var key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InZibnVxZ3lla2NxeG1mcWlpaGp3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzI1ODAwNTEsImV4cCI6MjA4ODE1NjA1MX0.PXiA-LED8v5WMg4KwR-d1mdNycn4lKu18pDWmpryStA";
                
                var options = new SupabaseOptions { AutoConnectRealtime = true };
                supabaseClient = new Client(url, key, options);
                
                await supabaseClient.InitializeAsync();
                
                // Inicia o timer que roda a cada 30min
                machineService.StartMonitoringTask(() => MonitorHardwareLoop());
                
                // Inicia a escuta de comandos remotos (Realtime)
                ListenToCommands();

                trayIcon.BalloonTipTitle = "Proteção Ativa";
                trayIcon.BalloonTipText = "EasyClean Agent foi iniciado e está monitorando o sistema silenciosamente.";
                trayIcon.ShowBalloonTip(3000);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro inicial: {ex.Message}", "EasyClean Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void MonitorHardwareLoop()
        {
            try 
            {
                var metrics = HardwareMonitor.GetMetrics();
                
                // Exemplo de alerta se uso da CPU > 90%
                if (metrics.CpuUsage.Contains("%"))
                {
                    double usage = double.Parse(metrics.CpuUsage.Replace("%", ""));
                    if (usage > 90) 
                    {
                        trayIcon.ShowBalloonTip(5000, "Alerta de Desempenho", "A CPU está operando com uso muito alto (>90%). Recomendamos fechar aplicativos em segundo plano ou realizar uma otimização no Portal.", ToolTipIcon.Warning);
                        
                        // Gravar Log no Supabase Avisando o Analista
                        if (supabaseClient != null)
                        {
                            await supabaseClient.From<LogModel>().Insert(new LogModel {
                                MaquinaId = metrics.MacAddress ?? "UNKNOWN_MAC",
                                Acao = "ALERTA_CPU",
                                Detalhes = $"Uso de CPU muito alto: {metrics.CpuUsage}",
                                Status = "ALERTA",
                                ComandoComando = "MONITORAMENTO"
                            });
                        }
                    }
                }

                // Enviar métricas (Heartbeat) p/ Tabela Maquinas no Supabase usando MAC (Upsert)
                if (supabaseClient != null)
                {
                    var maquina_atual = new MaquinaModel
                    {
                        Id = metrics.MacAddress, // O MAC é a Chave Primária Única!
                        EmpresaId = "11111111-1111-1111-1111-111111111111", // Fake por enquanto (Você criará uma empresa dps)
                        NomeMaquina = metrics.LoggedUser,
                        Hostname = metrics.Hostname,
                        SistemaOperacional = Environment.OSVersion.VersionString,
                        Status = metrics.IsNetworkConnected ? "ONLINE" : "OFFLINE",
                        TemperaturaCpu = metrics.CpuTemperature,
                        SaudeDisco = metrics.SystemDriveFreeSpace,
                        SystemUptime = metrics.SystemUptime,
                        IsNetworkConnected = metrics.IsNetworkConnected,
                        ModeloCpu = metrics.CpuModel,
                        RamTotal = metrics.TotalRam,
                        ModeloMaquinaHw = metrics.MachineModel,
                        Fabricante = metrics.Manufacturer,
                        UsoCpu = metrics.CpuUsage,
                        UsoRam = metrics.RamUsage,
                        UltimaConexao = DateTime.UtcNow
                    };

                    await supabaseClient.From<MaquinaModel>().Upsert(maquina_atual);
                    System.Diagnostics.Debug.WriteLine($"Sincronizado MAC: {metrics.MacAddress}");
                }
            }
            catch (Exception ex)
            {
               System.Diagnostics.Debug.WriteLine($"Erro no sync: {ex.Message}");
            }
        }

        private async void ListenToCommands()
        {
            try
            {
                var myMac = HardwareMonitor.GetMetrics().MacAddress;

                // Ouve inserções na tabela 'comandos'
                await supabaseClient.From<ComandoModel>().On(Supabase.Realtime.PostgresChanges.PostgresChangesOptions.ListenType.Inserts, async (sender, change) =>
                {
                    var novoComando = change.Model<ComandoModel>();
                    
                    // Se o comando for para esta máquina e estiver pendente
                    if (novoComando.MaquinaId == myMac && novoComando.Status == "PENDENTE")
                    {
                        trayIcon.ShowBalloonTip(3000, "Comando Recebido", "Iniciando limpeza remota solicitada pelo administrador...", ToolTipIcon.Info);

                        // 1. Atualiza p/ Executando
                        novoComando.Status = "EXECUTANDO";
                        await supabaseClient.From<ComandoModel>().Update(novoComando);

                        // 2. Roda o motor de limpeza (Modo Real/Direto)
                        try 
                        {
                            var result = CleanEngine.RunScript("DirectClean.ps1"); 

                            if (result.StartsWith("SUCESSO")) 
                            {
                                novoComando.Status = "CONCLUIDO";
                            }
                            else 
                            {
                                novoComando.Status = "FALHOU";
                            }

                            // 3. Registrar Log no Supabase
                            await supabaseClient.From<LogModel>().Insert(new LogModel {
                                MaquinaId = myMac,
                                Acao = "LIMPEZA_REMOTA",
                                Detalhes = result,
                                Status = novoComando.Status,
                                ComandoComando = "LIMPEZA_COMPLETA"
                            });
                        }
                        catch (Exception ex)
                        {
                            novoComando.Status = "FALHOU";
                            System.Diagnostics.Debug.WriteLine($"Erro na execução: {ex.Message}");
                        }

                        // 3. Atualiza status do comando
                        await supabaseClient.From<ComandoModel>().Update(novoComando);
                        
                        var notificationText = novoComando.Status == "CONCLUIDO" 
                            ? "A manutenção foi realizada com sucesso." 
                            : "Ocorreu um erro ao tentar realizar a limpeza.";
                            
                        trayIcon.ShowBalloonTip(3000, "Finalizado", notificationText, ToolTipIcon.Info);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro no Realtime: {ex.Message}");
            }
        }

        private void SyncNow(object? sender, EventArgs e)
        {
            MessageBox.Show("Forçando sincronização e coleta de dados...", "EasyClean Sync", MessageBoxButtons.OK, MessageBoxIcon.Information);
            MonitorHardwareLoop();
        }

        public async Task LogManualAction(string details, string status)
        {
            try
            {
                var myMac = HardwareMonitor.GetMetrics().MacAddress;
                if (supabaseClient != null)
                {
                    var novoLog = new LogModel 
                    {
                        MaquinaId = myMac ?? "UNKNOWN_MAC",
                        Acao = "LIMPEZA_LOCAL",
                        Detalhes = details ?? "",
                        Status = status ?? "DESCONHECIDO",
                        ComandoComando = "OTIMIZAR_SISTEMA" 
                    };
                    
                    var response = await supabaseClient.From<LogModel>().Insert(novoLog);
                    System.Diagnostics.Debug.WriteLine($"Log Enviado: {response.ResponseMessage.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Falha Crítica ao enviar Log Web: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private void Exit(object? sender, EventArgs e)
        {
            machineService.StopMonitoringTask();
            trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
