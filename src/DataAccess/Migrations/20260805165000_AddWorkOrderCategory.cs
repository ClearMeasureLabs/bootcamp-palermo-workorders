using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClearMeasure.Bootcamp.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add category column to WorkOrder table
            migrationBuilder.AddColumn<int>(
                name: "Category",
                schema: "dbo",
                table: "WorkOrder",
                type: "int",
                nullable: false,
                defaultValue: 4); // Default to Other

            // Add index for category-based queries
            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_Category",
                schema: "dbo",
                table: "WorkOrder",
                column: "Category");

            // Add check constraint for valid category values
            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkOrders_Category",
                schema: "dbo",
                table: "WorkOrder",
                sql: "Category IN (0, 1, 2, 3, 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop check constraint
            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkOrders_Category",
                schema: "dbo",
                table: "WorkOrder");

            // Drop index
            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_Category",
                schema: "dbo",
                table: "WorkOrder");

            // Drop column
            migrationBuilder.DropColumn(
                name: "Category",
                schema: "dbo",
                table: "WorkOrder");
        }
    }
}
