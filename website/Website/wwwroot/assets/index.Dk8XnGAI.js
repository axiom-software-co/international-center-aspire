import "./environments.ozjwLHW8.js";
let _servicesClient = null;
const servicesClient = {
  get instance() {
    if (!_servicesClient) {
      _servicesClient = new ServicesRestClient();
    }
    return _servicesClient;
  },
  // Proxy common methods to avoid breaking existing API
  async getServices(params = {}) {
    return this.instance.getServices(params);
  },
  async getServiceBySlug(slug) {
    return this.instance.getServiceBySlug(slug);
  },
  async getServiceCategories() {
    return this.instance.getServiceCategories();
  },
  async getFeaturedServices(limit) {
    return this.instance.getFeaturedServices(limit);
  },
  async searchServices(params) {
    return this.instance.searchServices(params);
  },
  async getServiceStats() {
    return this.instance.getServiceStats();
  }
};
export {
  servicesClient as s
};
