using System.ComponentModel.DataAnnotations;

namespace Docom.Application.DTOs.Session;

public record CreateSessionDto(
    [Required, MinLength(1), MaxLength(50)] string Label
);
