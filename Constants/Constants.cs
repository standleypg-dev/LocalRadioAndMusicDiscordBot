using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace radio_discord_bot;

public static class Constants
{
    public static string GET_HELP_MESSAGE = "Hello kaban! Enti ka ngena bot tu, titih ka aja command ba baruh nya:\n" +
               "1. `/pasang radio` - Start masang radio ba voice channel alai nuan.\n" +
               "2. `/pasang tajuk_lagu` - Start masang lagu ba voice channel alai nuan.\n" +
               "3. `/pasang url` - Start masang url ba voice channel alai nuan.\n" +
               "4. `/next` - Skip lagu ti benung dipasang.\n" +
               "5. `/playlist` - Meda playlist nuan.\n" +
               "6. `/tutup` - Badu masang. Bot pansut.";

}

public class Radio
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
