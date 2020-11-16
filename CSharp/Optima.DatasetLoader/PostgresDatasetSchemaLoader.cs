using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Dapper;
using Npgsql;
using static Optima.Domain.DatasetDefinition.PersistenceType.Types.DbDatasetInfo.Types.DbProvider.Types;

namespace Optima.DatasetLoader
{
    public static class PostgresDatasetSchemaLoader
    {
        private class DbColumn
        {
            public string table_catalog { get; set; }
            public string table_schema { get; set; }
            public string table_name { get; set; }
            public string column_name { get; set; }
            public int ordinal_position { get; set; }
            public object column_default { get; set; }
            public string is_nullable { get; set; }
            public string data_type { get; set; }
        }
        
        public static ImmutableArray<Postgres> LoadSchema()
        {
            var connectionString = "Host=localhost;Username=postgres;Password=example;Database=playground";

            using var con = new NpgsqlConnection(connectionString);
            con.Open();
            
            var ret = con.Query<DbColumn>(@"
                    SELECT *
                    FROM information_schema.columns
                    ORDER BY table_catalog, table_schema, table_name, ordinal_position
                ")
                .GroupBy(r => (r.table_catalog, r.table_schema, r.table_name), r => r)
                .Select(g => new Postgres
                {
                    TableCatalog = g.Key.table_catalog,
                    SchemaName = g.Key.table_schema,
                    TableName   = g.Key.table_name,
                    Columns = { g.Select(c => new Postgres.Types.Column {
                        ColumnName = c.column_name,
                        DataType = c.data_type,
                        OrdinalPosition = c.ordinal_position,
                        IsNullable = "YES".Equals(c.is_nullable, StringComparison.InvariantCultureIgnoreCase),
                        ColumnDefault = c.column_default?.ToString() ?? ""
                    }) }
                })
                .ToImmutableArray();

//             using var cmd = new NpgsqlCommand
//             {
//                 Connection = con,
//                 CommandText = @"
// SELECT *
// FROM pg_catalog.pg_tables
// WHERE schemaname != 'pg_catalog' AND 
//     schemaname != 'information_schema'; 
// "
//             };

            // var ret = ReadAll(cmd.ExecuteReader()).ToImmutableArray();
            Console.WriteLine(JsonSerializer.Serialize(ret));
            
            return ret;

            // table_catalog, table_schema, table_name, column_name, ordinal_position, column_default, is_nullable,
                    // data_type, character_maximum_length, character_octet_length, numeric_precision, numeric_precision_radix,
                    // numeric_scale, datetime_precision, interval_type, interval_precision
                // }
            // }
        }
    }
}
