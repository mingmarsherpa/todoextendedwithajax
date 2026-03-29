using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoView.Migrations
{
    /// <inheritdoc />
    public partial class AddTodoReminder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderAt",
                table: "TodoItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderTriggeredAt",
                table: "TodoItems",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReminderAt",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "ReminderTriggeredAt",
                table: "TodoItems");
        }
    }
}
