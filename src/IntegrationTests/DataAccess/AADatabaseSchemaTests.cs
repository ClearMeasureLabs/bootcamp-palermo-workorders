using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class AADatabaseSchemaTests : IntegratedTestBase
{
    [Test]
    [Order(1)]
    [Category("Database")]
    public void QueryAllTablesAndColumns_ShouldOutputSchemaInformation()
    {
   // Arrange
    var context = TestHost.GetRequiredService<DbContext>();
      var configuration = TestHost.GetRequiredService<IConfiguration>();
        var sqlExecuter = new SqlExecuter(context.Database);
     var tables = new List<(string Schema, string Table, string Column, string DataType, int? MaxLength, string Nullable)>();
        var logOutput = new System.Text.StringBuilder();
        
      // Output DbContext diagnostics
    var diagnosticsHeader = "\n=== DbContext Diagnostics ===\n";
        Console.WriteLine(diagnosticsHeader);
        TestContext.Out.WriteLine(diagnosticsHeader);
   logOutput.AppendLine(diagnosticsHeader);
        
        var connectionString = context.Database.GetConnectionString();
        var dbConnectionInfo = $"Connection String: {connectionString}";
        Console.WriteLine(dbConnectionInfo);
        TestContext.Out.WriteLine(dbConnectionInfo);
   logOutput.AppendLine(dbConnectionInfo);
        
        var providerName = context.Database.ProviderName;
 var providerInfo = $"Provider: {providerName}";
        Console.WriteLine(providerInfo);
        TestContext.Out.WriteLine(providerInfo);
  logOutput.AppendLine(providerInfo);
        
        var canConnect = context.Database.CanConnect();
        var canConnectInfo = $"Can Connect: {canConnect}";
        Console.WriteLine(canConnectInfo);
    TestContext.Out.WriteLine(canConnectInfo);
        logOutput.AppendLine(canConnectInfo);
   
        var isRelational = context.Database.IsRelational();
    var isRelationalInfo = $"Is Relational: {isRelational}";
      Console.WriteLine(isRelationalInfo);
        TestContext.Out.WriteLine(isRelationalInfo);
        logOutput.AppendLine(isRelationalInfo);
        
        var contextType = $"Context Type: {context.GetType().FullName}";
    Console.WriteLine(contextType);
        TestContext.Out.WriteLine(contextType);
        logOutput.AppendLine(contextType);
        
        var changeTrackerInfo = $"Change Tracker - Auto Detect Changes: {context.ChangeTracker.AutoDetectChangesEnabled}";
      Console.WriteLine(changeTrackerInfo);
   TestContext.Out.WriteLine(changeTrackerInfo);
 logOutput.AppendLine(changeTrackerInfo);
        
        var lazyLoadingInfo = $"Change Tracker - Lazy Loading: {context.ChangeTracker.LazyLoadingEnabled}";
 Console.WriteLine(lazyLoadingInfo);
        TestContext.Out.WriteLine(lazyLoadingInfo);
        logOutput.AppendLine(lazyLoadingInfo);
        
  var queryTrackingInfo = $"Change Tracker - Query Tracking Behavior: {context.ChangeTracker.QueryTrackingBehavior}";
  Console.WriteLine(queryTrackingInfo);
        TestContext.Out.WriteLine(queryTrackingInfo);
        logOutput.AppendLine(queryTrackingInfo);
        
        // Output Environment Variables
        var envHeader = "\n=== Environment Variables ===\n";
     Console.WriteLine(envHeader);
        TestContext.Out.WriteLine(envHeader);
        logOutput.AppendLine(envHeader);
        
  foreach (System.Collections.DictionaryEntry envVar in Environment.GetEnvironmentVariables())
        {
        var envInfo = $"{envVar.Key} = {envVar.Value}";
  Console.WriteLine(envInfo);
       TestContext.Out.WriteLine(envInfo);
     logOutput.AppendLine(envInfo);
      }

        // Output Configuration Values
        var configHeader = "\n=== Configuration Values ===\n";
        Console.WriteLine(configHeader);
    TestContext.Out.WriteLine(configHeader);
        logOutput.AppendLine(configHeader);
        
      foreach (var configItem in configuration.AsEnumerable())
        {
            var configInfo = $"{configItem.Key} = {configItem.Value}";
       Console.WriteLine(configInfo);
   TestContext.Out.WriteLine(configInfo);
   logOutput.AppendLine(configInfo);
        }
        
        var diagnosticsFooter = "\n=== End DbContext Diagnostics ===\n";
        Console.WriteLine(diagnosticsFooter);
        TestContext.Out.WriteLine(diagnosticsFooter);
    logOutput.AppendLine(diagnosticsFooter);
        
        const string query = @"
            SELECT 
         t.TABLE_SCHEMA,
       t.TABLE_NAME,
         c.COLUMN_NAME,
          c.DATA_TYPE,
        c.CHARACTER_MAXIMUM_LENGTH,
      c.IS_NULLABLE
   FROM 
    INFORMATION_SCHEMA.TABLES t
            INNER JOIN 
          INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME AND t.TABLE_SCHEMA = c.TABLE_SCHEMA
        WHERE 
        t.TABLE_TYPE = 'BASE TABLE'
            ORDER BY 
           t.TABLE_SCHEMA,
                t.TABLE_NAME,
    c.ORDINAL_POSITION";

        // Act
        sqlExecuter.ExecuteSql(query, reader =>
        {
    var schema = reader.GetString(0);
      var table = reader.GetString(1);
  var column = reader.GetString(2);
     var dataType = reader.GetString(3);
            var maxLength = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4);
     var nullable = reader.GetString(5);
            
     tables.Add((schema, table, column, dataType, maxLength, nullable));
  });

        // Assert
        tables.Count.ShouldBeGreaterThan(0);
        
        // Validate WorkOrder table has Instructions column
        var workOrderColumns = tables.Where(t => t.Table.Equals("WorkOrder", StringComparison.OrdinalIgnoreCase)).ToList();
        
        if (workOrderColumns.Count == 0)
        {
        Assert.Fail("WorkOrder table not found in database schema");
        }

 var hasInstructionsColumn = workOrderColumns.Any(t => t.Column.Equals("Instructions", StringComparison.OrdinalIgnoreCase));
        
      if (!hasInstructionsColumn)
        {
            var workOrderTableOutput = new System.Text.StringBuilder();
    workOrderTableOutput.AppendLine("\nWorkOrder table schema:");
          
      foreach (var col in workOrderColumns)
            {
        var columnInfo = $"  Column: {col.Column} ({col.DataType}";
           if (col.MaxLength.HasValue)
     {
  columnInfo += $"({col.MaxLength})";
      }
          columnInfo += $", Nullable: {col.Nullable})";
     workOrderTableOutput.AppendLine(columnInfo);
}

            Assert.Fail($"WorkOrder table does not have an 'Instructions' column. {workOrderTableOutput}");
        }
        
        // Output schema information to Console, TestContext, and log file
      var header = "\n=== Database Schema Information ===\n";
        Console.WriteLine(header);
  TestContext.Out.WriteLine(header);
        logOutput.AppendLine(header);
        
        string currentTable = string.Empty;
      foreach (var (schema, table, column, dataType, maxLength, nullable) in tables)
        {
        var fullTable = $"{schema}.{table}";
         
        if (fullTable != currentTable)
  {
      var tableOutput = $"\nTable: {fullTable}";
   Console.WriteLine(tableOutput);
            TestContext.Out.WriteLine(tableOutput);
       logOutput.AppendLine(tableOutput);
      currentTable = fullTable;
  }
        
            var columnInfo = $"  Column: {column} ({dataType}";
 if (maxLength.HasValue)
  {
    columnInfo += $"({maxLength})";
   }
       columnInfo += $", Nullable: {nullable})";
            
    Console.WriteLine(columnInfo);
          TestContext.Out.WriteLine(columnInfo);
   logOutput.AppendLine(columnInfo);
        }
        
  var footer = "\nQuery complete.";
        Console.WriteLine(footer);
        TestContext.Out.WriteLine(footer);
        logOutput.AppendLine(footer);
        
        // Save log output as test attachment for Azure Pipelines
        var logFilePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, 
  $"DatabaseSchema_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        File.WriteAllText(logFilePath, logOutput.ToString());
        TestContext.AddTestAttachment(logFilePath, "Database Schema Information");
    }
}
