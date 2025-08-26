using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Service.Audit.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuditSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    UserName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    Signature = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SignatureAlgorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.Id);
                },
                comment: "Medical-grade audit events with tamper-proof signatures for compliance tracking");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_correlation_id",
                table: "audit_events",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_entity",
                table: "audit_events",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_event_type",
                table: "audit_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_timestamp",
                table: "audit_events",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_user_id",
                table: "audit_events",
                column: "UserId");

            // Add comment to describe medical compliance requirements
            migrationBuilder.Sql(@"
                COMMENT ON TABLE audit_events IS 'Medical-grade audit events with tamper-proof signatures for 7-year retention compliance';
                COMMENT ON COLUMN audit_events.""Signature"" IS 'HMAC signature for tamper detection and medical compliance';
                COMMENT ON COLUMN audit_events.""OldValues"" IS 'JSON snapshot of entity state before change for audit trail';
                COMMENT ON COLUMN audit_events.""NewValues"" IS 'JSON snapshot of entity state after change for audit trail';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_events");
        }
    }
}