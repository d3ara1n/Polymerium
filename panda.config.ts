import { defineConfig } from "@pandacss/dev";
import { createPreset } from "@park-ui/panda-preset";

export default defineConfig({
    // Whether to use css reset
    preflight: true,

    // Park-UI preset
    presets: ["@pandacss/preset-base", createPreset({
      accentColor: 'amber',
      grayColor: "sand",
      borderRadius: "sm"
    })],
    jsxFramework: "solid",

    // Where to look for your css declarations
    include: ["./src/**/*.{js,jsx,ts,tsx}"],

    // Files to exclude
    exclude: [],

    // Useful for theme customization
    theme: {
        extend: {},
    },

    // The output directory for your css system
    outdir: "styled-system",
});
