using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace EasyCleanAgent
{
    [Table("logs")]
    public class LogModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("maquina_id")]
        public string MaquinaId { get; set; }

        [Column("acao")]
        public string Acao { get; set; }

        [Column("detalhes")]
        public string Detalhes { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("comando_comando")]
        public string ComandoComando { get; set; }

        [Column("created_at")]
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
