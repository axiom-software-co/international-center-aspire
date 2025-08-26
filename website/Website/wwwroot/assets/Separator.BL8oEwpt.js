import { defineComponent, useSSRContext, mergeProps } from "vue";
import { c as cn } from "./utils.H80jjgLf.js";
import { ssrRenderAttrs } from "vue/server-renderer";
import { _ as _export_sfc } from "./Layout.CZxktS_U.js";
const _sfc_main = /* @__PURE__ */ defineComponent({
  __name: "Separator",
  props: {
    orientation: { type: String, required: false, default: "horizontal" },
    decorative: { type: Boolean, required: false, default: true },
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
  _push(`<div${ssrRenderAttrs(mergeProps({
    class: $setup.cn(
      "shrink-0 bg-border",
      $props.orientation === "horizontal" ? "h-[1px] w-full" : "h-full w-[1px]",
      $setup.props.class
    ),
    role: $props.decorative ? "none" : "separator",
    "aria-orientation": $props.orientation
  }, _ctx.$attrs, _attrs))}></div>`);
}
const _sfc_setup = _sfc_main.setup;
_sfc_main.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/Separator.vue");
  return _sfc_setup ? _sfc_setup(props, ctx) : void 0;
};
const Separator = /* @__PURE__ */ _export_sfc(_sfc_main, [["ssrRender", _sfc_ssrRender], ["__file", "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/vue-ui/Separator.vue"]]);
export {
  Separator as S
};
