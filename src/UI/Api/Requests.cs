namespace Api;

public record UpdateRadioSourceRequest(string Name, string NewSourceUrl, bool IsActive);
public record AddRadioSourceRequest(string Name, string SourceUrl);

public record LoginRequest(string Username, string Password);