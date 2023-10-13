using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace radio_discord_bot;

public static class Constants
{
    public static List<Radio> radios = new List<Radio>()
    {
        new Radio(){Title = "Wai FM", Url = "https://23693.live.streamtheworld.com/WAI_FM_IBANAAC.aac?pname=StandardPlayerV4&pversion=4.19.2-044&dist=triton-web&tdsdk=js-2.9&swm=false&banners=300x250&burst-time=15&sbmid=ab12c2a8-8139-4947-d0ec-ae741ea1a594"},
        new Radio(){Title = "Traxx FM", Url = "https://22273.live.streamtheworld.com/TRAXX_FMAAC.aac?dist=triton-widget&tdsdk=js-2.9&swm=false&pname=tdwidgets&pversion=2.9&banners=none&burst-time=15&sbmid=25f8ff3f-5274-46fe-a7c3-d0f153efa0fe"},
        new Radio(){Title = "Hitz FM", Url = "https://astro1.rastream.com/hitz?awparams=companionads%3Afalse%3Btags%3Aradioactive%3Bstationid%3Ahitz&playerid=hitz_web&authtoken=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJvaWQiOiJsYXlsaW8iLCJpYXQiOjE1NzI1OTYyOTAsImV4cCI6MTU3MjY4MjY5MH0.PnOgVwp7bgBfdUtqLzE4g2wrHSab3JQ64pWjiGW3cOs&lan=%5B%22en%22%5D&setLanguage=true&listenerid=6cfa54807b2234df3d9c7f96963422d5&listening-from-radio-garden=1696362384"},
    };

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
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
