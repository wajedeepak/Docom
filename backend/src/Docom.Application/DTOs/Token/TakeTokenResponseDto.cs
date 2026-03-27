namespace Docom.Application.DTOs.Token;

public record TakeTokenResponseDto(
    long TokenId,
    int TokenNumber,
    int CurrentTokenNumber,
    int QueuePosition,
    int EstimatedWaitMinutes,
    string PublicTokenId
);
