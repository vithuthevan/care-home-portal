using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHome.Api.Migrations
{
    /// <inheritdoc />
    public partial class SyncCollectionReminderModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Collection reminder columns and CollectionReminderLogs were already created by
            // 20260926120000_AddCollectionReminderEmailAndPolicyTemplates. This migration only
            // updates the EF model snapshot so MigrateAsync no longer fails on pending model changes.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
