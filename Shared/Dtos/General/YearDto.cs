using Shared.Dtos.Business;

namespace Shared.Dtos.General
{
    public class YearDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsNext { get; set; }

        public List<SemesterDto> Semesters { get; set; } = new();
    }
}
