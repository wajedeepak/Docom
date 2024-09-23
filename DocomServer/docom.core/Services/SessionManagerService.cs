using docom.core.Models;
using docom.core.Services.Contracts;

namespace docom.core.Services
{
    public class SessionManagerService : ISessionManagerService
    {
        private DoctorSession? _doctorSession;

        public string GetSessionStatus()
        {
            if (_doctorSession != null)
            {
                return _doctorSession.Status.ToString();
            }
            return "";
        }
        public DoctorSession StartSession()
        {
            if(_doctorSession == null) {

                _doctorSession = new DoctorSession()
                {
                    SessionId = Guid.NewGuid(),
                    Status = SessionStatus.Started,
                    Queue = new List<DoctorQueueNumber>(),
                    CurrentNumber = new DoctorQueueNumber()
                    {
                        SerialNumber = 0,
                        Name = "",
                        ContactNumber = "",
                    }
                    
                };
            }
            else
            {
                _doctorSession.Status = SessionStatus.Started;
            }
            return _doctorSession;
        }

        public string PauseSession()
        {
            if (_doctorSession != null)
            {
                _doctorSession.Status = SessionStatus.Paused;
                return _doctorSession.Status.ToString();
            }
            return "";
        }
        public string EndSession()
        {
            if(_doctorSession != null)
            {
                _doctorSession = null;
                return SessionStatus.Ended.ToString();
            }
            return "";
        }

        public PatientQueueStatus? GetNumber(string patientName, string patientContact)
        {
            if (IsSessionActive())
            {
                DoctorQueueNumber patientNumber;
                var patientQueueNumber = _doctorSession?.Queue?.Find(q => q.ContactNumber == patientContact.Trim());
                if (patientQueueNumber == null)
                {
                    patientNumber = new DoctorQueueNumber()
                    {
                        SerialNumber = GetTotalNumbers() + 1,
                        Name = patientName,
                        ContactNumber = patientContact
                    };
                    _doctorSession?.Queue?.Add(patientNumber);
                }
                else
                {
                    patientNumber = patientQueueNumber;
                }
            
                return new PatientQueueStatus()
                {
                    PatientNumber = patientNumber?.SerialNumber,
                    CurrentNumber = _doctorSession?.CurrentNumber?.SerialNumber
                };
            }
            return null;
        }
        public DoctorQueueNumber MoveNumber()
        {
            var lastNumber = GetTotalNumbers();
            if (_doctorSession?.CurrentNumber?.SerialNumber < lastNumber)
            {
                var nextNumber = _doctorSession?.CurrentNumber?.SerialNumber + 1;
                var _currentNumber = _doctorSession?.Queue?.Find(q => q.SerialNumber == nextNumber);
                _doctorSession?.setCurrentNumber(_currentNumber);

            }
            return _doctorSession?.CurrentNumber;
        }

        public int GetTotalNumbers()
        {
            int lastNumber = 0;
            if (IsSessionActive())
            {
                foreach (DoctorQueueNumber doctorQueue in _doctorSession.Queue)
                {
                    if (doctorQueue.SerialNumber > lastNumber)
                    {
                        lastNumber = doctorQueue.SerialNumber;
                    }
                }
            }
            return lastNumber;
        }

        public int GetCurrentNumber()
        {
            var currentNumber = _doctorSession?.CurrentNumber?.SerialNumber;
            return (int)(currentNumber == null ? 0 : currentNumber);
        }
        
        private bool IsSessionActive()
        {
            if(_doctorSession != null)
            {
                if (_doctorSession.Status == SessionStatus.Started)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
