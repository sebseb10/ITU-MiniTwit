using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chirp.Infrastructure.Migrations
{
    public partial class AddPerformanceIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CONCURRENTLY: no table lock, reads/writes continue during index build.
            // suppressTransaction: true is required because CONCURRENTLY cannot run inside a transaction.
            migrationBuilder.Sql(
                @"CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_Cheeps_Timestamp"" ON ""Cheeps"" (""Timestamp"" DESC)",
                suppressTransaction: true);

            migrationBuilder.Sql(
                @"CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_Recheeps_CheepID"" ON ""Recheeps"" (""CheepID"")",
                suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DROP INDEX CONCURRENTLY IF EXISTS ""IX_Cheeps_Timestamp""",
                suppressTransaction: true);

            migrationBuilder.Sql(
                @"DROP INDEX CONCURRENTLY IF EXISTS ""IX_Recheeps_CheepID""",
                suppressTransaction: true);
        }
    }
}
