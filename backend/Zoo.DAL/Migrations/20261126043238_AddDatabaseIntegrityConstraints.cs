using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zoo.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseIntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1.1: Rà và xử lý dữ liệu lệch trước khi áp ràng buộc mới
            migrationBuilder.Sql(@"
WITH DuplicateJobs AS (
    SELECT Id, ROW_NUMBER() OVER (PARTITION BY RevisionId, Language ORDER BY Id DESC) AS rn
    FROM GenerationJobs
)
DELETE FROM DuplicateJobs WHERE rn > 1;

WITH DuplicatePublished AS (
    SELECT Id, ROW_NUMBER() OVER (PARTITION BY PlaceId ORDER BY PublishedAt DESC, Version DESC) AS rn
    FROM NarrationRevisions
    WHERE Status = 1
)
UPDATE NarrationRevisions
SET Status = 2
WHERE Id IN (SELECT Id FROM DuplicatePublished WHERE rn > 1);

UPDATE p
SET p.PublishedRevisionId = r.Id
FROM Places p
LEFT JOIN NarrationRevisions r ON r.PlaceId = p.Id AND r.Status = 1
WHERE ISNULL(CONVERT(varchar(36), p.PublishedRevisionId), '') <> ISNULL(CONVERT(varchar(36), r.Id), '');
");
            migrationBuilder.DropForeignKey(
                name: "FK_NarrationRevisions_Places_PlaceId",
                table: "NarrationRevisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Places_NarrationRevisions_PublishedRevisionId",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Places_PublishedRevisionId",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_GenerationJobs_RevisionId",
                table: "GenerationJobs");

            migrationBuilder.AddColumn<bool>(
                name: "NeedsReview",
                table: "NarrationTranslations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceContentHash",
                table: "NarrationTranslations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_NarrationRevisions_Id_PlaceId",
                table: "NarrationRevisions",
                columns: new[] { "Id", "PlaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Places_PublishedRevisionId_Id",
                table: "Places",
                columns: new[] { "PublishedRevisionId", "Id" });

            migrationBuilder.CreateIndex(
                name: "UX_NarrationRevisions_OnePublishedPerPlace",
                table: "NarrationRevisions",
                column: "PlaceId",
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationJobs_RevisionId_Language",
                table: "GenerationJobs",
                columns: new[] { "RevisionId", "Language" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NarrationRevisions_Places_PlaceId",
                table: "NarrationRevisions",
                column: "PlaceId",
                principalTable: "Places",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Places_NarrationRevisions_PublishedRevisionId_Id",
                table: "Places",
                columns: new[] { "PublishedRevisionId", "Id" },
                principalTable: "NarrationRevisions",
                principalColumns: new[] { "Id", "PlaceId" });
            // 1.3: Chặn gán PublishedRevisionId trỏ đến bản không phải Published (Status <> 1)
            migrationBuilder.Sql(@"
CREATE TRIGGER TR_Places_ValidatePublished
ON Places
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN NarrationRevisions r ON r.Id = i.PublishedRevisionId
        WHERE i.PublishedRevisionId IS NOT NULL AND r.Status <> 1
    )
    BEGIN
        RAISERROR('DB Integrity Error: PublishedRevisionId must point to a Published revision (Status = 1).', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;");

            // 1.4: Chặn sửa/xóa bản Published (1) và Archived (2), chỉ cho phép chuyển Published (1) -> Archived (2)
            migrationBuilder.Sql(@"
CREATE TRIGGER TR_NarrationRevisions_ProtectImmutable
ON NarrationRevisions
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    -- Chặn DELETE bản Published hoặc Archived
    IF NOT EXISTS (SELECT 1 FROM inserted)
    BEGIN
        IF EXISTS (SELECT 1 FROM deleted WHERE Status IN (1, 2))
        BEGIN
            RAISERROR('DB Integrity Error: Cannot delete Published or Archived revision.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
        RETURN;
    END

    -- Chặn UPDATE bản Archived (2), hoặc sửa bất kỳ cột nào của Published (1) ngoại trừ chuyển Status: 1 -> 2
    IF EXISTS (
        SELECT 1
        FROM deleted d
        JOIN inserted i ON d.Id = i.Id
        WHERE d.Status = 2
           OR (d.Status = 1 AND NOT (
                i.Status = 2
                AND i.PlaceId = d.PlaceId
                AND i.Version = d.Version
                AND ISNULL(i.CreatedByAdminId, '00000000-0000-0000-0000-000000000000') = ISNULL(d.CreatedByAdminId, '00000000-0000-0000-0000-000000000000')
                AND i.CreatedAt = d.CreatedAt
                AND ISNULL(i.PublishedAt, '1900-01-01') = ISNULL(d.PublishedAt, '1900-01-01')
           ))
    )
    BEGIN
        RAISERROR('DB Integrity Error: Published and Archived revisions are immutable (only Published -> Archived is allowed).', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;");

            // 1.4: Chặn sửa/xóa bản dịch thuộc phiên bản đã Published (1) hoặc Archived (2)
            migrationBuilder.Sql(@"
CREATE TRIGGER TR_NarrationTranslations_ProtectImmutable
ON NarrationTranslations
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM deleted d
        JOIN NarrationRevisions r ON r.Id = d.RevisionId
        WHERE r.Status IN (1, 2)
    )
    BEGIN
        RAISERROR('DB Integrity Error: Cannot modify or delete translations of Published or Archived revisions.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_NarrationTranslations_ProtectImmutable;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_NarrationRevisions_ProtectImmutable;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Places_ValidatePublished;");
            migrationBuilder.DropForeignKey(
                name: "FK_NarrationRevisions_Places_PlaceId",
                table: "NarrationRevisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Places_NarrationRevisions_PublishedRevisionId_Id",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Places_PublishedRevisionId_Id",
                table: "Places");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_NarrationRevisions_Id_PlaceId",
                table: "NarrationRevisions");

            migrationBuilder.DropIndex(
                name: "UX_NarrationRevisions_OnePublishedPerPlace",
                table: "NarrationRevisions");

            migrationBuilder.DropIndex(
                name: "IX_GenerationJobs_RevisionId_Language",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "NeedsReview",
                table: "NarrationTranslations");

            migrationBuilder.DropColumn(
                name: "SourceContentHash",
                table: "NarrationTranslations");

            migrationBuilder.CreateIndex(
                name: "IX_Places_PublishedRevisionId",
                table: "Places",
                column: "PublishedRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationJobs_RevisionId",
                table: "GenerationJobs",
                column: "RevisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_NarrationRevisions_Places_PlaceId",
                table: "NarrationRevisions",
                column: "PlaceId",
                principalTable: "Places",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Places_NarrationRevisions_PublishedRevisionId",
                table: "Places",
                column: "PublishedRevisionId",
                principalTable: "NarrationRevisions",
                principalColumn: "Id");
        }
    }
}
