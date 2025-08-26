// Environment configuration for International Center Website
// Provides type-safe environment detection and configuration for .NET Aspire integration

export type Environment = 'Development' | 'Staging' | 'Production';

export interface EnvironmentConfig {
  environment: Environment;
  apiBaseUrl: string;
  publicGatewayUrl: string;
  isDevelopment: boolean;
  isStaging: boolean;
  isProduction: boolean;
  features: {
    enableDebugTools: boolean;
    enableAnalytics: boolean;
    enableCaching: boolean;
  };
  domains: {
    services: {
      baseUrl: string;
      timeout: number;
      retryAttempts: number;
    };
  };
}

// Get environment from .NET Aspire or fallback to NODE_ENV
const aspireEnvironment = (import.meta.env.ASPIRE_ENVIRONMENT || 
                          import.meta.env.PUBLIC_ASPIRE_ENVIRONMENT || 
                          import.meta.env.NODE_ENV || 
                          'Development') as Environment;

// Normalize environment names
const normalizeEnvironment = (env: string): Environment => {
  const normalized = env.toLowerCase();
  if (normalized.includes('prod')) return 'Production';
  if (normalized.includes('stag')) return 'Staging';
  return 'Development';
};

const currentEnvironment = normalizeEnvironment(aspireEnvironment);

// Environment flags
export const isLocal = currentEnvironment === 'Development';
export const isDevelopment = currentEnvironment === 'Development';
export const isStaging = currentEnvironment === 'Staging';
export const isProduction = currentEnvironment === 'Production';

// API URLs from .NET Aspire service discovery or environment variables
const getApiBaseUrl = (): string => {
  // Try Aspire service discovery first
  if (import.meta.env.VITE_PUBLIC_GATEWAY_URL) {
    return import.meta.env.VITE_PUBLIC_GATEWAY_URL;
  }
  
  // Fallback to environment-specific defaults
  switch (currentEnvironment) {
    case 'Production':
      return 'https://api.internationalcenter.com';
    case 'Staging':
      return 'https://api-staging.internationalcenter.com';
    default:
      return 'http://localhost:7220'; // Public Gateway development port
  }
};

const getPublicGatewayUrl = (): string => {
  return getApiBaseUrl(); // Same as API base URL for now
};

// Main configuration object
export const config: EnvironmentConfig = {
  environment: currentEnvironment,
  apiBaseUrl: getApiBaseUrl(),
  publicGatewayUrl: getPublicGatewayUrl(),
  isDevelopment,
  isStaging,
  isProduction,
  features: {
    enableDebugTools: isDevelopment,
    enableAnalytics: isProduction,
    enableCaching: !isDevelopment,
  },
  domains: {
    services: {
      baseUrl: getApiBaseUrl(),
      timeout: 10000, // 10 seconds
      retryAttempts: 3,
    },
  },
};

// Export environment name for compatibility
export const environment = currentEnvironment;

// Debug logging in development
if (isDevelopment) {
  console.log('🔧 International Center Website Environment:', {
    environment: currentEnvironment,
    apiBaseUrl: config.apiBaseUrl,
    publicGatewayUrl: config.publicGatewayUrl,
    features: config.features,
    environmentVariables: {
      ASPIRE_ENVIRONMENT: import.meta.env.ASPIRE_ENVIRONMENT,
      PUBLIC_ASPIRE_ENVIRONMENT: import.meta.env.PUBLIC_ASPIRE_ENVIRONMENT,
      VITE_PUBLIC_GATEWAY_URL: import.meta.env.VITE_PUBLIC_GATEWAY_URL,
      NODE_ENV: import.meta.env.NODE_ENV,
    }
  });
}