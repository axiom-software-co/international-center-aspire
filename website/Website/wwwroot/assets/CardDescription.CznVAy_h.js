import { defineComponent, useSSRContext, mergeProps } from "vue";
import { c as cn } from "./utils.H80jjgLf.js";
import { ssrRenderAttrs, ssrRenderSlot } from "vue/server-renderer";
import { _ as _export_sfc } from "./Layout.CZxktS_U.js";
const _sfc_main = /* @__PURE__ */ defineComponent({
  __name: "CardDescription",
  props: {
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const __returned__ = { props, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
function _sfc_ssrRender(_ctx, _push, _parent, _attrs, $props, $setup, $data, $options) {
  _push(`<p${ssrRenderAttrs(mergeProps({
    class: $setup.cn("text-sm text-muted-foreground", _ctx.$props.class)
  }, _attrs))}>`);
  ssrRenderSlot(_ctx.$slots, "default", {}, null, _push, _parent);
  _push(`</p>`);
}
const _sfc_setup = _sfc_main.setup;
_sfc_main.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/CardDescription.vue");
  return _sfc_setup ? _sfc_setup(props, ctx) : void 0;
};
const CardDescription = /* @__PURE__ */ _export_sfc(_sfc_main, [["ssrRender", _sfc_ssrRender], ["__file", "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/vue-ui/CardDescription.vue"]]);
export {
  CardDescription as C
};
