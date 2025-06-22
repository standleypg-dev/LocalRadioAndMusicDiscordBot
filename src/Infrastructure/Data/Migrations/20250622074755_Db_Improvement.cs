using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Db_Improvement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalPlays",
                table: "PlayHistory",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
              CREATE TEMP TABLE tmp_playhistory_aggregated AS
                SELECT
                    (
                        SELECT ph2.""Id""
                        FROM ""PlayHistory"" ph2
                        WHERE ph2.""UserId"" = ph.""UserId""
                          AND ph2.""SongId"" = ph.""SongId""
                        ORDER BY ph2.""PlayedAt""
                        LIMIT 1
                    ) AS ""Id"",
                    ph.""UserId"",
                    ph.""SongId"",
                    MIN(ph.""PlayedAt"") AS ""PlayedAt"",
                    COUNT(*) AS ""TotalPlays"",
                    MIN(ph.""CreatedAt"") AS ""CreatedAt"",
                    MAX(ph.""UpdatedAt"") AS ""UpdatedAt""
                FROM ""PlayHistory"" ph
                GROUP BY ph.""UserId"", ph.""SongId"";

                DELETE FROM ""PlayHistory""
                WHERE (""UserId"", ""SongId"") IN (
                    SELECT ""UserId"", ""SongId"" FROM tmp_playhistory_aggregated
                );

                INSERT INTO ""PlayHistory"" (
                    ""Id"", ""UserId"", ""SongId"", ""PlayedAt"", ""TotalPlays"", ""CreatedAt"", ""UpdatedAt""
                )
                SELECT
                    ""Id"", ""UserId"", ""SongId"", ""PlayedAt"", ""TotalPlays"", ""CreatedAt"", ""UpdatedAt""
                FROM tmp_playhistory_aggregated;

                DROP TABLE tmp_playhistory_aggregated;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalPlays",
                table: "PlayHistory");
        }
    }
}
