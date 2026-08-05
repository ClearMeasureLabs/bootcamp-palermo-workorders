using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClearMeasure.Bootcamp.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add email notification preference to Employee table
            migrationBuilder.AddColumn<bool>(
                name: "EmailNotificationsEnabled",
                schema: "dbo",
                table: "Employee",
                type: "bit",
                nullable: false,
                defaultValue: true);

            // Add index for notification queries
            migrationBuilder.CreateIndex(
                name: "IX_Employee_EmailNotificationsEnabled",
                schema: "dbo",
                table: "Employee",
                column: "EmailNotificationsEnabled",
                filter: "EmailNotificationsEnabled = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop index
            migrationBuilder.DropIndex(
                name: "IX_Employee_EmailNotificationsEnabled",
                schema: "dbo",
                table: "Employee");

            // Drop column
            migrationBuilder.DropColumn(
                name: "EmailNotificationsEnabled",
                schema: "dbo",
                table: "Employee");
        }
    }
}
