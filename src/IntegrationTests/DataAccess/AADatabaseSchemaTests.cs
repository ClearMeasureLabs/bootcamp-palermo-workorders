using Microsoft.EntityFrameworkCore;
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
   var sqlExecuter = new SqlExecuter(context.Database);
     var tables = new List<(string Schema, string Table, string Column, string DataType, int? MaxLength, string Nullable)>();
        
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
        
        // Output schema information to both Console and TestContext
        var header = "\n=== Database Schema Information ===\n";
        Console.WriteLine(header);
        TestContext.Out.WriteLine(header);
        
        string currentTable = string.Empty;
        foreach (var (schema, table, column, dataType, maxLength, nullable) in tables)
        {
 var fullTable = $"{schema}.{table}";
  
      if (fullTable != currentTable)
   {
       var tableOutput = $"\nTable: {fullTable}";
        Console.WriteLine(tableOutput);
           TestContext.Out.WriteLine(tableOutput);
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
        }
        
  var footer = "\nQuery complete.";
     Console.WriteLine(footer);
        TestContext.Out.WriteLine(footer);
    }
}
