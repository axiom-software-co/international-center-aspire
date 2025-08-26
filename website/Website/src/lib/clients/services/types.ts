// Services Domain Types - Updated for REST API responses

import {
  BaseEntity,
  PaginationParams,
  FilterParams,
  StandardRestResponse,
  SingleRestResponse,
} from '../rest/types';

export interface Service extends BaseEntity {
  title: string;
  slug: string;
  description: string;
  detailed_description: string;
  technologies: string[];
  features: string[];
  delivery_modes: string[];
  icon: string;
  image: string;
  status: 'published' | 'draft' | 'archived';
  sort_order: number;
  meta_title: string;
  meta_description: string;
}

// Standardized response types
export type ServicesResponse = StandardRestResponse<Service>;
export type ServiceResponse = SingleRestResponse<Service>;

export interface GetServicesParams extends PaginationParams, FilterParams {
  category?: string;
  featured?: boolean;
}

export interface SearchServicesParams extends PaginationParams {
  q: string;
  category?: string;
  sortBy?: string;
}

// Legacy support - will be deprecated
export interface LegacyServicesResponse {
  services: Service[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
