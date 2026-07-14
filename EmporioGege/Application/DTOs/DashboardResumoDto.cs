namespace EmporioGege.Application.DTOs
{
    public record DashboardResumoDto(decimal Faturamento, decimal Cmv, decimal LucroBruto, decimal? RoiPercentual, int TotalVendas);
}
