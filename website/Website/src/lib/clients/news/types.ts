// News Domain Types - Updated for standardized API responses

import {
  BaseEntity,
  PaginationParams,
  FilterParams,
  StandardResponse,
  SingleResponse,
} from '../shared/types';

export interface NewsArticle extends BaseEntity {
  title: string;
  slug: string;
  excerpt: string;
  content: string;
  featured_image: string;
  author: string;
  tags: string[];
  status: 'published' | 'draft' | 'archived';
  featured: boolean;
  category: string;
  meta_title: string;
  meta_description: string;
  published_at: string;
}

// Standardized response types
export type NewsResponse = StandardResponse<NewsArticle>;
export type NewsArticleResponse = SingleResponse<NewsArticle>;

export interface GetNewsParams extends PaginationParams, FilterParams {
  category?: string;
  featured?: boolean;
  sortBy?: 'date-asc' | 'date-desc' | 'title';
}

export interface SearchNewsParams extends PaginationParams {
  q: string;
  category?: string;
  sortBy?: string;
}

// Legacy support - will be deprecated
export interface LegacyNewsResponse {
  articles: NewsArticle[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
