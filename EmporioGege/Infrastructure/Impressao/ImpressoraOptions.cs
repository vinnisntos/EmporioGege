namespace EmporioGege.Infrastructure.Impressao
{
    // Configuração por máquina física (igual à connection string) - cada PC de caixa tem
    // sua própria impressora térmica pareada, numa porta COM que pode variar de loja pra
    // loja. Ver seção "Impressora" em appsettings.json/appsettings.Development.json.
    public class ImpressoraOptions
    {
        public bool Habilitada { get; set; }

        public string Porta { get; set; } = "COM1";

        public int BaudRate { get; set; } = 9600;

        public int ColunasPorLinha { get; set; } = 32;
    }
}
