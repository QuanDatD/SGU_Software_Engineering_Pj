namespace Zoo.DAL.Entities;

public class Place
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = null!;          // Không trùng (VD: ELEPHANT_ZONE)
    public string Category { get; set; } = "animal";   // animal | plant | facility
    public double Latitude { get; set; }               // Tọa độ thật [-90, 90]
    public double Longitude { get; set; }              // Tọa độ thật [-180, 180]
    public double RadiusMeters { get; set; } = 20;     // Bán kính nhận diện (mét > 0)
    public double MapX { get; set; }                   // Pixel X trên sơ đồ
    public double MapY { get; set; }                   // Pixel Y trên sơ đồ
    public int Priority { get; set; } = 1;             // Số lớn hơn được ưu tiên khi chồng vùng
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;         // Xóa mềm bằng IsActive = false
    public Guid? PublishedRevisionId { get; set; }     // Phiên bản đang xuất bản công khai
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public NarrationRevision? PublishedRevision { get; set; }
    public ICollection<NarrationRevision> Revisions { get; set; } = new List<NarrationRevision>();
}