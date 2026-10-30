using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Zoo.DAL.Data;

#nullable disable

namespace Zoo.DAL.Migrations
{
    [DbContext(typeof(ZooDbContext))]
    [Migration("20261029000000_AddQrVisits")]
    /// <inheritdoc />
    public partial class AddQrVisits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QrVisits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VisitedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDemo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrVisits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QrVisits_SessionId",
                table: "QrVisits",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QrVisits_VisitedAtUtc",
                table: "QrVisits",
                column: "VisitedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "QrVisits");
        }
    }
}
