import { defineComponent, useSSRContext, mergeProps } from "vue";
import { ssrRenderAttrs, ssrRenderStyle } from "vue/server-renderer";
import { _ as _export_sfc } from "./Layout.CZxktS_U.js";
const _sfc_main = /* @__PURE__ */ defineComponent({
  __name: "ServicesBackgroundLogo",
  setup(__props, { expose: __expose }) {
    __expose();
    const __returned__ = {};
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
function _sfc_ssrRender(_ctx, _push, _parent, _attrs, $props, $setup, $data, $options) {
  _push(`<div${ssrRenderAttrs(mergeProps({ class: "absolute inset-0 flex items-center justify-center pointer-events-none" }, _attrs))}><div class="opacity-[0.08] dark:opacity-[0.12]"><img src="/rc-logo-blue.svg" alt="International Center Logo" width="240" height="240" class="w-60 h-60" style="${ssrRenderStyle({ "filter": "grayscale(100%)" })}"></div></div>`);
}
const _sfc_setup = _sfc_main.setup;
_sfc_main.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/ServicesBackgroundLogo.vue");
  return _sfc_setup ? _sfc_setup(props, ctx) : void 0;
};
const ServicesBackgroundLogo = /* @__PURE__ */ _export_sfc(_sfc_main, [["ssrRender", _sfc_ssrRender], ["__file", "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/ServicesBackgroundLogo.vue"]]);
export {
  ServicesBackgroundLogo as S
};
