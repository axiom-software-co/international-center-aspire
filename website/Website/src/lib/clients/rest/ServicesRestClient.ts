import { BaseRestClient, RestClientConfig, PaginatedRestResponse, RestResponse } from './BaseRestClient';
import { config } from '../../../environments';
import type { 
  Service, 
  GetServicesParams, 
  SearchServicesParams 
} from '../services/types';

export interface ServiceCategory {
  id: string;
  name: string;
  description: string;
  slug: string;
  displayOrder: number;
  minPriorityOrder: number;
  maxPriorityOrder: number;
  featured1: boolean;
  featured2: boolean;
  active: boolean;
  createdAt: string;
  updatedAt: string;
}

export class ServicesRestClient extends BaseRestClient {
  constructor() {
    super({
      baseUrl: config.domains.services.baseUrl,
      timeout: config.domains.services.timeout,
      retryAttempts: config.domains.services.retryAttempts,
    });
  }

  /**
   * Get paginated list of services
   * Maps to GET /api/services endpoint through Public Gateway
   */
  async getServices(params: GetServicesParams = {}): Promise<PaginatedRestResponse<Service>> {
    const queryParams = new URLSearchParams();
    
    if (params.page !== undefined) queryParams.set('page', params.page.toString());
    if (params.pageSize !== undefined) queryParams.set('pageSize', params.pageSize.toString());
    if (params.category) queryParams.set('category', params.category);
    if (params.featured !== undefined) queryParams.set('featured', params.featured.toString());
    if (params.status) queryParams.set('status', params.status);

    const endpoint = `/api/services${queryParams.toString() ? `?${queryParams}` : ''}`;
    
    return this.request<PaginatedRestResponse<Service>>(endpoint, {
      method: 'GET',
    });
  }

  /**
   * Get service by slug
   * Maps to GET /api/services/{slug} endpoint through Public Gateway
   */
  async getServiceBySlug(slug: string): Promise<RestResponse<Service>> {
    if (!slug) {
      throw new Error('Service slug is required');
    }

    const endpoint = `/api/services/${encodeURIComponent(slug)}`;
    
    return this.request<RestResponse<Service>>(endpoint, {
      method: 'GET',
    });
  }

  /**
   * Get service categories
   * Maps to GET /api/categories endpoint through Public Gateway
   */
  async getServiceCategories(): Promise<RestResponse<ServiceCategory[]>> {
    const endpoint = '/api/categories';
    
    return this.request<RestResponse<ServiceCategory[]>>(endpoint, {
      method: 'GET',
    });
  }

  /**
   * Get featured services
   * Maps to GET /api/services/featured endpoint through Public Gateway
   */
  async getFeaturedServices(limit?: number): Promise<RestResponse<Service[]>> {
    const queryParams = new URLSearchParams();
    if (limit !== undefined) queryParams.set('limit', limit.toString());

    const endpoint = `/api/services/featured${queryParams.toString() ? `?${queryParams}` : ''}`;
    
    return this.request<RestResponse<Service[]>>(endpoint, {
      method: 'GET',
    });
  }

  /**
   * Search services
   * Maps to GET /api/services/search endpoint through Public Gateway
   */
  async searchServices(params: SearchServicesParams): Promise<PaginatedRestResponse<Service>> {
    const queryParams = new URLSearchParams();
    
    queryParams.set('q', params.q);
    if (params.page !== undefined) queryParams.set('page', params.page.toString());
    if (params.pageSize !== undefined) queryParams.set('pageSize', params.pageSize.toString());
    if (params.category) queryParams.set('category', params.category);
    if (params.sortBy) queryParams.set('sortBy', params.sortBy);

    const endpoint = `/api/services/search?${queryParams}`;
    
    return this.request<PaginatedRestResponse<Service>>(endpoint, {
      method: 'GET',
    });
  }

  /**
   * Get service statistics (if available)
   * Maps to GET /api/services/stats endpoint through Public Gateway
   */
  async getServiceStats(): Promise<RestResponse<{ totalServices: number; totalCategories: number; featuredServices: number }>> {
    const endpoint = '/api/services/stats';
    
    return this.request<RestResponse<{ totalServices: number; totalCategories: number; featuredServices: number }>>(endpoint, {
      method: 'GET',
    });
  }
}