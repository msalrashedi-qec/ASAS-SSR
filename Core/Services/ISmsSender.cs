namespace Core.Services;

public interface ISmsSender
{
    Task<SmsSendResult> SendAsync(SmsSendRequest request, CancellationToken cancellationToken = default);
}

public sealed record SmsSendRequest(
    string Sender,
    string UserName,
    string ApiKey,
    IReadOnlyCollection<string> Recipients,
    string Message);

public sealed record SmsSendResult(bool IsSuccess, int RecipientCount, string? ErrorMessage = null);
