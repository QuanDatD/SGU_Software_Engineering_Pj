using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Zoo.DAL.Data;

#nullable disable

namespace Zoo.DAL.Migrations
{
    [DbContext(typeof(ZooDbContext))]
    [Migration("20261029120000_AddRevisionCreatedByFk")]
    /// <inheritdoc />
    public partial class AddRevisionCreatedByFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FK CreatedByAdminId → AdminUsers (PRD 9.5)
            // SET NULL khi admin bị xóa để không mất lịch sử phiên bản
            migrationBuilder.AddForeignKey(
                name: "FK_NarrationRevisions_AdminUsers_CreatedByAdminId",
                table: "NarrationRevisions",
                column: "CreatedByAdminId",
                principalTable: "AdminUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.CreateIndex(
                name: "IX_NarrationRevisions_CreatedByAdminId",
                table: "NarrationRevisions",
                column: "CreatedByAdminId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NarrationRevisions_AdminUsers_CreatedByAdminId",
                table: "NarrationRevisions");

            migrationBuilder.DropIndex(
                name: "IX_NarrationRevisions_CreatedByAdminId",
                table: "NarrationRevisions");
        }
    }
}
