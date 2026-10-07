namespace Core.Entities.Business
{
    public class StudentBusAssignment : GuidEntityBase
    {
        public Guid SubscriptionId { get; set; }
        public int BusId { get; set; }

        public virtual StudentSubscription Subscription { get; set; } = null!;
        public virtual Bus Bus { get; set; } = null!;
    }
}
