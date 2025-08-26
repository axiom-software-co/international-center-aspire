// Domain Clients - Main exports (Migrated to REST architecture)
// Clean exports for all domain clients and types

// Environment configuration
export { config, isLocal, isStaging, isProduction } from '../../environments';
export type { EnvironmentConfig, Environment } from '../../environments';

// Shared REST types
export type {
  // REST response types
  RestPaginationInfo,
  StandardRestResponse,
  SingleRestResponse,
  RestError,
  // Common parameter types
  PaginationParams,
  FilterParams,
  SortParams,
  SearchParams,
  BaseEntity,
} from './rest/types';

// Services domain (REST-enabled through Public Gateway)
export { servicesClient } from './rest';
export type {
  Service,
  ServicesResponse,
  ServiceResponse,
  GetServicesParams,
  SearchServicesParams,
  ServiceCategory,
} from './rest';

// ====================================================================================
// DOMAINS ON HOLD - These will be implemented with REST architecture when development resumes
// ====================================================================================

// Placeholder types for domains on hold
export interface ContactSubmission {
  name: string;
  email: string;
  phone?: string;
  message: string;
  subject?: string;
  type?: 'general' | 'donation' | 'volunteer';
}

export interface NewsletterSubscriptionData {
  email: string;
  name?: string;
  preferences?: string[];
}

// Placeholder clients for domains on hold - these return mock success responses
export const contactsClient = {
  async submitContact(contactData: ContactSubmission): Promise<{ success: boolean; message: string; data?: any }> {
    console.log('🔒 [CONTACTS] Domain on hold - returning mock success response for:', contactData);
    return {
      success: true,
      message: 'Thank you for your submission. We will contact you soon.',
      data: { id: 'mock-' + Date.now(), status: 'received' }
    };
  }
};

export const newsletterClient = {
  async subscribe(subscriptionData: NewsletterSubscriptionData): Promise<{ success: boolean; message: string; data?: any }> {
    console.log('🔒 [NEWSLETTER] Domain on hold - returning mock success response for:', subscriptionData);
    return {
      success: true,
      message: 'Thank you for subscribing to our newsletter!',
      data: { id: 'mock-newsletter-' + Date.now(), status: 'subscribed' }
    };
  }
};

export const newsClient = {
  async getNews(params?: any): Promise<{ success: boolean; message: string; data: any[] }> {
    console.log('🔒 [NEWS] Domain on hold - returning mock news data');
    return {
      success: true,
      message: 'News loaded successfully',
      data: [
        {
          id: 'mock-news-1',
          title: 'Latest Medical Breakthroughs',
          excerpt: 'Stay updated with our latest medical research and treatments.',
          publishedAt: new Date().toISOString(),
          slug: 'latest-medical-breakthroughs'
        }
      ]
    };
  }
};

export const researchClient = {
  async getResearch(params?: any): Promise<{ success: boolean; message: string; data: any[] }> {
    console.log('🔒 [RESEARCH] Domain on hold - returning mock research data');
    return {
      success: true,
      message: 'Research loaded successfully',
      data: [
        {
          id: 'mock-research-1',
          title: 'Regenerative Medicine Studies',
          excerpt: 'Latest research in regenerative medicine and stem cell therapy.',
          publishedAt: new Date().toISOString(),
          slug: 'regenerative-medicine-studies'
        }
      ]
    };
  }
};

// News, Research, Events, Search, Newsletter domains are on hold
// They will use REST clients through the Public Gateway when implemented
