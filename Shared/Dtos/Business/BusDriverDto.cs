using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos.Business;

public class BusDriverDto
{
    public Guid Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string NationalID { get; set; } = string.Empty;

    [Required, Phone, StringLength(20)]
    public string Mobile { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int NationalityId { get; set; }
}
