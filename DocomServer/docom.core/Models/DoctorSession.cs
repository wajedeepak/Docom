namespace docom.core.Models
{
    public class DoctorSession
    {
        public Guid SessionId { get; set; }
        public SessionStatus Status { get; set;}
        public List<DoctorQueueNumber> Queue { get; set; }
        public DoctorQueueNumber? CurrentNumber { get; set; }

        public DoctorSession()
        {
            Queue = new List<DoctorQueueNumber>();
        }

        public void setCurrentNumber(DoctorQueueNumber newCurrent)
        {
            CurrentNumber = newCurrent;
        }
    }
}
