using Postgrest.Attributes;
using Postgrest.Models;

namespace EmporioGege.Models
{
    [Table("tenants")]
    public class Tenant : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = default!;

        [Column("nome_representante")]
        public string NomeRepresentante { get; set; } = default!;

        [Column("cpf_rg_dono")]
        public string CpfRgDono { get; set; } = default!;

        [Column("nome_fantasia")]
        public string NomeFantasia { get; set; } = default!;

        [Column("cnpj")]
        public string Cnpj { get; set; } = default!;

        [Column("cidade_estado")]
        public string CidadeEstado { get; set; } = default!;

        [Column("telefone_empresa")]
        public string? TelefoneEmpresa { get; set; } // ? indica que aceita nulo no C#

        [Column("telefone_dono")]
        public string TelefoneDono { get; set; } = default!;

        [Column("email_empresa")]
        public string? EmailEmpresa { get; set; }

        [Column("email_dono")]
        public string EmailDono { get; set; } = default!;

        [Column("status_licenca")]
        public string StatusLicenca { get; set; } = "ativo";

        [Column("data_expiracao")]
        public DateTime DataExpiracao { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}