using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos.Business;

public class BusDto
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string PlateNo { get; set; } = string.Empty;

    [Range(1, byte.MaxValue)]
    public byte TypeId { get; set; }

    [Range(1950, 2200)]
    public int ManufacturingYear { get; set; } = DateTime.Today.Year;

    [Required, StringLength(50)]
    public string Color { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string VIN { get; set; } = string.Empty;

    [Range(1, 200)]
    public int Capacity { get; set; }

    [Required, StringLength(50)]
    public string RegisterNo { get; set; } = string.Empty;

    [Required]
    public DateTime RegistrationExpiry { get; set; } = DateTime.Today.AddYears(1);
}
