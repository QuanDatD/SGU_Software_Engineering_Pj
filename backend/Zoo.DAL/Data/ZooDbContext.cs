using Microsoft.EntityFrameworkCore;
using Zoo.DAL.Entities;

namespace Zoo.DAL.Data;

public class ZooDbContext : DbContext
{
    public ZooDbContext(DbContextOptions<ZooDbContext> options) : base(options) { }

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<NarrationRevision> NarrationRevisions => Set<NarrationRevision>();
    public DbSet<NarrationTranslation> NarrationTranslations => Set<NarrationTranslation>();
    public DbSet<GenerationJob> GenerationJobs => Set<GenerationJob>();
    public DbSet<SiteText> SiteTexts => Set<SiteText>();
    public DbSet<QrVisit> QrVisits => Set<QrVisit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AdminUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<Place>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Category).HasMaxLength(30).IsRequired();

            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Places_Latitude", "[Latitude] >= -90 AND [Latitude] <= 90");
                t.HasCheckConstraint("CK_Places_Longitude", "[Longitude] >= -180 AND [Longitude] <= 180");
                t.HasCheckConstraint("CK_Places_RadiusMeters", "[RadiusMeters] > 0 AND [RadiusMeters] <= 500");
                t.HasTrigger("TR_Places_ValidatePublished");
            });

            // Ràng buộc 1.3: Composite FK (PublishedRevisionId, Id) -> (Id, PlaceId)
            // Đảm bảo ở mức DB rằng PublishedRevisionId luôn thuộc đúng Place này
            e.HasOne(x => x.PublishedRevision)
                .WithMany()
                .HasForeignKey(x => new { x.PublishedRevisionId, x.Id })
                .HasPrincipalKey(r => new { r.Id, r.PlaceId })
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<NarrationRevision>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasAlternateKey(x => new { x.Id, x.PlaceId });
            e.HasIndex(x => new { x.PlaceId, x.Version }).IsUnique();

            // Ràng buộc 1.3: Mỗi điểm có tối đa một bản Published (Status = 1)
            e.HasIndex(x => x.PlaceId)
                .HasFilter("[Status] = 1")
                .IsUnique()
                .HasDatabaseName("UX_NarrationRevisions_OnePublishedPerPlace");

            e.ToTable(t =>
            {
                t.HasTrigger("TR_NarrationRevisions_ProtectImmutable");
            });

            // Ràng buộc 1.4: ON DELETE NO ACTION -> SQL Server tự chặn xóa Place nếu còn phiên bản
            e.HasOne(x => x.Place)
                .WithMany(p => p.Revisions)
                .HasForeignKey(x => x.PlaceId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne<AdminUser>()
                .WithMany()
                .HasForeignKey(x => x.CreatedByAdminId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });

        modelBuilder.Entity<NarrationTranslation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Language).HasMaxLength(10).IsRequired();
            e.Property(x => x.SourceContentHash).HasMaxLength(128);
            e.HasIndex(x => new { x.RevisionId, x.Language }).IsUnique();

            e.ToTable(t =>
            {
                t.HasTrigger("TR_NarrationTranslations_ProtectImmutable");
            });

            e.HasOne(x => x.Revision)
                .WithMany(r => r.Translations)
                .HasForeignKey(x => x.RevisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GenerationJob>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Language).HasMaxLength(10).IsRequired();
            e.HasIndex(x => new { x.Status, x.LockedUntilUtc });

            // Ràng buộc 1.3: Mỗi cặp (RevisionId, Language) chỉ có một job
            e.HasIndex(x => new { x.RevisionId, x.Language }).IsUnique();

            e.HasOne(x => x.Revision)
                .WithMany(r => r.Jobs)
                .HasForeignKey(x => x.RevisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SiteText>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(150).IsRequired();
            e.Property(x => x.Language).HasMaxLength(10).IsRequired();
            e.HasIndex(x => new { x.Key, x.Language }).IsUnique();
        });

        modelBuilder.Entity<QrVisit>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SessionId).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.SessionId).IsUnique();
            e.HasIndex(x => x.VisitedAtUtc);
        });
    }
}