using Shared.Dtos.Discounts;
using Shared.Dtos.General;

namespace Shared.Dtos.Business
{
    public class DiscountRequestDto
    {
        public Guid Id { get; set; }
        public Guid WfProcessId { get; set; }
        public string? RefNo { get; set; }
        public Guid StudentId { get; set; }
        public int DiscountId { get; set; }
        public string? DiscountName { get; set; }
        public string? DiscountNameAr { get; set; }

        public byte? StatusId { get; set; }
        public string? StatusName { get; set; }
        public string? ColorCode { get; set; }
       
        public string? Comments { get; set; }
        public List<FileDto> Attachments { get; set; } = new();
        public List<DiscountRequestDetailDto> Details { get; set; } = new();

    }
}
