using Domain.Events;

namespace Domain.Common;

public static class Constants
{
    public static class CustomIds
    {
        public const string Play = nameof(EventType.Play);
        public const string PlayListPlay = nameof(EventType.PlayListPlay);
        public const string Skip = nameof(EventType.Skip);
        public const string Stop = nameof(EventType.Stop);
    }
}

public class EventType
{
    public record Play : IEvent;
    public record PlayListPlay : IEvent;
    public record Stop : IEvent;
    public record Skip : IEvent;
}

public enum AudioSource
{
    Youtube,
    SoundCloud,
    Url,
    Radio
}