using System.ComponentModel;

namespace Domain.Common.Enums;

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
