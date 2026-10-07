using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos.Business;

public class BusAssignmentDto
{
    public Guid Id { get; set; }

    [Range(1, int.MaxValue)]
    public int BusId { get; set; }

    [Required]
    public Guid DriverId { get; set; }

    [Required]
    public DateTime FromDate { get; set; } = DateTime.Today;

    [Required]
    public DateTime ToDate { get; set; } = DateTime.Today.AddMonths(6);

    [Required, StringLength(100)]
    public string SupervisorName { get; set; } = string.Empty;

    [Required, Phone, StringLength(20)]
    public string SupervisorMobile { get; set; } = string.Empty;
}
