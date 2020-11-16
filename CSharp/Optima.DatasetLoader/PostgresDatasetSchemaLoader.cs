using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Npgsql;
using static Optima.Domain.DatasetDefinition.PersistenceType.Types.DbDatasetInfo.Types.DbProvider.Types;

namespace Optima.DatasetLoader
{
    public static class PostgresDatasetSchemaLoader
    {
        public static ImmutableArray<Postgres> LoadSchema()
        {
            var connectionString = "Host=localhost;Username=postgres;Password=example;Database=playground";

            using var con = new NpgsqlConnection(connectionString);
            con.Open();

            using var cmd = new NpgsqlCommand
            {
                Connection = con,
                CommandText = @"
SELECT *
FROM pg_catalog.pg_tables
WHERE schemaname != 'pg_catalog' AND 
    schemaname != 'information_schema'; 
"
            };

            var ret = ReadAll(cmd.ExecuteReader()).ToImmutableArray();
            Console.WriteLine(JsonSerializer.Serialize(ret));
            
            return ret;

            IEnumerable<Postgres> ReadAll(NpgsqlDataReader reader)
            {
                while (reader.Read())
                {
                    yield return new Postgres
                    {
                        SchemaName = reader.GetString(0),
                        TableName   = reader.GetString(1),
                        TableOwner  = reader.GetString(2),
                        TableSpace  = reader.GetString(3),
                        HasIndexes  = reader.GetBoolean(4),
                        HasRules    = reader.GetBoolean(5),
                        HasTriggers = reader.GetBoolean(6),
                        RowSecurity = reader.GetBoolean(7)
                    };
                }
            }
        }
    }
}
