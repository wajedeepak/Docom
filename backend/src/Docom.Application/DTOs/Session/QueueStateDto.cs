using Docom.Application.DTOs.Token;

namespace Docom.Application.DTOs.Session;

public record QueueStateDto(
    int SessionId,
    string SessionLabel,
    string SessionStatus,
    int CurrentTokenNumber,
    int LastIssuedTokenNumber,
    int WaitingCount,
    int EstimatedWaitMinutes,
    IEnumerable<TokenDto> Queue
);

public record PublicQueueStateDto(
    int? SessionId,
    string DoctorName,
    string Specialization,
    bool SessionActive,
    int CurrentTokenNumber,
    int WaitingCount,
    int EstimatedWaitMinutes,
    string SessionStatus
);
