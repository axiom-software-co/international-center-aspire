// Static service categories for navigation menu
// This provides fallback data for the navigation when API is unavailable

export interface StaticServiceCategory {
  id: string;
  name: string;
  slug: string;
  description: string;
  services: StaticService[];
  featured: boolean;
  displayOrder: number;
}

export interface StaticService {
  id: string;
  name: string;
  slug: string;
  description: string;
  category: string;
  featured: boolean;
  displayOrder: number;
}

// Static service categories for International Center navigation
export const staticServiceCategories: StaticServiceCategory[] = [
  {
    id: "regenerative-medicine",
    name: "Regenerative Medicine",
    slug: "regenerative-medicine",
    description: "Advanced regenerative treatments for healing and recovery",
    featured: true,
    displayOrder: 1,
    services: [
      {
        id: "stem-cell-therapy",
        name: "Stem Cell Therapy",
        slug: "stem-cell-therapy",
        description: "Cutting-edge stem cell treatments for various conditions",
        category: "regenerative-medicine",
        featured: true,
        displayOrder: 1
      },
      {
        id: "platelet-rich-plasma",
        name: "Platelet-Rich Plasma (PRP)",
        slug: "platelet-rich-plasma",
        description: "PRP therapy for accelerated healing and tissue regeneration",
        category: "regenerative-medicine",
        featured: true,
        displayOrder: 2
      }
    ]
  },
  {
    id: "pain-management",
    name: "Pain Management",
    slug: "pain-management", 
    description: "Comprehensive pain management solutions",
    featured: true,
    displayOrder: 2,
    services: [
      {
        id: "chronic-pain-treatment",
        name: "Chronic Pain Treatment",
        slug: "chronic-pain-treatment",
        description: "Advanced treatments for chronic pain conditions",
        category: "pain-management",
        featured: false,
        displayOrder: 1
      },
      {
        id: "joint-injections",
        name: "Joint Injections",
        slug: "joint-injections",
        description: "Targeted joint injection therapies",
        category: "pain-management",
        featured: false,
        displayOrder: 2
      }
    ]
  },
  {
    id: "wellness-optimization",
    name: "Wellness & Optimization",
    slug: "wellness-optimization",
    description: "Comprehensive wellness and health optimization services",
    featured: true,
    displayOrder: 3,
    services: [
      {
        id: "nutritional-therapy",
        name: "Nutritional Therapy",
        slug: "nutritional-therapy",
        description: "Personalized nutritional therapy programs",
        category: "wellness-optimization",
        featured: false,
        displayOrder: 1
      },
      {
        id: "hormone-optimization",
        name: "Hormone Optimization",
        slug: "hormone-optimization",
        description: "Bioidentical hormone replacement and optimization",
        category: "wellness-optimization", 
        featured: false,
        displayOrder: 2
      }
    ]
  },
  {
    id: "consultation-services",
    name: "Consultation Services",
    slug: "consultation-services",
    description: "Expert medical consultations and assessments",
    featured: false,
    displayOrder: 4,
    services: [
      {
        id: "initial-consultation",
        name: "Initial Consultation",
        slug: "initial-consultation",
        description: "Comprehensive initial medical consultation",
        category: "consultation-services",
        featured: false,
        displayOrder: 1
      },
      {
        id: "follow-up-care",
        name: "Follow-up Care",
        slug: "follow-up-care",
        description: "Ongoing follow-up care and monitoring",
        category: "consultation-services",
        featured: false,
        displayOrder: 2
      }
    ]
  }
];

// Helper functions for navigation menu
export const getFeaturedServiceCategories = (): StaticServiceCategory[] => {
  return staticServiceCategories.filter(category => category.featured);
};

export const getAllServices = (): StaticService[] => {
  return staticServiceCategories.flatMap(category => category.services);
};

export const getFeaturedServices = (): StaticService[] => {
  return getAllServices().filter(service => service.featured);
};

export const getServicesByCategory = (categorySlug: string): StaticService[] => {
  const category = staticServiceCategories.find(cat => cat.slug === categorySlug);
  return category ? category.services : [];
};