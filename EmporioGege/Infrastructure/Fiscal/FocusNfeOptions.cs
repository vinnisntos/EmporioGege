namespace EmporioGege.Infrastructure.Fiscal
{
    public class FocusNfeOptions
    {
        // Token DE CONTA - usado só pra criar/listar/consultar empresas, nunca pra emitir
        // notas (cada empresa cadastrada recebe seu PRÓPRIO token pra emissão - ver
        // CriarEmpresaFocusNfeResultado.TokenProducao/TokenHomologacao).
        public string Token { get; set; } = default!;

        // "homologacao" (sandbox, notas sem valor fiscal) ou "producao".
        public string Ambiente { get; set; } = "homologacao";
    }
}
