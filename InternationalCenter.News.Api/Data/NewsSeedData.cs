using InternationalCenter.Shared.Models;
using InternationalCenter.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InternationalCenter.News.Api.Data;

public static class NewsSeedData
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Check if data already exists
        if (await context.NewsArticles.AnyAsync())
        {
            return;
        }

        // Seed news categories first
        var categories = new List<NewsCategory>
        {
            new NewsCategory
            {
                Id = "news-cat-company-updates",
                Name = "Company Updates",
                Description = "Latest news and announcements from International Center",
                Slug = "company-updates",
                DisplayOrder = 1,
                Active = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new NewsCategory
            {
                Id = "news-cat-community-engagement",
                Name = "Community Engagement",
                Description = "Community outreach, education, and engagement initiatives",
                Slug = "community-engagement",
                DisplayOrder = 2,
                Active = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new NewsCategory
            {
                Id = "news-cat-industry-insights",
                Name = "Industry Insights",
                Description = "Healthcare industry trends, insights, and thought leadership",
                Slug = "industry-insights",
                DisplayOrder = 3,
                Active = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await context.NewsCategories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        // Seed news articles
        var articles = new List<NewsArticle>
        {
            // Company Updates
            new NewsArticle
            {
                Id = "news-11111111-1111-1111-1111-111111111111",
                Title = "International Center Expands Regenerative Medicine Services",
                Slug = "international-center-expands-regenerative-medicine",
                Excerpt = "We're proud to announce the expansion of our regenerative medicine capabilities with new PRP and stem cell therapies.",
                Content = "At International Center, we continue to lead in innovative healthcare solutions. Our expanded regenerative medicine program now includes advanced PRP therapy, cutting-edge exosome treatments, and comprehensive stem cell therapies. These additions represent our commitment to providing the most advanced healing options available.\n\nOur new regenerative medicine center features state-of-the-art laboratory facilities, sterile processing areas, and specialized treatment rooms designed specifically for regenerative procedures. The expansion allows us to offer a comprehensive range of treatments including autologous stem cell therapy, platelet-rich plasma (PRP) injections, exosome therapy, and advanced tissue engineering solutions.\n\nDr. Sarah Johnson, our Director of Regenerative Medicine, brings over 15 years of experience in the field and has been instrumental in developing our protocols. 'This expansion represents a significant milestone in our mission to provide cutting-edge healthcare solutions,' says Dr. Johnson. 'We're now able to offer treatments that were previously only available at major research institutions.'\n\nThe regenerative medicine program targets a wide range of conditions including orthopedic injuries, joint degeneration, autoimmune disorders, and age-related conditions. Each treatment is personalized based on comprehensive diagnostic evaluation and patient-specific needs.",
                AuthorName = "Dr. Sarah Johnson",
                AuthorEmail = "s.johnson@internationalcenter.com",
                Tags = new[] { "regenerative medicine", "expansion", "healthcare innovation", "stem cell therapy", "PRP" },
                Status = "published",
                Featured = true,
                Category = "Company Updates",
                ImageUrl = "http://localhost:8099/assets/images/news/regenerative-medicine-expansion-hero.png",
                MetaTitle = "International Center Expands Regenerative Medicine Services",
                MetaDescription = "International Center announces expansion of regenerative medicine services including PRP and stem cell therapies.",
                PublishedAt = DateTime.UtcNow.AddDays(-30),
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new NewsArticle
            {
                Id = "news-22222222-2222-2222-2222-222222222222",
                Title = "New Mobile Health Services Now Available",
                Slug = "new-mobile-health-services-available",
                Excerpt = "Bringing advanced healthcare directly to your home with our new mobile service platform.",
                Content = "We're excited to introduce our mobile healthcare services, designed to bring advanced medical treatments directly to your home or office. This innovative approach ensures convenient access to our full range of services including IV nutritional therapy, wellness screenings, and select regenerative treatments.\n\nOur mobile health program represents a paradigm shift in healthcare delivery, prioritizing patient convenience without compromising quality of care. The specially equipped mobile units feature advanced medical equipment, sterile environments, and comprehensive safety protocols to ensure the highest standards of care in any location.\n\nServices available through our mobile platform include:\n- IV nutritional therapy and vitamin infusions\n- Comprehensive health screenings and diagnostics\n- Hormone optimization consultations\n- Select regenerative medicine treatments\n- Preventive care assessments\n- Follow-up consultations and monitoring\n\nEach mobile unit is staffed by licensed healthcare professionals and equipped with state-of-the-art medical technology. We maintain the same rigorous safety and quality standards whether treatment is provided in our facility or at your location.",
                AuthorName = "International Center Team",
                AuthorEmail = "info@internationalcenter.com",
                Tags = new[] { "mobile health", "convenience", "innovation", "home care", "IV therapy" },
                Status = "published",
                Featured = false,
                Category = "Company Updates",
                ImageUrl = "http://localhost:8099/assets/images/news/mobile-health-services-hero.png",
                MetaTitle = "New Mobile Health Services - International Center",
                MetaDescription = "International Center introduces mobile health services for convenient at-home medical treatments.",
                PublishedAt = DateTime.UtcNow.AddDays(-25),
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new NewsArticle
            {
                Id = "news-55555555-5555-5555-5555-555555555555",
                Title = "International Center Achieves Joint Commission Accreditation",
                Slug = "joint-commission-accreditation-achievement",
                Excerpt = "Our commitment to excellence recognized with prestigious Joint Commission accreditation for quality and safety.",
                Content = "International Center has achieved Joint Commission accreditation, recognizing our unwavering commitment to patient safety and quality care. This prestigious accreditation validates our comprehensive quality management systems, rigorous safety protocols, and evidence-based treatment approaches across all service lines.\n\nThe Joint Commission accreditation process involves a comprehensive evaluation of our facilities, procedures, staff qualifications, and patient outcomes. This rigorous assessment ensures that we meet the highest national standards for healthcare quality and safety.\n\nKey areas of recognition include:\n- Patient safety protocols and risk management\n- Quality assurance and continuous improvement programs\n- Staff credentialing and competency verification\n- Infection control and prevention measures\n- Medical record management and documentation\n- Patient rights and communication standards\n\nThis accreditation demonstrates our commitment to maintaining the highest standards of care and continuous improvement in all aspects of our operations. It provides our patients with confidence that they are receiving care that meets or exceeds national quality benchmarks.",
                AuthorName = "Dr. David Martinez",
                AuthorEmail = "d.martinez@internationalcenter.com",
                Tags = new[] { "accreditation", "quality", "safety", "joint commission", "healthcare standards" },
                Status = "published",
                Featured = true,
                Category = "Company Updates",
                ImageUrl = "http://localhost:8099/assets/images/news/joint-commission-accreditation-hero.png",
                MetaTitle = "Joint Commission Accreditation - International Center",
                MetaDescription = "International Center achieves Joint Commission accreditation for quality and safety excellence.",
                PublishedAt = DateTime.UtcNow.AddDays(-15),
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new NewsArticle
            {
                Id = "news-66666666-6666-6666-6666-666666666666",
                Title = "New Advanced Diagnostics Center Opens",
                Slug = "advanced-diagnostics-center-opens",
                Excerpt = "State-of-the-art diagnostic facility featuring AI-enhanced imaging and precision medicine capabilities.",
                Content = "Our new Advanced Diagnostics Center represents a significant milestone in precision medicine. Featuring cutting-edge AI-enhanced imaging technology, comprehensive laboratory capabilities, and advanced biomarker analysis, this facility enables unprecedented diagnostic accuracy and personalized treatment planning for our patients.\n\nThe diagnostic center houses the latest in medical imaging technology including high-field MRI systems, advanced CT scanners, and specialized ultrasound equipment. These systems are enhanced with artificial intelligence algorithms that improve image quality, reduce scan times, and assist in diagnostic interpretation.\n\nKey features of the new center include:\n- AI-enhanced imaging with automated analysis\n- Comprehensive laboratory services with rapid turnaround\n- Advanced biomarker testing and genomic analysis\n- Specialized diagnostic procedures and consultations\n- Integrated electronic health records and reporting\n- Comfortable patient areas with amenities\n\nThe center represents our commitment to staying at the forefront of medical technology and providing our patients with the most advanced diagnostic capabilities available. This investment in cutting-edge technology enables more precise diagnoses, earlier detection of conditions, and more effective treatment planning.",
                AuthorName = "International Center Team",
                AuthorEmail = "info@internationalcenter.com",
                Tags = new[] { "diagnostics", "AI imaging", "precision medicine", "facility expansion", "technology" },
                Status = "published",
                Featured = false,
                Category = "Company Updates",
                ImageUrl = "http://localhost:8099/assets/images/news/advanced-diagnostics-center-hero.png",
                MetaTitle = "Advanced Diagnostics Center Opens - International Center",
                MetaDescription = "New state-of-the-art diagnostics center with AI-enhanced imaging and precision medicine.",
                PublishedAt = DateTime.UtcNow.AddDays(-10),
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            },

            // Community Engagement
            new NewsArticle
            {
                Id = "news-33333333-3333-3333-3333-333333333333",
                Title = "Community Wellness Workshop Series Launches",
                Slug = "community-wellness-workshop-series-launches",
                Excerpt = "Join us for educational workshops covering nutrition, fitness, and preventive health strategies.",
                Content = "International Center is committed to community health education. Our new workshop series covers essential topics including nutritional optimization, fitness strategies for different life stages, and evidence-based preventive health approaches. These workshops are designed to empower individuals with knowledge to make informed health decisions.\n\nThe workshop series features monthly sessions led by our healthcare professionals and guest experts. Each workshop combines educational presentations with interactive activities, practical demonstrations, and personalized guidance.\n\nUpcoming workshop topics include:\n- Nutritional Foundations for Optimal Health\n- Exercise and Movement for Longevity\n- Stress Management and Mental Wellness\n- Sleep Optimization Strategies\n- Preventive Health Screening Guidelines\n- Understanding Hormonal Health\n- Inflammation and Anti-Aging Approaches\n\nAll workshops are free to attend and open to community members. Registration is required due to limited seating, and participants receive educational materials, healthy refreshments, and access to follow-up resources.\n\nOur goal is to make evidence-based health information accessible to everyone in our community, regardless of their current relationship with our practice. We believe that education is the foundation of good health and disease prevention.",
                AuthorName = "Dr. Michael Chen",
                AuthorEmail = "m.chen@internationalcenter.com",
                Tags = new[] { "community", "education", "wellness", "workshops", "prevention" },
                Status = "published",
                Featured = true,
                Category = "Community Engagement",
                ImageUrl = "http://localhost:8099/assets/images/news/community-wellness-workshops-hero.png",
                MetaTitle = "Community Wellness Workshop Series - International Center",
                MetaDescription = "Join International Center's educational wellness workshops covering nutrition, fitness, and preventive health.",
                PublishedAt = DateTime.UtcNow.AddDays(-22),
                CreatedAt = DateTime.UtcNow.AddDays(-22),
                UpdatedAt = DateTime.UtcNow.AddDays(-22)
            },
            new NewsArticle
            {
                Id = "news-77777777-7777-7777-7777-777777777777",
                Title = "Free Health Screenings for Local Community",
                Slug = "free-community-health-screenings",
                Excerpt = "Providing free comprehensive health screenings to underserved community members throughout February.",
                Content = "International Center is proud to announce our community outreach initiative providing free comprehensive health screenings throughout February. These screenings include cardiovascular assessments, metabolic panels, and wellness consultations, designed to improve health outcomes in underserved populations and demonstrate our commitment to community wellness.\n\nThe free screening program offers:\n- Basic metabolic panel and lipid screening\n- Blood pressure and cardiovascular assessment\n- Body composition analysis\n- Nutritional consultation\n- Health risk assessment\n- Wellness education and resources\n- Referrals for follow-up care when needed\n\nScreenings are conducted by our licensed healthcare professionals and include personalized consultations to discuss results and provide health recommendations. Participants receive detailed reports of their screening results along with educational materials about maintaining optimal health.\n\nThis initiative is part of our ongoing commitment to community health and addressing healthcare disparities. We believe that everyone deserves access to quality healthcare and health education, regardless of their ability to pay.\n\nTo schedule a free screening appointment, community members can call our dedicated screening hotline or visit our website. Walk-in appointments are also available during designated community screening days.",
                AuthorName = "Community Outreach Team",
                AuthorEmail = "outreach@internationalcenter.com",
                Tags = new[] { "community health", "free screenings", "outreach", "wellness", "prevention" },
                Status = "published",
                Featured = true,
                Category = "Community Engagement",
                ImageUrl = "http://localhost:8099/assets/images/news/free-community-screenings-hero.png",
                MetaTitle = "Free Community Health Screenings - International Center",
                MetaDescription = "Free comprehensive health screenings for underserved community members throughout February.",
                PublishedAt = DateTime.UtcNow.AddDays(-18),
                CreatedAt = DateTime.UtcNow.AddDays(-18),
                UpdatedAt = DateTime.UtcNow.AddDays(-18)
            },
            new NewsArticle
            {
                Id = "news-88888888-8888-8888-8888-888888888888",
                Title = "Partnership with Local Universities for Health Education",
                Slug = "university-partnership-health-education",
                Excerpt = "Collaborating with leading universities to advance health education and research initiatives.",
                Content = "International Center has established partnerships with three leading universities to advance health education and research. These collaborations include student internship programs, joint research initiatives, and community health education seminars, fostering the next generation of healthcare professionals while advancing medical knowledge.\n\nOur university partnerships focus on several key areas:\n- Student internship and clinical rotation programs\n- Collaborative research projects in regenerative medicine\n- Community health education seminars\n- Professional development workshops for healthcare students\n- Mentorship programs connecting students with experienced practitioners\n- Joint publication opportunities in peer-reviewed journals\n\nThese partnerships provide valuable learning opportunities for students while bringing fresh perspectives and current research to our practice. Students gain hands-on experience in cutting-edge healthcare delivery, while our team benefits from exposure to the latest academic research and methodologies.\n\nThe collaboration also extends to community education, with university students and faculty participating in our wellness workshops and health screening programs. This partnership model creates a comprehensive ecosystem of learning, research, and community service.\n\nWe are committed to supporting the next generation of healthcare professionals and advancing the field through collaborative research and education initiatives.",
                AuthorName = "Dr. Angela Thompson",
                AuthorEmail = "a.thompson@internationalcenter.com",
                Tags = new[] { "education", "partnership", "research", "community", "students" },
                Status = "published",
                Featured = false,
                Category = "Community Engagement",
                ImageUrl = "http://localhost:8099/assets/images/news/university-partnership-hero.png",
                MetaTitle = "University Partnership for Health Education - International Center",
                MetaDescription = "International Center partners with universities for health education and research advancement.",
                PublishedAt = DateTime.UtcNow.AddDays(-12),
                CreatedAt = DateTime.UtcNow.AddDays(-12),
                UpdatedAt = DateTime.UtcNow.AddDays(-12)
            },

            // Industry Insights
            new NewsArticle
            {
                Id = "news-44444444-4444-4444-4444-444444444444",
                Title = "The Future of Personalized Medicine: 2024 Trends",
                Slug = "future-personalized-medicine-2024-trends",
                Excerpt = "Exploring how personalized medicine is revolutionizing healthcare delivery and patient outcomes.",
                Content = "Personalized medicine represents a paradigm shift in healthcare, moving from one-size-fits-all approaches to tailored treatments based on individual genetic, environmental, and lifestyle factors. At International Center, we're at the forefront of this revolution, incorporating advanced diagnostics and personalized treatment protocols to optimize patient outcomes.\n\nKey trends shaping personalized medicine in 2024:\n\n1. Genomic Medicine Integration: Advanced genetic testing is becoming more accessible and affordable, enabling precision treatment selection based on individual genetic profiles.\n\n2. Biomarker-Driven Therapies: Sophisticated biomarker analysis allows for more precise treatment monitoring and adjustment.\n\n3. AI-Enhanced Diagnostics: Artificial intelligence is improving diagnostic accuracy and enabling predictive health modeling.\n\n4. Microbiome Medicine: Understanding the role of the microbiome in health and disease is opening new therapeutic avenues.\n\n5. Regenerative Medicine Advances: Personalized regenerative therapies using patient-specific cells and tissues are becoming more sophisticated.\n\nAt International Center, we integrate these advances into comprehensive treatment protocols that address each patient's unique needs. Our approach combines advanced diagnostics, personalized treatment planning, and continuous monitoring to achieve optimal outcomes.\n\nThe future of healthcare lies in this personalized approach, and we're committed to leading this transformation while maintaining the highest standards of care and evidence-based practice.",
                AuthorName = "Dr. Emily Rodriguez",
                AuthorEmail = "e.rodriguez@internationalcenter.com",
                Tags = new[] { "personalized medicine", "healthcare trends", "innovation", "2024", "precision medicine" },
                Status = "published",
                Featured = true,
                Category = "Industry Insights",
                ImageUrl = "http://localhost:8099/assets/images/news/personalized-medicine-2024-hero.png",
                MetaTitle = "The Future of Personalized Medicine: 2024 Trends",
                MetaDescription = "Discover 2024 trends in personalized medicine and how International Center leads healthcare innovation.",
                PublishedAt = DateTime.UtcNow.AddDays(-20),
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new NewsArticle
            {
                Id = "news-99999999-9999-9999-9999-999999999999",
                Title = "Telemedicine Revolution: Remote Care Best Practices",
                Slug = "telemedicine-revolution-remote-care-practices",
                Excerpt = "How telemedicine is transforming healthcare delivery and improving patient access to quality care.",
                Content = "The telemedicine revolution has fundamentally changed healthcare delivery, enabling improved access to quality care while maintaining safety and effectiveness. International Center has been at the forefront of this transformation, implementing comprehensive remote care protocols that ensure continuity of care and expanded access to specialized services for patients regardless of geographic location.\n\nKey benefits of our telemedicine platform:\n\n1. Improved Access: Patients in remote areas or with mobility limitations can access specialized care without travel barriers.\n\n2. Continuity of Care: Regular follow-up appointments and monitoring can be conducted virtually, ensuring consistent care.\n\n3. Efficiency: Reduced wait times and streamlined appointment scheduling improve patient satisfaction.\n\n4. Cost-Effectiveness: Lower overhead costs can be passed on to patients, making quality care more affordable.\n\n5. Safety: During health emergencies or pandemics, telemedicine ensures continued access to care while minimizing exposure risks.\n\nOur telemedicine protocols include:\n- Secure video consultations with board-certified physicians\n- Remote monitoring of chronic conditions\n- Virtual health coaching and education sessions\n- Digital prescription management\n- Integration with wearable devices and health apps\n- Emergency consultation capabilities\n\nWe maintain the same high standards of care in our virtual consultations as in our in-person visits, ensuring that patients receive comprehensive, personalized attention regardless of the delivery method.",
                AuthorName = "Dr. Jennifer Liu",
                AuthorEmail = "j.liu@internationalcenter.com",
                Tags = new[] { "telemedicine", "remote care", "healthcare access", "innovation", "technology" },
                Status = "published",
                Featured = true,
                Category = "Industry Insights",
                ImageUrl = "http://localhost:8099/assets/images/news/telemedicine-revolution-hero.png",
                MetaTitle = "Telemedicine Revolution and Best Practices - International Center",
                MetaDescription = "How telemedicine is transforming healthcare delivery and improving patient access to care.",
                PublishedAt = DateTime.UtcNow.AddDays(-8),
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                UpdatedAt = DateTime.UtcNow.AddDays(-8)
            },
            new NewsArticle
            {
                Id = "news-aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                Title = "Regulatory Changes in Regenerative Medicine: 2024 Update",
                Slug = "regulatory-changes-regenerative-medicine-2024",
                Excerpt = "Understanding the latest regulatory updates affecting regenerative medicine practices and patient care.",
                Content = "The regulatory landscape for regenerative medicine continues to evolve, with significant updates in 2024 affecting treatment protocols and patient access. International Center's regulatory affairs team provides insights into these changes and their implications for patient care, ensuring our practices remain compliant while maximizing treatment options for patients.\n\nKey regulatory developments in 2024:\n\n1. FDA Guidance Updates: New guidelines for cell and gene therapy products provide clearer pathways for treatment approval and clinical implementation.\n\n2. Manufacturing Standards: Enhanced requirements for cell processing and manufacturing ensure higher quality and safety standards.\n\n3. Clinical Trial Protocols: Streamlined approval processes for regenerative medicine clinical trials accelerate research and development.\n\n4. International Harmonization: Efforts to align regulatory standards across countries improve global access to treatments.\n\n5. Patient Access Programs: Expanded compassionate use and right-to-try programs provide access to investigational treatments.\n\nImplications for patients:\n- Improved safety through enhanced manufacturing standards\n- Faster access to innovative treatments through streamlined approval processes\n- Greater transparency in treatment outcomes and safety data\n- Expanded treatment options through international collaboration\n\nAt International Center, we work closely with regulatory agencies to ensure compliance while advocating for patient access to safe and effective treatments. Our regulatory affairs team continuously monitors developments and updates our protocols to reflect the latest standards and requirements.",
                AuthorName = "Regulatory Affairs Team",
                AuthorEmail = "regulatory@internationalcenter.com",
                Tags = new[] { "regulation", "regenerative medicine", "compliance", "2024 updates", "FDA" },
                Status = "published",
                Featured = false,
                Category = "Industry Insights",
                ImageUrl = "http://localhost:8099/assets/images/news/regulatory-changes-2024-hero.png",
                MetaTitle = "2024 Regulatory Changes in Regenerative Medicine",
                MetaDescription = "Latest regulatory updates affecting regenerative medicine practices and patient care in 2024.",
                PublishedAt = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            }
        };

        await context.NewsArticles.AddRangeAsync(articles);
        await context.SaveChangesAsync();
    }
}