import { defineComponent, useSSRContext, ref, computed, watch, mergeProps, withCtx, createVNode, createTextVNode, toDisplayString, renderSlot } from "vue";
import { B as Button } from "./Label.Xyga2qWh.js";
import { ChevronRight, ChevronLeft, Calendar as Calendar$1 } from "lucide-vue-next";
import { c as cn } from "./utils.H80jjgLf.js";
import { ssrRenderAttrs, ssrRenderComponent, ssrInterpolate, ssrRenderList, ssrRenderSlot } from "vue/server-renderer";
import { _ as _export_sfc } from "./Layout.CZxktS_U.js";
import { PopoverRoot, PopoverTrigger as PopoverTrigger$1, PopoverContent as PopoverContent$1, PopoverPortal } from "radix-vue";
const _sfc_main$4 = /* @__PURE__ */ defineComponent({
  __name: "Calendar",
  props: {
    class: { type: String, required: false },
    selected: { type: Date, required: false },
    disabled: { type: Function, required: false },
    minDate: { type: Date, required: false },
    maxDate: { type: Date, required: false }
  },
  emits: ["select"],
  setup(__props, { expose: __expose, emit: __emit }) {
    __expose();
    const props = __props;
    const emit = __emit;
    const DAYS = ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"];
    const MONTHS = [
      "January",
      "February",
      "March",
      "April",
      "May",
      "June",
      "July",
      "August",
      "September",
      "October",
      "November",
      "December"
    ];
    const currentDate = ref(props.selected || /* @__PURE__ */ new Date());
    const viewMonth = ref(currentDate.value.getMonth());
    const viewYear = ref(currentDate.value.getFullYear());
    const getDaysInMonth = (month, year) => {
      return new Date(year, month + 1, 0).getDate();
    };
    const getFirstDayOfMonth = (month, year) => {
      return new Date(year, month, 1).getDay();
    };
    const daysInMonth = computed(() => getDaysInMonth(viewMonth.value, viewYear.value));
    const firstDay = computed(() => getFirstDayOfMonth(viewMonth.value, viewYear.value));
    const isDateDisabled = (date) => {
      if (props.disabled && props.disabled(date)) return true;
      if (props.minDate && date < props.minDate) return true;
      if (props.maxDate && date > props.maxDate) return true;
      return false;
    };
    const isToday = (date) => {
      const today = /* @__PURE__ */ new Date();
      return date.toDateString() === today.toDateString();
    };
    const isSelected = (date) => {
      return props.selected && date.toDateString() === props.selected.toDateString();
    };
    const handleDateClick = (day) => {
      const date = new Date(viewYear.value, viewMonth.value, day);
      if (!isDateDisabled(date)) {
        emit("select", date);
      }
    };
    const navigateMonth = (direction) => {
      const newMonth = viewMonth.value + direction;
      if (newMonth < 0) {
        viewMonth.value = 11;
        viewYear.value = viewYear.value - 1;
      } else if (newMonth > 11) {
        viewMonth.value = 0;
        viewYear.value = viewYear.value + 1;
      } else {
        viewMonth.value = newMonth;
      }
    };
    watch(
      () => props.selected,
      (newSelected) => {
        if (newSelected) {
          currentDate.value = newSelected;
          viewMonth.value = newSelected.getMonth();
          viewYear.value = newSelected.getFullYear();
        }
      }
    );
    const __returned__ = { props, emit, DAYS, MONTHS, currentDate, viewMonth, viewYear, getDaysInMonth, getFirstDayOfMonth, daysInMonth, firstDay, isDateDisabled, isToday, isSelected, handleDateClick, navigateMonth, Button, get ChevronLeft() {
      return ChevronLeft;
    }, get ChevronRight() {
      return ChevronRight;
    }, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
function _sfc_ssrRender$4(_ctx, _push, _parent, _attrs, $props, $setup, $data, $options) {
  _push(`<div${ssrRenderAttrs(mergeProps({
    class: $setup.cn("p-3", $setup.props.class)
  }, _ctx.$attrs, _attrs))}><!-- Header --><div class="flex items-center justify-between mb-4">`);
  _push(ssrRenderComponent($setup["Button"], {
    variant: "outline",
    size: "sm",
    class: "h-7 w-7 p-0",
    onClick: ($event) => $setup.navigateMonth(-1)
  }, {
    default: withCtx((_, _push2, _parent2, _scopeId) => {
      if (_push2) {
        _push2(ssrRenderComponent($setup["ChevronLeft"], { class: "h-4 w-4" }, null, _parent2, _scopeId));
      } else {
        return [
          createVNode($setup["ChevronLeft"], { class: "h-4 w-4" })
        ];
      }
    }),
    _: 1
    /* STABLE */
  }, _parent));
  _push(`<div class="text-sm font-medium">${ssrInterpolate($setup.MONTHS[$setup.viewMonth])} ${ssrInterpolate($setup.viewYear)}</div>`);
  _push(ssrRenderComponent($setup["Button"], {
    variant: "outline",
    size: "sm",
    class: "h-7 w-7 p-0",
    onClick: ($event) => $setup.navigateMonth(1)
  }, {
    default: withCtx((_, _push2, _parent2, _scopeId) => {
      if (_push2) {
        _push2(ssrRenderComponent($setup["ChevronRight"], { class: "h-4 w-4" }, null, _parent2, _scopeId));
      } else {
        return [
          createVNode($setup["ChevronRight"], { class: "h-4 w-4" })
        ];
      }
    }),
    _: 1
    /* STABLE */
  }, _parent));
  _push(`</div><!-- Days of week header --><div class="grid grid-cols-7 mb-2"><!--[-->`);
  ssrRenderList($setup.DAYS, (day) => {
    _push(`<div class="text-center text-sm font-medium text-muted-foreground p-2">${ssrInterpolate(day)}</div>`);
  });
  _push(`<!--]--></div><!-- Calendar grid --><div class="grid grid-cols-7 gap-1"><!-- Empty cells for days before month starts --><!--[-->`);
  ssrRenderList($setup.firstDay, (i) => {
    _push(`<div class="p-2"></div>`);
  });
  _push(`<!--]--><!-- Days of the month --><!--[-->`);
  ssrRenderList($setup.daysInMonth, (day) => {
    _push(ssrRenderComponent($setup["Button"], {
      key: day,
      variant: "ghost",
      size: "sm",
      class: $setup.cn(
        "h-8 w-8 p-0 font-normal",
        $setup.isToday(new Date($setup.viewYear, $setup.viewMonth, day)) && "bg-accent text-accent-foreground",
        $setup.isSelected(new Date($setup.viewYear, $setup.viewMonth, day)) && "bg-primary text-primary-foreground hover:bg-primary hover:text-primary-foreground",
        $setup.isDateDisabled(new Date($setup.viewYear, $setup.viewMonth, day)) && "text-muted-foreground opacity-50 cursor-not-allowed"
      ),
      disabled: $setup.isDateDisabled(new Date($setup.viewYear, $setup.viewMonth, day)),
      onClick: ($event) => $setup.handleDateClick(day)
    }, {
      default: withCtx((_, _push2, _parent2, _scopeId) => {
        if (_push2) {
          _push2(`${ssrInterpolate(day)}`);
        } else {
          return [
            createTextVNode(
              toDisplayString(day),
              1
              /* TEXT */
            )
          ];
        }
      }),
      _: 2
      /* DYNAMIC */
    }, _parent));
  });
  _push(`<!--]--></div></div>`);
}
const _sfc_setup$4 = _sfc_main$4.setup;
_sfc_main$4.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/Calendar.vue");
  return _sfc_setup$4 ? _sfc_setup$4(props, ctx) : void 0;
};
const Calendar = /* @__PURE__ */ _export_sfc(_sfc_main$4, [["ssrRender", _sfc_ssrRender$4], ["__file", "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/vue-ui/Calendar.vue"]]);
const _sfc_main$3 = /* @__PURE__ */ defineComponent({
  __name: "Popover",
  setup(__props, { expose: __expose }) {
    __expose();
    const __returned__ = { get RadixPopoverRoot() {
      return PopoverRoot;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
function _sfc_ssrRender$3(_ctx, _push, _parent, _attrs, $props, $setup, $data, $options) {
  _push(ssrRenderComponent($setup["RadixPopoverRoot"], mergeProps(_ctx.$attrs, _attrs), {
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
const _sfc_setup$3 = _sfc_main$3.setup;
_sfc_main$3.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/Popover.vue");
  return _sfc_setup$3 ? _sfc_setup$3(props, ctx) : void 0;
};
const Popover = /* @__PURE__ */ _export_sfc(_sfc_main$3, [["ssrRender", _sfc_ssrRender$3], ["__file", "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/vue-ui/Popover.vue"]]);
const _sfc_main$2 = /* @__PURE__ */ defineComponent({
  __name: "PopoverTrigger",
  setup(__props, { expose: __expose }) {
    __expose();
    const __returned__ = { get RadixPopoverTrigger() {
      return PopoverTrigger$1;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
function _sfc_ssrRender$2(_ctx, _push, _parent, _attrs, $props, $setup, $data, $options) {
  _push(ssrRenderComponent($setup["RadixPopoverTrigger"], mergeProps(_ctx.$attrs, _attrs), {
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
const _sfc_setup$2 = _sfc_main$2.setup;
_sfc_main$2.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/PopoverTrigger.vue");
  return _sfc_setup$2 ? _sfc_setup$2(props, ctx) : void 0;
};
const PopoverTrigger = /* @__PURE__ */ _export_sfc(_sfc_main$2, [["ssrRender", _sfc_ssrRender$2], ["__file", "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/vue-ui/PopoverTrigger.vue"]]);
const _sfc_main$1 = /* @__PURE__ */ defineComponent({
  __name: "PopoverContent",
  props: {
    align: { type: String, required: false, default: "center" },
    sideOffset: { type: Number, required: false, default: 4 },
    class: { type: String, required: false }
  },
  setup(__props, { expose: __expose }) {
    __expose();
    const props = __props;
    const __returned__ = { props, get RadixPopoverPortal() {
      return PopoverPortal;
    }, get RadixPopoverContent() {
      return PopoverContent$1;
    }, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
function _sfc_ssrRender$1(_ctx, _push, _parent, _attrs, $props, $setup, $data, $options) {
  _push(ssrRenderComponent($setup["RadixPopoverPortal"], _attrs, {
    default: withCtx((_, _push2, _parent2, _scopeId) => {
      if (_push2) {
        _push2(ssrRenderComponent($setup["RadixPopoverContent"], mergeProps({
          align: $props.align,
          "side-offset": $props.sideOffset,
          class: $setup.cn(
            "z-50 w-72 rounded-md border bg-popover p-4 text-popover-foreground shadow-md outline-none data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 data-[side=bottom]:slide-in-from-top-2 data-[side=left]:slide-in-from-right-2 data-[side=right]:slide-in-from-left-2 data-[side=top]:slide-in-from-bottom-2",
            $setup.props.class
          )
        }, _ctx.$attrs), {
          default: withCtx((_2, _push3, _parent3, _scopeId2) => {
            if (_push3) {
              ssrRenderSlot(_ctx.$slots, "default", {}, null, _push3, _parent3, _scopeId2);
            } else {
              return [
                renderSlot(_ctx.$slots, "default")
              ];
            }
          }),
          _: 3
          /* FORWARDED */
        }, _parent2, _scopeId));
      } else {
        return [
          createVNode($setup["RadixPopoverContent"], mergeProps({
            align: $props.align,
            "side-offset": $props.sideOffset,
            class: $setup.cn(
              "z-50 w-72 rounded-md border bg-popover p-4 text-popover-foreground shadow-md outline-none data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 data-[side=bottom]:slide-in-from-top-2 data-[side=left]:slide-in-from-right-2 data-[side=right]:slide-in-from-left-2 data-[side=top]:slide-in-from-bottom-2",
              $setup.props.class
            )
          }, _ctx.$attrs), {
            default: withCtx(() => [
              renderSlot(_ctx.$slots, "default")
            ]),
            _: 3
            /* FORWARDED */
          }, 16, ["align", "side-offset", "class"])
        ];
      }
    }),
    _: 3
    /* FORWARDED */
  }, _parent));
}
const _sfc_setup$1 = _sfc_main$1.setup;
_sfc_main$1.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/PopoverContent.vue");
  return _sfc_setup$1 ? _sfc_setup$1(props, ctx) : void 0;
};
const PopoverContent = /* @__PURE__ */ _export_sfc(_sfc_main$1, [["ssrRender", _sfc_ssrRender$1], ["__file", "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/vue-ui/PopoverContent.vue"]]);
const _sfc_main = /* @__PURE__ */ defineComponent({
  __name: "DatePicker",
  props: {
    modelValue: { type: String, required: false },
    placeholder: { type: String, required: false, default: "Select date" },
    class: { type: String, required: false },
    id: { type: String, required: false },
    minDate: { type: String, required: false },
    maxDate: { type: String, required: false }
  },
  emits: ["update:modelValue"],
  setup(__props, { expose: __expose, emit: __emit }) {
    __expose();
    const props = __props;
    const emit = __emit;
    const open = ref(false);
    const formatDate = (date) => {
      return date.toLocaleDateString("en-US", {
        weekday: "short",
        year: "numeric",
        month: "short",
        day: "numeric"
      });
    };
    const parseDate = (dateString) => {
      if (!dateString) return void 0;
      const date = /* @__PURE__ */ new Date(dateString + "T00:00:00");
      return isNaN(date.getTime()) ? void 0 : date;
    };
    const formatDateString = (date) => {
      return date.toISOString().split("T")[0];
    };
    const today = /* @__PURE__ */ new Date();
    today.setHours(0, 0, 0, 0);
    const currentYear = (/* @__PURE__ */ new Date()).getFullYear();
    const maxYear = currentYear + 5;
    const defaultMaxDate = new Date(maxYear, 11, 31);
    const selectedDate = computed(() => parseDate(props.modelValue || ""));
    const minDateObj = computed(() => props.minDate ? parseDate(props.minDate) : today);
    const maxDateObj = computed(() => props.maxDate ? parseDate(props.maxDate) : defaultMaxDate);
    const isDateDisabled = (date) => {
      if (minDateObj.value && date < minDateObj.value) return true;
      if (maxDateObj.value && date > maxDateObj.value) return true;
      return false;
    };
    const handleSelect = (date) => {
      if (date) {
        const dateString = formatDateString(date);
        emit("update:modelValue", dateString);
        open.value = false;
      }
    };
    const __returned__ = { props, emit, open, formatDate, parseDate, formatDateString, today, currentYear, maxYear, defaultMaxDate, selectedDate, minDateObj, maxDateObj, isDateDisabled, handleSelect, Button, Calendar, Popover, PopoverTrigger, PopoverContent, get CalendarIcon() {
      return Calendar$1;
    }, get cn() {
      return cn;
    } };
    Object.defineProperty(__returned__, "__isScriptSetup", { enumerable: false, value: true });
    return __returned__;
  }
});
function _sfc_ssrRender(_ctx, _push, _parent, _attrs, $props, $setup, $data, $options) {
  _push(`<div${ssrRenderAttrs(mergeProps({ class: "relative" }, _attrs))}>`);
  _push(ssrRenderComponent($setup["Popover"], {
    open: $setup.open,
    "onUpdate:open": ($event) => $setup.open = $event
  }, {
    default: withCtx((_, _push2, _parent2, _scopeId) => {
      if (_push2) {
        _push2(ssrRenderComponent($setup["PopoverTrigger"], null, {
          default: withCtx((_2, _push3, _parent3, _scopeId2) => {
            if (_push3) {
              _push3(ssrRenderComponent($setup["Button"], {
                id: $props.id,
                variant: "outline",
                type: "button",
                class: $setup.cn(
                  "w-full justify-start text-left font-normal",
                  !$setup.selectedDate && "text-muted-foreground",
                  $setup.props.class
                )
              }, {
                default: withCtx((_3, _push4, _parent4, _scopeId3) => {
                  if (_push4) {
                    _push4(ssrRenderComponent($setup["CalendarIcon"], { class: "mr-2 h-4 w-4" }, null, _parent4, _scopeId3));
                    _push4(` ${ssrInterpolate($setup.selectedDate ? $setup.formatDate($setup.selectedDate) : $props.placeholder)}`);
                  } else {
                    return [
                      createVNode($setup["CalendarIcon"], { class: "mr-2 h-4 w-4" }),
                      createTextVNode(
                        " " + toDisplayString($setup.selectedDate ? $setup.formatDate($setup.selectedDate) : $props.placeholder),
                        1
                        /* TEXT */
                      )
                    ];
                  }
                }),
                _: 1
                /* STABLE */
              }, _parent3, _scopeId2));
            } else {
              return [
                createVNode($setup["Button"], {
                  id: $props.id,
                  variant: "outline",
                  type: "button",
                  class: $setup.cn(
                    "w-full justify-start text-left font-normal",
                    !$setup.selectedDate && "text-muted-foreground",
                    $setup.props.class
                  )
                }, {
                  default: withCtx(() => [
                    createVNode($setup["CalendarIcon"], { class: "mr-2 h-4 w-4" }),
                    createTextVNode(
                      " " + toDisplayString($setup.selectedDate ? $setup.formatDate($setup.selectedDate) : $props.placeholder),
                      1
                      /* TEXT */
                    )
                  ]),
                  _: 1
                  /* STABLE */
                }, 8, ["id", "class"])
              ];
            }
          }),
          _: 1
          /* STABLE */
        }, _parent2, _scopeId));
        _push2(ssrRenderComponent($setup["PopoverContent"], {
          class: "w-auto p-0",
          align: "start"
        }, {
          default: withCtx((_2, _push3, _parent3, _scopeId2) => {
            if (_push3) {
              _push3(ssrRenderComponent($setup["Calendar"], {
                selected: $setup.selectedDate,
                disabled: $setup.isDateDisabled,
                "min-date": $setup.minDateObj,
                "max-date": $setup.maxDateObj,
                onSelect: $setup.handleSelect
              }, null, _parent3, _scopeId2));
            } else {
              return [
                createVNode($setup["Calendar"], {
                  selected: $setup.selectedDate,
                  disabled: $setup.isDateDisabled,
                  "min-date": $setup.minDateObj,
                  "max-date": $setup.maxDateObj,
                  onSelect: $setup.handleSelect
                }, null, 8, ["selected", "min-date", "max-date"])
              ];
            }
          }),
          _: 1
          /* STABLE */
        }, _parent2, _scopeId));
      } else {
        return [
          createVNode($setup["PopoverTrigger"], null, {
            default: withCtx(() => [
              createVNode($setup["Button"], {
                id: $props.id,
                variant: "outline",
                type: "button",
                class: $setup.cn(
                  "w-full justify-start text-left font-normal",
                  !$setup.selectedDate && "text-muted-foreground",
                  $setup.props.class
                )
              }, {
                default: withCtx(() => [
                  createVNode($setup["CalendarIcon"], { class: "mr-2 h-4 w-4" }),
                  createTextVNode(
                    " " + toDisplayString($setup.selectedDate ? $setup.formatDate($setup.selectedDate) : $props.placeholder),
                    1
                    /* TEXT */
                  )
                ]),
                _: 1
                /* STABLE */
              }, 8, ["id", "class"])
            ]),
            _: 1
            /* STABLE */
          }),
          createVNode($setup["PopoverContent"], {
            class: "w-auto p-0",
            align: "start"
          }, {
            default: withCtx(() => [
              createVNode($setup["Calendar"], {
                selected: $setup.selectedDate,
                disabled: $setup.isDateDisabled,
                "min-date": $setup.minDateObj,
                "max-date": $setup.maxDateObj,
                onSelect: $setup.handleSelect
              }, null, 8, ["selected", "min-date", "max-date"])
            ]),
            _: 1
            /* STABLE */
          })
        ];
      }
    }),
    _: 1
    /* STABLE */
  }, _parent));
  _push(`</div>`);
}
const _sfc_setup = _sfc_main.setup;
_sfc_main.setup = (props, ctx) => {
  const ssrContext = useSSRContext();
  (ssrContext.modules || (ssrContext.modules = /* @__PURE__ */ new Set())).add("src/components/vue-ui/DatePicker.vue");
  return _sfc_setup ? _sfc_setup(props, ctx) : void 0;
};
const DatePicker = /* @__PURE__ */ _export_sfc(_sfc_main, [["ssrRender", _sfc_ssrRender], ["__file", "/home/tojkuv/Documents/GitHub/international-center/international-center-aspire/website/Website/src/components/vue-ui/DatePicker.vue"]]);
export {
  DatePicker as D
};
