const aspireEnvironment = process.env.ASPIRE_ENVIRONMENT || void 0 || process.env.NODE_ENV || "Development";
const normalizeEnvironment = (env) => {
  const normalized = env.toLowerCase();
  if (normalized.includes("prod")) return "Production";
  if (normalized.includes("stag")) return "Staging";
  return "Development";
};
const currentEnvironment = normalizeEnvironment(aspireEnvironment);
const isDevelopment = currentEnvironment === "Development";
const isProduction = currentEnvironment === "Production";
const getApiBaseUrl = () => {
  switch (currentEnvironment) {
    case "Production":
      return "https://api.internationalcenter.com";
    case "Staging":
      return "https://api-staging.internationalcenter.com";
    default:
      return "http://localhost:7220";
  }
};
const getPublicGatewayUrl = () => {
  return getApiBaseUrl();
};
const config = {
  environment: currentEnvironment,
  apiBaseUrl: getApiBaseUrl(),
  publicGatewayUrl: getPublicGatewayUrl(),
  features: {
    enableDebugTools: isDevelopment,
    enableAnalytics: isProduction,
    enableCaching: !isDevelopment
  },
  domains: {}
};
if (isDevelopment) {
  console.log("🔧 International Center Website Environment:", {
    environment: currentEnvironment,
    apiBaseUrl: config.apiBaseUrl,
    publicGatewayUrl: config.publicGatewayUrl,
    features: config.features,
    environmentVariables: {
      ASPIRE_ENVIRONMENT: process.env.ASPIRE_ENVIRONMENT,
      PUBLIC_ASPIRE_ENVIRONMENT: void 0,
      VITE_PUBLIC_GATEWAY_URL: void 0,
      NODE_ENV: process.env.NODE_ENV
    }
  });
}
export {
  config as c,
  isProduction as i
};
