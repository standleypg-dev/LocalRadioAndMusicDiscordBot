using Microsoft.Extensions.Configuration;

namespace Application.Configs;

public static class ConfigurationHelper
{
    public static T GetConfiguration<T>(IConfiguration configuration, string section)
    {
        return configuration.GetSection(section).Get<T>()
            ?? throw new ArgumentNullException($"Section '{section}' not found or null");
    }
}

