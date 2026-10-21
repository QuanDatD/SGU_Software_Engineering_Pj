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
            });

            e.HasOne(x => x.PublishedRevision)
                .WithMany()
                .HasForeignKey(x => x.PublishedRevisionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<NarrationRevision>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.PlaceId, x.Version }).IsUnique();

            e.HasOne(x => x.Place)
                .WithMany(p => p.Revisions)
                .HasForeignKey(x => x.PlaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NarrationTranslation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Language).HasMaxLength(10).IsRequired();
            e.HasIndex(x => new { x.RevisionId, x.Language }).IsUnique();

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
    }
}