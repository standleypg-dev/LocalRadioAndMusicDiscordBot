using Microsoft.Extensions.Configuration;

namespace radio_discord_bot.Configs;

public static class ConfigurationHelper
{
    public static T GetConfiguration<T>(IConfiguration configuration, string section)
    {
        return configuration.GetSection(section).Get<T>()
            ?? throw new ArgumentNullException($"Section '{section}' not found or null");
    }
}

