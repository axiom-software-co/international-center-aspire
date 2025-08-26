// REST API Clients - All domain clients using standard HTTP/REST
// Clean exports for all REST-enabled domain clients

// Environment configuration
export { config, isLocal, isStaging, isProduction } from '../../../environments';
export type { EnvironmentConfig, Environment } from '../../../environments';

// Shared REST types
export type {
  RestPaginationInfo,
  StandardRestResponse,
  SingleRestResponse,
  RestError,
  PaginationParams,
  FilterParams,
  SortParams,
  SearchParams,
  BaseEntity,
  ApiStatus,
} from './types';

// Base REST client
export { BaseRestClient, RestError as ClientRestError } from './BaseRestClient';
export type { RestClientConfig, RestResponse, PaginatedRestResponse } from './BaseRestClient';

// Services domain (REST-enabled)
export { ServicesRestClient } from './ServicesRestClient';
export type { ServiceCategory } from './ServicesRestClient';

// Re-export services types
export type {
  Service,
  ServicesResponse,
  ServiceResponse,
  GetServicesParams,
  SearchServicesParams,
  LegacyServicesResponse,
} from '../services/types';

// Create singleton instance - lazy loaded to avoid static build issues
let _servicesClient: ServicesRestClient | null = null;
export const servicesClient = {
  get instance(): ServicesRestClient {
    if (!_servicesClient) {
      _servicesClient = new ServicesRestClient();
    }
    return _servicesClient;
  },
  // Proxy common methods to avoid breaking existing API
  async getServices(params: any = {}) {
    return this.instance.getServices(params);
  },
  async getServiceBySlug(slug: string) {
    return this.instance.getServiceBySlug(slug);
  },
  async getServiceCategories() {
    return this.instance.getServiceCategories();
  },
  async getFeaturedServices(limit?: number) {
    return this.instance.getFeaturedServices(limit);
  },
  async searchServices(params: any) {
    return this.instance.searchServices(params);
  },
  async getServiceStats() {
    return this.instance.getServiceStats();
  }
};