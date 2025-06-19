namespace Domain.Common;

public class EntityBase
{
    public DateTime CreatedAt
    {
        get
        {
            var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore");
            return TimeZoneInfo.ConvertTimeFromUtc(field, malaysiaTimeZone);
        }
        private set;
    }

    public DateTime UpdatedAt
    {
        get
        {
            var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore");
            return TimeZoneInfo.ConvertTimeFromUtc(field, malaysiaTimeZone);
        }
        
        private set;
    }
}