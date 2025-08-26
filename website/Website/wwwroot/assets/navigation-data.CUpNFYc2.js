import { researchClient, newsClient } from "./index.CuysOuXH.js";
import { s as servicesClient } from "./index.Dk8XnGAI.js";
function transformToNavigationCategories(dbServices, categories) {
  if (!dbServices || dbServices.length === 0) {
    return [];
  }
  const categoryMap = /* @__PURE__ */ new Map();
  const categoryIdToName = {};
  categories.forEach((category) => {
    categoryIdToName[category.id] = category.name;
    categoryMap.set(category.name, []);
  });
  const categoryDescriptions = {
    "Primary Care": "Comprehensive primary care and preventive health services",
    "Primary Care Services": "Comprehensive primary care and preventive health services",
    "Regenerative Medicine": "Advanced cellular and biological treatments for healing and recovery",
    "Regenerative Therapies": "Advanced cellular and biological treatments for healing and recovery",
    "Pain Management": "Comprehensive solutions for chronic and acute pain relief",
    "Wellness & Prevention": "Preventive care and overall health optimization services",
    "Diagnostics": "Advanced diagnostic testing and health assessments",
    "Specialized Care": "Targeted treatments for specific health conditions",
    "Other Services": "Additional healthcare services and treatments"
  };
  categoryMap.set("Other Services", []);
  dbServices.forEach((service) => {
    let categoryName = "Other Services";
    if (service.category_id && categoryIdToName[service.category_id]) {
      categoryName = categoryIdToName[service.category_id];
    } else if (service.category?.name) {
      categoryName = service.category.name;
    }
    if (!categoryMap.has(categoryName)) {
      categoryMap.set(categoryName, []);
    }
    categoryMap.get(categoryName).push({
      title: service.title || "Unknown Service",
      url: `/services/${service.slug || "unknown"}`,
      description: service.description || ""
    });
  });
  categoryMap.forEach((services) => {
    services.sort((a, b) => {
      const aService = dbServices.find((s) => s.title === a.title);
      const bService = dbServices.find((s) => s.title === b.title);
      if (aService?.featured && !bService?.featured) return -1;
      if (!aService?.featured && bService?.featured) return 1;
      return a.title.localeCompare(b.title);
    });
  });
  const navigationCategoryOrder = [
    "Primary Care",
    "Primary Care Services",
    "Regenerative Medicine",
    "Regenerative Therapies",
    "Pain Management",
    "Diagnostics",
    "Wellness & Prevention",
    "Specialized Care",
    "Other Services"
  ];
  return Array.from(categoryMap.entries()).filter(([_, items]) => items.length > 0).map(([categoryName, items]) => ({
    title: categoryName,
    description: categoryDescriptions[categoryName] || "",
    items
  })).sort((a, b) => {
    const aIndex = navigationCategoryOrder.indexOf(a.title);
    const bIndex = navigationCategoryOrder.indexOf(b.title);
    if (aIndex !== -1 && bIndex !== -1) {
      return aIndex - bIndex;
    }
    if (aIndex !== -1) return -1;
    if (bIndex !== -1) return 1;
    return a.title.localeCompare(b.title);
  });
}
function transformToFooterCategories(dbServices, categories) {
  if (!dbServices || dbServices.length === 0) {
    return [];
  }
  const categoryMap = /* @__PURE__ */ new Map();
  const categoryIdToName = {};
  categories.forEach((category) => {
    categoryIdToName[category.id] = category.name;
    categoryMap.set(category.name, []);
  });
  categoryMap.set("Other Services", []);
  dbServices.forEach((service) => {
    let categoryName = "Other Services";
    if (service.category_id && categoryIdToName[service.category_id]) {
      categoryName = categoryIdToName[service.category_id];
    } else if (service.category?.name) {
      categoryName = service.category.name;
    }
    if (!categoryMap.has(categoryName)) {
      categoryMap.set(categoryName, []);
    }
    categoryMap.get(categoryName).push({
      name: service.title || "Unknown Service",
      href: `/services/${service.slug || "unknown"}`
    });
  });
  categoryMap.forEach((services) => {
    services.sort((a, b) => {
      const aService = dbServices.find((s) => s.title === a.name);
      const bService = dbServices.find((s) => s.title === b.name);
      if (aService?.featured && !bService?.featured) return -1;
      if (!aService?.featured && bService?.featured) return 1;
      return a.name.localeCompare(b.name);
    });
  });
  const footerCategoryOrder = [
    "Primary Care",
    "Regenerative Medicine",
    "Pain Management",
    "Diagnostics",
    "Specialized Care",
    "Other Services"
  ];
  return Array.from(categoryMap.entries()).filter(([_, services]) => services.length > 0).map(([categoryName, services]) => ({
    title: categoryName,
    services
  })).sort((a, b) => {
    const aIndex = footerCategoryOrder.indexOf(a.title);
    const bIndex = footerCategoryOrder.indexOf(b.title);
    if (aIndex !== -1 && bIndex !== -1) {
      return aIndex - bIndex;
    }
    if (aIndex !== -1) return -1;
    if (bIndex !== -1) return 1;
    return a.title.localeCompare(b.title);
  });
}
async function loadNavigationData() {
  try {
    console.log("🔄 Loading navigation data from Content API...");
    const [servicesResponse, categoriesResponse] = await Promise.all([
      servicesClient.getServices({ pageSize: 100 }),
      servicesClient.getServiceCategories()
    ]);
    const services = servicesResponse.services || servicesResponse.data || [];
    const categories = categoriesResponse.categories || categoriesResponse.data || [];
    if (!services || !Array.isArray(services) || services.length === 0) {
      console.warn("⚠️ No services returned, navigation will show empty state");
      return {
        navigationCategories: [],
        footerCategories: []
      };
    }
    console.log(`✅ Loaded ${services.length} services for navigation`);
    console.log(`✅ Loaded ${categories.length} categories for navigation`);
    return {
      navigationCategories: transformToNavigationCategories(services, categories),
      footerCategories: transformToFooterCategories(services, categories)
    };
  } catch (error) {
    console.error("❌ Failed to load navigation data:", error);
    return {
      navigationCategories: [],
      footerCategories: []
    };
  }
}
async function loadHeroServicesData() {
  try {
    console.log("🔄 Loading hero services data from Content API...");
    const categoriesResponse = await servicesClient.getServiceCategories();
    const categories = categoriesResponse.categories || categoriesResponse.data || [];
    if (!categories || !Array.isArray(categories) || categories.length === 0) {
      console.warn("⚠️ No categories returned, hero will show empty state");
      return {
        primaryCareServices: [],
        regenerativeTherapies: [],
        primaryCategoryName: "Featured Category 1",
        secondaryCategoryName: "Featured Category 2"
      };
    }
    const featured1Category = categories.find((cat) => cat.featured1 === true);
    const featured2Category = categories.find((cat) => cat.featured2 === true);
    if (!featured1Category || !featured2Category) {
      console.warn("⚠️ No featured categories found, using fallback");
      return {
        primaryCareServices: [],
        regenerativeTherapies: [],
        primaryCategoryName: "Featured Category 1",
        secondaryCategoryName: "Featured Category 2"
      };
    }
    console.log(
      `✅ Found featured categories: ${featured1Category.name} (featured1), ${featured2Category.name} (featured2)`
    );
    const [primaryServicesResponse, secondaryServicesResponse] = await Promise.all([
      servicesClient.getServices({
        category: featured1Category.slug,
        pageSize: 4
      }),
      servicesClient.getServices({
        category: featured2Category.slug,
        pageSize: 4
      })
    ]);
    const primaryServices = primaryServicesResponse.services || primaryServicesResponse.data || [];
    const secondaryServices = secondaryServicesResponse.services || secondaryServicesResponse.data || [];
    const primaryCareServices = primaryServices.map((service) => ({
      title: service.title || "Unknown Service",
      url: `/services/${service.slug || "unknown"}`
    }));
    const regenerativeTherapies = secondaryServices.map((service) => ({
      title: service.title || "Unknown Service",
      url: `/services/${service.slug || "unknown"}`
    }));
    console.log(
      `✅ Hero services loaded: ${primaryCareServices.length} from ${featured1Category.name}, ${regenerativeTherapies.length} from ${featured2Category.name}`
    );
    return {
      primaryCareServices,
      regenerativeTherapies,
      primaryCategoryName: featured1Category.name,
      secondaryCategoryName: featured2Category.name
    };
  } catch (error) {
    console.error("❌ Failed to load hero services data:", error);
    return {
      primaryCareServices: [],
      regenerativeTherapies: [],
      primaryCategoryName: "Featured Category 1",
      secondaryCategoryName: "Featured Category 2"
    };
  }
}
async function loadRecentContentData() {
  try {
    console.log("🔄 Loading recent content data from Content API...");
    const [researchArticlesResponse, newsResponse] = await Promise.all([
      researchClient.getResearchArticles({ pageSize: 3 }),
      newsClient.getNewsArticles({ pageSize: 3 })
    ]);
    const researchArticles = researchArticlesResponse.articles || researchArticlesResponse.data || [];
    const newsArticles = newsResponse.articles || newsResponse.data || [];
    console.log(
      `✅ Loaded recent content: ${researchArticles?.length || 0} research articles, ${newsArticles?.length || 0} news articles`
    );
    return {
      researchArticles: researchArticles || [],
      newsArticles: newsArticles || []
    };
  } catch (error) {
    console.error("❌ Failed to load recent content data:", error);
    return {
      researchArticles: [],
      newsArticles: []
    };
  }
}
async function loadServicesPageData() {
  try {
    console.log("🔄 Loading services page data from REST API through Public Gateway...");
    const [servicesResponse, categoriesResponse] = await Promise.all([
      servicesClient.getServices({ pageSize: 100 }),
      servicesClient.getServiceCategories()
    ]);
    if (!servicesResponse.success) {
      console.error("❌ Services API request failed:", servicesResponse.message);
      return { serviceCategories: [] };
    }
    if (!categoriesResponse.success) {
      console.error("❌ Categories API request failed:", categoriesResponse.message);
      return { serviceCategories: [] };
    }
    const allServices = servicesResponse.data || [];
    const categories = categoriesResponse.data || [];
    if (!allServices || !Array.isArray(allServices) || allServices.length === 0) {
      console.warn("⚠️ No services returned from API");
      return { serviceCategories: [] };
    }
    console.log(`✅ Loaded ${allServices.length} services from API`);
    console.log(`✅ Loaded ${categories.length} categories from API`);
    const categoryMap = /* @__PURE__ */ new Map();
    const categoryIdToName = {};
    categories.forEach((category) => {
      categoryIdToName[category.id] = category.name;
      categoryMap.set(category.name, {
        id: category.id,
        title: category.name,
        description: category.description || "",
        services: []
      });
    });
    categoryMap.set("Other Services", {
      id: null,
      title: "Other Services",
      description: "Additional healthcare services and treatments",
      services: []
    });
    allServices.forEach((service) => {
      let categoryName = "Other Services";
      if (service.category_id && categoryIdToName[service.category_id]) {
        categoryName = categoryIdToName[service.category_id];
      } else if (service.category?.name) {
        categoryName = service.category.name;
      }
      const categoryData = categoryMap.get(categoryName);
      if (categoryData) {
        const serviceForDisplay = {
          name: service.title || "Unknown Service",
          href: `/services/${service.slug || ""}`,
          description: service.description || "",
          duration: service.duration || "45-90 minutes",
          available: service.available === true,
          featured: service.featured || false,
          // Pass through the actual delivery_modes array for dynamic processing
          delivery_modes: Array.isArray(service.delivery_modes) ? service.delivery_modes : []
        };
        categoryData.services.push(serviceForDisplay);
      }
    });
    const serviceCategories = Array.from(categoryMap.values()).filter((category) => category.services.length > 0).map((category) => ({
      ...category,
      services: category.services.sort((a, b) => {
        if (a.featured && !b.featured) return -1;
        if (!a.featured && b.featured) return 1;
        return a.name.localeCompare(b.name);
      })
    })).sort((a, b) => {
      const categoryOrder = [
        "Primary Care",
        "Regenerative Medicine",
        "Pain Management",
        "Diagnostics",
        "Specialized Care",
        "Other Services"
      ];
      const aIndex = categoryOrder.indexOf(a.title);
      const bIndex = categoryOrder.indexOf(b.title);
      if (aIndex !== -1 && bIndex !== -1) {
        return aIndex - bIndex;
      }
      if (aIndex !== -1) return -1;
      if (bIndex !== -1) return 1;
      return a.title.localeCompare(b.title);
    });
    console.log(`✅ Organized services into ${serviceCategories.length} categories`);
    return { serviceCategories };
  } catch (error) {
    console.error("❌ Failed to load services page data:", error);
    return { serviceCategories: [] };
  }
}
export {
  loadHeroServicesData,
  loadNavigationData,
  loadRecentContentData,
  loadServicesPageData,
  transformToFooterCategories,
  transformToNavigationCategories
};
