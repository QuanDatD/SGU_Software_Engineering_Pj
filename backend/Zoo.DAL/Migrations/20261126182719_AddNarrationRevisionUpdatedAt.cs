using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zoo.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddNarrationRevisionUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "NarrationRevisions",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "NarrationRevisions");
        }
    }
}
