namespace Domain.Common;

public class TokenContainer
{
    public required CancellationToken StopToken { get; init; }
    public required CancellationToken SkipToken { get; init; }
}