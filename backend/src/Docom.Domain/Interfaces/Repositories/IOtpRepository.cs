using Docom.Domain.Entities;

namespace Docom.Domain.Interfaces.Repositories;

public interface IOtpRepository
{
    Task<OtpRequest> CreateAsync(OtpRequest request);
    Task<OtpRequest?> GetLatestValidAsync(string email);
    Task MarkUsedAsync(int id);
    Task InvalidatePreviousAsync(string email);
}
