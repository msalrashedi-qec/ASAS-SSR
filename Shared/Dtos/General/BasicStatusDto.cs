using System.ComponentModel.DataAnnotations;

namespace Shared.Dto.General
{
    public class BasicStatusDto
    {
        public byte Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string NameAr { get; set; }
        public string? ColorCode { get; set; }
        public bool IsActive { get; set; }

    }
}
