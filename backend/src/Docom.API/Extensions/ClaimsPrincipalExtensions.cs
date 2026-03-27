using System.Security.Claims;

namespace Docom.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim missing.");
        return int.Parse(value);
    }

    public static int GetDoctorId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("doctorId")
            ?? throw new UnauthorizedAccessException("Doctor ID claim missing. Ensure you are logged in as a doctor.");
        return int.Parse(value);
    }

    public static string GetDoctorSlug(this ClaimsPrincipal user)
        => user.FindFirstValue("doctorSlug")
            ?? throw new UnauthorizedAccessException("Doctor slug claim missing.");
}
