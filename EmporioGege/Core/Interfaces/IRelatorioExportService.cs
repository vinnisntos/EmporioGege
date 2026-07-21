using EmporioGege.Application.DTOs;

namespace EmporioGege.Core.Interfaces
{
    public interface IRelatorioExportService
    {
        byte[] ExportarXlsx(RelatorioTabularDto relatorio);
        byte[] ExportarPdf(RelatorioTabularDto relatorio);
        byte[] ExportarXml(RelatorioTabularDto relatorio);
        byte[] ExportarCsv(RelatorioTabularDto relatorio);
    }
}
