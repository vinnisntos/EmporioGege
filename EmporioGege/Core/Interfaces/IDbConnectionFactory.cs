using System.Data.Common;

namespace EmporioGege.Core.Interfaces
{
    // Abstração do Core sobre acesso a banco — usa só tipos do BCL (System.Data.Common),
    // não Npgsql, pra Application depender de uma interface e não da classe concreta de
    // Infrastructure. A implementação concreta (Npgsql) vive em Infrastructure/Data.
    public interface IDbConnectionFactory
    {
        Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);

        Task SetTenantContextAsync(DbConnection connection, DbTransaction transaction, Guid tenantId, CancellationToken ct = default);
    }
}
