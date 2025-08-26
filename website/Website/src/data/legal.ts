// Legal page data for International Center Website
// Provides standardized legal content for privacy policy and terms of service

export interface LegalPageData {
  slug: string;
  title: string;
  lastUpdated: string;
  content: string;
  sections: LegalSection[];
}

export interface LegalSection {
  id: string;
  title: string;
  content: string;
  subsections?: LegalSubsection[];
}

export interface LegalSubsection {
  id: string;
  title: string;
  content: string;
}

const legalPages: Record<string, LegalPageData> = {
  'privacy-policy': {
    slug: 'privacy-policy',
    title: 'Privacy Policy',
    lastUpdated: '2025-08-26',
    content: 'This Privacy Policy describes how International Medical Center collects, uses, and protects your personal information.',
    sections: [
      {
        id: 'information-collection',
        title: 'Information We Collect',
        content: 'We collect information you provide directly to us, such as when you create an account, contact us, or use our services.',
        subsections: [
          {
            id: 'personal-information',
            title: 'Personal Information',
            content: 'This may include your name, email address, phone number, and other contact information.'
          },
          {
            id: 'health-information',
            title: 'Health Information',
            content: 'We may collect health-related information in accordance with HIPAA regulations and medical privacy standards.'
          }
        ]
      },
      {
        id: 'information-use',
        title: 'How We Use Your Information',
        content: 'We use the information we collect to provide, maintain, and improve our services, communicate with you, and comply with legal obligations.'
      },
      {
        id: 'information-sharing',
        title: 'Information Sharing and Disclosure',
        content: 'We do not sell, trade, or otherwise transfer your personal information to third parties without your consent, except as described in this policy.'
      },
      {
        id: 'data-security',
        title: 'Data Security',
        content: 'We implement appropriate security measures to protect your personal information against unauthorized access, alteration, disclosure, or destruction.'
      },
      {
        id: 'your-rights',
        title: 'Your Rights',
        content: 'You have the right to access, update, or delete your personal information. Please contact us to exercise these rights.'
      },
      {
        id: 'contact-us',
        title: 'Contact Information',
        content: 'If you have any questions about this Privacy Policy, please contact us at privacy@internationalsolutions.medical'
      }
    ]
  },
  'terms-of-service': {
    slug: 'terms-of-service',
    title: 'Terms of Service',
    lastUpdated: '2025-08-26',
    content: 'These Terms of Service govern your use of International Medical Center\'s website and services.',
    sections: [
      {
        id: 'acceptance-of-terms',
        title: 'Acceptance of Terms',
        content: 'By accessing and using our website, you accept and agree to be bound by the terms and provision of this agreement.'
      },
      {
        id: 'use-of-website',
        title: 'Use of Website',
        content: 'You may use our website for lawful purposes only. You agree not to use the website in any way that violates applicable laws or regulations.'
      },
      {
        id: 'medical-disclaimer',
        title: 'Medical Disclaimer',
        content: 'The information provided on this website is for educational purposes only and is not intended as a substitute for professional medical advice, diagnosis, or treatment.'
      },
      {
        id: 'intellectual-property',
        title: 'Intellectual Property',
        content: 'All content on this website, including text, graphics, logos, and images, is the property of International Medical Center and is protected by copyright laws.'
      },
      {
        id: 'limitation-of-liability',
        title: 'Limitation of Liability',
        content: 'International Medical Center shall not be liable for any damages arising from the use of this website or the inability to use this website.'
      },
      {
        id: 'modifications',
        title: 'Modifications to Terms',
        content: 'We reserve the right to modify these terms at any time. Changes will be effective immediately upon posting on the website.'
      }
    ]
  }
};

export function getLegalPageBySlug(slug: string): LegalPageData | null {
  return legalPages[slug] || null;
}

export function getAllLegalPages(): LegalPageData[] {
  return Object.values(legalPages);
}

export function getLegalPageSlugs(): string[] {
  return Object.keys(legalPages);
}