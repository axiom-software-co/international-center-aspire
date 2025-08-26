import { c as createComponent, a as renderTemplate, b as createAstro, d as addAttribute, f as renderHead, r as renderComponent, e as renderSlot } from "./astro/server.GZtb3PXx.js";
import "kleur/colors";
import "html-escaper";
import { defineComponent, useSSRContext, ref, computed, onMounted, onUnmounted, watch, mergeProps, createVNode, resolveDynamicComponent } from "vue";
import { GraduationCap, LayoutGrid, Smartphone, ClipboardList, Search, IdCard, ChevronDown, X, Menu, Calendar, Heart, Linkedin, Facebook, Instagram, FileText } from "lucide-vue-next";
import { ssrRenderAttrs, ssrRenderClass, ssrRenderComponent, ssrRenderAttr, ssrInterpolate, ssrRenderList, ssrRenderStyle, ssrIncludeBooleanAttr, ssrRenderVNode } from "vue/server-renderer";
/* empty css                                         */
import "clsx";
const staticServiceCategories = [
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
const _export_sfc = (sfc, props) => {
  const target = sfc.__vccOpts || sfc;
  for (const [key, val] of props) {
    target[key] = val;
  }
  return target;
};
const _sfc_main$1 = /* @__PURE__ */ defineComponent({
  __name: "NavigationMenu",
  props: {
    logo: { type: Object, required: false, default: () => ({
      url: "/",
      alt: "International Center Logo",
      title: "International Center"
    }) },
    menu: { type: Array, required: false, default: () => [] },
    serviceCategories: { type: Array, required: false, default: () => staticServiceCategories }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const apiServiceCategories = ref([]);
    const isLoadingServices = ref(false);
    const apiError = ref(null);
    const useStaticData = ref(false);
    const isMobileMenuOpen = ref(false);
    const isSearchOpen = ref(false);
    const isScrolled = ref(false);
    const activeDropdown = ref(null);
    const activeSubmenu = ref(null);
    const isTouchDevice = ref(false);
    const isComponentMounted = ref(false);
    const savedScrollPosition = ref(0);
    const isNavigating = ref(false);
    const navRef = ref();
    const timeoutRef = ref(null);
    const effectiveServiceCategories = computed(() => {
      if (useStaticData.value || apiError.value) {
        return staticServiceCategories;
      }
      return apiServiceCategories.value;
    });
    async function loadServiceCategories() {
      if (isLoadingServices.value) return;
      isLoadingServices.value = true;
      apiError.value = null;
      try {
        const { loadNavigationData } = await import("./navigation-data.CUpNFYc2.js");
        const navigationData = await loadNavigationData();
        apiServiceCategories.value = navigationData.navigationCategories || [];
        useStaticData.value = false;
        console.log(`✅ Navigation menu loaded ${apiServiceCategories.value.length} service categories`);
      } catch (error) {
        console.warn("❌ Navigation API failed, falling back to static data:", error);
        apiError.value = error instanceof Error ? error.message : "Unknown error";
        useStaticData.value = true;
      } finally {
        isLoadingServices.value = false;
      }
    }
    const menu = computed(() => {
      if (props.menu && props.menu.length > 0) return props.menu;
      const serviceCategories = effectiveServiceCategories.value;
      return [
        { title: "Home", url: "/" },
        {
          title: "Services",
          url: "/services",
          items: serviceCategories.length > 0 ? serviceCategories.flatMap(
            (category) => category.items.map((service) => ({
              title: service.title,
              description: service.description || `${service.title} treatment`,
              url: service.url
            }))
          ) : []
          // Empty services menu if no data available
        },
        {
          title: "Patient Resources",
          url: "/patient-resources",
          items: [
            {
              title: "Pre-Treatment Preparation",
              description: "Guidelines and instructions to prepare for your treatments",
              url: "/patient-resources/pre-treatment"
            },
            {
              title: "Post-Treatment Care",
              description: "Recovery protocols and aftercare instructions",
              url: "/patient-resources/post-treatment"
            },
            {
              title: "Patient Forms",
              description: "Download and complete necessary medical forms",
              url: "/patient-resources/forms"
            },
            {
              title: "Support Groups",
              description: "Connect with other patients and support resources",
              url: "/patient-resources/support-groups"
            },
            {
              title: "Standard Charges",
              description: "Transparent pricing information for all services and procedures",
              url: "/patient-resources/standard-charges"
            },
            {
              title: "Insurance & Billing",
              description: "Insurance coverage information and billing assistance",
              url: "/patient-resources/insurance-billing"
            },
            {
              title: "Financial Options",
              description: "Payment plans and financing options for treatments",
              url: "/patient-resources/financial-options"
            }
          ]
        },
        {
          title: "Community",
          url: "/community",
          items: [
            {
              title: "About Our Values",
              description: "Our commitment to patient care and medical excellence",
              url: "/community"
            },
            {
              title: "Research & Innovation",
              description: "Cutting-edge research and innovative treatments",
              url: "/community/research"
            },
            {
              title: "Certification Programs",
              description: "Professional training through in-person and online learning",
              url: "/community/certification-programs"
            },
            {
              title: "Donations",
              description: "Support our mission of advancing regenerative medicine",
              url: "/community/donations"
            },
            {
              title: "Volunteer",
              description: "Community outreach and volunteer opportunities",
              url: "/community/volunteer"
            },
            {
              title: "Events",
              description: "Upcoming seminars, workshops, and community events",
              url: "/community/events"
            }
          ]
        },
        {
          title: "Company",
          url: "/company",
          items: [
            {
              title: "About Our Team",
              description: "Meet our board-certified physicians and medical team",
              url: "/company/team"
            },
            {
              title: "News & Insights",
              description: "Company news and medical breakthroughs",
              url: "/company/news"
            },
            {
              title: "Credentials",
              description: "Certifications, accreditations, and professional memberships",
              url: "/company/credentials"
            },
            {
              title: "Careers",
              description: "Join our team of healthcare professionals",
              url: "/company/careers"
            }
          ]
        }
      ];
    });
    const communityMenuItems = computed(() => [
      {
        title: "About Our Values",
        description: "Our commitment to patient care and medical excellence",
        url: "/community"
      },
      {
        title: "Research & Innovation",
        description: "Cutting-edge research and innovative treatments",
        url: "/community/research"
      },
      {
        title: "Certification Programs",
        description: "Professional training through in-person and online learning",
        url: "/community/certification-programs"
      },
      {
        title: "Donations",
        description: "Support our mission of advancing regenerative medicine",
        url: "/community/donations"
      },
      {
        title: "Volunteer",
        description: "Community outreach and volunteer opportunities",
        url: "/community/volunteer"
      },
      {
        title: "Events",
        description: "Upcoming seminars, workshops, and community events",
        url: "/community/events"
      }
    ]);
    const companyMenuItems = computed(() => [
      {
        title: "About Our Team",
        description: "Meet our board-certified physicians and medical team",
        url: "/company/team"
      },
      {
        title: "News & Insights",
        description: "Company news and medical breakthroughs",
        url: "/company/news"
      },
      {
        title: "Credentials",
        description: "Certifications, accreditations, and professional memberships",
        url: "/company/credentials"
      },
      {
        title: "Careers",
        description: "Join our team of healthcare professionals",
        url: "/company/careers"
      }
    ]);
    const handleDropdownClick = (title) => {
      if (timeoutRef.value) {
        clearTimeout(timeoutRef.value);
        timeoutRef.value = null;
      }
      const isOpening = activeDropdown.value !== title;
      const isSwitching = activeDropdown.value && activeDropdown.value !== title;
      if (isSwitching || !isOpening) {
        activeSubmenu.value = null;
      }
      activeDropdown.value = activeDropdown.value === title ? null : title;
    };
    const handleDropdownLeave = () => {
      if (isTouchDevice.value) return;
      timeoutRef.value = setTimeout(() => {
        activeDropdown.value = null;
        activeSubmenu.value = null;
      }, 500);
    };
    const handleNavigationClick = () => {
      isNavigating.value = true;
      isMobileMenuOpen.value = false;
      activeDropdown.value = null;
      activeSubmenu.value = null;
      setTimeout(() => {
        isNavigating.value = false;
      }, 100);
    };
    const handleClickOutside = (event) => {
      if (isMobileMenuOpen.value || !isComponentMounted.value) return;
      if (navRef.value && !navRef.value.contains(event.target)) {
        activeDropdown.value = null;
        activeSubmenu.value = null;
      }
    };
    const handleScroll = () => {
      if (typeof window !== "undefined") {
        isScrolled.value = window.scrollY > 10;
      }
    };
    const handleEscapeKey = (event) => {
      if (event.key === "Escape") {
        if (isSearchOpen.value) {
          isSearchOpen.value = false;
        } else if (activeDropdown.value) {
          activeDropdown.value = null;
          activeSubmenu.value = null;
        }
      }
    };
    const checkTouchDevice = () => {
      if (typeof window !== "undefined") {
        isTouchDevice.value = "ontouchstart" in window || navigator.maxTouchPoints > 0;
      }
    };
    onMounted(() => {
      isComponentMounted.value = true;
      loadServiceCategories();
      if (typeof window !== "undefined") {
        window.addEventListener("scroll", handleScroll);
        window.addEventListener("resize", checkTouchDevice);
      }
      if (typeof document !== "undefined") {
        document.addEventListener("mousedown", handleClickOutside, { passive: true });
        document.addEventListener("touchstart", handleClickOutside, { passive: true });
        document.addEventListener("keydown", handleEscapeKey);
      }
      checkTouchDevice();
    });
    onUnmounted(() => {
      if (typeof window !== "undefined") {
        window.removeEventListener("scroll", handleScroll);
        window.removeEventListener("resize", checkTouchDevice);
      }
      if (typeof document !== "undefined") {
        document.removeEventListener("mousedown", handleClickOutside);
        document.removeEventListener("touchstart", handleClickOutside);
        document.removeEventListener("keydown", handleEscapeKey);
      }
      if (timeoutRef.value) {
        clearTimeout(timeoutRef.value);
      }
      document.body.style.overflow = "";
      document.body.style.position = "";
      document.body.style.width = "";
      document.body.style.top = "";
    });
    watch(isMobileMenuOpen, (newValue) => {
      if (!isComponentMounted.value) return;
      if (newValue) {
        const scrollY = typeof window !== "undefined" ? window.scrollY || window.pageYOffset || 0 : 0;
        savedScrollPosition.value = scrollY;
        const originalPosition = document.body.style.position;
        const originalTop = document.body.style.top;
        const originalOverflow = document.body.style.overflow;
        const originalWidth = document.body.style.width;
        document.body.dataset.originalPosition = originalPosition;
        document.body.dataset.originalTop = originalTop;
        document.body.dataset.originalOverflow = originalOverflow;
        document.body.dataset.originalWidth = originalWidth;
        document.body.style.overflow = "hidden";
        document.body.style.position = "fixed";
        document.body.style.width = "100%";
        document.body.style.top = `-${scrollY}px`;
      } else {
        const originalPosition = document.body.dataset.originalPosition || "";
        const originalTop = document.body.dataset.originalTop || "";
        const originalOverflow = document.body.dataset.originalOverflow || "";
        const originalWidth = document.body.dataset.originalWidth || "";
        document.body.style.position = originalPosition;
        document.body.style.top = originalTop;
        document.body.style.overflow = originalOverflow;
        document.body.style.width = originalWidth;
        delete document.body.dataset.originalPosition;
        delete document.body.dataset.originalTop;
        delete document.body.dataset.originalOverflow;
        delete document.body.dataset.originalWidth;
        if (!isNavigating.value && typeof window !== "undefined") {
          window.scrollTo(0, savedScrollPosition.value);
        }
      }
    });
    watch(isMobileMenuOpen, (newValue) => {
      if (newValue) {
        activeDropdown.value = null;
      }
    });
    const __returned__ = { props, apiServiceCategories, isLoadingServices, apiError, useStaticData, isMobileMenuOpen, isSearchOpen, isScrolled, activeDropdown, activeSubmenu, isTouchDevice, isComponentMounted, savedScrollPosition, isNavigating, navRef, timeoutRef, effectiveServiceCategories, loadServiceCategories, menu, communityMenuItems, companyMenuItems, handleDropdownClick, handleDropdownLeave, handleNavigationClick, handleClickOutside, handleScroll, handleEscapeKey, checkTouchDevice, get Heart() {
      return Heart;
    }, get Calendar() {
      return Calendar;
    }, get Menu() {
      return Menu;
    }, get X() {
      return X;
    }, get ChevronDown() {
      return ChevronDown;
    }, get IdCard() {
      return IdCard;
    }, get Search() {
      return Search;
    }, get ClipboardList() {
      return ClipboardList;
    }, get Smartphone() {
      return Smartphone;
    }, get LayoutGrid() {
      return LayoutGrid;
    }, get GraduationCap() {
      return GraduationCap;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
function _sfc_ssrRender$1(_ctx, _push, _parent, _attrs, $props, $setup, $data, $options) {
  _push(`<div${ssrRenderAttrs(_attrs)} data-v-fe35c617><nav class="${ssrRenderClass(`fixed top-0 left-0 right-0 z-50 transition-all duration-300 border-b border-gray-200 bg-white dark:bg-gray-900 ${($setup.isScrolled || $setup.activeDropdown) && !$setup.isMobileMenuOpen ? "shadow-lg border-gray-200" : "border-gray-200"}`)}" data-v-fe35c617><!-- Top Banner Section - Desktop Only --><div class="hidden md:block border-b border-gray-200" data-v-fe35c617><div class="container mx-auto px-4" data-v-fe35c617><div class="flex items-center justify-between h-16" data-v-fe35c617><!-- Left side - Emergency disclaimer --><div class="flex items-center" data-v-fe35c617><div class="text-xs text-gray-700 dark:text-gray-300 font-medium" data-v-fe35c617> Emergency? Call 911 </div></div><!-- Right side - Actions --><div class="flex items-center gap-3" data-v-fe35c617><a href="tel:+1-561-555-0123" class="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 transition-colors duration-200" data-v-fe35c617>`);
  _push(ssrRenderComponent($setup["Smartphone"], { class: "w-4 h-4 flex-shrink-0" }, null, _parent));
  _push(`<span class="leading-none" data-v-fe35c617>(561) 555-0123</span></a><a href="/community/donations" class="flex items-center gap-2 px-4 py-2 text-sm font-medium text-black bg-white hover:bg-gray-100 rounded transition-colors border border-gray-300" data-v-fe35c617>`);
  _push(ssrRenderComponent($setup["Heart"], { class: "w-4 h-4 flex-shrink-0" }, null, _parent));
  _push(`<span class="leading-none" data-v-fe35c617>Donations</span></a><a href="/appointment" class="flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-black hover:bg-gray-800 rounded transition-colors" data-v-fe35c617>`);
  _push(ssrRenderComponent($setup["Calendar"], { class: "w-4 h-4 flex-shrink-0" }, null, _parent));
  _push(`<span class="leading-none" data-v-fe35c617>Book Appointment</span></a></div></div></div></div><!-- Main Navigation Section --><div class="container mx-auto px-4 relative" data-v-fe35c617><div class="flex items-center justify-between h-16" data-v-fe35c617><!-- Logo --><a${ssrRenderAttr("href", $props.logo.url)} class="flex items-center gap-2 lg:gap-3 text-gray-900 dark:text-gray-100 hover:opacity-80 transition-opacity" data-v-fe35c617><svg width="32" height="32" viewBox="0 0 286 326" class="w-8 h-8 lg:w-8 lg:h-8 text-blue-600 flex-shrink-0" xmlns="http://www.w3.org/2000/svg" data-v-fe35c617><path d="M266.006 -9.15527e-05C277.052 -9.15527e-05 286.006 8.95421 286.006 19.9999C286.006 63.3016 262.147 96.1501 235.27 120.882C208.842 145.199 175.696 164.877 154.286 179.512C133.026 194.044 104.6 210.659 80.1709 232.959C68.9387 243.212 59.5065 253.86 52.6094 265H233.995C241.496 277.403 245.806 290.607 245.997 305H40.0098C40.0049 305.333 40 305.666 40 306C40 317.046 31.0457 326 20 326C8.95431 326 0 317.046 0 306C3.61802e-05 262.353 25.8692 228.368 53.2041 203.416C80.3999 178.591 113.474 158.957 131.714 146.489C148.121 135.274 166.244 124.141 183.51 111.5H102.705C95.0362 105.785 87.4268 99.6644 80.1709 93.0409C76.0176 89.2496 72.1106 85.404 68.4863 81.4999H218.233C235.492 62.9951 246.006 43.0175 246.006 19.9999C246.006 8.95421 254.96 -9.15527e-05 266.006 -9.15527e-05Z" fill="currentColor" fill-opacity="0.75" data-v-fe35c617></path><path d="M198.562 176.105C211.057 184.88 223.797 194.561 235.27 205.118C262.147 229.85 286.006 262.698 286.006 306C286.006 317.046 277.052 326 266.006 326C254.96 326 246.006 317.046 246.006 306C246.006 282.982 235.492 263.005 218.233 244.5H68.4863C72.1106 240.596 76.0176 236.75 80.1709 232.959C87.4272 226.335 95.0379 220.215 102.707 214.5H183.514C176.716 209.523 169.785 204.78 162.878 200.174L198.562 176.105ZM20 0C31.0457 0 40 8.95431 40 20C40 20.3338 40.0049 20.6671 40.0098 21H245.997C245.806 35.393 241.496 48.5969 233.995 61H52.6094C59.5065 72.1396 68.9387 82.7879 80.1709 93.041C100.482 111.581 123.555 126.194 142.976 138.945C139.155 141.468 135.391 143.976 131.714 146.489C125.025 151.062 116.341 156.598 106.683 163.001C90.0062 151.944 70.4265 138.305 53.2041 122.584C25.8692 97.6318 3.61802e-05 63.6471 0 20C0 8.95431 8.95431 0 20 0Z" fill="currentColor" data-v-fe35c617></path></svg><div class="flex flex-col min-w-0" data-v-fe35c617><span class="font-bold text-lg leading-tight truncate" data-v-fe35c617>${ssrInterpolate($props.logo.title)}</span><span class="text-xs font-medium text-gray-600 dark:text-gray-400 leading-tight tracking-wide truncate" data-v-fe35c617> for Regenerative Medicine </span></div></a><!-- Desktop Navigation --><div class="hidden md:flex items-center space-x-4" data-v-fe35c617><!--[-->`);
  ssrRenderList($setup.menu, (item) => {
    _push(`<!--[-->`);
    if (item.items) {
      _push(`<div class="relative" data-v-fe35c617><button class="${ssrRenderClass(`flex items-center gap-1 px-2 py-3 text-sm font-medium leading-none transition-colors duration-200 rounded ${$setup.activeDropdown === item.title ? "text-gray-900 dark:text-gray-100 bg-gray-100 dark:bg-gray-800 border border-gray-200 dark:border-gray-700" : "text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100 hover:bg-gray-50 dark:hover:bg-gray-800/50"}`)}" data-v-fe35c617>${ssrInterpolate(item.title)} <div class="flex items-center justify-center ml-0.5" data-v-fe35c617>`);
      _push(ssrRenderComponent($setup["ChevronDown"], {
        class: `w-4 h-4 transition-transform duration-200 flex-shrink-0 ${$setup.activeDropdown === item.title ? "rotate-180 text-gray-900 dark:text-gray-100" : ""}`
      }, null, _parent));
      _push(`</div></button></div>`);
    } else {
      _push(`<a${ssrRenderAttr("href", item.url)} class="${ssrRenderClass(`px-2 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100 hover:bg-gray-100/30 dark:hover:bg-gray-800/50 transition-colors duration-200 ${item.title === "Home" ? "md:hidden lg:block" : ""}`)}" data-v-fe35c617>${ssrInterpolate(item.title)}</a>`);
    }
    _push(`<!--]-->`);
  });
  _push(`<!--]--><button class="flex items-center justify-center w-10 h-10 flex-shrink-0 text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-800 rounded transition-colors duration-200 border border-gray-200 ml-2" aria-label="Search" data-v-fe35c617>`);
  _push(ssrRenderComponent($setup["Search"], { class: "w-5 h-5" }, null, _parent));
  _push(`</button></div><!-- Mobile Controls --><div class="md:hidden flex items-center space-x-2" data-v-fe35c617><!-- Mobile Search Button --><button class="flex items-center justify-center w-10 h-10 flex-shrink-0 text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-800 rounded transition-colors duration-200 border border-gray-200" aria-label="Search" data-v-fe35c617>`);
  _push(ssrRenderComponent($setup["Search"], { class: "w-5 h-5" }, null, _parent));
  _push(`</button><!-- Mobile Menu Toggle --><button class="flex items-center justify-center w-10 h-10 flex-shrink-0 text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-800 rounded transition-colors duration-200 border border-gray-200" aria-label="Toggle navigation" data-v-fe35c617>`);
  if ($setup.isMobileMenuOpen) {
    _push(ssrRenderComponent($setup["X"], { class: "w-6 h-6" }, null, _parent));
  } else {
    _push(ssrRenderComponent($setup["Menu"], { class: "w-6 h-6" }, null, _parent));
  }
  _push(`</button></div></div></div><!-- Desktop Navigation Dropdown - Full React Implementation -->`);
  if ($setup.activeDropdown && !$setup.isMobileMenuOpen) {
    _push(`<div class="fixed inset-0 z-[45] hidden md:block" style="${ssrRenderStyle({ "top": "128px" })}" data-v-fe35c617><!-- Background overlay - click to close --><div class="absolute inset-0 bg-black/20 backdrop-blur-sm" data-v-fe35c617></div><!-- Main dropdown container --><div class="relative bg-white border border-gray-200 shadow-2xl w-full h-full overflow-hidden" data-v-fe35c617><div class="w-full mx-auto h-full" style="${ssrRenderStyle({ "max-width": "1400px" })}" data-v-fe35c617><div class="md:flex h-full" data-v-fe35c617><!-- Left Creative Section - Large screens only --><div class="${ssrRenderClass(`hidden lg:block lg:w-1/4 flex-shrink-0 h-full overflow-hidden relative ${$setup.activeDropdown === "Services" ? "bg-blue-600" : $setup.activeDropdown === "Patient Resources" ? "bg-green-600" : $setup.activeDropdown === "Community" ? "bg-amber-600" : $setup.activeDropdown === "Company" ? "bg-purple-600" : "bg-black"}`)}" data-v-fe35c617><!-- Services Professional Decoration -->`);
    if ($setup.activeDropdown === "Services") {
      _push(`<div class="relative h-full" data-v-fe35c617><!-- Sparse Circle Stars - Services --><div class="absolute inset-0" data-v-fe35c617><div class="absolute inset-0" data-v-fe35c617><!--[-->`);
      ssrRenderList(35, (i) => {
        _push(`<div class="w-2 h-2 bg-white rounded-full absolute animate-pulse" style="${ssrRenderStyle({
          left: `${Math.random() * 97}%`,
          top: `${12 + Math.random() * 85}%`,
          opacity: 0.08 + Math.random() * 0.3,
          animationDelay: `${Math.random() * 8}s`,
          animationDuration: `${8 + Math.random() * 17}s`
        })}" data-v-fe35c617></div>`);
      });
      _push(`<!--]--></div></div><!-- Brand Typography --><div class="absolute top-8 left-0 right-0 z-20 flex justify-center" data-v-fe35c617><div class="text-center" data-v-fe35c617><div class="text-xs font-semibold text-white tracking-[0.2em] mb-1" data-v-fe35c617> INTERNATIONAL CENTER </div><div class="text-[10px] font-medium text-gray-100 tracking-widest" data-v-fe35c617> REGENERATIVE MEDICINE </div></div></div></div>`);
    } else {
      _push(`<!---->`);
    }
    _push(`<!-- Patient Resources Professional Decoration -->`);
    if ($setup.activeDropdown === "Patient Resources") {
      _push(`<div class="relative h-full" data-v-fe35c617><!-- Sparse Triangle Stars - Patient Resources --><div class="absolute inset-0" data-v-fe35c617><div class="absolute inset-0" data-v-fe35c617><!--[-->`);
      ssrRenderList(32, (i) => {
        _push(`<div class="w-0 h-0 absolute animate-pulse" style="${ssrRenderStyle({
          borderLeft: "6px solid transparent",
          borderRight: "6px solid transparent",
          borderBottom: "8px solid white",
          left: `${Math.random() * 97}%`,
          top: `${12 + Math.random() * 85}%`,
          opacity: 0.08 + Math.random() * 0.3,
          animationDelay: `${Math.random() * 8}s`,
          animationDuration: `${8 + Math.random() * 17}s`
        })}" data-v-fe35c617></div>`);
      });
      _push(`<!--]--></div></div><!-- Brand Typography --><div class="absolute top-8 left-0 right-0 z-20 flex justify-center" data-v-fe35c617><div class="text-center" data-v-fe35c617><div class="text-xs font-semibold text-white tracking-[0.2em] mb-1" data-v-fe35c617> INTERNATIONAL CENTER </div><div class="text-[10px] font-medium text-gray-100 tracking-widest" data-v-fe35c617> REGENERATIVE MEDICINE </div></div></div></div>`);
    } else {
      _push(`<!---->`);
    }
    _push(`<!-- Community Professional Decoration -->`);
    if ($setup.activeDropdown === "Community") {
      _push(`<div class="relative h-full" data-v-fe35c617><!-- Sparse Diamond Stars - Community --><div class="absolute inset-0" data-v-fe35c617><div class="absolute inset-0" data-v-fe35c617><!--[-->`);
      ssrRenderList(38, (i) => {
        _push(`<div class="w-2 h-2 bg-white transform rotate-45 absolute animate-pulse" style="${ssrRenderStyle({
          left: `${Math.random() * 97}%`,
          top: `${12 + Math.random() * 85}%`,
          opacity: 0.08 + Math.random() * 0.3,
          animationDelay: `${Math.random() * 8}s`,
          animationDuration: `${8 + Math.random() * 17}s`
        })}" data-v-fe35c617></div>`);
      });
      _push(`<!--]--></div></div><!-- Brand Typography --><div class="absolute top-8 left-0 right-0 z-20 flex justify-center" data-v-fe35c617><div class="text-center" data-v-fe35c617><div class="text-xs font-semibold text-white tracking-[0.2em] mb-1" data-v-fe35c617> INTERNATIONAL CENTER </div><div class="text-[10px] font-medium text-gray-100 tracking-widest" data-v-fe35c617> REGENERATIVE MEDICINE </div></div></div></div>`);
    } else {
      _push(`<!---->`);
    }
    _push(`<!-- Company Professional Decoration -->`);
    if ($setup.activeDropdown === "Company") {
      _push(`<div class="relative h-full" data-v-fe35c617><!-- Sparse Square Stars - Company --><div class="absolute inset-0" data-v-fe35c617><div class="absolute inset-0" data-v-fe35c617><!--[-->`);
      ssrRenderList(30, (i) => {
        _push(`<div class="w-2 h-2 bg-white absolute animate-pulse" style="${ssrRenderStyle({
          left: `${Math.random() * 97}%`,
          top: `${12 + Math.random() * 85}%`,
          opacity: 0.08 + Math.random() * 0.3,
          animationDelay: `${Math.random() * 8}s`,
          animationDuration: `${8 + Math.random() * 17}s`
        })}" data-v-fe35c617></div>`);
      });
      _push(`<!--]--></div></div><!-- Brand Typography --><div class="absolute top-8 left-0 right-0 z-20 flex justify-center" data-v-fe35c617><div class="text-center" data-v-fe35c617><div class="text-xs font-semibold text-white tracking-[0.2em] mb-1" data-v-fe35c617> INTERNATIONAL CENTER </div><div class="text-[10px] font-medium text-gray-100 tracking-widest" data-v-fe35c617> REGENERATIVE MEDICINE </div></div></div></div>`);
    } else {
      _push(`<!---->`);
    }
    _push(`</div><!-- Right Main Dropdown - All screens --><div class="flex-1 h-full bg-white" data-v-fe35c617><div class="flex flex-col h-full" data-v-fe35c617><!-- Header with title and close button --><div class="flex-shrink-0 pt-2 pb-4 pl-4 pr-4 sm:pt-4 sm:pb-6 sm:pl-6 sm:pr-6 lg:pt-6 lg:pb-8 lg:pl-8 lg:pr-8" data-v-fe35c617><div class="flex items-start justify-between gap-4 pb-4 border-b border-gray-200" data-v-fe35c617><!-- Title --><div class="flex-1" data-v-fe35c617><h2 class="text-3xl font-bold text-black" data-v-fe35c617>${ssrInterpolate($setup.activeDropdown)}</h2></div><!-- Action buttons and close button --><div class="flex-shrink-0 flex items-center gap-3" data-v-fe35c617><!-- Services dropdown action button -->`);
    if ($setup.activeDropdown === "Services" && $setup.effectiveServiceCategories.length > 0) {
      _push(`<a href="/services" class="flex items-center gap-2 px-4 h-10 text-sm font-medium text-white bg-black hover:bg-gray-800 rounded transition-colors" data-v-fe35c617>`);
      _push(ssrRenderComponent($setup["LayoutGrid"], { class: "w-4 h-4" }, null, _parent));
      _push(` View All Services </a>`);
    } else {
      _push(`<!---->`);
    }
    _push(`<!-- Patient Resources dropdown action button -->`);
    if ($setup.activeDropdown === "Patient Resources") {
      _push(`<a href="/patient-resources/portal" class="flex items-center gap-2 px-4 h-10 text-sm font-medium text-white bg-black hover:bg-gray-800 rounded transition-colors" data-v-fe35c617>`);
      _push(ssrRenderComponent($setup["IdCard"], { class: "w-4 h-4" }, null, _parent));
      _push(` Patient Portal </a>`);
    } else {
      _push(`<!---->`);
    }
    _push(`<!-- Community dropdown action button -->`);
    if ($setup.activeDropdown === "Community") {
      _push(`<a href="/community/learning-portal" class="flex items-center gap-2 px-4 h-10 text-sm font-medium text-white bg-black hover:bg-gray-800 rounded transition-colors" data-v-fe35c617>`);
      _push(ssrRenderComponent($setup["GraduationCap"], { class: "w-4 h-4" }, null, _parent));
      _push(` Learning Portal </a>`);
    } else {
      _push(`<!---->`);
    }
    _push(`<!-- Company dropdown action button -->`);
    if ($setup.activeDropdown === "Company") {
      _push(`<a href="/company/contact" class="flex items-center gap-2 px-4 h-10 text-sm font-medium text-white bg-black hover:bg-gray-800 rounded transition-colors" data-v-fe35c617>`);
      _push(ssrRenderComponent($setup["ClipboardList"], { class: "w-4 h-4" }, null, _parent));
      _push(` Contact Us </a>`);
    } else {
      _push(`<!---->`);
    }
    _push(`<!-- Close button --><button class="flex items-center justify-center w-10 h-10 text-gray-600 hover:text-gray-800 bg-white hover:bg-gray-50 rounded transition-colors duration-200 border border-gray-300" aria-label="Close navigation" data-v-fe35c617>`);
    _push(ssrRenderComponent($setup["X"], { class: "w-6 h-6" }, null, _parent));
    _push(`</button></div></div></div><!-- Main Dropdown Content --><div class="flex overflow-hidden flex-1" data-v-fe35c617><div class="w-full h-full overflow-y-auto pl-4 pr-4 sm:pl-6 sm:pr-6 lg:pl-8 lg:pr-8" data-v-fe35c617><!-- Services Multi-column layout -->`);
    if ($setup.activeDropdown === "Services") {
      _push(`<div class="w-full h-full" data-v-fe35c617><!-- Loading state -->`);
      if ($setup.isLoadingServices) {
        _push(`<div class="flex items-center justify-center h-64" data-v-fe35c617><div class="text-center" data-v-fe35c617><div class="w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full animate-spin mx-auto mb-4" data-v-fe35c617></div><h3 class="text-lg font-semibold text-gray-900 mb-2" data-v-fe35c617> Loading Services </h3><p class="text-gray-600" data-v-fe35c617>Please wait while we load our services...</p></div></div>`);
      } else if ($setup.effectiveServiceCategories.length > 0) {
        _push(`<!--[--><!-- Services content --><div class="grid grid-cols-2 lg:grid-cols-3 gap-x-8 gap-y-6 pb-8" data-v-fe35c617><!--[-->`);
        ssrRenderList($setup.effectiveServiceCategories.slice(0, 6), (category) => {
          _push(`<div class="flex flex-col space-y-2" data-v-fe35c617><!-- Category header --><div class="border-b border-gray-200 pb-3" data-v-fe35c617><h3 class="text-lg font-semibold text-black" data-v-fe35c617>${ssrInterpolate(category.title)}</h3></div><!-- Category services --><div class="space-y-1" data-v-fe35c617><!--[-->`);
          ssrRenderList(category.items, (service) => {
            _push(`<a${ssrRenderAttr("href", service.url)} class="block py-2 text-gray-700 hover:text-black hover:bg-blue-50 transition-colors border-l-4 border-l-transparent hover:border-l-blue-400" data-v-fe35c617><div class="text-base font-medium pl-3" data-v-fe35c617>${ssrInterpolate(service.title)}</div></a>`);
          });
          _push(`<!--]--></div></div>`);
        });
        _push(`<!--]--></div><!--]-->`);
      } else {
        _push(`<!--[--><!-- Error state --><div class="flex items-center justify-center h-64" data-v-fe35c617><div class="text-center" data-v-fe35c617><h3 class="text-lg font-semibold text-gray-900 mb-2" data-v-fe35c617> Services Temporarily Unavailable </h3><p class="text-gray-600 mb-4" data-v-fe35c617> We&#39;re unable to load service information at the moment. </p><button class="px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded transition-colors"${ssrIncludeBooleanAttr($setup.isLoadingServices) ? " disabled" : ""} data-v-fe35c617>${ssrInterpolate($setup.isLoadingServices ? "Retrying..." : "Retry")}</button>`);
        if ($setup.apiError) {
          _push(`<p class="text-xs text-red-600 mt-2" data-v-fe35c617>${ssrInterpolate($setup.apiError)}</p>`);
        } else {
          _push(`<!---->`);
        }
        _push(`</div></div><!--]-->`);
      }
      _push(`</div>`);
    } else if ($setup.activeDropdown === "Patient Resources") {
      _push(`<!--[--><!-- Patient Resources 3-column layout --><div class="w-full h-full" data-v-fe35c617><div class="grid grid-cols-3 gap-x-8 gap-y-6 pb-8" data-v-fe35c617><!-- Treatment Care --><div class="flex flex-col space-y-2" data-v-fe35c617><div class="border-b border-gray-200 pb-3" data-v-fe35c617><h3 class="text-lg font-semibold text-black" data-v-fe35c617>Treatment Care</h3></div><div class="space-y-1" data-v-fe35c617><a href="/patient-resources/pre-treatment" class="block py-2 text-gray-700 hover:text-black hover:bg-green-50 transition-colors border-l-4 border-l-transparent hover:border-l-green-400" data-v-fe35c617><div class="text-base font-medium pl-3" data-v-fe35c617> Pre-Treatment Preparation </div></a><a href="/patient-resources/post-treatment" class="block py-2 text-gray-700 hover:text-black hover:bg-green-50 transition-colors border-l-4 border-l-transparent hover:border-l-green-400" data-v-fe35c617><div class="text-base font-medium pl-3" data-v-fe35c617>Post-Treatment Care</div></a></div></div><!-- Patient Support --><div class="flex flex-col space-y-2" data-v-fe35c617><div class="border-b border-gray-200 pb-3" data-v-fe35c617><h3 class="text-lg font-semibold text-black" data-v-fe35c617>Patient Support</h3></div><div class="space-y-1" data-v-fe35c617><a href="/patient-resources/forms" class="block py-2 text-gray-700 hover:text-black hover:bg-green-50 transition-colors border-l-4 border-l-transparent hover:border-l-green-400" data-v-fe35c617><div class="text-base font-medium pl-3" data-v-fe35c617>Patient Forms</div></a><a href="/patient-resources/support-groups" class="block py-2 text-gray-700 hover:text-black hover:bg-green-50 transition-colors border-l-4 border-l-transparent hover:border-l-green-400" data-v-fe35c617><div class="text-base font-medium pl-3" data-v-fe35c617>Support Groups</div></a></div></div><!-- Financial Services --><div class="flex flex-col space-y-2" data-v-fe35c617><div class="border-b border-gray-200 pb-3" data-v-fe35c617><h3 class="text-lg font-semibold text-black" data-v-fe35c617>Financial Services</h3></div><div class="space-y-1" data-v-fe35c617><a href="/patient-resources/standard-charges" class="block py-2 text-gray-700 hover:text-black hover:bg-green-50 transition-colors border-l-4 border-l-transparent hover:border-l-green-400" data-v-fe35c617><div class="text-base font-medium pl-3" data-v-fe35c617>Standard Charges</div></a><a href="/patient-resources/insurance-billing" class="block py-2 text-gray-700 hover:text-black hover:bg-green-50 transition-colors border-l-4 border-l-transparent hover:border-l-green-400" data-v-fe35c617><div class="text-base font-medium pl-3" data-v-fe35c617>Insurance &amp; Billing</div></a><a href="/patient-resources/financial-options" class="block py-2 text-gray-700 hover:text-black hover:bg-green-50 transition-colors border-l-4 border-l-transparent hover:border-l-green-400" data-v-fe35c617><div class="text-base font-medium pl-3" data-v-fe35c617>Financial Options</div></a></div></div></div></div><!--]-->`);
    } else if ($setup.activeDropdown === "Community") {
      _push(`<!--[--><!-- Community Layout --><div class="w-full h-full" data-v-fe35c617><div class="flex flex-col lg:grid lg:grid-cols-2 lg:gap-8 h-full" data-v-fe35c617><!-- Menu items --><div class="space-y-1 mb-8 lg:mb-0 lg:order-2" data-v-fe35c617><!--[-->`);
      ssrRenderList($setup.communityMenuItems, (subItem) => {
        _push(`<a${ssrRenderAttr("href", subItem.url)} class="block py-2 text-gray-700 hover:text-black hover:bg-amber-50 transition-colors border-l-4 border-l-transparent hover:border-l-amber-400" data-v-fe35c617><div class="text-base font-medium pl-3" data-v-fe35c617>${ssrInterpolate(subItem.title)}</div></a>`);
      });
      _push(`<!--]--></div><!-- Featured content placeholder --><div class="space-y-6 pt-6 lg:pt-0 border-t lg:border-t-0 border-gray-200 lg:order-1" data-v-fe35c617><div class="p-4 sm:p-6 rounded border border-gray-200" data-v-fe35c617><div class="h-36 sm:h-44 flex flex-col justify-between" data-v-fe35c617><div class="flex-shrink-0" data-v-fe35c617><span class="text-xs font-semibold text-amber-600 uppercase tracking-wide mb-3 block" data-v-fe35c617>Featured Content</span><h3 class="text-lg font-bold text-gray-900 line-clamp-2 mt-1 mb-2 leading-tight" data-v-fe35c617> Community Updates Coming Soon </h3></div><div class="flex-shrink-0 border-t border-gray-200 pt-3" data-v-fe35c617><div class="space-y-2 text-xs" data-v-fe35c617><div class="flex justify-between" data-v-fe35c617><span class="font-medium text-gray-600" data-v-fe35c617>Status:</span><span class="text-xs text-gray-700" data-v-fe35c617>In Development</span></div></div></div></div></div></div></div></div><!--]-->`);
    } else if ($setup.activeDropdown === "Company") {
      _push(`<!--[--><!-- Company Layout --><div class="w-full h-full" data-v-fe35c617><div class="flex flex-col lg:grid lg:grid-cols-2 lg:gap-8 h-full" data-v-fe35c617><!-- Menu items --><div class="space-y-1 mb-8 lg:mb-0 lg:order-2" data-v-fe35c617><!--[-->`);
      ssrRenderList($setup.companyMenuItems, (subItem) => {
        _push(`<a${ssrRenderAttr("href", subItem.url)} class="block py-2 text-gray-700 hover:text-black hover:bg-purple-50 transition-colors border-l-4 border-l-transparent hover:border-l-purple-400" data-v-fe35c617><div class="text-base font-medium pl-3" data-v-fe35c617>${ssrInterpolate(subItem.title)}</div></a>`);
      });
      _push(`<!--]--></div><!-- Featured content placeholder --><div class="space-y-6 pt-6 lg:pt-0 border-t lg:border-t-0 border-gray-200 lg:order-1" data-v-fe35c617><div class="p-4 sm:p-6 border border-gray-200 rounded" data-v-fe35c617><div class="h-36 sm:h-44 flex flex-col justify-between" data-v-fe35c617><div class="flex-shrink-0" data-v-fe35c617><span class="text-xs font-semibold text-purple-600 uppercase tracking-wide mb-3 block" data-v-fe35c617>Featured News</span><h3 class="text-lg font-bold text-gray-900 line-clamp-2 mt-1 mb-2 leading-tight" data-v-fe35c617> Company News Coming Soon </h3></div><div class="flex-shrink-0 border-t border-gray-200 pt-3" data-v-fe35c617><div class="space-y-2 text-xs" data-v-fe35c617><div class="flex justify-between" data-v-fe35c617><span class="font-medium text-gray-600" data-v-fe35c617>Status:</span><span class="text-xs text-gray-700" data-v-fe35c617>In Development</span></div></div></div></div></div></div></div></div><!--]-->`);
    } else {
      _push(`<!---->`);
    }
    _push(`</div></div></div></div></div></div></div></div>`);
  } else {
    _push(`<!---->`);
  }
  _push(`</nav><!-- Search Overlay -->`);
  if ($setup.isSearchOpen) {
    _push(`<div class="fixed inset-0 z-50 bg-black/50 flex items-start justify-center pt-20" data-v-fe35c617><div class="w-full max-w-2xl mx-4 bg-white rounded-lg shadow-xl animate-in fade-in-0 slide-in-from-top-4 duration-200" data-v-fe35c617><div class="p-4" data-v-fe35c617><div class="flex items-center justify-between mb-4" data-v-fe35c617><h3 class="text-lg font-semibold text-gray-900" data-v-fe35c617>Search</h3><button class="flex items-center justify-center w-8 h-8 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded transition-colors" aria-label="Close search" data-v-fe35c617>`);
    _push(ssrRenderComponent($setup["X"], { class: "w-5 h-5" }, null, _parent));
    _push(`</button></div><div class="space-y-2" data-v-fe35c617><input type="text" placeholder="Search treatments, articles, and case studies..." class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none" data-v-fe35c617><p class="text-sm text-gray-600" data-v-fe35c617>Search functionality coming soon...</p></div></div></div></div>`);
  } else {
    _push(`<!---->`);
  }
  _push(`<!-- Mobile Navigation --><div class="${ssrRenderClass(`fixed inset-0 z-40 md:hidden ${$setup.isMobileMenuOpen ? "opacity-100 visible" : "opacity-0 invisible"}`)}" data-v-fe35c617><div class="absolute inset-0 bg-black/50" data-v-fe35c617></div><div class="${ssrRenderClass(`fixed inset-0 bg-white ${$setup.isMobileMenuOpen ? "translate-y-0 opacity-100" : "-translate-y-full opacity-0"}`)}" style="${ssrRenderStyle({ "top": "64px" })}" data-v-fe35c617><div class="mobile-nav-container w-full h-full overflow-y-auto overflow-x-hidden" data-v-fe35c617><div class="mobile-nav-top-section pt-4 px-4 pb-4 border-b border-gray-200 bg-gray-50" data-v-fe35c617><div class="text-xs text-black text-center mb-2 font-medium" data-v-fe35c617>Emergency? Call 911</div><div class="flex rounded border border-gray-200 mb-3 overflow-hidden" data-v-fe35c617><a href="tel:+1-561-555-0123" class="flex-1 flex items-center justify-center gap-1.5 py-3 px-3 text-sm font-medium text-black bg-white hover:bg-gray-100 border-r border-gray-200" data-v-fe35c617>`);
  _push(ssrRenderComponent($setup["Smartphone"], { class: "w-4 h-4 flex-shrink-0" }, null, _parent));
  _push(`<span class="leading-none" data-v-fe35c617>(561) 555-0123</span></a><a href="/community/donations" class="flex-1 flex items-center justify-center gap-1.5 py-3 px-3 text-sm font-medium text-black bg-white hover:bg-gray-100" data-v-fe35c617>`);
  _push(ssrRenderComponent($setup["Heart"], { class: "w-4 h-4 flex-shrink-0" }, null, _parent));
  _push(`<span class="leading-none" data-v-fe35c617>Donations</span></a></div><a href="/appointment" class="flex items-center justify-center gap-1.5 w-full py-3 px-4 text-sm font-medium text-white bg-black hover:bg-gray-800 rounded transition-colors duration-200" data-v-fe35c617>`);
  _push(ssrRenderComponent($setup["Calendar"], { class: "w-4 h-4 flex-shrink-0" }, null, _parent));
  _push(`<span class="leading-none" data-v-fe35c617>Book Appointment</span></a></div><!-- Mobile Menu Items --><!--[-->`);
  ssrRenderList($setup.menu, (item) => {
    _push(`<!--[-->`);
    if (item.items) {
      _push(`<div data-v-fe35c617><div class="${ssrRenderClass(`sticky top-0 z-10 flex items-center w-full p-4 text-left text-white text-lg font-medium ${item.title === "Company" ? "bg-purple-600" : item.title === "Community" ? "bg-amber-600" : item.title === "Patient Resources" ? "bg-green-600" : "bg-blue-600"}`)}" data-v-fe35c617>${ssrInterpolate(item.title)}</div><div data-v-fe35c617><!-- Services Mobile Menu -->`);
      if (item.title === "Services") {
        _push(`<!--[--><!-- Loading state for mobile -->`);
        if ($setup.isLoadingServices) {
          _push(`<div class="border-b border-gray-200" data-v-fe35c617><div class="px-4 py-6 text-center text-gray-600" data-v-fe35c617><div class="w-6 h-6 border-4 border-blue-600 border-t-transparent rounded-full animate-spin mx-auto mb-2" data-v-fe35c617></div><div class="text-base font-medium" data-v-fe35c617>Loading Services...</div></div></div>`);
        } else if ($setup.effectiveServiceCategories.length > 0) {
          _push(`<!--[--><!-- Services content for mobile --><!--[-->`);
          ssrRenderList($setup.effectiveServiceCategories, (category) => {
            _push(`<div data-v-fe35c617><div class="border-b border-gray-200" data-v-fe35c617><div class="px-4 py-4 text-black text-base font-medium bg-gray-100" data-v-fe35c617>${ssrInterpolate(category.title)}</div></div><!--[-->`);
            ssrRenderList(category.items, (service) => {
              _push(`<div class="border-b border-gray-200" data-v-fe35c617><a${ssrRenderAttr("href", service.url)} class="block px-4 py-4 text-black text-base" data-v-fe35c617>${ssrInterpolate(service.title)}</a></div>`);
            });
            _push(`<!--]--></div>`);
          });
          _push(`<!--]--><div class="border-b border-gray-200" data-v-fe35c617><a href="/services" class="block px-4 py-4 text-black text-base font-semibold" data-v-fe35c617> View All Services </a></div><!--]-->`);
        } else {
          _push(`<!--[--><!-- Error state for mobile --><div class="border-b border-gray-200" data-v-fe35c617><div class="px-4 py-6 text-center text-gray-600" data-v-fe35c617><div class="text-base font-medium mb-2" data-v-fe35c617>Services Temporarily Unavailable</div><div class="text-sm mb-3" data-v-fe35c617>Unable to load service information</div><button class="px-3 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded transition-colors"${ssrIncludeBooleanAttr($setup.isLoadingServices) ? " disabled" : ""} data-v-fe35c617>${ssrInterpolate($setup.isLoadingServices ? "Retrying..." : "Retry")}</button></div></div><!--]-->`);
        }
        _push(`<!--]-->`);
      } else {
        _push(`<!--[--><!-- Other menu items --><!--[-->`);
        ssrRenderList(item.items, (subItem) => {
          _push(`<div class="border-b border-gray-200" data-v-fe35c617><a${ssrRenderAttr("href", subItem.url)} class="block px-4 py-4 text-black text-base" data-v-fe35c617>${ssrInterpolate(subItem.title)}</a></div>`);
        });
        _push(`<!--]--><!-- Action items for each section -->`);
        if (item.title === "Patient Resources") {
          _push(`<div class="border-b border-gray-200" data-v-fe35c617><a href="/patient-resources/portal" class="block px-4 py-4 text-black text-base font-semibold" data-v-fe35c617> Patient Portal </a></div>`);
        } else {
          _push(`<!---->`);
        }
        if (item.title === "Community") {
          _push(`<div class="border-b border-gray-200" data-v-fe35c617><a href="/community/learning-portal" class="block px-4 py-4 text-black text-base font-semibold" data-v-fe35c617> Learning Portal </a></div>`);
        } else {
          _push(`<!---->`);
        }
        if (item.title === "Company") {
          _push(`<div class="border-b border-gray-200" data-v-fe35c617><a href="/company/contact" class="block px-4 py-4 text-black text-base font-semibold" data-v-fe35c617> Contact Us </a></div>`);
        } else {
          _push(`<!---->`);
        }
        _push(`<!--]-->`);
      }
      _push(`</div></div>`);
    } else {
      _push(`<div data-v-fe35c617><a${ssrRenderAttr("href", item.url)} class="block p-4 text-black text-base font-semibold transition-colors w-full text-left border-b border-gray-200" data-v-fe35c617>${ssrInterpolate(item.title)}</a></div>`);
    }
    _push(`<!--]-->`);
  });
  _push(`<!--]--></div></div></div></div>`);
}
const _sfc_setup$1 = _sfc_main$1.setup;
_sfc_main$1.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/NavigationMenu.vue");
  return _sfc_setup$1 ? _sfc_setup$1(props, ctx) : void 0;
};
const NavigationMenu = /* @__PURE__ */ _export_sfc(_sfc_main$1, [["ssrRender", _sfc_ssrRender$1], ["__scopeId", "data-v-fe35c617"], ["__file", "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/NavigationMenu.vue"]]);
const _sfc_main = /* @__PURE__ */ defineComponent({
  __name: "Footer",
  setup(__props, { expose: __expose }) {
    __expose();
    const serviceCategories = ref([]);
    const isLoadingServices = ref(false);
    const expandedCategory = ref(null);
    const toggleCategory = (categoryTitle) => {
      expandedCategory.value = expandedCategory.value === categoryTitle ? null : categoryTitle;
    };
    async function loadFooterServices() {
      if (isLoadingServices.value) return;
      isLoadingServices.value = true;
      try {
        const { loadNavigationData } = await import("./navigation-data.CUpNFYc2.js");
        const navigationData = await loadNavigationData();
        serviceCategories.value = navigationData.footerCategories || [];
        console.log(`✅ Footer loaded ${serviceCategories.value.length} service categories`);
      } catch (error) {
        console.warn("❌ Failed to load footer services:", error);
        serviceCategories.value = [];
      } finally {
        isLoadingServices.value = false;
      }
    }
    onMounted(() => {
      loadFooterServices();
    });
    const XIcon = {
      template: `
    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
      <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/>
    </svg>
  `
    };
    const socialLinks = computed(() => [
      { icon: Linkedin, href: "#", label: "LinkedIn" },
      { icon: Facebook, href: "#", label: "Facebook" },
      { icon: Instagram, href: "#", label: "Instagram" },
      { icon: XIcon, href: "#", label: "X" }
    ]);
    const communityLinks = [
      { name: "About Our Values", href: "/community" },
      { name: "Research & Innovation", href: "/community/research" },
      { name: "Certification Programs", href: "/community/certification-programs" },
      { name: "Donations", href: "/community/donations" },
      { name: "Volunteer", href: "/community/volunteer" },
      { name: "Events", href: "/community/events" }
    ];
    const patientResourcesLinks = [
      { name: "Pre-Treatment Preparation", href: "/patient-resources/pre-treatment" },
      { name: "Post-Treatment Care", href: "/patient-resources/post-treatment" },
      { name: "Patient Forms", href: "/patient-resources/forms" },
      { name: "Support Groups", href: "/patient-resources/support-groups" },
      { name: "Standard Charges", href: "/patient-resources/standard-charges" },
      { name: "Insurance & Billing", href: "/patient-resources/insurance-billing" },
      { name: "Financial Options", href: "/patient-resources/financial-options" }
    ];
    const companyLinks = [
      { name: "About Our Team", href: "/company/team" },
      { name: "News & Insights", href: "/company/news" },
      { name: "Credentials", href: "/company/credentials" },
      { name: "Careers", href: "/company/careers" }
    ];
    const contactInfo = {
      mainPhone: "(561) 555-0123",
      alternatePhone: "+1 (561) 555-5555",
      email: "info@internationalcenter.com",
      address: {
        street: "1234 Medical Plaza Drive",
        city: "Boynton Beach",
        state: "FL",
        zip: "33426"
      }
    };
    const __returned__ = { serviceCategories, isLoadingServices, expandedCategory, toggleCategory, loadFooterServices, XIcon, socialLinks, communityLinks, patientResourcesLinks, companyLinks, contactInfo, get FileText() {
      return FileText;
    }, get ChevronDown() {
      return ChevronDown;
    }, get IdCard() {
      return IdCard;
    }, get Calendar() {
      return Calendar;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
function _sfc_ssrRender(_ctx, _push, _parent, _attrs, $props, $setup, $data, $options) {
  _push(`<footer${ssrRenderAttrs(mergeProps({ class: "bg-black text-white" }, _attrs))}><div class="container"><!-- Desktop Layout --><div class="hidden xl:block"><!-- Main Footer Content --><div class="py-8 border-b border-gray-800"><div class="container"><!-- Desktop Layout --><div class="flex items-start gap-8"><!-- Contact Information - Far Left --><div class="flex-shrink-0 w-80 max-w-sm"><!-- Company Logo and Name --><div class="mb-6"><a href="/" class="flex items-center gap-3 hover:opacity-80 transition-opacity"><svg width="32" height="32" viewBox="0 0 286 326" class="w-8 h-8 text-blue-600" xmlns="http://www.w3.org/2000/svg"><path d="M266.006 -9.15527e-05C277.052 -9.15527e-05 286.006 8.95421 286.006 19.9999C286.006 63.3016 262.147 96.1501 235.27 120.882C208.842 145.199 175.696 164.877 154.286 179.512C133.026 194.044 104.6 210.659 80.1709 232.959C68.9387 243.212 59.5065 253.86 52.6094 265H233.995C241.496 277.403 245.806 290.607 245.997 305H40.0098C40.0049 305.333 40 305.666 40 306C40 317.046 31.0457 326 20 326C8.95431 326 0 317.046 0 306C3.61802e-05 262.353 25.8692 228.368 53.2041 203.416C80.3999 178.591 113.474 158.957 131.714 146.489C148.121 135.274 166.244 124.141 183.51 111.5H102.705C95.0362 105.785 87.4268 99.6644 80.1709 93.0409C76.0176 89.2496 72.1106 85.404 68.4863 81.4999H218.233C235.492 62.9951 246.006 43.0175 246.006 19.9999C246.006 8.95421 254.96 -9.15527e-05 266.006 -9.15527e-05Z" fill="currentColor" fill-opacity="0.75"></path><path d="M198.562 176.105C211.057 184.88 223.797 194.561 235.27 205.118C262.147 229.85 286.006 262.698 286.006 306C286.006 317.046 277.052 326 266.006 326C254.96 326 246.006 317.046 246.006 306C246.006 282.982 235.492 263.005 218.233 244.5H68.4863C72.1106 240.596 76.0176 236.75 80.1709 232.959C87.4272 226.335 95.0379 220.215 102.707 214.5H183.514C176.716 209.523 169.785 204.78 162.878 200.174L198.562 176.105ZM20 0C31.0457 0 40 8.95431 40 20C40 20.3338 40.0049 20.6671 40.0098 21H245.997C245.806 35.393 241.496 48.5969 233.995 61H52.6094C59.5065 72.1396 68.9387 82.7879 80.1709 93.041C100.482 111.581 123.555 126.194 142.976 138.945C139.155 141.468 135.391 143.976 131.714 146.489C125.025 151.062 116.341 156.598 106.683 163.001C90.0062 151.944 70.4265 138.305 53.2041 122.584C25.8692 97.6318 3.61802e-05 63.6471 0 20C0 8.95431 8.95431 0 20 0Z" fill="currentColor"></path></svg><div class="flex flex-col"><h2 class="text-xl font-bold text-white leading-tight">International Center</h2><span class="text-xs font-medium text-gray-400 leading-tight tracking-wide"> for Regenerative Medicine </span></div></a></div><div class="bg-gray-900/50 rounded p-6"><table class="w-full text-sm"><thead><tr><th colspan="2" class="font-semibold text-white pb-4 text-left text-lg"> Contact Information </th></tr></thead><tbody><tr class="border-b border-gray-800"><td class="font-medium text-gray-300 py-3 pr-4">Phone</td><td class="py-3 text-right"><a${ssrRenderAttr("href", `tel:${$setup.contactInfo.mainPhone}`)} class="text-gray-300 hover:text-white transition-colors font-medium">${ssrInterpolate($setup.contactInfo.mainPhone)}</a></td></tr><tr class="border-b border-gray-800"><td class="font-medium text-gray-300 py-3 pr-4">Email</td><td class="py-3 text-right"><a${ssrRenderAttr("href", `mailto:${$setup.contactInfo.email}`)} class="text-gray-300 hover:text-white transition-colors">${ssrInterpolate($setup.contactInfo.email)}</a></td></tr><tr class="last:border-b-0"><td class="font-medium text-gray-300 pt-3 pr-4">Address</td><td class="pt-3 text-right"><div class="text-gray-300 text-right"><div>${ssrInterpolate($setup.contactInfo.address.street)}</div><div>${ssrInterpolate($setup.contactInfo.address.city)}, ${ssrInterpolate($setup.contactInfo.address.state)} ${ssrInterpolate($setup.contactInfo.address.zip)}</div></div></td></tr></tbody></table></div></div><!-- Services, Patient Resources, Community, and Company - Evenly spaced in remaining area --><div class="flex-1 flex justify-evenly items-start min-w-0"><!-- Services with Dropdowns --><div class="w-52 flex-shrink-0"><h3 class="font-semibold text-white mb-6 text-lg">Services</h3><div class="space-y-4"><!-- Service Categories with Dropdowns --><div class="space-y-3"><!-- Loading state -->`);
  if ($setup.isLoadingServices) {
    _push(`<div class="text-center py-4"><p class="text-gray-400 text-sm">Loading services...</p></div>`);
  } else if ($setup.serviceCategories.length > 0) {
    _push(`<!--[--><!-- Services content --><!--[-->`);
    ssrRenderList($setup.serviceCategories, (category) => {
      _push(`<div><button class="${ssrRenderClass(`flex items-center justify-between w-full text-left hover:text-white transition-colors font-medium ${$setup.expandedCategory === category.title ? "text-white" : "text-gray-400"}`)}"><span>${ssrInterpolate(category.title)}</span>`);
      _push(ssrRenderComponent($setup["ChevronDown"], {
        class: `w-4 h-4 transition-transform ${$setup.expandedCategory === category.title ? "rotate-180" : ""}`
      }, null, _parent));
      _push(`</button>`);
      if ($setup.expandedCategory === category.title) {
        _push(`<div class="mt-2 space-y-3"><!--[-->`);
        ssrRenderList(category.services, (service) => {
          _push(`<a${ssrRenderAttr("href", service.href)} class="flex items-center text-gray-400 hover:text-white transition-colors font-normal"><span class="text-blue-600 mr-2 text-sm font-bold">—</span> ${ssrInterpolate(service.name)}</a>`);
        });
        _push(`<!--]--></div>`);
      } else {
        _push(`<!---->`);
      }
      _push(`</div>`);
    });
    _push(`<!--]--><!--]-->`);
  } else {
    _push(`<!--[--><!-- Error state --><div class="text-center py-4"><p class="text-gray-400 text-sm">Services temporarily unavailable</p></div><!--]-->`);
  }
  _push(`</div>`);
  if ($setup.serviceCategories.length > 0) {
    _push(`<a href="/services" class="block text-gray-300 hover:text-white transition-colors font-semibold mt-4"> View All Services </a>`);
  } else {
    _push(`<!---->`);
  }
  _push(`</div></div><!-- Patient Resources --><div class="w-52 flex-shrink-0 ml-8"><h3 class="font-semibold text-white mb-6 text-lg">Patient Resources</h3><ul class="space-y-3 mb-4"><!--[-->`);
  ssrRenderList($setup.patientResourcesLinks, (link, idx) => {
    _push(`<li><a${ssrRenderAttr("href", link.href)} class="text-gray-400 hover:text-white transition-colors">${ssrInterpolate(link.name)}</a></li>`);
  });
  _push(`<!--]--></ul><a href="/patient-resources/portal" class="block text-gray-300 hover:text-white transition-colors font-semibold"> Patient Portal </a></div><!-- Community --><div class="w-48 flex-shrink-0"><h3 class="font-semibold text-white mb-6 text-lg">Community</h3><ul class="space-y-3 mb-4"><!--[-->`);
  ssrRenderList($setup.communityLinks, (link, idx) => {
    _push(`<li><a${ssrRenderAttr("href", link.href)} class="text-gray-400 hover:text-white transition-colors">${ssrInterpolate(link.name)}</a></li>`);
  });
  _push(`<!--]--></ul><a href="/community/learning-portal" class="block text-gray-300 hover:text-white transition-colors font-semibold"> Learning Portal </a></div><!-- Company --><div class="w-44 flex-shrink-0"><h3 class="font-semibold text-white mb-6 text-lg">Company</h3><ul class="space-y-3 mb-4"><!--[-->`);
  ssrRenderList($setup.companyLinks, (link, idx) => {
    _push(`<li><a${ssrRenderAttr("href", link.href)} class="text-gray-400 hover:text-white transition-colors">${ssrInterpolate(link.name)}</a></li>`);
  });
  _push(`<!--]--></ul><a href="/company/contact" class="block text-gray-300 hover:text-white transition-colors font-semibold"> Contact Us </a></div></div></div></div></div></div><!-- Mobile and Tablet Layout --><div class="xl:hidden"><!-- Mobile First: Contact Information (Company Logo and Name) --><div class="md:hidden py-8 border-b border-gray-800"><!-- Company Logo and Name --><div class="mb-6"><a href="/" class="flex items-center gap-3 hover:opacity-80 transition-opacity"><svg width="32" height="32" viewBox="0 0 286 326" class="w-8 h-8 text-blue-600" xmlns="http://www.w3.org/2000/svg"><path d="M266.006 -9.15527e-05C277.052 -9.15527e-05 286.006 8.95421 286.006 19.9999C286.006 63.3016 262.147 96.1501 235.27 120.882C208.842 145.199 175.696 164.877 154.286 179.512C133.026 194.044 104.6 210.659 80.1709 232.959C68.9387 243.212 59.5065 253.86 52.6094 265H233.995C241.496 277.403 245.806 290.607 245.997 305H40.0098C40.0049 305.333 40 305.666 40 306C40 317.046 31.0457 326 20 326C8.95431 326 0 317.046 0 306C3.61802e-05 262.353 25.8692 228.368 53.2041 203.416C80.3999 178.591 113.474 158.957 131.714 146.489C148.121 135.274 166.244 124.141 183.51 111.5H102.705C95.0362 105.785 87.4268 99.6644 80.1709 93.0409C76.0176 89.2496 72.1106 85.404 68.4863 81.4999H218.233C235.492 62.9951 246.006 43.0175 246.006 19.9999C246.006 8.95421 254.96 -9.15527e-05 266.006 -9.15527e-05Z" fill="currentColor" fill-opacity="0.75"></path><path d="M198.562 176.105C211.057 184.88 223.797 194.561 235.27 205.118C262.147 229.85 286.006 262.698 286.006 306C286.006 317.046 277.052 326 266.006 326C254.96 326 246.006 317.046 246.006 306C246.006 282.982 235.492 263.005 218.233 244.5H68.4863C72.1106 240.596 76.0176 236.75 80.1709 232.959C87.4272 226.335 95.0379 220.215 102.707 214.5H183.514C176.716 209.523 169.785 204.78 162.878 200.174L198.562 176.105ZM20 0C31.0457 0 40 8.95431 40 20C40 20.3338 40.0049 20.6671 40.0098 21H245.997C245.806 35.393 241.496 48.5969 233.995 61H52.6094C59.5065 72.1396 68.9387 82.7879 80.1709 93.041C100.482 111.581 123.555 126.194 142.976 138.945C139.155 141.468 135.391 143.976 131.714 146.489C125.025 151.062 116.341 156.598 106.683 163.001C90.0062 151.944 70.4265 138.305 53.2041 122.584C25.8692 97.6318 3.61802e-05 63.6471 0 20C0 8.95431 8.95431 0 20 0Z" fill="currentColor"></path></svg><div class="flex flex-col"><h2 class="text-xl font-bold text-white leading-tight">International Center</h2><span class="text-xs font-medium text-gray-400 leading-tight tracking-wide"> for Regenerative Medicine </span></div></a></div><div class="bg-gray-900/50 rounded p-6"><table class="w-full text-sm"><thead><tr><th colspan="2" class="font-semibold text-white pb-4 text-left text-lg"> Contact Information </th></tr></thead><tbody><tr class="border-b border-gray-800"><td class="font-medium text-gray-300 py-3 pr-4">Phone</td><td class="py-3 text-right"><a${ssrRenderAttr("href", `tel:${$setup.contactInfo.mainPhone}`)} class="text-gray-300 md:hover:text-white transition-colors font-medium">${ssrInterpolate($setup.contactInfo.mainPhone)}</a></td></tr><tr class="border-b border-gray-800"><td class="font-medium text-gray-300 py-3 pr-4">Email</td><td class="py-3 text-right"><a${ssrRenderAttr("href", `mailto:${$setup.contactInfo.email}`)} class="text-gray-300 md:hover:text-white transition-colors">${ssrInterpolate($setup.contactInfo.email)}</a></td></tr><tr class="last:border-b-0"><td class="font-medium text-gray-300 pt-3 pr-4">Address</td><td class="pt-3 text-right"><div class="text-gray-300 text-right"><div>${ssrInterpolate($setup.contactInfo.address.street)}</div><div>${ssrInterpolate($setup.contactInfo.address.city)}, ${ssrInterpolate($setup.contactInfo.address.state)} ${ssrInterpolate($setup.contactInfo.address.zip)}</div></div></td></tr></tbody></table></div></div><!-- Mobile: Services Section --><div class="md:hidden py-8 border-b border-gray-800"><div><h3 class="font-semibold text-white mb-6 text-lg">Services</h3><div class="space-y-4"><!-- Service Categories with Dropdowns --><div class="space-y-3"><!-- Loading state -->`);
  if ($setup.isLoadingServices) {
    _push(`<div class="text-center py-4"><p class="text-gray-400 text-sm">Loading services...</p></div>`);
  } else if ($setup.serviceCategories.length > 0) {
    _push(`<!--[--><!-- Services content --><!--[-->`);
    ssrRenderList($setup.serviceCategories, (category) => {
      _push(`<div><button class="${ssrRenderClass(`flex items-center justify-between w-full text-left py-2 md:hover:text-white transition-colors ${$setup.expandedCategory === category.title ? "text-white" : "text-gray-400"}`)}"><span>${ssrInterpolate(category.title)}</span>`);
      _push(ssrRenderComponent($setup["ChevronDown"], {
        class: `w-4 h-4 transition-transform ${$setup.expandedCategory === category.title ? "rotate-180" : ""}`
      }, null, _parent));
      _push(`</button>`);
      if ($setup.expandedCategory === category.title) {
        _push(`<div class="mt-2 space-y-2"><!--[-->`);
        ssrRenderList(category.services, (service) => {
          _push(`<a${ssrRenderAttr("href", service.href)} class="flex items-center py-2 text-gray-400 md:hover:text-white transition-colors"><span class="text-blue-600 mr-2 text-sm font-bold">—</span> ${ssrInterpolate(service.name)}</a>`);
        });
        _push(`<!--]--></div>`);
      } else {
        _push(`<!---->`);
      }
      _push(`</div>`);
    });
    _push(`<!--]--><!--]-->`);
  } else {
    _push(`<!--[--><!-- Error state --><div class="text-center py-4"><p class="text-gray-400 text-sm">Services temporarily unavailable</p></div><!--]-->`);
  }
  _push(`</div>`);
  if ($setup.serviceCategories.length > 0) {
    _push(`<a href="/services" class="block text-gray-300 md:hover:text-white transition-colors font-semibold mt-4"> View All Services </a>`);
  } else {
    _push(`<!---->`);
  }
  _push(`</div></div></div><!-- Tablet Grid Layout --><div class="hidden md:block xl:hidden py-8"><!-- Top Row --><div class="grid grid-cols-2 gap-8 mb-8"><!-- Contact Information - Top Left --><div><!-- Company Logo and Name --><div class="mb-6"><a href="/" class="flex items-center gap-3 hover:opacity-80 transition-opacity"><svg width="32" height="32" viewBox="0 0 286 326" class="w-8 h-8 text-blue-600" xmlns="http://www.w3.org/2000/svg"><path d="M266.006 -9.15527e-05C277.052 -9.15527e-05 286.006 8.95421 286.006 19.9999C286.006 63.3016 262.147 96.1501 235.27 120.882C208.842 145.199 175.696 164.877 154.286 179.512C133.026 194.044 104.6 210.659 80.1709 232.959C68.9387 243.212 59.5065 253.86 52.6094 265H233.995C241.496 277.403 245.806 290.607 245.997 305H40.0098C40.0049 305.333 40 305.666 40 306C40 317.046 31.0457 326 20 326C8.95431 326 0 317.046 0 306C3.61802e-05 262.353 25.8692 228.368 53.2041 203.416C80.3999 178.591 113.474 158.957 131.714 146.489C148.121 135.274 166.244 124.141 183.51 111.5H102.705C95.0362 105.785 87.4268 99.6644 80.1709 93.0409C76.0176 89.2496 72.1106 85.404 68.4863 81.4999H218.233C235.492 62.9951 246.006 43.0175 246.006 19.9999C246.006 8.95421 254.96 -9.15527e-05 266.006 -9.15527e-05Z" fill="currentColor" fill-opacity="0.75"></path><path d="M198.562 176.105C211.057 184.88 223.797 194.561 235.27 205.118C262.147 229.85 286.006 262.698 286.006 306C286.006 317.046 277.052 326 266.006 326C254.96 326 246.006 317.046 246.006 306C246.006 282.982 235.492 263.005 218.233 244.5H68.4863C72.1106 240.596 76.0176 236.75 80.1709 232.959C87.4272 226.335 95.0379 220.215 102.707 214.5H183.514C176.716 209.523 169.785 204.78 162.878 200.174L198.562 176.105ZM20 0C31.0457 0 40 8.95431 40 20C40 20.3338 40.0049 20.6671 40.0098 21H245.997C245.806 35.393 241.496 48.5969 233.995 61H52.6094C59.5065 72.1396 68.9387 82.7879 80.1709 93.041C100.482 111.581 123.555 126.194 142.976 138.945C139.155 141.468 135.391 143.976 131.714 146.489C125.025 151.062 116.341 156.598 106.683 163.001C90.0062 151.944 70.4265 138.305 53.2041 122.584C25.8692 97.6318 3.61802e-05 63.6471 0 20C0 8.95431 8.95431 0 20 0Z" fill="currentColor"></path></svg><div class="flex flex-col"><h2 class="text-xl font-bold text-white leading-tight">International Center</h2><span class="text-xs font-medium text-gray-400 leading-tight tracking-wide"> for Regenerative Medicine </span></div></a></div><div class="bg-gray-900/50 rounded p-6"><table class="w-full text-sm"><thead><tr><th colspan="2" class="font-semibold text-white pb-4 text-left text-lg"> Contact Information </th></tr></thead><tbody><tr class="border-b border-gray-800"><td class="font-medium text-gray-300 py-3 pr-4">Phone</td><td class="py-3 text-right"><a${ssrRenderAttr("href", `tel:${$setup.contactInfo.mainPhone}`)} class="text-gray-300 md:hover:text-white transition-colors font-medium">${ssrInterpolate($setup.contactInfo.mainPhone)}</a></td></tr><tr class="border-b border-gray-800"><td class="font-medium text-gray-300 py-3 pr-4">Email</td><td class="py-3 text-right"><a${ssrRenderAttr("href", `mailto:${$setup.contactInfo.email}`)} class="text-gray-300 md:hover:text-white transition-colors">${ssrInterpolate($setup.contactInfo.email)}</a></td></tr><tr class="last:border-b-0"><td class="font-medium text-gray-300 pt-3 pr-4">Address</td><td class="pt-3 text-right"><div class="text-gray-300 text-right"><div>${ssrInterpolate($setup.contactInfo.address.street)}</div><div>${ssrInterpolate($setup.contactInfo.address.city)}, ${ssrInterpolate($setup.contactInfo.address.state)} ${ssrInterpolate($setup.contactInfo.address.zip)}</div></div></td></tr></tbody></table></div></div><!-- Services - Top Right --><div><h3 class="font-semibold text-white mb-6 text-lg">Services</h3><div class="space-y-4"><!-- Service Categories with Dropdowns --><div class="space-y-3"><!-- Loading state -->`);
  if ($setup.isLoadingServices) {
    _push(`<div class="text-center py-4"><p class="text-gray-400 text-sm">Loading services...</p></div>`);
  } else if ($setup.serviceCategories.length > 0) {
    _push(`<!--[--><!-- Services content --><!--[-->`);
    ssrRenderList($setup.serviceCategories, (category) => {
      _push(`<div><button class="${ssrRenderClass(`flex items-center justify-between w-full text-left md:hover:text-white transition-colors font-medium ${$setup.expandedCategory === category.title ? "text-white" : "text-gray-400"}`)}"><span>${ssrInterpolate(category.title)}</span>`);
      _push(ssrRenderComponent($setup["ChevronDown"], {
        class: `w-4 h-4 transition-transform ${$setup.expandedCategory === category.title ? "rotate-180" : ""}`
      }, null, _parent));
      _push(`</button>`);
      if ($setup.expandedCategory === category.title) {
        _push(`<div class="mt-2 space-y-3"><!--[-->`);
        ssrRenderList(category.services, (service) => {
          _push(`<a${ssrRenderAttr("href", service.href)} class="flex items-center text-gray-400 md:hover:text-white transition-colors font-normal"><span class="text-blue-600 mr-2 text-sm font-bold">—</span> ${ssrInterpolate(service.name)}</a>`);
        });
        _push(`<!--]--></div>`);
      } else {
        _push(`<!---->`);
      }
      _push(`</div>`);
    });
    _push(`<!--]--><!--]-->`);
  } else {
    _push(`<!--[--><!-- Error state --><div class="text-center py-4"><p class="text-gray-400 text-sm">Services temporarily unavailable</p></div><!--]-->`);
  }
  _push(`</div>`);
  if ($setup.serviceCategories.length > 0) {
    _push(`<a href="/services" class="block text-gray-300 md:hover:text-white transition-colors font-semibold mt-4"> View All Services </a>`);
  } else {
    _push(`<!---->`);
  }
  _push(`</div></div></div><!-- Horizontal Divider --><div class="border-t border-gray-800 mb-8"></div><!-- Bottom Row --><div class="flex justify-center gap-16 max-w-4xl mx-auto"><!-- Patient Resources - Bottom Left --><div class="flex-1"><h3 class="font-semibold text-white mb-6 text-lg">Patient Resources</h3><div><ul class="space-y-3 mb-4"><!--[-->`);
  ssrRenderList($setup.patientResourcesLinks, (link, idx) => {
    _push(`<li><a${ssrRenderAttr("href", link.href)} class="text-gray-400 md:hover:text-white transition-colors">${ssrInterpolate(link.name)}</a></li>`);
  });
  _push(`<!--]--></ul><a href="/patient-resources/portal" class="block text-gray-300 md:hover:text-white transition-colors font-semibold"> Patient Portal </a></div></div><!-- Community - Bottom Middle --><div class="flex-1"><h3 class="font-semibold text-white mb-6 text-lg">Community</h3><div><ul class="space-y-3 mb-4"><!--[-->`);
  ssrRenderList($setup.communityLinks, (link, idx) => {
    _push(`<li><a${ssrRenderAttr("href", link.href)} class="text-gray-400 md:hover:text-white transition-colors">${ssrInterpolate(link.name)}</a></li>`);
  });
  _push(`<!--]--></ul><a href="/community/learning-portal" class="block text-gray-300 md:hover:text-white transition-colors font-semibold"> Learning Portal </a></div></div><!-- Company - Bottom Right --><div class="flex-1"><h3 class="font-semibold text-white mb-6 text-lg">Company</h3><div><ul class="space-y-3 mb-4"><!--[-->`);
  ssrRenderList($setup.companyLinks, (link, idx) => {
    _push(`<li><a${ssrRenderAttr("href", link.href)} class="text-gray-400 md:hover:text-white transition-colors">${ssrInterpolate(link.name)}</a></li>`);
  });
  _push(`<!--]--></ul><a href="/company/contact" class="block text-gray-300 md:hover:text-white transition-colors font-semibold"> Contact Us </a></div></div></div></div><!-- Mobile: Patient Resources --><div class="md:hidden py-8 border-b border-gray-800"><h3 class="font-semibold text-white mb-6 text-lg">Patient Resources</h3><div><ul class="space-y-3 mb-4"><!--[-->`);
  ssrRenderList($setup.patientResourcesLinks, (link, idx) => {
    _push(`<li><a${ssrRenderAttr("href", link.href)} class="text-gray-400 md:hover:text-white transition-colors">${ssrInterpolate(link.name)}</a></li>`);
  });
  _push(`<!--]--></ul><a href="/patient-resources/portal" class="block text-gray-300 md:hover:text-white transition-colors font-semibold"> Patient Portal </a></div></div><!-- Mobile: Community --><div class="md:hidden py-8 border-b border-gray-800"><h3 class="font-semibold text-white mb-6 text-lg">Community</h3><div><ul class="space-y-3 mb-4"><!--[-->`);
  ssrRenderList($setup.communityLinks, (link, idx) => {
    _push(`<li><a${ssrRenderAttr("href", link.href)} class="text-gray-400 md:hover:text-white transition-colors">${ssrInterpolate(link.name)}</a></li>`);
  });
  _push(`<!--]--></ul><a href="/community/learning-portal" class="block text-gray-300 md:hover:text-white transition-colors font-semibold"> Learning Portal </a></div></div><!-- Mobile: Company --><div class="md:hidden py-8 border-b border-gray-800"><h3 class="font-semibold text-white mb-6 text-lg">Company</h3><div><ul class="space-y-3 mb-4"><!--[-->`);
  ssrRenderList($setup.companyLinks, (link, idx) => {
    _push(`<li><a${ssrRenderAttr("href", link.href)} class="text-gray-400 md:hover:text-white transition-colors">${ssrInterpolate(link.name)}</a></li>`);
  });
  _push(`<!--]--></ul><a href="/company/contact" class="block text-gray-300 md:hover:text-white transition-colors font-semibold"> Contact Us </a></div></div><!-- Mobile: Quick Access --><div class="md:hidden py-8"><h3 class="font-semibold text-white text-lg mb-4">Quick Access</h3><div class="flex flex-col gap-3"><a href="/patient-resources/portal" class="flex items-center gap-2 px-4 py-3 text-sm font-medium text-gray-900 bg-white hover:bg-gray-100 rounded transition-colors border border-gray-300 justify-center">`);
  _push(ssrRenderComponent($setup["IdCard"], { class: "w-4 h-4" }, null, _parent));
  _push(` Patient Portal </a><a href="/appointment" class="flex items-center gap-2 px-4 py-3 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded transition-colors justify-center">`);
  _push(ssrRenderComponent($setup["Calendar"], { class: "w-4 h-4" }, null, _parent));
  _push(` Book Appointment </a></div></div></div><!-- Footer Bottom --><div class="py-8 border-t border-gray-800"><div class="flex flex-col md:flex-row justify-between items-center gap-6"><!-- Legal Links --><div class="flex flex-row gap-4 sm:gap-6 order-2 md:order-1"><a href="/privacy-policy" class="flex items-center gap-2 text-xs text-gray-400 hover:text-gray-300 transition-colors">`);
  _push(ssrRenderComponent($setup["FileText"], { class: "w-4 h-4" }, null, _parent));
  _push(` Privacy Policy </a><a href="/terms-of-service" class="flex items-center gap-2 text-xs text-gray-400 hover:text-gray-300 transition-colors">`);
  _push(ssrRenderComponent($setup["FileText"], { class: "w-4 h-4" }, null, _parent));
  _push(` Terms of Service </a></div><!-- Copyright --><div class="text-center order-1 md:order-2"><p class="text-sm text-gray-300">© 2025 International Center. All rights reserved.</p></div><!-- Social Links --><div class="flex items-center gap-4 order-3"><span class="text-xs text-gray-500 font-medium">Follow Us:</span><!--[-->`);
  ssrRenderList($setup.socialLinks, (social) => {
    _push(`<a${ssrRenderAttr("href", social.href)}${ssrRenderAttr("aria-label", social.label)} class="text-gray-500 hover:text-white transition-colors p-1">`);
    ssrRenderVNode(_push, createVNode(resolveDynamicComponent(social.icon), { class: "w-5 h-5" }, null), _parent);
    _push(`</a>`);
  });
  _push(`<!--]--></div></div></div></div></footer>`);
}
const _sfc_setup = _sfc_main.setup;
_sfc_main.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/Footer.vue");
  return _sfc_setup ? _sfc_setup(props, ctx) : void 0;
};
const Footer = /* @__PURE__ */ _export_sfc(_sfc_main, [["ssrRender", _sfc_ssrRender], ["__file", "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/Footer.vue"]]);
const $$HoverClassesReference = createComponent(($$result, $$props, $$slots) => {
  return renderTemplate`<!-- Hover classes disabled to prevent global blue hover effects --><!-- <div class="hidden hover:border-primary group-hover:text-primary transition-colors group hover:border-blue-500 group-hover:text-blue-500">
  This div ensures hover classes are included in build
</div> -->`;
}, "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/HoverClassesReference.astro", void 0);
const $$Astro = createAstro();
const $$Layout = createComponent(($$result, $$props, $$slots) => {
  const Astro2 = $$result.createAstro($$Astro, $$props, $$slots);
  Astro2.self = $$Layout;
  const {
    title,
    description = "Advanced regenerative medicine solutions for a healthier tomorrow. Mobile PRP therapy, exosome treatments, and personalized regenerative care throughout South Florida."
  } = Astro2.props;
  return renderTemplate`<html lang="en"> <head><meta charset="UTF-8"><meta name="description"${addAttribute(description, "content")}><meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover"><!-- Favicon - 2025 Minimal Approach with Cache Busting --><link rel="icon" href="/favicon.ico?v=4" sizes="32x32"><link rel="icon" href="/icon-simple.svg?v=4" type="image/svg+xml"><link rel="apple-touch-icon" href="/apple-touch-icon.svg?v=4"><link rel="manifest" href="/site.webmanifest"><meta name="generator"${addAttribute(Astro2.generator, "content")}><title>${title}</title><!-- Algolia Search Verification --><meta name="algolia-site-verification" content="04278B71DB9637E9"><!-- Additional SEO meta tags --><meta property="og:title"${addAttribute(title, "content")}><meta property="og:description"${addAttribute(description, "content")}><meta property="og:type" content="website"><meta property="og:image" content="/og-image.svg"><meta name="twitter:card" content="summary_large_image"><meta name="twitter:title"${addAttribute(title, "content")}><meta name="twitter:description"${addAttribute(description, "content")}><meta name="twitter:image" content="/og-image.svg"><!-- Medical/Healthcare specific meta tags --><meta name="robots" content="index, follow"><meta name="theme-color" content="#2563eb"><meta name="msapplication-TileColor" content="#2563eb"><!-- iOS Safari specific meta tags --><meta name="apple-mobile-web-app-capable" content="yes"><meta name="apple-mobile-web-app-status-bar-style" content="default"><meta name="apple-mobile-web-app-title" content="International Center"><meta name="format-detection" content="telephone=no">${renderHead()}</head> <body> ${renderComponent($$result, "NavigationMenu", NavigationMenu, { "client:load": true, "client:component-hydration": "load", "client:component-path": "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/NavigationMenu.vue", "client:component-export": "default" })} <main class="pt-16 md:pt-32"> ${renderSlot($$result, $$slots["default"])} </main> ${renderComponent($$result, "Footer", Footer, { "client:idle": true, "client:component-hydration": "idle", "client:component-path": "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/Footer.vue", "client:component-export": "default" })} ${renderComponent($$result, "HoverClassesReference", $$HoverClassesReference, {})} </body></html>`;
}, "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/layouts/Layout.astro", void 0);
export {
  $$Layout as $,
  _export_sfc as _
};
