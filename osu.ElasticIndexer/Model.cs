// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-elastic-indexer/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dapper;
using Dapper.Contrib.Extensions;
using MySql.Data.MySqlClient;

namespace osu.ElasticIndexer
{
    [CursorColumn("id")]
    public abstract class Model
    {
        public abstract long CursorValue { get; }

        public static IEnumerable<List<T>> Chunk<T>(int chunkSize = 10000, long? resumeFrom = null) where T : Model
        {
            using (var dbConnection = new MySqlConnection(AppSettings.ConnectionString))
            {
                long? lastId = resumeFrom ?? 0;
                Console.WriteLine($"Starting from {lastId}...");

                var cursorColumn = typeof(T).GetCustomAttributes<CursorColumnAttribute>().First().Name;
                var table = typeof(T).GetCustomAttributes<TableAttribute>().First().Name;

                dbConnection.Open();

                while (lastId != null)
                {
                    string query = $"select * from {table} where {cursorColumn} > @lastId order by {cursorColumn} asc limit @chunkSize;";
                    var parameters = new { lastId, chunkSize };
                    Console.WriteLine("{0} {1}", query, parameters);
                    var queryResult = dbConnection.Query<T>(query, parameters).AsList();

                    lastId = queryResult.LastOrDefault()?.CursorValue;
                    if (lastId.HasValue) yield return queryResult;
                }
            }
        }
    }
}
