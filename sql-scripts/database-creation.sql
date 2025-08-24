CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE contacts (
    "Id" text NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Email" character varying(255) NOT NULL,
    "Phone" character varying(20) NOT NULL,
    "Subject" character varying(255) NOT NULL,
    "Message" text NOT NULL,
    "Status" character varying(50) NOT NULL,
    "Type" character varying(100) NOT NULL,
    "Source" character varying(100) NOT NULL,
    "IsUrgent" boolean NOT NULL,
    "ResponseSentAt" timestamp with time zone,
    "RespondedBy" character varying(255) NOT NULL,
    "InternalNotes" text NOT NULL,
    "Metadata" jsonb NOT NULL,
    "ConsentGiven" boolean NOT NULL,
    "ConsentDate" timestamp with time zone,
    "DataRetentionDate" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_contacts" PRIMARY KEY ("Id")
);

CREATE TABLE events (
    "Id" text NOT NULL,
    "Slug" character varying(255) NOT NULL,
    "Title" character varying(500) NOT NULL,
    "Description" text NOT NULL,
    "Content" text NOT NULL,
    "StartDate" timestamp with time zone NOT NULL,
    "EndDate" timestamp with time zone NOT NULL,
    "Location" character varying(255) NOT NULL,
    "Address" character varying(500) NOT NULL,
    "IsVirtual" boolean NOT NULL,
    "VirtualLink" character varying(500) NOT NULL,
    "MaxAttendees" integer NOT NULL,
    "CurrentAttendees" integer NOT NULL,
    "Price" numeric NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "IsFree" boolean NOT NULL,
    "RequiresRegistration" boolean NOT NULL,
    "RegistrationDeadline" timestamp with time zone,
    "OrganizerName" character varying(255) NOT NULL,
    "OrganizerEmail" character varying(255) NOT NULL,
    "OrganizerPhone" character varying(20) NOT NULL,
    "Featured" boolean NOT NULL,
    "Status" character varying(50) NOT NULL,
    "Category" character varying(100) NOT NULL,
    "Tags" text[] NOT NULL,
    "ImageUrl" character varying(500) NOT NULL,
    "MetaTitle" character varying(255) NOT NULL,
    "MetaDescription" text NOT NULL,
    "PublishedAt" timestamp with time zone,
    "Metadata" jsonb NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_events" PRIMARY KEY ("Id")
);

CREATE TABLE news_articles (
    "Id" text NOT NULL,
    "Slug" character varying(255) NOT NULL,
    "Title" character varying(500) NOT NULL,
    "Excerpt" text NOT NULL,
    "Content" text NOT NULL,
    "AuthorName" character varying(255) NOT NULL,
    "AuthorEmail" character varying(255) NOT NULL,
    "PublishedAt" timestamp with time zone,
    "Featured" boolean NOT NULL,
    "Status" character varying(50) NOT NULL,
    "Category" character varying(100) NOT NULL,
    "Tags" text[] NOT NULL,
    "ImageUrl" character varying(500) NOT NULL,
    "MetaTitle" character varying(255) NOT NULL,
    "MetaDescription" text NOT NULL,
    "Metadata" jsonb NOT NULL,
    search_vector text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_news_articles" PRIMARY KEY ("Id")
);

CREATE TABLE news_categories (
    "Id" text NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Description" text NOT NULL,
    "Slug" character varying(255) NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "Active" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_news_categories" PRIMARY KEY ("Id")
);

CREATE TABLE newsletter_subscriptions (
    "Id" text NOT NULL,
    "Email" character varying(255) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Status" character varying(50) NOT NULL,
    "Preferences" text[] NOT NULL,
    "Source" character varying(100) NOT NULL,
    "ConfirmedAt" timestamp with time zone,
    "UnsubscribedAt" timestamp with time zone,
    "ConfirmationToken" character varying(255) NOT NULL,
    "UnsubscribeToken" character varying(255) NOT NULL,
    "ConsentGiven" boolean NOT NULL,
    "ConsentDate" timestamp with time zone,
    "Metadata" jsonb NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_newsletter_subscriptions" PRIMARY KEY ("Id")
);

CREATE TABLE research_articles (
    "Id" text NOT NULL,
    "Slug" character varying(255) NOT NULL,
    "Title" character varying(500) NOT NULL,
    "Excerpt" text NOT NULL,
    "Content" text NOT NULL,
    "AuthorName" character varying(255) NOT NULL,
    "AuthorEmail" character varying(255) NOT NULL,
    "PublishedAt" timestamp with time zone,
    "Featured" boolean NOT NULL,
    "Status" character varying(50) NOT NULL,
    "Category" character varying(100) NOT NULL,
    "Tags" text[] NOT NULL,
    "ImageUrl" character varying(500) NOT NULL,
    "MetaTitle" character varying(255) NOT NULL,
    "MetaDescription" text NOT NULL,
    "Metadata" jsonb NOT NULL,
    search_vector text,
    "StudyType" character varying(255) NOT NULL,
    "Keywords" text[] NOT NULL,
    "DOI" character varying(255) NOT NULL,
    "StudyDate" timestamp with time zone,
    "Collaborators" text[] NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_research_articles" PRIMARY KEY ("Id")
);

CREATE TABLE service_categories (
    "Id" text NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Description" text NOT NULL,
    "Slug" character varying(255) NOT NULL,
    "MinPriorityOrder" integer NOT NULL,
    "MaxPriorityOrder" integer NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "Featured1" boolean NOT NULL,
    "Featured2" boolean NOT NULL,
    "Active" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_service_categories" PRIMARY KEY ("Id")
);

CREATE TABLE unified_search (
    "Id" text NOT NULL,
    "ContentId" character varying(255) NOT NULL,
    "ContentType" character varying(50) NOT NULL,
    "Title" character varying(500) NOT NULL,
    "Content" text NOT NULL,
    "Summary" text NOT NULL,
    "Slug" character varying(255) NOT NULL,
    "Category" character varying(100) NOT NULL,
    "Tags" text[] NOT NULL,
    "Author" character varying(255) NOT NULL,
    "PublishedAt" timestamp with time zone,
    "ImageUrl" character varying(500) NOT NULL,
    "Url" character varying(500) NOT NULL,
    "IsPublished" boolean NOT NULL,
    "IsFeatured" boolean NOT NULL,
    "Priority" integer NOT NULL,
    search_vector text,
    "Metadata" jsonb NOT NULL,
    "LastIndexed" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_unified_search" PRIMARY KEY ("Id")
);

CREATE TABLE event_registrations (
    "Id" text NOT NULL,
    "EventId" text NOT NULL,
    "AttendeeName" character varying(255) NOT NULL,
    "AttendeeEmail" character varying(255) NOT NULL,
    "AttendeePhone" character varying(20) NOT NULL,
    "Status" character varying(50) NOT NULL,
    "CheckedInAt" timestamp with time zone,
    "SpecialRequirements" text NOT NULL,
    "Metadata" jsonb NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_event_registrations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_event_registrations_events_EventId" FOREIGN KEY ("EventId") REFERENCES events ("Id") ON DELETE CASCADE
);

CREATE TABLE services (
    "Id" text NOT NULL,
    "Title" character varying(500) NOT NULL,
    "Slug" character varying(255) NOT NULL,
    "Description" text NOT NULL,
    "DetailedDescription" text NOT NULL,
    "Technologies" text[] NOT NULL,
    "Features" text[] NOT NULL,
    "DeliveryModes" text[] NOT NULL,
    "Icon" character varying(255) NOT NULL,
    "Image" character varying(500) NOT NULL,
    "Status" character varying(50) NOT NULL,
    priority bigint NOT NULL,
    "CategoryId" text,
    "Available" boolean NOT NULL,
    "Featured" boolean NOT NULL,
    "Category" character varying(100) NOT NULL,
    "MetaTitle" character varying(255) NOT NULL,
    "MetaDescription" text NOT NULL,
    "PublishedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_services" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_services_service_categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES service_categories ("Id")
);

CREATE INDEX "IX_contacts_CreatedAt" ON contacts ("CreatedAt");

CREATE INDEX "IX_contacts_Email" ON contacts ("Email");

CREATE INDEX "IX_contacts_Status" ON contacts ("Status");

CREATE INDEX "IX_contacts_Type" ON contacts ("Type");

CREATE INDEX "IX_event_registrations_EventId" ON event_registrations ("EventId");

CREATE INDEX "IX_news_articles_Category" ON news_articles ("Category");

CREATE INDEX "IX_news_articles_Featured" ON news_articles ("Featured");

CREATE INDEX "IX_news_articles_PublishedAt" ON news_articles ("PublishedAt");

CREATE UNIQUE INDEX "IX_news_articles_Slug" ON news_articles ("Slug");

CREATE INDEX "IX_news_articles_Status" ON news_articles ("Status");

CREATE INDEX "IX_service_categories_Active" ON service_categories ("Active");

CREATE UNIQUE INDEX "IX_service_categories_Name" ON service_categories ("Name");

CREATE UNIQUE INDEX "IX_service_categories_Slug" ON service_categories ("Slug");

CREATE INDEX "IX_services_CategoryId" ON services ("CategoryId");

CREATE INDEX "IX_services_Featured" ON services ("Featured");

CREATE UNIQUE INDEX "IX_services_Slug" ON services ("Slug");

CREATE INDEX "IX_services_Status" ON services ("Status");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250822025618_InitialCreate', '9.0.7');


                -- Convert news_articles search_vector to proper tsvector type
                ALTER TABLE news_articles 
                ALTER COLUMN search_vector TYPE tsvector USING to_tsvector('english', coalesce("Title", '') || ' ' || coalesce("Content", ''));
                
                -- Convert research_articles search_vector to proper tsvector type  
                ALTER TABLE research_articles 
                ALTER COLUMN search_vector TYPE tsvector USING to_tsvector('english', coalesce("Title", '') || ' ' || coalesce("Content", ''));
                
                -- Convert unified_search search_vector to proper tsvector type
                ALTER TABLE unified_search 
                ALTER COLUMN search_vector TYPE tsvector USING to_tsvector('english', coalesce("Title", '') || ' ' || coalesce("Content", ''));
            


                CREATE INDEX IF NOT EXISTS IX_news_articles_search_vector 
                ON news_articles USING gin(search_vector);
                
                CREATE INDEX IF NOT EXISTS IX_research_articles_search_vector 
                ON research_articles USING gin(search_vector);
                
                CREATE INDEX IF NOT EXISTS IX_unified_search_search_vector 
                ON unified_search USING gin(search_vector);
            

CREATE INDEX "IX_news_articles_status_published_featured" ON news_articles ("Status", "PublishedAt", "Featured");

CREATE INDEX "IX_research_articles_status_published_featured" ON research_articles ("Status", "PublishedAt", "Featured");

CREATE INDEX "IX_events_status_start_date" ON events ("Status", "StartDate");

CREATE INDEX "IX_services_category_status_featured" ON services ("CategoryId", "Status", "Featured");

CREATE UNIQUE INDEX "IX_newsletter_subscriptions_email_unique" ON newsletter_subscriptions ("Email");

CREATE UNIQUE INDEX "IX_events_slug_unique" ON events ("Slug");

CREATE UNIQUE INDEX "IX_research_articles_slug_unique" ON research_articles ("Slug");

CREATE UNIQUE INDEX "IX_news_categories_slug_unique" ON news_categories ("Slug");


                CREATE INDEX IF NOT EXISTS IX_contacts_metadata_gin 
                ON contacts USING gin("Metadata");
                
                CREATE INDEX IF NOT EXISTS IX_events_metadata_gin 
                ON events USING gin("Metadata");
                
                CREATE INDEX IF NOT EXISTS IX_news_articles_metadata_gin 
                ON news_articles USING gin("Metadata");
                
                CREATE INDEX IF NOT EXISTS IX_research_articles_metadata_gin 
                ON research_articles USING gin("Metadata");
                
                CREATE INDEX IF NOT EXISTS IX_newsletter_subscriptions_metadata_gin 
                ON newsletter_subscriptions USING gin("Metadata");
                
                CREATE INDEX IF NOT EXISTS IX_unified_search_metadata_gin 
                ON unified_search USING gin("Metadata");
            


                -- Contact status validation
                ALTER TABLE contacts 
                ADD CONSTRAINT CK_contacts_status 
                CHECK ("Status" IN ('new', 'pending', 'in_progress', 'resolved', 'closed', 'spam'));
                
                -- Contact type validation  
                ALTER TABLE contacts
                ADD CONSTRAINT CK_contacts_type
                CHECK ("Type" IN ('general', 'support', 'sales', 'partnership', 'media', 'other'));
                
                -- Event status validation
                ALTER TABLE events
                ADD CONSTRAINT CK_events_status
                CHECK ("Status" IN ('draft', 'published', 'cancelled', 'completed', 'archived'));
                
                -- Event registration status validation
                ALTER TABLE event_registrations 
                ADD CONSTRAINT CK_event_registrations_status
                CHECK ("Status" IN ('pending', 'confirmed', 'cancelled', 'attended', 'no_show'));
                
                -- News article status validation
                ALTER TABLE news_articles
                ADD CONSTRAINT CK_news_articles_status  
                CHECK ("Status" IN ('draft', 'review', 'published', 'archived', 'deleted'));
                
                -- Research article status validation
                ALTER TABLE research_articles
                ADD CONSTRAINT CK_research_articles_status
                CHECK ("Status" IN ('draft', 'review', 'published', 'archived', 'deleted'));
                
                -- Service status validation
                ALTER TABLE services
                ADD CONSTRAINT CK_services_status
                CHECK ("Status" IN ('draft', 'active', 'inactive', 'deprecated', 'archived'));
                
                -- Newsletter subscription status validation
                ALTER TABLE newsletter_subscriptions
                ADD CONSTRAINT CK_newsletter_subscriptions_status
                CHECK ("Status" IN ('pending', 'confirmed', 'unsubscribed', 'bounced', 'complained'));
            

CREATE INDEX "IX_event_registrations_attendee_email" ON event_registrations ("AttendeeEmail");

CREATE INDEX "IX_event_registrations_status_created" ON event_registrations ("Status", "CreatedAt");

CREATE INDEX "IX_events_start_end_dates" ON events ("StartDate", "EndDate");

CREATE INDEX "IX_contacts_created_status" ON contacts ("CreatedAt", "Status");


                CREATE INDEX IF NOT EXISTS IX_news_articles_tags_gin 
                ON news_articles USING gin("Tags");
                
                CREATE INDEX IF NOT EXISTS IX_research_articles_tags_gin 
                ON research_articles USING gin("Tags");
                
                CREATE INDEX IF NOT EXISTS IX_research_articles_keywords_gin 
                ON research_articles USING gin("Keywords");
                
                CREATE INDEX IF NOT EXISTS IX_events_tags_gin 
                ON events USING gin("Tags");
                
                CREATE INDEX IF NOT EXISTS IX_services_technologies_gin 
                ON services USING gin("Technologies");
                
                CREATE INDEX IF NOT EXISTS IX_services_features_gin 
                ON services USING gin("Features");
                
                CREATE INDEX IF NOT EXISTS IX_newsletter_subscriptions_preferences_gin 
                ON newsletter_subscriptions USING gin("Preferences");
                
                CREATE INDEX IF NOT EXISTS IX_unified_search_tags_gin 
                ON unified_search USING gin("Tags");
            

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250822030914_DatabaseArchitectureOptimizations', '9.0.7');


                INSERT INTO service_categories ("Id", "Name", "Description", "Slug", "MinPriorityOrder", "MaxPriorityOrder", "DisplayOrder", "Featured1", "Featured2", "Active", "CreatedAt", "UpdatedAt") VALUES
                ('sc_001', 'Regenerative Therapies', 'Advanced regenerative medicine treatments and therapies', 'regenerative-therapies', 1, 10, 1, true, false, true, NOW(), NOW()),
                ('sc_002', 'Wellness & Prevention', 'Preventive care and wellness optimization services', 'wellness-prevention', 11, 20, 2, true, false, true, NOW(), NOW()),
                ('sc_003', 'Pain Management', 'Comprehensive pain management and treatment solutions', 'pain-management', 21, 30, 3, false, true, true, NOW(), NOW()),
                ('sc_004', 'Specialized Care', 'Specialized medical care and treatment services', 'specialized-care', 41, 50, 4, false, false, true, NOW(), NOW()),
                ('sc_005', 'Primary Care Services', 'Essential primary care and general medical services', 'primary-care-services', 51, 60, 5, false, false, true, NOW(), NOW())
                ON CONFLICT ("Id") DO NOTHING;
            


                INSERT INTO news_categories ("Id", "Name", "Description", "Slug", "DisplayOrder", "Active", "CreatedAt", "UpdatedAt") VALUES
                ('nc_001', 'Company Updates', 'Latest company announcements and organizational updates', 'company-updates', 1, true, NOW(), NOW()),
                ('nc_002', 'Community Engagement', 'Community outreach, education, and engagement initiatives', 'community-engagement', 2, true, NOW(), NOW()),
                ('nc_003', 'Industry Insights', 'Healthcare industry insights, trends, and analysis', 'industry-insights', 3, true, NOW(), NOW())
                ON CONFLICT ("Slug") DO NOTHING;
            


                INSERT INTO services ("Id", "Title", "Slug", "Description", "DetailedDescription", "Technologies", "Features", "DeliveryModes", "Icon", "Image", "Status", "priority", "CategoryId", "Available", "Featured", "Category", "MetaTitle", "MetaDescription", "PublishedAt", "CreatedAt", "UpdatedAt") VALUES
                -- Primary Care Services
                ('srv_001', 'Annual Wellness Exams', 'annual-wellness-exams', 'Comprehensive annual health assessments and preventive care planning', 'Our annual wellness exams provide comprehensive health assessments including vital signs, blood work, physical examination, and personalized health planning. We focus on preventive care to identify potential health issues early and create customized wellness plans.', '{"Primary Care", "Preventive Medicine", "Health Assessment"}', '{"Comprehensive Physical Exam", "Blood Work Analysis", "Personalized Health Plan", "Preventive Care Counseling"}', '{"mobile", "outpatient"}', 'stethoscope', '/images/services/annual-wellness-hero.png', 'active', 55, 'sc_005', true, false, 'Primary Care Services', 'Annual Wellness Exams | Comprehensive Health Assessment', 'Complete annual health exams with preventive care planning and personalized wellness strategies.', NOW(), NOW(), NOW()),
                
                ('srv_002', 'Chronic Disease Management', 'chronic-disease-management', 'Ongoing care and monitoring for chronic health conditions', 'Specialized management programs for diabetes, hypertension, heart disease, and other chronic conditions. Our approach includes regular monitoring, medication management, lifestyle counseling, and coordination with specialists to optimize health outcomes.', '{"Chronic Care", "Disease Management", "Care Coordination"}', '{"Regular Monitoring", "Medication Management", "Lifestyle Counseling", "Specialist Coordination"}', '{"mobile", "outpatient"}', 'heart-pulse', '/images/services/chronic-disease-hero.png', 'active', 56, 'sc_005', true, true, 'Primary Care Services', 'Chronic Disease Management | Ongoing Health Support', 'Comprehensive management of chronic conditions with regular monitoring and specialized care.', NOW(), NOW(), NOW()),
                
                ('srv_003', 'Preventive Health Screenings', 'preventive-health-screenings', 'Early detection screenings for various health conditions', 'Comprehensive screening programs including cancer screenings, cardiovascular assessments, bone density testing, and other preventive health measures. Early detection is key to successful treatment and optimal health outcomes.', '{"Preventive Medicine", "Early Detection", "Health Screening"}', '{"Cancer Screenings", "Cardiovascular Assessment", "Bone Density Testing", "Comprehensive Lab Work"}', '{"mobile", "outpatient"}', 'search', '/images/services/preventive-screening-hero.png', 'active', 57, 'sc_005', true, false, 'Primary Care Services', 'Preventive Health Screenings | Early Detection', 'Comprehensive preventive screenings for early detection and optimal health maintenance.', NOW(), NOW(), NOW()),
                
                -- Regenerative Medicine
                ('srv_004', 'PRP Therapy', 'prp-therapy', 'Platelet-Rich Plasma therapy for tissue regeneration and healing', 'PRP therapy uses your own blood platelets to accelerate healing and tissue regeneration. Effective for joint pain, sports injuries, hair loss, and skin rejuvenation. This minimally invasive treatment promotes natural healing processes.', '{"Regenerative Medicine", "PRP Therapy", "Tissue Regeneration"}', '{"Natural Healing", "Minimally Invasive", "Joint Pain Relief", "Sports Injury Recovery"}', '{"outpatient"}', 'droplet', '/images/services/prp-therapy-hero.png', 'active', 5, 'sc_001', true, true, 'Regenerative Therapies', 'PRP Therapy | Platelet-Rich Plasma Treatment', 'Advanced PRP therapy for natural healing, tissue regeneration, and injury recovery.', NOW(), NOW(), NOW()),
                
                ('srv_005', 'Stem Cell Therapies', 'stem-cell-therapies', 'Regenerative stem cell treatments for various conditions', 'Advanced stem cell therapies using autologous stem cells to promote tissue repair and regeneration. Effective for joint disorders, autoimmune conditions, and degenerative diseases. Our protocols follow the latest scientific research.', '{"Stem Cell Therapy", "Regenerative Medicine", "Tissue Repair"}', '{"Autologous Stem Cells", "Tissue Regeneration", "Joint Repair", "Anti-inflammatory Effects"}', '{"outpatient"}', 'dna', '/images/services/stem-cell-hero.png', 'active', 3, 'sc_001', true, true, 'Regenerative Therapies', 'Stem Cell Therapy | Advanced Regenerative Medicine', 'Cutting-edge stem cell treatments for tissue repair and regenerative healing.', NOW(), NOW(), NOW()),
                
                ('srv_006', 'Peptide Therapy', 'peptide-therapy', 'Targeted peptide treatments for health optimization', 'Therapeutic peptides for growth hormone optimization, immune system support, cognitive enhancement, and anti-aging. Personalized peptide protocols based on individual health goals and biomarker analysis.', '{"Peptide Therapy", "Growth Hormone", "Anti-Aging"}', '{"Growth Hormone Optimization", "Immune Support", "Cognitive Enhancement", "Personalized Protocols"}', '{"outpatient"}', 'molecule', '/images/services/peptide-therapy-hero.png', 'active', 7, 'sc_001', true, false, 'Regenerative Therapies', 'Peptide Therapy | Targeted Health Optimization', 'Advanced peptide treatments for growth hormone, immunity, and anti-aging benefits.', NOW(), NOW(), NOW()),
                
                -- Pain Management
                ('srv_007', 'Joint Pain Relief', 'joint-pain-relief', 'Comprehensive joint pain treatment and management', 'Multi-modal approach to joint pain including injections, physical therapy, regenerative treatments, and lifestyle modifications. Effective for arthritis, sports injuries, and degenerative joint conditions.', '{"Pain Management", "Joint Treatment", "Physical Therapy"}', '{"Joint Injections", "Physical Therapy", "Regenerative Options", "Lifestyle Counseling"}', '{"mobile", "outpatient"}', 'bone', '/images/services/joint-pain-hero.png', 'active', 25, 'sc_003', true, true, 'Pain Management', 'Joint Pain Relief | Comprehensive Treatment', 'Multi-modal joint pain treatment including injections and regenerative therapies.', NOW(), NOW(), NOW()),
                
                ('srv_008', 'Sports Medicine', 'sports-medicine', 'Specialized care for athletes and sports-related injuries', 'Comprehensive sports medicine services including injury prevention, performance optimization, rehabilitation, and return-to-play protocols. Serving athletes of all levels from weekend warriors to professionals.', '{"Sports Medicine", "Athletic Performance", "Injury Prevention"}', '{"Injury Prevention", "Performance Optimization", "Rehabilitation", "Return-to-Play Protocols"}', '{"mobile", "outpatient"}', 'activity', '/images/services/sports-medicine-hero.png', 'active', 24, 'sc_003', true, true, 'Pain Management', 'Sports Medicine | Athletic Performance & Recovery', 'Specialized sports medicine for injury prevention, treatment, and performance optimization.', NOW(), NOW(), NOW()),
                
                -- Specialized Care
                ('srv_009', 'Hormone Replacement Therapy', 'hormone-replacement-therapy', 'Comprehensive hormone optimization and replacement therapy', 'Personalized hormone replacement therapy for both men and women. Includes comprehensive testing, bioidentical hormone protocols, and ongoing monitoring to optimize hormonal balance and improve quality of life.', '{"Hormone Therapy", "Bioidentical Hormones", "Endocrinology"}', '{"Comprehensive Testing", "Bioidentical Protocols", "Ongoing Monitoring", "Quality of Life Improvement"}', '{"mobile", "outpatient"}', 'trending-up', '/images/services/hormone-therapy-hero.png', 'active', 45, 'sc_004', true, true, 'Specialized Care', 'Hormone Replacement Therapy | Hormonal Balance', 'Personalized hormone optimization therapy with bioidentical hormones and comprehensive monitoring.', NOW(), NOW(), NOW()),
                
                ('srv_010', 'Genetic Testing', 'genetic-testing', 'Comprehensive genetic analysis for personalized medicine', 'Advanced genetic testing to identify genetic predispositions, optimize treatment protocols, and guide personalized medicine approaches. Includes pharmacogenomics, disease risk assessment, and nutritional genetics.', '{"Genetic Testing", "Personalized Medicine", "Pharmacogenomics"}', '{"Disease Risk Assessment", "Treatment Optimization", "Pharmacogenomics", "Nutritional Genetics"}', '{"mobile", "outpatient"}', 'dna', '/images/services/genetic-testing-hero.png', 'active', 42, 'sc_004', true, false, 'Specialized Care', 'Genetic Testing | Personalized Medicine', 'Advanced genetic analysis for personalized treatment and disease prevention strategies.', NOW(), NOW(), NOW())
                ON CONFLICT ("Id") DO NOTHING;
            


                INSERT INTO news_articles ("Id", "Slug", "Title", "Excerpt", "Content", "AuthorName", "AuthorEmail", "PublishedAt", "Featured", "Status", "Category", "Tags", "ImageUrl", "MetaTitle", "MetaDescription", "Metadata", "search_vector", "CreatedAt", "UpdatedAt") VALUES
                ('news_001', 'advanced-cardiac-rehabilitation-program', 'Advanced Cardiac Rehabilitation Program Launches', 'Comprehensive heart health recovery program now available for post-cardiac event patients.', 'Our new Advanced Cardiac Rehabilitation Program provides comprehensive care for patients recovering from cardiac events. The program includes supervised exercise therapy, nutritional counseling, and psychological support to ensure optimal recovery and prevent future cardiac incidents.', 'Dr. Sarah Martinez', 'smartinez@internationalcenter.org', '2024-12-01T09:00:00Z', false, 'published', 'Company Updates', '{"cardiac care", "rehabilitation", "heart health"}', '/images/news/cardiac-rehabilitation-hero.png', 'Advanced Cardiac Rehabilitation Program Launches', 'Comprehensive heart health recovery program now available for post-cardiac event patients.', '{}', NULL, '2024-12-01T09:00:00Z', '2024-12-01T09:00:00Z'),
                
                ('news_002', 'breakthrough-alzheimers-research-collaboration', 'Breakthrough Alzheimers Research Collaboration', 'Partnership with leading university to advance Alzheimers treatment research.', 'We are proud to announce our collaboration with renowned researchers to advance Alzheimers disease treatment. This partnership will focus on innovative therapeutic approaches and early detection methods to improve patient outcomes.', 'Dr. Michael Chen', 'mchen@internationalcenter.org', '2024-11-28T14:30:00Z', true, 'published', 'Industry Insights', '{"alzheimers", "research", "collaboration"}', '/images/news/alzheimers-research-hero.png', 'Breakthrough Alzheimers Research Collaboration', 'Partnership with leading university to advance Alzheimers treatment research.', '{}', NULL, '2024-11-28T14:30:00Z', '2024-11-28T14:30:00Z'),
                
                ('news_003', 'community-health-screenings-expand', 'Free Community Health Screenings Expand', 'Monthly health screenings now available in additional neighborhoods.', 'Our free community health screening program has expanded to serve more neighborhoods. These monthly screenings include blood pressure checks, cholesterol testing, and diabetes screening to promote preventive healthcare in underserved communities.', 'International Center Team', 'team@internationalcenter.org', '2024-11-25T10:15:00Z', false, 'published', 'Community Engagement', '{"community health", "screenings", "prevention"}', '/images/news/community-screenings-hero.png', 'Free Community Health Screenings Expand', 'Monthly health screenings now available in additional neighborhoods.', '{}', NULL, '2024-11-25T10:15:00Z', '2024-11-25T10:15:00Z'),
                
                ('news_004', 'digital-health-records-upgrade', 'Digital Health Records System Upgrade Complete', 'Enhanced patient portal with improved security and accessibility features.', 'Our digital health records system has been upgraded with state-of-the-art security features and improved patient accessibility. The new system provides better integration with mobile devices and enhanced data protection measures.', 'Dr. Jennifer Wu', 'jwu@internationalcenter.org', '2024-11-22T16:45:00Z', false, 'published', 'Company Updates', '{"digital health", "technology", "patient portal"}', '/images/news/digital-records-hero.png', 'Digital Health Records System Upgrade Complete', 'Enhanced patient portal with improved security and accessibility features.', '{}', NULL, '2024-11-22T16:45:00Z', '2024-11-22T16:45:00Z'),
                
                ('news_005', 'exercise-medicine-program-launch', 'Exercise Medicine Program Launches', 'Physician-supervised exercise therapy for chronic disease management.', 'Our new Exercise Medicine Program offers physician-supervised exercise therapy for patients with chronic diseases. This evidence-based approach helps manage conditions like diabetes, hypertension, and arthritis through targeted physical activity.', 'Dr. Robert Thompson', 'rthompson@internationalcenter.org', '2024-11-20T11:30:00Z', true, 'published', 'Company Updates', '{"exercise medicine", "chronic disease", "therapy"}', '/images/news/exercise-medicine-hero.png', 'Exercise Medicine Program Launches', 'Physician-supervised exercise therapy for chronic disease management.', '{}', NULL, '2024-11-20T11:30:00Z', '2024-11-20T11:30:00Z')
                ON CONFLICT ("Id") DO NOTHING;
            


                INSERT INTO research_articles ("Id", "Slug", "Title", "Excerpt", "Content", "AuthorName", "AuthorEmail", "PublishedAt", "Featured", "Status", "Category", "Tags", "Keywords", "ImageUrl", "MetaTitle", "MetaDescription", "Metadata", "search_vector", "StudyType", "Collaborators", "DOI", "StudyDate", "CreatedAt", "UpdatedAt") VALUES
                ('research_001', 'hormone-optimization-clinical-outcomes', 'Hormone Replacement Therapy: Clinical Outcomes Study', 'Comprehensive analysis of hormone replacement therapy effectiveness in optimizing health outcomes across diverse patient populations.', 'This comprehensive study evaluated hormone replacement therapy protocols in 200 patients over 18 months. Results demonstrated significant improvements in energy levels, cognitive function, bone density, and overall quality of life measures. The study established evidence-based protocols for personalized hormone optimization.', 'Dr. Patricia Williams', 'pwilliams@research.org', '2024-11-12T09:30:00Z', true, 'published', 'Clinical Research', '{"hormone therapy", "endocrinology", "clinical outcomes"}', '{"hormone therapy", "endocrinology", "clinical outcomes", "quality of life"}', '/images/research/hormone-optimization-hero.png', 'Hormone Replacement Therapy: Clinical Outcomes Study', 'Comprehensive analysis of hormone replacement therapy effectiveness in optimizing health outcomes.', '{}', NULL, 'Clinical Trial', '{"Dr. Patricia Williams", "Endocrinology Research Institute"}', '10.1016/j.hrj.2024.11.010', '2024-11-12T00:00:00Z', '2024-11-12T09:30:00Z', '2024-11-12T09:30:00Z'),
                
                ('research_002', 'genetic-testing-personalized-medicine', 'Genetic Testing for Personalized Medicine: Clinical Implementation Study', 'Implementation of genetic testing protocols for personalized treatment optimization demonstrates improved clinical outcomes.', 'This implementation study evaluated the clinical utility of comprehensive genetic testing in personalizing treatment protocols for 120 patients across multiple health conditions. Genetic markers guided treatment selection, dosing optimization, and risk stratification, resulting in improved clinical outcomes and reduced adverse events.', 'Dr. Elizabeth Carter', 'ecarter@genomics.org', '2024-10-15T10:00:00Z', true, 'published', 'Technology Research', '{"genetic testing", "personalized medicine", "pharmacogenomics"}', '{"genetic testing", "personalized medicine", "pharmacogenomics", "precision healthcare"}', '/images/research/genetic-testing-hero.png', 'Genetic Testing for Personalized Medicine: Clinical Implementation Study', 'Implementation of genetic testing protocols for personalized treatment optimization.', '{}', NULL, 'Implementation Study', '{"Dr. Elizabeth Carter", "Genomic Medicine Center"}', '10.1016/j.gmt.2024.10.014', '2024-10-15T00:00:00Z', '2024-10-15T10:00:00Z', '2024-10-15T10:00:00Z'),
                
                ('research_003', 'nutritional-counseling-metabolic-syndrome', 'Personalized Nutritional Counseling for Metabolic Syndrome Management', 'Individualized nutrition therapy protocols demonstrate superior outcomes in metabolic syndrome treatment compared to standard care.', 'This randomized controlled trial compared personalized nutritional counseling protocols versus standard dietary recommendations in 180 patients with metabolic syndrome. The personalized approach, incorporating genetic factors and metabolic profiling, achieved superior outcomes in weight reduction, blood sugar control, and cardiovascular risk factors.', 'Dr. Maria Gonzalez', 'mgonzalez@nutrition.org', '2024-10-28T11:45:00Z', true, 'published', 'Clinical Research', '{"nutritional counseling", "metabolic syndrome", "personalized medicine"}', '{"nutritional counseling", "metabolic syndrome", "personalized medicine", "diabetes prevention"}', '/images/research/nutritional-counseling-hero.png', 'Personalized Nutritional Counseling for Metabolic Syndrome Management', 'Individualized nutrition therapy protocols demonstrate superior outcomes in metabolic syndrome treatment.', '{}', NULL, 'Randomized Controlled Trial', '{"Dr. Maria Gonzalez", "Clinical Nutrition Research Lab"}', '10.1016/j.cnr.2024.10.012', '2024-10-28T00:00:00Z', '2024-10-28T11:45:00Z', '2024-10-28T11:45:00Z')
                ON CONFLICT ("Id") DO NOTHING;
            

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250822031654_SeedDataFromInfrastructure', '9.0.7');

COMMIT;

