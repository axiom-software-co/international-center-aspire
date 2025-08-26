import { defineComponent, useSSRContext, mergeProps, withCtx, renderSlot, ref, computed, provide, inject, onMounted, onUnmounted } from "vue";
import { cva } from "class-variance-authority";
import { c as cn } from "./utils.H80jjgLf.js";
import { ssrRenderAttrs, ssrRenderSlot, ssrRenderComponent } from "vue/server-renderer";
import { _ as _export_sfc } from "./Layout.CZxktS_U.js";
import { AvatarRoot, AvatarImage as AvatarImage$1, AvatarFallback as AvatarFallback$1, AccordionRoot, AccordionItem, AccordionTrigger, AccordionHeader, AccordionContent, SelectLabel, SelectSeparator } from "radix-vue";
import { ChevronDown, ChevronLeft, ChevronRight, MoreHorizontal, ArrowLeft, ArrowRight } from "lucide-vue-next";
import { B as Button } from "./Label.Xyga2qWh.js";
const _sfc_main$m = /* @__PURE__ */ defineComponent({
  __name: "Badge",
  props: {
    variant: { type: null, required: false, default: "default" },
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const badgeVariants = cva(
      "inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2",
      {
        variants: {
          variant: {
            default: "border-transparent bg-primary text-primary-foreground hover:bg-primary/80",
            secondary: "border-transparent bg-secondary text-secondary-foreground hover:bg-secondary/80",
            destructive: "border-transparent bg-destructive text-destructive-foreground hover:bg-destructive/80",
            outline: "text-foreground"
          }
        },
        defaultVariants: {
          variant: "default"
        }
      }
    );
    const props = __props;
    const __returned__ = { badgeVariants, props, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
function _sfc_ssrRender$5(_ctx, _push, _parent, _attrs, $props, $setup, $data, $options) {
  _push(`<div${ssrRenderAttrs(mergeProps({
    class: $setup.cn($setup.badgeVariants({ variant: $props.variant, class: $setup.props.class }))
  }, _attrs))}>`);
  ssrRenderSlot(_ctx.$slots, "default", {}, null, _push, _parent);
  _push(`</div>`);
}
const _sfc_setup$m = _sfc_main$m.setup;
_sfc_main$m.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/Badge.vue");
  return _sfc_setup$m ? _sfc_setup$m(props, ctx) : void 0;
};
const Badge = /* @__PURE__ */ _export_sfc(_sfc_main$m, [["ssrRender", _sfc_ssrRender$5], ["__file", "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/vue-ui/Badge.vue"]]);
const _sfc_main$l = /* @__PURE__ */ defineComponent({
  __name: "CardFooter",
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
function _sfc_ssrRender$4(_ctx, _push, _parent, _attrs, $props, $setup, $data, $options) {
  _push(`<div${ssrRenderAttrs(mergeProps({
    class: $setup.cn("flex items-center p-6 pt-0", _ctx.$props.class)
  }, _attrs))}>`);
  ssrRenderSlot(_ctx.$slots, "default", {}, null, _push, _parent);
  _push(`</div>`);
}
const _sfc_setup$l = _sfc_main$l.setup;
_sfc_main$l.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/CardFooter.vue");
  return _sfc_setup$l ? _sfc_setup$l(props, ctx) : void 0;
};
const CardFooter = /* @__PURE__ */ _export_sfc(_sfc_main$l, [["ssrRender", _sfc_ssrRender$4], ["__file", "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/vue-ui/CardFooter.vue"]]);
const _sfc_main$k = /* @__PURE__ */ defineComponent({
  __name: "Avatar",
  props: {
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const __returned__ = { props, get AvatarRoot() {
      return AvatarRoot;
    }, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
function _sfc_ssrRender$3(_ctx, _push, _parent, _attrs, $props, $setup, $data, $options) {
  _push(ssrRenderComponent($setup["AvatarRoot"], mergeProps({
    class: $setup.cn("relative flex h-10 w-10 shrink-0 overflow-hidden rounded-full", _ctx.$props.class)
  }, _attrs), {
    default: withCtx((_, _push2, _parent2, _scopeId) => {
      if (_push2) {
        ssrRenderSlot(_ctx.$slots, "default", {}, null, _push2, _parent2, _scopeId);
      } else {
        return [
          renderSlot(_ctx.$slots, "default")
        ];
      }
    }),
    _: 3
    /* FORWARDED */
  }, _parent));
}
const _sfc_setup$k = _sfc_main$k.setup;
_sfc_main$k.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/Avatar.vue");
  return _sfc_setup$k ? _sfc_setup$k(props, ctx) : void 0;
};
const Avatar = /* @__PURE__ */ _export_sfc(_sfc_main$k, [["ssrRender", _sfc_ssrRender$3], ["__file", "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/vue-ui/Avatar.vue"]]);
const _sfc_main$j = /* @__PURE__ */ defineComponent({
  __name: "AvatarImage",
  props: {
    src: { type: String, required: false },
    alt: { type: String, required: false },
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const __returned__ = { props, get AvatarImage() {
      return AvatarImage$1;
    }, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
function _sfc_ssrRender$2(_ctx, _push, _parent, _attrs, $props, $setup, $data, $options) {
  _push(ssrRenderComponent($setup["AvatarImage"], mergeProps({
    class: $setup.cn("aspect-square h-full w-full", _ctx.$props.class),
    src: $props.src,
    alt: $props.alt
  }, _attrs), null, _parent));
}
const _sfc_setup$j = _sfc_main$j.setup;
_sfc_main$j.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/AvatarImage.vue");
  return _sfc_setup$j ? _sfc_setup$j(props, ctx) : void 0;
};
const AvatarImage = /* @__PURE__ */ _export_sfc(_sfc_main$j, [["ssrRender", _sfc_ssrRender$2], ["__file", "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/vue-ui/AvatarImage.vue"]]);
const _sfc_main$i = /* @__PURE__ */ defineComponent({
  __name: "AvatarFallback",
  props: {
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const __returned__ = { props, get AvatarFallback() {
      return AvatarFallback$1;
    }, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
function _sfc_ssrRender$1(_ctx, _push, _parent, _attrs, $props, $setup, $data, $options) {
  _push(ssrRenderComponent($setup["AvatarFallback"], mergeProps({
    class: $setup.cn("flex h-full w-full items-center justify-center rounded-full bg-muted", _ctx.$props.class)
  }, _attrs), {
    default: withCtx((_, _push2, _parent2, _scopeId) => {
      if (_push2) {
        ssrRenderSlot(_ctx.$slots, "default", {}, null, _push2, _parent2, _scopeId);
      } else {
        return [
          renderSlot(_ctx.$slots, "default")
        ];
      }
    }),
    _: 3
    /* FORWARDED */
  }, _parent));
}
const _sfc_setup$i = _sfc_main$i.setup;
_sfc_main$i.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/AvatarFallback.vue");
  return _sfc_setup$i ? _sfc_setup$i(props, ctx) : void 0;
};
const AvatarFallback = /* @__PURE__ */ _export_sfc(_sfc_main$i, [["ssrRender", _sfc_ssrRender$1], ["__file", "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/vue-ui/AvatarFallback.vue"]]);
const _sfc_main$h = /* @__PURE__ */ defineComponent({
  __name: "Accordion",
  props: {
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const __returned__ = { props, get AccordionRoot() {
      return AccordionRoot;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
const _sfc_setup$h = _sfc_main$h.setup;
_sfc_main$h.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/Accordion.vue");
  return _sfc_setup$h ? _sfc_setup$h(props, ctx) : void 0;
};
const _sfc_main$g = /* @__PURE__ */ defineComponent({
  __name: "AccordionItem",
  props: {
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const __returned__ = { props, get AccordionItem() {
      return AccordionItem;
    }, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
const _sfc_setup$g = _sfc_main$g.setup;
_sfc_main$g.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/AccordionItem.vue");
  return _sfc_setup$g ? _sfc_setup$g(props, ctx) : void 0;
};
const _sfc_main$f = /* @__PURE__ */ defineComponent({
  __name: "AccordionTrigger",
  props: {
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const __returned__ = { props, get AccordionHeader() {
      return AccordionHeader;
    }, get AccordionTrigger() {
      return AccordionTrigger;
    }, get ChevronDown() {
      return ChevronDown;
    }, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
const _sfc_setup$f = _sfc_main$f.setup;
_sfc_main$f.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/AccordionTrigger.vue");
  return _sfc_setup$f ? _sfc_setup$f(props, ctx) : void 0;
};
const _sfc_main$e = /* @__PURE__ */ defineComponent({
  __name: "AccordionContent",
  props: {
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const __returned__ = { props, get AccordionContent() {
      return AccordionContent;
    }, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
const _sfc_setup$e = _sfc_main$e.setup;
_sfc_main$e.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/AccordionContent.vue");
  return _sfc_setup$e ? _sfc_setup$e(props, ctx) : void 0;
};
const _sfc_main$d = /* @__PURE__ */ defineComponent({
  __name: "SelectLabel",
  props: {
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const __returned__ = { props, get SelectLabel() {
      return SelectLabel;
    }, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
const _sfc_setup$d = _sfc_main$d.setup;
_sfc_main$d.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/SelectLabel.vue");
  return _sfc_setup$d ? _sfc_setup$d(props, ctx) : void 0;
};
const _sfc_main$c = /* @__PURE__ */ defineComponent({
  __name: "SelectSeparator",
  props: {
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const __returned__ = { props, get SelectSeparator() {
      return SelectSeparator;
    }, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
const _sfc_setup$c = _sfc_main$c.setup;
_sfc_main$c.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/SelectSeparator.vue");
  return _sfc_setup$c ? _sfc_setup$c(props, ctx) : void 0;
};
const _sfc_main$b = /* @__PURE__ */ defineComponent({
  __name: "Pagination",
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
const _sfc_setup$b = _sfc_main$b.setup;
_sfc_main$b.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/Pagination.vue");
  return _sfc_setup$b ? _sfc_setup$b(props, ctx) : void 0;
};
const _sfc_main$a = /* @__PURE__ */ defineComponent({
  __name: "PaginationContent",
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
const _sfc_setup$a = _sfc_main$a.setup;
_sfc_main$a.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/PaginationContent.vue");
  return _sfc_setup$a ? _sfc_setup$a(props, ctx) : void 0;
};
const _sfc_main$9 = /* @__PURE__ */ defineComponent({
  __name: "PaginationItem",
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
const _sfc_setup$9 = _sfc_main$9.setup;
_sfc_main$9.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/PaginationItem.vue");
  return _sfc_setup$9 ? _sfc_setup$9(props, ctx) : void 0;
};
const _sfc_main$8 = /* @__PURE__ */ defineComponent({
  __name: "PaginationLink",
  props: {
    isActive: { type: Boolean, required: false },
    size: { type: null, required: false, default: "icon" },
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const buttonVariants = cva(
      "inline-flex items-center justify-center whitespace-nowrap rounded-md text-sm font-medium ring-offset-background transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50",
      {
        variants: {
          variant: {
            default: "bg-primary text-primary-foreground hover:bg-primary/90",
            destructive: "bg-destructive text-destructive-foreground hover:bg-destructive/90",
            outline: "border border-input bg-background hover:bg-accent hover:text-accent-foreground",
            secondary: "bg-secondary text-secondary-foreground hover:bg-secondary/80",
            ghost: "hover:bg-accent hover:text-accent-foreground",
            link: "text-primary underline-offset-4 hover:underline"
          },
          size: {
            default: "h-10 px-4 py-2",
            sm: "h-9 rounded-md px-3",
            lg: "h-11 rounded-md px-8",
            icon: "h-10 w-10"
          }
        },
        defaultVariants: {
          variant: "default",
          size: "default"
        }
      }
    );
    const props = __props;
    const __returned__ = { buttonVariants, props, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
function _sfc_ssrRender(_ctx, _push, _parent, _attrs, $props, $setup, $data, $options) {
  _push(`<a${ssrRenderAttrs(mergeProps({
    "aria-current": $props.isActive ? "page" : void 0,
    class: $setup.cn(
      $setup.buttonVariants({
        variant: $props.isActive ? "outline" : "ghost",
        size: $props.size
      }),
      $setup.props.class
    )
  }, _ctx.$attrs, _attrs))}>`);
  ssrRenderSlot(_ctx.$slots, "default", {}, null, _push, _parent);
  _push(`</a>`);
}
const _sfc_setup$8 = _sfc_main$8.setup;
_sfc_main$8.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/PaginationLink.vue");
  return _sfc_setup$8 ? _sfc_setup$8(props, ctx) : void 0;
};
const PaginationLink = /* @__PURE__ */ _export_sfc(_sfc_main$8, [["ssrRender", _sfc_ssrRender], ["__file", "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/vue-ui/PaginationLink.vue"]]);
const _sfc_main$7 = /* @__PURE__ */ defineComponent({
  __name: "PaginationPrevious",
  props: {
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const __returned__ = { props, PaginationLink, get ChevronLeft() {
      return ChevronLeft;
    }, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
const _sfc_setup$7 = _sfc_main$7.setup;
_sfc_main$7.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/PaginationPrevious.vue");
  return _sfc_setup$7 ? _sfc_setup$7(props, ctx) : void 0;
};
const _sfc_main$6 = /* @__PURE__ */ defineComponent({
  __name: "PaginationNext",
  props: {
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const __returned__ = { props, PaginationLink, get ChevronRight() {
      return ChevronRight;
    }, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
const _sfc_setup$6 = _sfc_main$6.setup;
_sfc_main$6.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/PaginationNext.vue");
  return _sfc_setup$6 ? _sfc_setup$6(props, ctx) : void 0;
};
const _sfc_main$5 = /* @__PURE__ */ defineComponent({
  __name: "PaginationEllipsis",
  props: {
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const __returned__ = { props, get MoreHorizontal() {
      return MoreHorizontal;
    }, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
const _sfc_setup$5 = _sfc_main$5.setup;
_sfc_main$5.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/PaginationEllipsis.vue");
  return _sfc_setup$5 ? _sfc_setup$5(props, ctx) : void 0;
};
const _sfc_main$4 = /* @__PURE__ */ defineComponent({
  __name: "Carousel",
  props: {
    orientation: { type: String, required: false, default: "horizontal" },
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const currentIndex = ref(0);
    const itemCount = ref(0);
    const carouselRef = ref();
    const canScrollPrev = computed(() => currentIndex.value > 0);
    const canScrollNext = computed(() => currentIndex.value < itemCount.value - 1);
    const scrollPrev = () => {
      if (canScrollPrev.value) {
        currentIndex.value--;
      }
    };
    const scrollNext = () => {
      if (canScrollNext.value) {
        currentIndex.value++;
      }
    };
    const setItemCount = (count) => {
      itemCount.value = count;
    };
    const handleKeyDown = (event) => {
      if (event.key === "ArrowLeft") {
        event.preventDefault();
        scrollPrev();
      } else if (event.key === "ArrowRight") {
        event.preventDefault();
        scrollNext();
      }
    };
    provide("carousel", {
      orientation: props.orientation,
      currentIndex,
      scrollPrev,
      scrollNext,
      canScrollPrev,
      canScrollNext,
      setItemCount,
      carouselRef
    });
    const __returned__ = { props, currentIndex, itemCount, carouselRef, canScrollPrev, canScrollNext, scrollPrev, scrollNext, setItemCount, handleKeyDown, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
const _sfc_setup$4 = _sfc_main$4.setup;
_sfc_main$4.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/Carousel.vue");
  return _sfc_setup$4 ? _sfc_setup$4(props, ctx) : void 0;
};
const _sfc_main$3 = /* @__PURE__ */ defineComponent({
  __name: "CarouselContent",
  props: {
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const carousel = inject("carousel");
    if (!carousel) {
      throw new Error("CarouselContent must be used within a Carousel component");
    }
    const { orientation, currentIndex, carouselRef } = carousel;
    const transformStyle = computed(() => {
      if (orientation === "horizontal") {
        return {
          transform: `translateX(-${currentIndex.value * 100}%)`
        };
      } else {
        return {
          transform: `translateY(-${currentIndex.value * 100}%)`
        };
      }
    });
    const __returned__ = { props, carousel, orientation, currentIndex, carouselRef, transformStyle, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
const _sfc_setup$3 = _sfc_main$3.setup;
_sfc_main$3.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/CarouselContent.vue");
  return _sfc_setup$3 ? _sfc_setup$3(props, ctx) : void 0;
};
const _sfc_main$2 = /* @__PURE__ */ defineComponent({
  __name: "CarouselItem",
  props: {
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const carousel = inject("carousel");
    if (!carousel) {
      throw new Error("CarouselItem must be used within a Carousel component");
    }
    const { orientation, setItemCount } = carousel;
    onMounted(() => {
      setItemCount(carousel.itemCount.value + 1);
    });
    onUnmounted(() => {
      setItemCount(carousel.itemCount.value - 1);
    });
    const __returned__ = { props, carousel, orientation, setItemCount, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
const _sfc_setup$2 = _sfc_main$2.setup;
_sfc_main$2.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/CarouselItem.vue");
  return _sfc_setup$2 ? _sfc_setup$2(props, ctx) : void 0;
};
const _sfc_main$1 = /* @__PURE__ */ defineComponent({
  __name: "CarouselPrevious",
  props: {
    variant: { type: null, required: false, default: "outline" },
    size: { type: null, required: false, default: "icon" },
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const carousel = inject("carousel");
    if (!carousel) {
      throw new Error("CarouselPrevious must be used within a Carousel component");
    }
    const { orientation, scrollPrev, canScrollPrev } = carousel;
    const __returned__ = { props, carousel, orientation, scrollPrev, canScrollPrev, Button, get ArrowLeft() {
      return ArrowLeft;
    }, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
const _sfc_setup$1 = _sfc_main$1.setup;
_sfc_main$1.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/CarouselPrevious.vue");
  return _sfc_setup$1 ? _sfc_setup$1(props, ctx) : void 0;
};
const _sfc_main = /* @__PURE__ */ defineComponent({
  __name: "CarouselNext",
  props: {
    variant: { type: null, required: false, default: "outline" },
    size: { type: null, required: false, default: "icon" },
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const carousel = inject("carousel");
    if (!carousel) {
      throw new Error("CarouselNext must be used within a Carousel component");
    }
    const { orientation, scrollNext, canScrollNext } = carousel;
    const __returned__ = { props, carousel, orientation, scrollNext, canScrollNext, Button, get ArrowRight() {
      return ArrowRight;
    }, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
const _sfc_setup = _sfc_main.setup;
_sfc_main.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/CarouselNext.vue");
  return _sfc_setup ? _sfc_setup(props, ctx) : void 0;
};
export {
  AvatarFallback as A,
  Badge as B,
  CardFooter as C,
  AvatarImage as a,
  Avatar as b
};
