using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ClearMeasure.Bootcamp.IntegrationTests;

public sealed class DatabaseEmptier
{
    private static readonly string[] _ignoredTables = { "[dbo].[sysdiagrams]", "[dbo].[SchemaVersions]" };
    private static string? _deleteSql;
    private readonly DatabaseFacade _database;

    public DatabaseEmptier(DatabaseFacade database)
    {
        _database = database;
    }

    public void DeleteAllData()
    {
        if (IsSqlite())
        {
            DeleteAllDataSqlite();
            return;
        }

        if (_deleteSql == null)
        {
            _deleteSql = BuildDeleteTableSqlStatement();
        }

        _database.ExecuteSqlRaw(_deleteSql);
    }

    private bool IsSqlite()
    {
        return _database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
    }

    private void DeleteAllDataSqlite()
    {
        var tables = new List<string>();
        new SqlExecuter(_database).ExecuteSql(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'",
            reader => tables.Add(reader.GetString(0)));

        _database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        foreach (var table in tables)
        {
#pragma warning disable EF1002, EF1003 // Table names from sqlite_master are safe
            _database.ExecuteSqlRaw("DELETE FROM \"" + table + "\"");
#pragma warning restore EF1002, EF1003
        }
        _database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");
    }

    private string BuildDeleteTableSqlStatement()
    {
        var allTables = GetAllTables();
        var allRelationships = GetRelationships();
        var tablesToDelete = BuildTableList(allTables, allRelationships);

        return BuildTableSql(tablesToDelete);
    }

    private static string BuildTableSql(IEnumerable<string> tablesToDelete)
    {
        var completeQuery = "";
        foreach (var tableName in tablesToDelete)
        {
            completeQuery += $"delete from {tableName};";
        }

        return completeQuery;
    }

    private static string[] BuildTableList(ICollection<string> allTables, ICollection<Relationship> allRelationships)
    {
        var tablesToDelete = new List<string>();

        while (allTables.Any())
        {
            var leafTables = allTables.Except(allRelationships.Select(rel => rel.PrimaryKeyTable)).ToArray();

            if (leafTables.Length == 0)
            {
                tablesToDelete.AddRange(allTables);
                break;
            }

            tablesToDelete.AddRange(leafTables);

            foreach (var leafTable in leafTables)
            {
                allTables.Remove(leafTable);
                var relToRemove =
                    allRelationships.Where(rel => rel.ForeignKeyTable == leafTable).ToArray();
                foreach (var rel in relToRemove)
                {
                    allRelationships.Remove(rel);
                }
            }
        }

        return tablesToDelete.ToArray();
    }

    private IList<Relationship> GetRelationships()
    {
        var relationships = new List<Relationship>();
        new SqlExecuter(_database).ExecuteSql(
            @"select
	'[' + ss_pk.name + '].[' + so_pk.name + ']' as PrimaryKeyTable
, '[' + ss_fk.name + '].[' + so_fk.name + ']' as ForeignKeyTable
from
	sysforeignkeys sfk
	  inner join sysobjects so_pk on sfk.rkeyid = so_pk.id
	  inner join sys.tables st_pk on so_pk.id = st_pk.object_id
	  inner join sys.schemas ss_pk on st_pk.schema_id = ss_pk.schema_id
	  inner join sysobjects so_fk on sfk.fkeyid = so_fk.id
	  inner join sys.tables st_fk on so_fk.id = st_fk.object_id
	  inner join sys.schemas ss_fk on st_fk.schema_id = ss_fk.schema_id
order by
	so_pk.name
,   so_fk.name;",
            reader => relationships.Add(
                new Relationship
                {
                    PrimaryKeyTable = reader["PrimaryKeyTable"].ToString()!,
                    ForeignKeyTable = reader["ForeignKeyTable"].ToString()!
                }));

        return relationships;
    }

    private IList<string> GetAllTables()
    {
        var tables = new List<string>();

        new SqlExecuter(_database).ExecuteSql(
            @"select '[' + s.name + '].[' + t.name + ']'
from sys.tables t
inner join sys.schemas s on t.schema_id = s.schema_id",
            reader => tables.Add(reader.GetString(0)));
        var list = tables.Except(_ignoredTables);
        return list.Where(s => s.Contains("[dbo]")).ToList();
    }

    private class Relationship
    {
        public string PrimaryKeyTable { get; set; } = null!;
        public string ForeignKeyTable { get; set; } = null!;
    }
}