using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace EasyCleanAgent
{
    [Table("maquinas")]
    public class MaquinaModel : BaseModel
    {
        [PrimaryKey("id", false)] // False significa que a API/Agente injeta o ID, e não o banco serial.
        public string Id { get; set; } // O ID será o MacAddress

        [Column("empresa_id")]
        public string EmpresaId { get; set; }

        [Column("nome_maquina")]
        public string NomeMaquina { get; set; }

        [Column("hostname")]
        public string Hostname { get; set; }

        [Column("sistema_operacional")]
        public string SistemaOperacional { get; set; }

        // Mapeamento Status (Ex: ONLINE, OFFLINE)
        [Column("status")]
        public string Status { get; set; }

        [Column("temperatura_cpu")]
        public string TemperaturaCpu { get; set; }

        [Column("saude_disco")]
        public string SaudeDisco { get; set; }

        // Colunas adicionais baseadas no último requisito
        [Column("system_uptime")]
        public string SystemUptime { get; set; }

        [Column("is_network_connected")]
        public bool IsNetworkConnected { get; set; }

        [Column("modelo_cpu")]
        public string ModeloCpu { get; set; }

        [Column("ram_total")]
        public string RamTotal { get; set; }

        [Column("modelo_maquina_hw")]
        public string ModeloMaquinaHw { get; set; }

        [Column("fabricante")]
        public string Fabricante { get; set; }

        [Column("uso_cpu")]
        public string UsoCpu { get; set; }

        [Column("uso_ram")]
        public string UsoRam { get; set; }

        [Column("ultima_conexao")]
        public DateTime UltimaConexao { get; set; }
    }
}
