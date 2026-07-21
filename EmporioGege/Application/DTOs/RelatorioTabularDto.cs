namespace EmporioGege.Application.DTOs
{
    // Modelo genérico pra qualquer relatório exportável (Extrato de Vendas, Histórico de
    // Movimentação de Estoque, Produtos Mais Vendidos, etc.) - cada linha já vem formatada
    // como string (moeda em pt-BR, data no formato certo) por quem monta o relatório, então
    // o exportador (Infrastructure/Relatorios) não precisa saber nada sobre o domínio de
    // cada um - só desenha colunas e linhas de texto.
    public record RelatorioTabularDto(string Titulo, IReadOnlyList<string> Colunas, IReadOnlyList<IReadOnlyList<string>> Linhas);
}
