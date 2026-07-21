namespace EmporioGege.Core.Common
{
    /// <summary>
    /// Converte entre o dia/hora local da loja (Brasil) e UTC (usado em toda coluna
    /// timestamp do banco). Offset fixo de -3h em vez de TimeZoneInfo por horário de
    /// verão: o Brasil não observa DST desde 2019, e um offset fixo evita depender de
    /// IDs de fuso horário do SO, que divergem entre Windows ("E. South America
    /// Standard Time") e Linux ("America/Sao_Paulo").
    /// </summary>
    public static class FusoHorarioBrasil
    {
        public static readonly TimeSpan Offset = TimeSpan.FromHours(-3);

        public static DateTime AgoraLocal() => DateTime.UtcNow + Offset;

        public static DateOnly HojeLocal() => DateOnly.FromDateTime(AgoraLocal());

        /// <summary>Início (00:00) de um dia local, convertido pra UTC.</summary>
        public static DateTime InicioDoDiaLocalEmUtc(DateOnly diaLocal) =>
            DateTime.SpecifyKind(diaLocal.ToDateTime(TimeOnly.MinValue) - Offset, DateTimeKind.Utc);
    }
}
