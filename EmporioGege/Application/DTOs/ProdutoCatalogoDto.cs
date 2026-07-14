namespace EmporioGege.Application.DTOs
{
    // Forma enxuta de Produto usada só pelo catálogo do PDV — não é o Produto (Models/,
    // mapeado ao cliente Postgrest) porque essa leitura agora passa por Dapper: o cliente
    // Supabase é Scoped (uma instância nova por requisição) e nunca persiste a sessão de
    // login entre requisições, então qualquer leitura via Postgrest FORA do próprio
    // request de login roda como anon, não como o usuário logado — foi exatamente isso
    // que deixava o catálogo do PDV sempre vazio, mesmo com RLS/produto corretos.
    public record ProdutoCatalogoDto(Guid Id, string Nome, string? CodigoBarras, decimal PrecoVendaBase, int EstoqueAtual, int QuantidadePorCaixa);
}
