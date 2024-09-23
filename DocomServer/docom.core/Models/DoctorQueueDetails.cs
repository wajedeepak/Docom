namespace docom.core.Models
{
    public class DoctorQueueDetails 
    {
        public Guid QueueId { get; set; }   
        public List<DoctorQueueNumber>? Queue { get; set; }
    }
}
