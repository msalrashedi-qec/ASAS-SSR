namespace Shared.Dtos.General
{
    public class BgJobDto
    {
        public int Id { get; set; }
        public int RecurringTypeId { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
    }
}
