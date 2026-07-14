using System.Data;
using Dapper;

namespace EmporioGege.Infrastructure.Data
{
    // Dapper (core) não conhece System.DateOnly como parâmetro de query — sua tabela
    // interna de mapeamento CLR->DbType é anterior ao .NET 6 e lança NotSupportedException
    // ao tentar montar o parâmetro, mesmo o Npgsql aceitando DateOnly nativamente pra
    // colunas "date". Este handler resolve só a ponta do Dapper; registrado uma vez no
    // startup via SqlMapper.AddTypeHandler (cobre DateOnly e DateOnly? automaticamente).
    public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.DbType = DbType.Date;
            parameter.Value = value;
        }

        public override DateOnly Parse(object value) => value switch
        {
            DateOnly dateOnly => dateOnly,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            _ => DateOnly.Parse(value.ToString()!)
        };
    }
}
