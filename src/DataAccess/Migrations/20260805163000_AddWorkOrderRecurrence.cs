using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClearMeasure.Bootcamp.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderRecurrence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add recurrence columns to WorkOrder table
            migrationBuilder.AddColumn<bool>(
                name: "IsRecurring",
                schema: "dbo",
                table: "WorkOrder",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RecurrencePattern",
                schema: "dbo",
                table: "WorkOrder",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceInterval",
                schema: "dbo",
                table: "WorkOrder",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextScheduledDate",
                schema: "dbo",
                table: "WorkOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentWorkOrderId",
                schema: "dbo",
                table: "WorkOrder",
                type: "uniqueidentifier",
                nullable: true);

            // Add foreign key for parent-child relationship
            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrder_WorkOrder_ParentWorkOrderId",
                schema: "dbo",
                table: "WorkOrder",
                column: "ParentWorkOrderId",
                principalSchema: "dbo",
                principalTable: "WorkOrder",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Add index for recurring work order queries
            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_Recurring",
                schema: "dbo",
                table: "WorkOrder",
                columns: new[] { "IsRecurring", "NextScheduledDate" },
                filter: "IsRecurring = 1");

            // Add index for parent-child lookups
            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_ParentId",
                schema: "dbo",
                table: "WorkOrder",
                column: "ParentWorkOrderId",
                filter: "ParentWorkOrderId IS NOT NULL");

            // Add check constraint for recurrence interval
            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkOrders_RecurrenceInterval",
                schema: "dbo",
                table: "WorkOrder",
                sql: "RecurrenceInterval >= 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop check constraint
            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkOrders_RecurrenceInterval",
                schema: "dbo",
                table: "WorkOrder");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_Recurring",
                schema: "dbo",
                table: "WorkOrder");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_ParentId",
                schema: "dbo",
                table: "WorkOrder");

            // Drop foreign key
            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrder_WorkOrder_ParentWorkOrderId",
                schema: "dbo",
                table: "WorkOrder");

            // Drop columns
            migrationBuilder.DropColumn(
                name: "IsRecurring",
                schema: "dbo",
                table: "WorkOrder");

            migrationBuilder.DropColumn(
                name: "RecurrencePattern",
                schema: "dbo",
                table: "WorkOrder");

            migrationBuilder.DropColumn(
                name: "RecurrenceInterval",
                schema: "dbo",
                table: "WorkOrder");

            migrationBuilder.DropColumn(
                name: "NextScheduledDate",
                schema: "dbo",
                table: "WorkOrder");

            migrationBuilder.DropColumn(
                name: "ParentWorkOrderId",
                schema: "dbo",
                table: "WorkOrder");
        }
    }
}
