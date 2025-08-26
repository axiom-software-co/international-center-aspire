import { c, i } from "./environments.ozjwLHW8.js";
import { s } from "./index.Dk8XnGAI.js";
const contactsClient = {
  async submitContact(contactData) {
    console.log("🔒 [CONTACTS] Domain on hold - returning mock success response for:", contactData);
    return {
      success: true,
      message: "Thank you for your submission. We will contact you soon.",
      data: { id: "mock-" + Date.now(), status: "received" }
    };
  }
};
const newsletterClient = {
  async subscribe(subscriptionData) {
    console.log("🔒 [NEWSLETTER] Domain on hold - returning mock success response for:", subscriptionData);
    return {
      success: true,
      message: "Thank you for subscribing to our newsletter!",
      data: { id: "mock-newsletter-" + Date.now(), status: "subscribed" }
    };
  }
};
const newsClient = {
  async getNews(params) {
    console.log("🔒 [NEWS] Domain on hold - returning mock news data");
    return {
      success: true,
      message: "News loaded successfully",
      data: [
        {
          id: "mock-news-1",
          title: "Latest Medical Breakthroughs",
          excerpt: "Stay updated with our latest medical research and treatments.",
          publishedAt: (/* @__PURE__ */ new Date()).toISOString(),
          slug: "latest-medical-breakthroughs"
        }
      ]
    };
  }
};
const researchClient = {
  async getResearch(params) {
    console.log("🔒 [RESEARCH] Domain on hold - returning mock research data");
    return {
      success: true,
      message: "Research loaded successfully",
      data: [
        {
          id: "mock-research-1",
          title: "Regenerative Medicine Studies",
          excerpt: "Latest research in regenerative medicine and stem cell therapy.",
          publishedAt: (/* @__PURE__ */ new Date()).toISOString(),
          slug: "regenerative-medicine-studies"
        }
      ]
    };
  }
};
export {
  c as config,
  contactsClient,
  i as isProduction,
  newsClient,
  newsletterClient,
  researchClient,
  s as servicesClient
};
