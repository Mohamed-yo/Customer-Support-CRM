using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerSupportCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class BackfillWebhookSigningSecrets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Story 15 Phase 4: existing subscriptions predate HMAC signing and would
            // otherwise send unsigned forever. One unique, unguessable secret per row -
            // no C# code runs during a migration, so this generates it in T-SQL.
            migrationBuilder.Sql(@"
                UPDATE OutboundWebhookSubscriptions
                SET SigningSecret = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varchar(36), NEWID())), 2)
                WHERE SigningSecret IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only migration - no schema change to revert. Down deliberately leaves the
            // backfilled secrets in place (clearing them would break signing on rollback for
            // no benefit).
        }
    }
}
