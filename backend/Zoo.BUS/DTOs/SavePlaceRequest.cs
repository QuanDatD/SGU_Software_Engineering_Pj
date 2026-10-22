using System.ComponentModel.DataAnnotations;
namespace Zoo.BUS.DTOs;
public class SavePlaceRequest
{
    [Required, StringLength(200)] public string NameVi { get; set; } = "";
    [StringLength(4000)] public string DescriptionVi { get; set; } = "";
    [RegularExpression("^(animal|plant|facility)$")] public string Category { get; set; } = "animal";
    [Range(-90d, 90d)] public double Latitude { get; set; }
    [Range(-180d, 180d)] public double Longitude { get; set; }
    [Range(5d, 500d)] public double RadiusMeters { get; set; } = 30;
    [Range(0d, 1024d)] public double MapX { get; set; }
    [Range(0d, 643d)] public double MapY { get; set; }
    public int Priority { get; set; } = 1;
}
