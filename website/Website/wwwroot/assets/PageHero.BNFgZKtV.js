import { c as createComponent, b as createAstro, m as maybeRenderHead, a as renderTemplate } from "./astro/server.GZtb3PXx.js";
import "kleur/colors";
import "html-escaper";
import "clsx";
const $$Astro = createAstro();
const $$PageHero = createComponent(($$result, $$props, $$slots) => {
  const Astro2 = $$result.createAstro($$Astro, $$props, $$slots);
  Astro2.self = $$PageHero;
  const { title, showBottomMargin = false, badge, subtitle } = Astro2.props;
  return renderTemplate`${maybeRenderHead()}<section class="pt-12 pb-16 bg-gray-200 dark:bg-gray-800 relative overflow-hidden"> <div class="container mx-auto px-4 relative z-10"> <div class="text-center max-w-4xl mx-auto"> ${(badge || subtitle) && renderTemplate`<p class="text-sm font-semibold text-blue-600 dark:text-blue-400 mb-2 uppercase tracking-wider"> ${badge || subtitle} </p>`} <h1 class="text-4xl font-bold text-gray-900 dark:text-white md:text-5xl break-words leading-tight py-2"> ${title} </h1> </div> </div> <!-- Background Company Logo - Hidden on mobile --> <div class="absolute inset-0 pointer-events-none hidden md:block"> <div class="w-full h-full flex items-center justify-center"> <div class="opacity-[0.08] dark:opacity-[0.12]"> <img src="/rc-logo-blue.svg" alt="International Center Logo" width="240" height="240" class="w-60 h-60" style="filter: grayscale(100%);" loading="lazy"> </div> </div> </div> </section>`;
}, "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/PageHero.astro", void 0);
export {
  $$PageHero as $
};
