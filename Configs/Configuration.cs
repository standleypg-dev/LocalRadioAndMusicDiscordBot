using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace radio_discord_bot.Configs;

public static class Configuration
{
    public static T GetConfiguration<T>(string section)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
    
        return configuration.GetSection(section).Get<T>() ?? throw new ArgumentNullException();
    }
}
