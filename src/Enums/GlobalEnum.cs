using System.ComponentModel;

namespace radio_discord_bot.Enums;

public enum YtSearchCollection
{
    FirstFive,
    Random
}

public enum PostRequestMediaType
{
    [Description("application/json")]
    Json,
    [Description("application/x-www-form-urlencoded")]
    FormUrlEncoded
}
