import { c as createComponent, b as createAstro, r as renderComponent, a as renderTemplate, m as maybeRenderHead, d as addAttribute, e as renderSlot } from "./astro/server.GZtb3PXx.js";
import "kleur/colors";
import "html-escaper";
import { $ as $$Layout } from "./Layout.CZxktS_U.js";
import { $ as $$PageHero } from "./PageHero.BNFgZKtV.js";
const $$Astro = createAstro();
const $$InformationPage = createComponent(($$result, $$props, $$slots) => {
  const Astro2 = $$result.createAstro($$Astro, $$props, $$slots);
  Astro2.self = $$InformationPage;
  const {
    title,
    description,
    heroTitle,
    heroDescription,
    maxWidth = "max-w-4xl"
  } = Astro2.props;
  return renderTemplate`${renderComponent($$result, "Layout", $$Layout, { "title": title, "description": description }, { "default": ($$result2) => renderTemplate` ${maybeRenderHead()}<main> <!-- Hero Section --> ${renderComponent($$result2, "PageHero", $$PageHero, { "title": heroTitle, "description": heroDescription, "showBottomMargin": true })} <!-- Content Section --> <section class="py-8 lg:py-12"> <div class="container"> <div${addAttribute(`${maxWidth} mx-auto`, "class")}> <!-- Main Content --> <div class="prose prose-lg max-w-none"> ${renderSlot($$result2, $$slots["default"])} </div> </div> </div> </section> </main> ` })}`;
}, "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/InformationPage.astro", void 0);
export {
  $$InformationPage as $
};
