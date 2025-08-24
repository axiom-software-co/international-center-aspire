using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternationalCenter.Migrations.Service.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseArchitectureOptimizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============ FOREIGN KEY RELATIONSHIPS ============
            
            // Add CategoryId foreign key to news_articles (if needed in future)
            // Note: Keeping current string-based category for now, can add FK later when news categories are actively used
            
            // ============ SEARCH INFRASTRUCTURE IMPROVEMENTS ============
            
            // Convert search_vector columns from text to tsvector for proper full-text search
            migrationBuilder.Sql(@"
                -- Convert news_articles search_vector to proper tsvector type
                ALTER TABLE news_articles 
                ALTER COLUMN search_vector TYPE tsvector USING to_tsvector('english', coalesce(""Title"", '') || ' ' || coalesce(""Content"", ''));
                
                -- Convert research_articles search_vector to proper tsvector type  
                ALTER TABLE research_articles 
                ALTER COLUMN search_vector TYPE tsvector USING to_tsvector('english', coalesce(""Title"", '') || ' ' || coalesce(""Content"", ''));
                
                -- Convert unified_search search_vector to proper tsvector type
                ALTER TABLE unified_search 
                ALTER COLUMN search_vector TYPE tsvector USING to_tsvector('english', coalesce(""Title"", '') || ' ' || coalesce(""Content"", ''));
            ");

            // ============ FULL-TEXT SEARCH INDEXES ============
            
            // Create GIN indexes for full-text search performance
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS IX_news_articles_search_vector 
                ON news_articles USING gin(search_vector);
                
                CREATE INDEX IF NOT EXISTS IX_research_articles_search_vector 
                ON research_articles USING gin(search_vector);
                
                CREATE INDEX IF NOT EXISTS IX_unified_search_search_vector 
                ON unified_search USING gin(search_vector);
            ");

            // ============ PERFORMANCE INDEXES ============
            
            // Composite indexes for common query patterns
            migrationBuilder.CreateIndex(
                name: "IX_news_articles_status_published_featured",
                table: "news_articles",
                columns: new[] { "Status", "PublishedAt", "Featured" });

            migrationBuilder.CreateIndex(
                name: "IX_research_articles_status_published_featured",
                table: "research_articles", 
                columns: new[] { "Status", "PublishedAt", "Featured" });

            migrationBuilder.CreateIndex(
                name: "IX_events_status_start_date",
                table: "events",
                columns: new[] { "Status", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_services_category_status_featured",
                table: "services",
                columns: new[] { "CategoryId", "Status", "Featured" });

            // ============ UNIQUE CONSTRAINTS ============
            
            // Add missing unique constraints
            migrationBuilder.CreateIndex(
                name: "IX_newsletter_subscriptions_email_unique",
                table: "newsletter_subscriptions",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_events_slug_unique", 
                table: "events",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_research_articles_slug_unique",
                table: "research_articles", 
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_news_categories_slug_unique",
                table: "news_categories",
                column: "Slug", 
                unique: true);

            // ============ JSONB PERFORMANCE OPTIMIZATION ============
            
            // Add GIN indexes for JSONB metadata columns for better query performance
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS IX_contacts_metadata_gin 
                ON contacts USING gin(""Metadata"");
                
                CREATE INDEX IF NOT EXISTS IX_events_metadata_gin 
                ON events USING gin(""Metadata"");
                
                CREATE INDEX IF NOT EXISTS IX_news_articles_metadata_gin 
                ON news_articles USING gin(""Metadata"");
                
                CREATE INDEX IF NOT EXISTS IX_research_articles_metadata_gin 
                ON research_articles USING gin(""Metadata"");
                
                CREATE INDEX IF NOT EXISTS IX_newsletter_subscriptions_metadata_gin 
                ON newsletter_subscriptions USING gin(""Metadata"");
                
                CREATE INDEX IF NOT EXISTS IX_unified_search_metadata_gin 
                ON unified_search USING gin(""Metadata"");
            ");

            // ============ DATA VALIDATION CONSTRAINTS ============
            
            // Add check constraints for status fields to ensure data integrity
            migrationBuilder.Sql(@"
                -- Contact status validation
                ALTER TABLE contacts 
                ADD CONSTRAINT CK_contacts_status 
                CHECK (""Status"" IN ('new', 'pending', 'in_progress', 'resolved', 'closed', 'spam'));
                
                -- Contact type validation  
                ALTER TABLE contacts
                ADD CONSTRAINT CK_contacts_type
                CHECK (""Type"" IN ('general', 'support', 'sales', 'partnership', 'media', 'other'));
                
                -- Event status validation
                ALTER TABLE events
                ADD CONSTRAINT CK_events_status
                CHECK (""Status"" IN ('draft', 'published', 'cancelled', 'completed', 'archived'));
                
                -- Event registration status validation
                ALTER TABLE event_registrations 
                ADD CONSTRAINT CK_event_registrations_status
                CHECK (""Status"" IN ('pending', 'confirmed', 'cancelled', 'attended', 'no_show'));
                
                -- News article status validation
                ALTER TABLE news_articles
                ADD CONSTRAINT CK_news_articles_status  
                CHECK (""Status"" IN ('draft', 'review', 'published', 'archived', 'deleted'));
                
                -- Research article status validation
                ALTER TABLE research_articles
                ADD CONSTRAINT CK_research_articles_status
                CHECK (""Status"" IN ('draft', 'review', 'published', 'archived', 'deleted'));
                
                -- Service status validation
                ALTER TABLE services
                ADD CONSTRAINT CK_services_status
                CHECK (""Status"" IN ('draft', 'active', 'inactive', 'deprecated', 'archived'));
                
                -- Newsletter subscription status validation
                ALTER TABLE newsletter_subscriptions
                ADD CONSTRAINT CK_newsletter_subscriptions_status
                CHECK (""Status"" IN ('pending', 'confirmed', 'unsubscribed', 'bounced', 'complained'));
            ");

            // ============ ADDITIONAL PERFORMANCE INDEXES ============
            
            // Missing indexes for foreign key columns
            migrationBuilder.CreateIndex(
                name: "IX_event_registrations_attendee_email",
                table: "event_registrations",
                column: "AttendeeEmail");

            migrationBuilder.CreateIndex(
                name: "IX_event_registrations_status_created",
                table: "event_registrations", 
                columns: new[] { "Status", "CreatedAt" });

            // Date-based indexes for time-series queries
            migrationBuilder.CreateIndex(
                name: "IX_events_start_end_dates",
                table: "events",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_contacts_created_status",
                table: "contacts",
                columns: new[] { "CreatedAt", "Status" });

            // ============ ARRAY COLUMN INDEXES ============
            
            // Add GIN indexes for array columns to enable efficient queries on tags, features, etc.
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS IX_news_articles_tags_gin 
                ON news_articles USING gin(""Tags"");
                
                CREATE INDEX IF NOT EXISTS IX_research_articles_tags_gin 
                ON research_articles USING gin(""Tags"");
                
                CREATE INDEX IF NOT EXISTS IX_research_articles_keywords_gin 
                ON research_articles USING gin(""Keywords"");
                
                CREATE INDEX IF NOT EXISTS IX_events_tags_gin 
                ON events USING gin(""Tags"");
                
                CREATE INDEX IF NOT EXISTS IX_services_technologies_gin 
                ON services USING gin(""Technologies"");
                
                CREATE INDEX IF NOT EXISTS IX_services_features_gin 
                ON services USING gin(""Features"");
                
                CREATE INDEX IF NOT EXISTS IX_newsletter_subscriptions_preferences_gin 
                ON newsletter_subscriptions USING gin(""Preferences"");
                
                CREATE INDEX IF NOT EXISTS IX_unified_search_tags_gin 
                ON unified_search USING gin(""Tags"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ============ ROLLBACK DATA VALIDATION CONSTRAINTS ============
            
            migrationBuilder.Sql(@"
                ALTER TABLE contacts DROP CONSTRAINT IF EXISTS CK_contacts_status;
                ALTER TABLE contacts DROP CONSTRAINT IF EXISTS CK_contacts_type;
                ALTER TABLE events DROP CONSTRAINT IF EXISTS CK_events_status;
                ALTER TABLE event_registrations DROP CONSTRAINT IF EXISTS CK_event_registrations_status;
                ALTER TABLE news_articles DROP CONSTRAINT IF EXISTS CK_news_articles_status;
                ALTER TABLE research_articles DROP CONSTRAINT IF EXISTS CK_research_articles_status;
                ALTER TABLE services DROP CONSTRAINT IF EXISTS CK_services_status;
                ALTER TABLE newsletter_subscriptions DROP CONSTRAINT IF EXISTS CK_newsletter_subscriptions_status;
            ");

            // ============ ROLLBACK ARRAY INDEXES ============
            
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_news_articles_tags_gin;
                DROP INDEX IF EXISTS IX_research_articles_tags_gin;
                DROP INDEX IF EXISTS IX_research_articles_keywords_gin;
                DROP INDEX IF EXISTS IX_events_tags_gin;
                DROP INDEX IF EXISTS IX_services_technologies_gin;
                DROP INDEX IF EXISTS IX_services_features_gin;
                DROP INDEX IF EXISTS IX_newsletter_subscriptions_preferences_gin;
                DROP INDEX IF EXISTS IX_unified_search_tags_gin;
            ");

            // ============ ROLLBACK PERFORMANCE INDEXES ============
            
            migrationBuilder.DropIndex(
                name: "IX_contacts_created_status",
                table: "contacts");

            migrationBuilder.DropIndex(
                name: "IX_events_start_end_dates",
                table: "events");

            migrationBuilder.DropIndex(
                name: "IX_event_registrations_status_created",
                table: "event_registrations");

            migrationBuilder.DropIndex(
                name: "IX_event_registrations_attendee_email",
                table: "event_registrations");

            // ============ ROLLBACK JSONB INDEXES ============
            
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_contacts_metadata_gin;
                DROP INDEX IF EXISTS IX_events_metadata_gin;
                DROP INDEX IF EXISTS IX_news_articles_metadata_gin;
                DROP INDEX IF EXISTS IX_research_articles_metadata_gin;
                DROP INDEX IF EXISTS IX_newsletter_subscriptions_metadata_gin;
                DROP INDEX IF EXISTS IX_unified_search_metadata_gin;
            ");

            // ============ ROLLBACK UNIQUE CONSTRAINTS ============
            
            migrationBuilder.DropIndex(
                name: "IX_news_categories_slug_unique",
                table: "news_categories");

            migrationBuilder.DropIndex(
                name: "IX_research_articles_slug_unique",
                table: "research_articles");

            migrationBuilder.DropIndex(
                name: "IX_events_slug_unique",
                table: "events");

            migrationBuilder.DropIndex(
                name: "IX_newsletter_subscriptions_email_unique",
                table: "newsletter_subscriptions");

            // ============ ROLLBACK COMPOSITE INDEXES ============
            
            migrationBuilder.DropIndex(
                name: "IX_services_category_status_featured",
                table: "services");

            migrationBuilder.DropIndex(
                name: "IX_events_status_start_date",
                table: "events");

            migrationBuilder.DropIndex(
                name: "IX_research_articles_status_published_featured",
                table: "research_articles");

            migrationBuilder.DropIndex(
                name: "IX_news_articles_status_published_featured",
                table: "news_articles");

            // ============ ROLLBACK FULL-TEXT SEARCH INDEXES ============
            
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_news_articles_search_vector;
                DROP INDEX IF EXISTS IX_research_articles_search_vector;
                DROP INDEX IF EXISTS IX_unified_search_search_vector;
            ");

            // ============ ROLLBACK SEARCH INFRASTRUCTURE ============
            
            // Convert tsvector columns back to text
            migrationBuilder.Sql(@"
                ALTER TABLE news_articles 
                ALTER COLUMN search_vector TYPE text USING search_vector::text;
                
                ALTER TABLE research_articles 
                ALTER COLUMN search_vector TYPE text USING search_vector::text;
                
                ALTER TABLE unified_search 
                ALTER COLUMN search_vector TYPE text USING search_vector::text;
            ");
        }
    }
}
