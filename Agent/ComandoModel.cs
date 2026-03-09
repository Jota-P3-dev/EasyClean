using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace EasyCleanAgent
{
    [Table("comandos")]
    public class ComandoModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("maquina_id")]
        public string MaquinaId { get; set; }

        [Column("comando")]
        public string Comando { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("agendado_para")]
        public DateTime? AgendadoPara { get; set; }

        [Column("criado_em")]
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
