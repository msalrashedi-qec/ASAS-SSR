
using Core.Entities.Business;

namespace Core.Entities.General
{
    public class BusType
    {
        public byte Id { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }

        public virtual ICollection<Bus> Buses { get; set; } = new List<Bus>();
    }
}
