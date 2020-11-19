using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using Dapper;
using Google.Protobuf.Reflection;
using Npgsql;
using Optima.Domain.DatasetDefinition;
using static Optima.Domain.DatasetDefinition.PersistenceType.Types.DbDatasetInfo.Types.DbProvider.Types;

namespace Optima.DatasetLoader
{
    public static class PostgresDatasetSchemaLoader
    {
        public static ImmutableArray<PersistenceType> LoadSchema(string connectionName)
        {
            using var con = new NpgsqlConnection(ConnectionNameResolver(connectionName));
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
                    Columns = { g.Select(c => new DatasetColumn {
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
            
            
            return ret.Select(ToPersistenceType).ToImmutableArray();

            // table_catalog, table_schema, table_name, column_name, ordinal_position, column_default, is_nullable,
                    // data_type, character_maximum_length, character_octet_length, numeric_precision, numeric_precision_radix,
                    // numeric_scale, datetime_precision, interval_type, interval_precision
                // }
            // }
        }
        
        private static string ConnectionNameResolver(string connectionName) => "Host=localhost;Username=postgres;Password=example;Database=playground";
        
        private static PersistenceType ToPersistenceType(Postgres postgres) =>
            new PersistenceType
            {
                DescriptorProto = new DescriptorProto
                {
                    Name = $"{postgres.SchemaName}_{postgres.TableName}",
                    Field = { postgres.Columns.Select(PersistenceTypeHelper.ColumnToProto) }
                },
                Db = new PersistenceType.Types.DbDatasetInfo { 
                    DbProvider = new PersistenceType.Types.DbDatasetInfo.Types.DbProvider { Postgres = postgres },
                }
            };

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
    }
}
