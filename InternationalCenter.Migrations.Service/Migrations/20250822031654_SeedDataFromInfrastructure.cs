using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternationalCenter.Migrations.Service.Migrations
{
    /// <inheritdoc />
    public partial class SeedDataFromInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============ SEED DATA FROM EXISTING INFRASTRUCTURE ============
            // This migration ports all existing data from the Go microservices infrastructure
            // to the new .NET Aspire unified database schema
            
            // ============ SERVICE CATEGORIES ============
            
            migrationBuilder.Sql(@"
                INSERT INTO service_categories (""Id"", ""Name"", ""Description"", ""Slug"", ""MinPriorityOrder"", ""MaxPriorityOrder"", ""DisplayOrder"", ""Featured1"", ""Featured2"", ""Active"", ""CreatedAt"", ""UpdatedAt"") VALUES
                ('sc_001', 'Regenerative Therapies', 'Advanced regenerative medicine treatments and therapies', 'regenerative-therapies', 1, 10, 1, true, false, true, NOW(), NOW()),
                ('sc_002', 'Wellness & Prevention', 'Preventive care and wellness optimization services', 'wellness-prevention', 11, 20, 2, true, false, true, NOW(), NOW()),
                ('sc_003', 'Pain Management', 'Comprehensive pain management and treatment solutions', 'pain-management', 21, 30, 3, false, true, true, NOW(), NOW()),
                ('sc_004', 'Specialized Care', 'Specialized medical care and treatment services', 'specialized-care', 41, 50, 4, false, false, true, NOW(), NOW()),
                ('sc_005', 'Primary Care Services', 'Essential primary care and general medical services', 'primary-care-services', 51, 60, 5, false, false, true, NOW(), NOW())
                ON CONFLICT (""Id"") DO NOTHING;
            ");

            // ============ NEWS CATEGORIES ============
            
            migrationBuilder.Sql(@"
                INSERT INTO news_categories (""Id"", ""Name"", ""Description"", ""Slug"", ""DisplayOrder"", ""Active"", ""CreatedAt"", ""UpdatedAt"") VALUES
                ('nc_001', 'Company Updates', 'Latest company announcements and organizational updates', 'company-updates', 1, true, NOW(), NOW()),
                ('nc_002', 'Community Engagement', 'Community outreach, education, and engagement initiatives', 'community-engagement', 2, true, NOW(), NOW()),
                ('nc_003', 'Industry Insights', 'Healthcare industry insights, trends, and analysis', 'industry-insights', 3, true, NOW(), NOW())
                ON CONFLICT (""Slug"") DO NOTHING;
            ");

            // ============ SERVICES SEED DATA ============
            
            migrationBuilder.Sql(@"
                INSERT INTO services (""Id"", ""Title"", ""Slug"", ""Description"", ""DetailedDescription"", ""Technologies"", ""Features"", ""DeliveryModes"", ""Icon"", ""Image"", ""Status"", ""priority"", ""CategoryId"", ""Available"", ""Featured"", ""Category"", ""MetaTitle"", ""MetaDescription"", ""PublishedAt"", ""CreatedAt"", ""UpdatedAt"") VALUES
                -- Primary Care Services
                ('srv_001', 'Annual Wellness Exams', 'annual-wellness-exams', 'Comprehensive annual health assessments and preventive care planning', 'Our annual wellness exams provide comprehensive health assessments including vital signs, blood work, physical examination, and personalized health planning. We focus on preventive care to identify potential health issues early and create customized wellness plans.', '{""Primary Care"", ""Preventive Medicine"", ""Health Assessment""}', '{""Comprehensive Physical Exam"", ""Blood Work Analysis"", ""Personalized Health Plan"", ""Preventive Care Counseling""}', '{""mobile"", ""outpatient""}', 'stethoscope', '/images/services/annual-wellness-hero.png', 'active', 55, 'sc_005', true, false, 'Primary Care Services', 'Annual Wellness Exams | Comprehensive Health Assessment', 'Complete annual health exams with preventive care planning and personalized wellness strategies.', NOW(), NOW(), NOW()),
                
                ('srv_002', 'Chronic Disease Management', 'chronic-disease-management', 'Ongoing care and monitoring for chronic health conditions', 'Specialized management programs for diabetes, hypertension, heart disease, and other chronic conditions. Our approach includes regular monitoring, medication management, lifestyle counseling, and coordination with specialists to optimize health outcomes.', '{""Chronic Care"", ""Disease Management"", ""Care Coordination""}', '{""Regular Monitoring"", ""Medication Management"", ""Lifestyle Counseling"", ""Specialist Coordination""}', '{""mobile"", ""outpatient""}', 'heart-pulse', '/images/services/chronic-disease-hero.png', 'active', 56, 'sc_005', true, true, 'Primary Care Services', 'Chronic Disease Management | Ongoing Health Support', 'Comprehensive management of chronic conditions with regular monitoring and specialized care.', NOW(), NOW(), NOW()),
                
                ('srv_003', 'Preventive Health Screenings', 'preventive-health-screenings', 'Early detection screenings for various health conditions', 'Comprehensive screening programs including cancer screenings, cardiovascular assessments, bone density testing, and other preventive health measures. Early detection is key to successful treatment and optimal health outcomes.', '{""Preventive Medicine"", ""Early Detection"", ""Health Screening""}', '{""Cancer Screenings"", ""Cardiovascular Assessment"", ""Bone Density Testing"", ""Comprehensive Lab Work""}', '{""mobile"", ""outpatient""}', 'search', '/images/services/preventive-screening-hero.png', 'active', 57, 'sc_005', true, false, 'Primary Care Services', 'Preventive Health Screenings | Early Detection', 'Comprehensive preventive screenings for early detection and optimal health maintenance.', NOW(), NOW(), NOW()),
                
                -- Regenerative Medicine
                ('srv_004', 'PRP Therapy', 'prp-therapy', 'Platelet-Rich Plasma therapy for tissue regeneration and healing', 'PRP therapy uses your own blood platelets to accelerate healing and tissue regeneration. Effective for joint pain, sports injuries, hair loss, and skin rejuvenation. This minimally invasive treatment promotes natural healing processes.', '{""Regenerative Medicine"", ""PRP Therapy"", ""Tissue Regeneration""}', '{""Natural Healing"", ""Minimally Invasive"", ""Joint Pain Relief"", ""Sports Injury Recovery""}', '{""outpatient""}', 'droplet', '/images/services/prp-therapy-hero.png', 'active', 5, 'sc_001', true, true, 'Regenerative Therapies', 'PRP Therapy | Platelet-Rich Plasma Treatment', 'Advanced PRP therapy for natural healing, tissue regeneration, and injury recovery.', NOW(), NOW(), NOW()),
                
                ('srv_005', 'Stem Cell Therapies', 'stem-cell-therapies', 'Regenerative stem cell treatments for various conditions', 'Advanced stem cell therapies using autologous stem cells to promote tissue repair and regeneration. Effective for joint disorders, autoimmune conditions, and degenerative diseases. Our protocols follow the latest scientific research.', '{""Stem Cell Therapy"", ""Regenerative Medicine"", ""Tissue Repair""}', '{""Autologous Stem Cells"", ""Tissue Regeneration"", ""Joint Repair"", ""Anti-inflammatory Effects""}', '{""outpatient""}', 'dna', '/images/services/stem-cell-hero.png', 'active', 3, 'sc_001', true, true, 'Regenerative Therapies', 'Stem Cell Therapy | Advanced Regenerative Medicine', 'Cutting-edge stem cell treatments for tissue repair and regenerative healing.', NOW(), NOW(), NOW()),
                
                ('srv_006', 'Peptide Therapy', 'peptide-therapy', 'Targeted peptide treatments for health optimization', 'Therapeutic peptides for growth hormone optimization, immune system support, cognitive enhancement, and anti-aging. Personalized peptide protocols based on individual health goals and biomarker analysis.', '{""Peptide Therapy"", ""Growth Hormone"", ""Anti-Aging""}', '{""Growth Hormone Optimization"", ""Immune Support"", ""Cognitive Enhancement"", ""Personalized Protocols""}', '{""outpatient""}', 'molecule', '/images/services/peptide-therapy-hero.png', 'active', 7, 'sc_001', true, false, 'Regenerative Therapies', 'Peptide Therapy | Targeted Health Optimization', 'Advanced peptide treatments for growth hormone, immunity, and anti-aging benefits.', NOW(), NOW(), NOW()),
                
                -- Pain Management
                ('srv_007', 'Joint Pain Relief', 'joint-pain-relief', 'Comprehensive joint pain treatment and management', 'Multi-modal approach to joint pain including injections, physical therapy, regenerative treatments, and lifestyle modifications. Effective for arthritis, sports injuries, and degenerative joint conditions.', '{""Pain Management"", ""Joint Treatment"", ""Physical Therapy""}', '{""Joint Injections"", ""Physical Therapy"", ""Regenerative Options"", ""Lifestyle Counseling""}', '{""mobile"", ""outpatient""}', 'bone', '/images/services/joint-pain-hero.png', 'active', 25, 'sc_003', true, true, 'Pain Management', 'Joint Pain Relief | Comprehensive Treatment', 'Multi-modal joint pain treatment including injections and regenerative therapies.', NOW(), NOW(), NOW()),
                
                ('srv_008', 'Sports Medicine', 'sports-medicine', 'Specialized care for athletes and sports-related injuries', 'Comprehensive sports medicine services including injury prevention, performance optimization, rehabilitation, and return-to-play protocols. Serving athletes of all levels from weekend warriors to professionals.', '{""Sports Medicine"", ""Athletic Performance"", ""Injury Prevention""}', '{""Injury Prevention"", ""Performance Optimization"", ""Rehabilitation"", ""Return-to-Play Protocols""}', '{""mobile"", ""outpatient""}', 'activity', '/images/services/sports-medicine-hero.png', 'active', 24, 'sc_003', true, true, 'Pain Management', 'Sports Medicine | Athletic Performance & Recovery', 'Specialized sports medicine for injury prevention, treatment, and performance optimization.', NOW(), NOW(), NOW()),
                
                -- Specialized Care
                ('srv_009', 'Hormone Replacement Therapy', 'hormone-replacement-therapy', 'Comprehensive hormone optimization and replacement therapy', 'Personalized hormone replacement therapy for both men and women. Includes comprehensive testing, bioidentical hormone protocols, and ongoing monitoring to optimize hormonal balance and improve quality of life.', '{""Hormone Therapy"", ""Bioidentical Hormones"", ""Endocrinology""}', '{""Comprehensive Testing"", ""Bioidentical Protocols"", ""Ongoing Monitoring"", ""Quality of Life Improvement""}', '{""mobile"", ""outpatient""}', 'trending-up', '/images/services/hormone-therapy-hero.png', 'active', 45, 'sc_004', true, true, 'Specialized Care', 'Hormone Replacement Therapy | Hormonal Balance', 'Personalized hormone optimization therapy with bioidentical hormones and comprehensive monitoring.', NOW(), NOW(), NOW()),
                
                ('srv_010', 'Genetic Testing', 'genetic-testing', 'Comprehensive genetic analysis for personalized medicine', 'Advanced genetic testing to identify genetic predispositions, optimize treatment protocols, and guide personalized medicine approaches. Includes pharmacogenomics, disease risk assessment, and nutritional genetics.', '{""Genetic Testing"", ""Personalized Medicine"", ""Pharmacogenomics""}', '{""Disease Risk Assessment"", ""Treatment Optimization"", ""Pharmacogenomics"", ""Nutritional Genetics""}', '{""mobile"", ""outpatient""}', 'dna', '/images/services/genetic-testing-hero.png', 'active', 42, 'sc_004', true, false, 'Specialized Care', 'Genetic Testing | Personalized Medicine', 'Advanced genetic analysis for personalized treatment and disease prevention strategies.', NOW(), NOW(), NOW())
                ON CONFLICT (""Id"") DO NOTHING;
            ");

            // ============ NEWS ARTICLES SEED DATA ============
            
            migrationBuilder.Sql(@"
                INSERT INTO news_articles (""Id"", ""Slug"", ""Title"", ""Excerpt"", ""Content"", ""AuthorName"", ""AuthorEmail"", ""PublishedAt"", ""Featured"", ""Status"", ""Category"", ""Tags"", ""ImageUrl"", ""MetaTitle"", ""MetaDescription"", ""Metadata"", ""search_vector"", ""CreatedAt"", ""UpdatedAt"") VALUES
                ('news_001', 'advanced-cardiac-rehabilitation-program', 'Advanced Cardiac Rehabilitation Program Launches', 'Comprehensive heart health recovery program now available for post-cardiac event patients.', 'Our new Advanced Cardiac Rehabilitation Program provides comprehensive care for patients recovering from cardiac events. The program includes supervised exercise therapy, nutritional counseling, and psychological support to ensure optimal recovery and prevent future cardiac incidents.', 'Dr. Sarah Martinez', 'smartinez@internationalcenter.org', '2024-12-01T09:00:00Z', false, 'published', 'Company Updates', '{""cardiac care"", ""rehabilitation"", ""heart health""}', '/images/news/cardiac-rehabilitation-hero.png', 'Advanced Cardiac Rehabilitation Program Launches', 'Comprehensive heart health recovery program now available for post-cardiac event patients.', '{}', NULL, '2024-12-01T09:00:00Z', '2024-12-01T09:00:00Z'),
                
                ('news_002', 'breakthrough-alzheimers-research-collaboration', 'Breakthrough Alzheimers Research Collaboration', 'Partnership with leading university to advance Alzheimers treatment research.', 'We are proud to announce our collaboration with renowned researchers to advance Alzheimers disease treatment. This partnership will focus on innovative therapeutic approaches and early detection methods to improve patient outcomes.', 'Dr. Michael Chen', 'mchen@internationalcenter.org', '2024-11-28T14:30:00Z', true, 'published', 'Industry Insights', '{""alzheimers"", ""research"", ""collaboration""}', '/images/news/alzheimers-research-hero.png', 'Breakthrough Alzheimers Research Collaboration', 'Partnership with leading university to advance Alzheimers treatment research.', '{}', NULL, '2024-11-28T14:30:00Z', '2024-11-28T14:30:00Z'),
                
                ('news_003', 'community-health-screenings-expand', 'Free Community Health Screenings Expand', 'Monthly health screenings now available in additional neighborhoods.', 'Our free community health screening program has expanded to serve more neighborhoods. These monthly screenings include blood pressure checks, cholesterol testing, and diabetes screening to promote preventive healthcare in underserved communities.', 'International Center Team', 'team@internationalcenter.org', '2024-11-25T10:15:00Z', false, 'published', 'Community Engagement', '{""community health"", ""screenings"", ""prevention""}', '/images/news/community-screenings-hero.png', 'Free Community Health Screenings Expand', 'Monthly health screenings now available in additional neighborhoods.', '{}', NULL, '2024-11-25T10:15:00Z', '2024-11-25T10:15:00Z'),
                
                ('news_004', 'digital-health-records-upgrade', 'Digital Health Records System Upgrade Complete', 'Enhanced patient portal with improved security and accessibility features.', 'Our digital health records system has been upgraded with state-of-the-art security features and improved patient accessibility. The new system provides better integration with mobile devices and enhanced data protection measures.', 'Dr. Jennifer Wu', 'jwu@internationalcenter.org', '2024-11-22T16:45:00Z', false, 'published', 'Company Updates', '{""digital health"", ""technology"", ""patient portal""}', '/images/news/digital-records-hero.png', 'Digital Health Records System Upgrade Complete', 'Enhanced patient portal with improved security and accessibility features.', '{}', NULL, '2024-11-22T16:45:00Z', '2024-11-22T16:45:00Z'),
                
                ('news_005', 'exercise-medicine-program-launch', 'Exercise Medicine Program Launches', 'Physician-supervised exercise therapy for chronic disease management.', 'Our new Exercise Medicine Program offers physician-supervised exercise therapy for patients with chronic diseases. This evidence-based approach helps manage conditions like diabetes, hypertension, and arthritis through targeted physical activity.', 'Dr. Robert Thompson', 'rthompson@internationalcenter.org', '2024-11-20T11:30:00Z', true, 'published', 'Company Updates', '{""exercise medicine"", ""chronic disease"", ""therapy""}', '/images/news/exercise-medicine-hero.png', 'Exercise Medicine Program Launches', 'Physician-supervised exercise therapy for chronic disease management.', '{}', NULL, '2024-11-20T11:30:00Z', '2024-11-20T11:30:00Z')
                ON CONFLICT (""Id"") DO NOTHING;
            ");

            // ============ RESEARCH ARTICLES SEED DATA ============
            
            migrationBuilder.Sql(@"
                INSERT INTO research_articles (""Id"", ""Slug"", ""Title"", ""Excerpt"", ""Content"", ""AuthorName"", ""AuthorEmail"", ""PublishedAt"", ""Featured"", ""Status"", ""Category"", ""Tags"", ""Keywords"", ""ImageUrl"", ""MetaTitle"", ""MetaDescription"", ""Metadata"", ""search_vector"", ""StudyType"", ""Collaborators"", ""DOI"", ""StudyDate"", ""CreatedAt"", ""UpdatedAt"") VALUES
                ('research_001', 'hormone-optimization-clinical-outcomes', 'Hormone Replacement Therapy: Clinical Outcomes Study', 'Comprehensive analysis of hormone replacement therapy effectiveness in optimizing health outcomes across diverse patient populations.', 'This comprehensive study evaluated hormone replacement therapy protocols in 200 patients over 18 months. Results demonstrated significant improvements in energy levels, cognitive function, bone density, and overall quality of life measures. The study established evidence-based protocols for personalized hormone optimization.', 'Dr. Patricia Williams', 'pwilliams@research.org', '2024-11-12T09:30:00Z', true, 'published', 'Clinical Research', '{""hormone therapy"", ""endocrinology"", ""clinical outcomes""}', '{""hormone therapy"", ""endocrinology"", ""clinical outcomes"", ""quality of life""}', '/images/research/hormone-optimization-hero.png', 'Hormone Replacement Therapy: Clinical Outcomes Study', 'Comprehensive analysis of hormone replacement therapy effectiveness in optimizing health outcomes.', '{}', NULL, 'Clinical Trial', '{""Dr. Patricia Williams"", ""Endocrinology Research Institute""}', '10.1016/j.hrj.2024.11.010', '2024-11-12T00:00:00Z', '2024-11-12T09:30:00Z', '2024-11-12T09:30:00Z'),
                
                ('research_002', 'genetic-testing-personalized-medicine', 'Genetic Testing for Personalized Medicine: Clinical Implementation Study', 'Implementation of genetic testing protocols for personalized treatment optimization demonstrates improved clinical outcomes.', 'This implementation study evaluated the clinical utility of comprehensive genetic testing in personalizing treatment protocols for 120 patients across multiple health conditions. Genetic markers guided treatment selection, dosing optimization, and risk stratification, resulting in improved clinical outcomes and reduced adverse events.', 'Dr. Elizabeth Carter', 'ecarter@genomics.org', '2024-10-15T10:00:00Z', true, 'published', 'Technology Research', '{""genetic testing"", ""personalized medicine"", ""pharmacogenomics""}', '{""genetic testing"", ""personalized medicine"", ""pharmacogenomics"", ""precision healthcare""}', '/images/research/genetic-testing-hero.png', 'Genetic Testing for Personalized Medicine: Clinical Implementation Study', 'Implementation of genetic testing protocols for personalized treatment optimization.', '{}', NULL, 'Implementation Study', '{""Dr. Elizabeth Carter"", ""Genomic Medicine Center""}', '10.1016/j.gmt.2024.10.014', '2024-10-15T00:00:00Z', '2024-10-15T10:00:00Z', '2024-10-15T10:00:00Z'),
                
                ('research_003', 'nutritional-counseling-metabolic-syndrome', 'Personalized Nutritional Counseling for Metabolic Syndrome Management', 'Individualized nutrition therapy protocols demonstrate superior outcomes in metabolic syndrome treatment compared to standard care.', 'This randomized controlled trial compared personalized nutritional counseling protocols versus standard dietary recommendations in 180 patients with metabolic syndrome. The personalized approach, incorporating genetic factors and metabolic profiling, achieved superior outcomes in weight reduction, blood sugar control, and cardiovascular risk factors.', 'Dr. Maria Gonzalez', 'mgonzalez@nutrition.org', '2024-10-28T11:45:00Z', true, 'published', 'Clinical Research', '{""nutritional counseling"", ""metabolic syndrome"", ""personalized medicine""}', '{""nutritional counseling"", ""metabolic syndrome"", ""personalized medicine"", ""diabetes prevention""}', '/images/research/nutritional-counseling-hero.png', 'Personalized Nutritional Counseling for Metabolic Syndrome Management', 'Individualized nutrition therapy protocols demonstrate superior outcomes in metabolic syndrome treatment.', '{}', NULL, 'Randomized Controlled Trial', '{""Dr. Maria Gonzalez"", ""Clinical Nutrition Research Lab""}', '10.1016/j.cnr.2024.10.012', '2024-10-28T00:00:00Z', '2024-10-28T11:45:00Z', '2024-10-28T11:45:00Z')
                ON CONFLICT (""Id"") DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ============ ROLLBACK SEED DATA FROM INFRASTRUCTURE ============
            // Remove all seed data added in this migration in reverse order
            
            // Remove research articles
            migrationBuilder.Sql(@"
                DELETE FROM research_articles WHERE ""Id"" IN (
                    'research_001', 'research_002', 'research_003'
                );
            ");
            
            // Remove news articles
            migrationBuilder.Sql(@"
                DELETE FROM news_articles WHERE ""Id"" IN (
                    'news_001', 'news_002', 'news_003', 'news_004', 'news_005'
                );
            ");
            
            // Remove services
            migrationBuilder.Sql(@"
                DELETE FROM services WHERE ""Id"" IN (
                    'srv_001', 'srv_002', 'srv_003', 'srv_004', 'srv_005', 
                    'srv_006', 'srv_007', 'srv_008', 'srv_009', 'srv_010'
                );
            ");
            
            // Remove news categories
            migrationBuilder.Sql(@"
                DELETE FROM news_categories WHERE ""Id"" IN (
                    'nc_001', 'nc_002', 'nc_003'
                );
            ");
            
            // Remove service categories
            migrationBuilder.Sql(@"
                DELETE FROM service_categories WHERE ""Id"" IN (
                    'sc_001', 'sc_002', 'sc_003', 'sc_004', 'sc_005'
                );
            ");
        }
    }
}
