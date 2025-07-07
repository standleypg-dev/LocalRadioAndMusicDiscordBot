using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Radio_Source : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RadioSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SourceUrl = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioSources", x => x.Id); 
                });

            migrationBuilder.Sql("""
                                    INSERT INTO "RadioSources" ("Id", "Name", "SourceUrl", "CreatedAt", "UpdatedAt") VALUES
                                    (gen_random_uuid(), 'Wai FM', 'https://28153.live.streamtheworld.com/WAI_FM_IBANAAC.aac', now(), now()),
                                    (gen_random_uuid(), 'Cats FM', 'https://s4.yesstreaming.net:7019/stream', now(), now()),
                                    (gen_random_uuid(), 'Sarawak FM', 'https://28103.live.streamtheworld.com/SARAWAK_FMAAC.aac', now(), now()),
                                    (gen_random_uuid(), 'Hitz FM', 'https://n09.rcs.revma.com/488kt4sbv4uvv?rj-ttl=5&rj-tok=AAABl-TNQ8MAAqxQIIxvj7gh5A', now(), now()),
                                    (gen_random_uuid(), 'Traxx FM', 'https://22253.live.streamtheworld.com/TRAXX_FMAAC.aac', now(), now()),
                                    (gen_random_uuid(), 'Klasik FM', 'https://22273.live.streamtheworld.com/RADIO_KLASIKAAC_SC', now(), now()),
                                    (gen_random_uuid(), 'Hot FM', 'https://mediaprima.rastream.com/mediaprima-hotfm?awparams=companionads%3Afalse%3Btags%3Aradioactive%3Bstationid%3Amediaprima-hotfm&playerid=Hot%20FM_web&authtoken=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJvaWQiOiJsYXlsaW8iLCJpYXQiOjE1NzQxNTI5MjMsImV4cCI6MTU3NDIzOTMyM30.1xeZBOUhd1OeGsUnMEZgDdaLjKTrSrtxU3eSqLlZ5nE&aw_0_1st.lotame_segments=%5B%5D&lan=%5B%22ms%22%5D&setLanguage=true&listenerid=cae4395ba6c6a3a473e2af6af5f9f6fd', now(), now()),
                                    (gen_random_uuid(), 'Sinar FM', 'https://n13.rcs.revma.com/azatk0tbv4uvv?rj-ttl=5&rj-tok=AAABl-TO6SMAk3NBussdDeaJpA', now(), now());
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RadioSources");
        }
    }
}
