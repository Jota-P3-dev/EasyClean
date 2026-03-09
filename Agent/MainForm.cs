using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace EasyCleanAgent
{
    public partial class MainForm : Form
    {
        private Button btnClean;
        private Label lblStatus;
        private Label lblTitle;
        private Label lblHwInfo;
        private EasyCleanContext _context;
        private RichTextBox txtLog;
        private Panel topBar;
        private Panel leftPanel;
        private Panel rightPanel;
        private ProgressBar progressBar;
        private Button btnLogs;
        private Label lblUltimaOtimizacao;
        private Label lblVersion;
        private System.Windows.Forms.Timer uiUpdateTimer;

        // Para arrastar a tela sem bordas
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public MainForm(EasyCleanContext context)
        {
            _context = context;
            InitializeComponent();
            SetupUI();
            AtualizarHwInfo();
            AtualizarStatusOtimizacao();
            SetupRealtimeTimer();
            _ = UpdateManager.CheckForUpdates(); // Verifica atualização em segundo plano
        }

        private void SetupRealtimeTimer()
        {
            uiUpdateTimer = new System.Windows.Forms.Timer();
            uiUpdateTimer.Interval = 2000; // 2 segundos
            uiUpdateTimer.Tick += (s, e) => AtualizarMetricasTempoReal();
            uiUpdateTimer.Start();
        }

        private void AtualizarMetricasTempoReal()
        {
            try
            {
                var usageCpu = HardwareMonitor.GetMetrics().CpuUsage;
                var usageRam = HardwareMonitor.GetMetrics().RamUsage;
                
                // Tenta atualizar o label de CPU se ele estiver visível
                // Como as métricas de hardware podem vir formatadas, vamos apenas atualizar o texto
                // No SetupUI o CPU é exibido em lblHwInfo ou num local específico? 
                // Vamos atualizar o lblHwInfo que contém o resumo.
                
                var metrics = HardwareMonitor.GetMetrics();
                lblHwInfo.Text = $"{metrics.Manufacturer} - {metrics.MachineModel}\n" +
                               $"Processador: {metrics.CpuModel} ({usageCpu})\n" +
                               $"Memória Total: {metrics.TotalRam} (Uso: {usageRam})";
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.Text = "EasyClean Agent";
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(248, 250, 252); // slate-50 (Claro Corporativo)
            this.ForeColor = Color.FromArgb(30, 41, 59); // slate-800
            this.Icon = SystemIcons.Shield;

            this.ResumeLayout(false);
        }

        private void SetupUI()
        {
            // --- Barra Superior (Arrastável) ---
            topBar = new Panel
            {
                Size = new Size(this.Width, 40),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(241, 245, 249), // slate-100
                Dock = DockStyle.Top
            };
            topBar.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left) {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };
            this.Controls.Add(topBar);

            var titleMini = new Label
            {
                Text = "EASYCLEAN AGENT",
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = Color.FromArgb(100, 116, 139), // slate-500
                Location = new Point(15, 10),
                AutoSize = true
            };
            topBar.Controls.Add(titleMini);

            var btnMinimizeToTray = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(40, 40),
                Location = new Point(this.Width - 40, 0),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnMinimizeToTray.FlatAppearance.BorderSize = 0;
            btnMinimizeToTray.FlatAppearance.MouseOverBackColor = Color.FromArgb(226, 232, 240); // slate-200
            btnMinimizeToTray.Click += (s, e) => { this.Hide(); };
            topBar.Controls.Add(btnMinimizeToTray);

            // --- Layout Split (Esquerda e Direita) ---
            
            // Painel Esquerdo (Controles - 55% da tela)
            leftPanel = new Panel
            {
                Width = 550,
                Dock = DockStyle.Left,
                Padding = new Padding(40, 60, 40, 40)
            };
            this.Controls.Add(leftPanel);

            // Painel Direito (Terminal - 45% da tela)
            rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 23, 42), // slate-900
                Padding = new Padding(20)
            };
            this.Controls.Add(rightPanel);

            // --- Conteúdo Esquerdo ---
            
            lblTitle = new Label
            {
                Text = "Manutenção do Sistema",
                Font = new Font("Segoe UI", 26, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42), // slate-900
                AutoSize = true,
                Location = new Point(40, 60)
            };
            leftPanel.Controls.Add(lblTitle);

            lblHwInfo = new Label
            {
                Text = "Buscando informações do hardware...",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(100, 116, 139), // slate-500
                AutoSize = true,
                Location = new Point(45, 110)
            };
            leftPanel.Controls.Add(lblHwInfo);

            btnClean = new Button
            {
                Text = "OTIMIZAR AGORA",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 185, 129), // emerald-500
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(350, 70),
                Location = new Point(45, 200),
                Cursor = Cursors.Hand
            };
            btnClean.FlatAppearance.BorderSize = 0;
            btnClean.FlatAppearance.MouseOverBackColor = Color.FromArgb(5, 150, 105);
            btnClean.Click += BtnClean_Click;
            leftPanel.Controls.Add(btnClean);

            progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                Size = new Size(350, 8),
                Location = new Point(45, 280),
                Visible = false,
                MarqueeAnimationSpeed = 30
            };
            leftPanel.Controls.Add(progressBar);

            lblStatus = new Label
            {
                Text = "Sistema monitorado e protegido.",
                Font = new Font("Segoe UI Semibold", 12),
                ForeColor = Color.FromArgb(16, 185, 129), // emerald-500
                AutoSize = true,
                Location = new Point(45, 310)
            };
            leftPanel.Controls.Add(lblStatus);

            lblUltimaOtimizacao = new Label
            {
                Text = "Última otimização: Carregando...",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(148, 163, 184), // slate-400
                AutoSize = true,
                Location = new Point(45, 340)
            };
            leftPanel.Controls.Add(lblUltimaOtimizacao);

            btnLogs = new Button
            {
                Text = "Ver Histórico de Logs",
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(241, 245, 249), // slate-100
                ForeColor = Color.FromArgb(71, 85, 105), // slate-600
                FlatStyle = FlatStyle.Flat,
                Size = new Size(200, 40),
                Location = new Point(45, leftPanel.Height - 80),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnLogs.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225); // slate-300
            btnLogs.FlatAppearance.MouseOverBackColor = Color.FromArgb(226, 232, 240); // slate-200
            btnLogs.Click += (s, e) => {
                string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EasyClean");
                if (Directory.Exists(logPath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", logPath);
                }
                else
                {
                    MessageBox.Show("Ainda não existem logs gerados nesta máquina.", "Sem Logs", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            leftPanel.Controls.Add(btnLogs);

            lblVersion = new Label
            {
                Text = $"v{UpdateManager.CurrentVersion}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(148, 163, 184), // slate-400
                AutoSize = true,
                Location = new Point(leftPanel.Width - 60, leftPanel.Height - 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            leftPanel.Controls.Add(lblVersion);

            // --- Conteúdo Direito (Terminal) ---
            
            var lblTerminalTitle = new Label
            {
                Text = "> LOGS DE ATIVIDADE EM TEMPO REAL",
                Font = new Font("Consolas", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 211, 153), // emerald-400
                AutoSize = true,
                Location = new Point(20, 20),
                BackColor = Color.Transparent
            };
            rightPanel.Controls.Add(lblTerminalTitle);

            txtLog = new RichTextBox
            {
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BackColor = Color.FromArgb(2, 6, 23), // slate-950
                ForeColor = Color.FromArgb(148, 163, 184), // slate-400
                Font = new Font("Consolas", 10),
                BorderStyle = BorderStyle.None,
                Location = new Point(20, 50),
                Size = new Size(rightPanel.Width - 40, rightPanel.Height - 70),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            rightPanel.Controls.Add(txtLog);
            
            // Layout fix para garantir que o painel direito preencha corretament após a barra superior
            leftPanel.BringToFront();
            rightPanel.BringToFront();
            topBar.BringToFront();
        }

        private void AtualizarHwInfo()
        {
            var metrics = HardwareMonitor.GetMetrics();
            lblHwInfo.Text = $"{metrics.Manufacturer} - {metrics.MachineModel}\nProcessador: {metrics.CpuModel}\nMemória Total: {metrics.TotalRam}";
        }
        
        private void AtualizarStatusOtimizacao()
        {
            // Busca data do ultimo log salvo para popular a UI
            try
            {
                string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EasyClean");
                if (Directory.Exists(logPath))
                {
                    var file = new DirectoryInfo(logPath).GetFiles("EasyClean_Log_*.txt").OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
                    if (file != null)
                    {
                        lblUltimaOtimizacao.Text = $"Última otimização: {file.LastWriteTime:dd/MM/yyyy HH:mm}";
                        return;
                    }
                }
                lblUltimaOtimizacao.Text = "Última otimização: Nenhuma realizada ainda.";
            }
            catch
            {
                lblUltimaOtimizacao.Text = "Última otimização: Indisponível";
            }
        }

        private void SaveLocalLog(string message)
        {
            try
            {
                string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EasyClean");
                if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);
                
                string file = Path.Combine(logPath, $"EasyClean_Log_{DateTime.Now:yyyy-MM-dd}.txt");
                File.AppendAllText(file, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Falha ao salvar log local: {ex.Message}");
            }
        }

        private async void BtnClean_Click(object? sender, EventArgs e)
        {
            btnClean.Enabled = false;
            btnClean.Text = "VERIFICANDO...";
            btnClean.BackColor = Color.FromArgb(245, 158, 11); // amber-500
            lblStatus.Text = "Varredura e limpeza em andamento, aguarde...";
            lblStatus.ForeColor = Color.FromArgb(245, 158, 11); // amber-500
            progressBar.Visible = true;
            
            txtLog.Clear(); // Limpa o terminal a cada nova execução manual
            UpdateLog("--- Iniciando ciclo de manutenção local ---", Color.White);
            SaveLocalLog("--- INICIO DE CICLO MANUTENCAO MANUAL ---");

            try
            {
                string result = await Task.Run(() => CleanEngine.RunScript("PowerClean.bat", message => ProcessRealtimeOutput(message)));
                
                SaveLocalLog($"ENGINE RUN: {result}");
                
                if (result.StartsWith("SUCESSO") || result.Contains("SUCESSO"))
                {
                    lblStatus.Text = "A máquina foi otimizada com sucesso!";
                    lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
                    btnClean.BackColor = Color.FromArgb(16, 185, 129);
                    UpdateLog($"[RESULTADO] {result}", Color.FromArgb(52, 211, 153)); // verde string
                    _ = _context.LogManualAction("Otimização Concluída Localmente", "CONCLUIDO");
                }
                else
                {
                    lblStatus.Text = "Atenção: A limpeza reportou erros (Ver logs).";
                    lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                    btnClean.BackColor = Color.FromArgb(239, 68, 68);
                    UpdateLog($"[ALERTA] Diferenças reportadas: {result}", Color.FromArgb(248, 113, 113)); // vermelho string
                    _ = _context.LogManualAction("Otimização Concluida com Alertas: " + result, "ALERTA");
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Ocorreu um erro estrutural.";
                lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                UpdateLog($"ERRO FATAL: {ex.Message}", Color.FromArgb(239, 68, 68)); // vermelho forte
            }
            finally
            {
                btnClean.Enabled = true;
                btnClean.Text = "OTIMIZAR NOVAMENTE";
                progressBar.Visible = false;
                AtualizarStatusOtimizacao();
            }
        }
        
        private void ProcessRealtimeOutput(string message)
        {
            Color logColor = Color.FromArgb(148, 163, 184); // slate-400 (cinza padrão)
            
            if (message.Contains("Erro", StringComparison.OrdinalIgnoreCase) || message.Contains("FALHA") || message.Contains("Access Denied"))
                logColor = Color.FromArgb(248, 113, 113); // red-400
            else if (message.Contains("Excluído", StringComparison.OrdinalIgnoreCase) || message.Contains("Limpo", StringComparison.OrdinalIgnoreCase))
                logColor = Color.FromArgb(52, 211, 153); // emerald-400
            else if (message.Contains("Aviso", StringComparison.OrdinalIgnoreCase))
                logColor = Color.FromArgb(251, 191, 36); // amber-400
            else if (message.StartsWith("---"))
                logColor = Color.White;

            UpdateLog(message, logColor);
        }

        private void UpdateLog(string message, Color color)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => UpdateLog(message, color)));
            }
            else
            {
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.SelectionLength = 0;
                txtLog.SelectionColor = Color.FromArgb(100, 116, 139); // Time escuro
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] ");
                
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.SelectionLength = 0;
                txtLog.SelectionColor = color;
                txtLog.AppendText($"{message}{Environment.NewLine}");
                
                txtLog.SelectionColor = txtLog.ForeColor;
                txtLog.ScrollToCaret();
            }
        }
    }
}
