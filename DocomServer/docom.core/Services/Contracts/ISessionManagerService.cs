using docom.core.Models;

namespace docom.core.Services.Contracts
{
    public interface ISessionManagerService
    {
        public string GetSessionStatus();
        public DoctorSession StartSession();
        public string PauseSession();
        public string EndSession();
        public PatientQueueStatus? GetNumber(string patientName, string patientContact);
        public DoctorQueueNumber MoveNumber();
        public int GetTotalNumbers();
        public int GetCurrentNumber();
    }
}
