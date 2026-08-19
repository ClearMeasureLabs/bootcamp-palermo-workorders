using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClearMeasure.Bootcamp.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "WorkOrders",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkOrders_Priority",
                table: "WorkOrders",
                sql: "Priority IN (0, 1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_Priority_CreatedDate",
                table: "WorkOrders",
                columns: new[] { "Priority", "CreatedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_Priority_CreatedDate",
                table: "WorkOrders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkOrders_Priority",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "WorkOrders");
        }
    }
}
