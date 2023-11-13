/** @type {import('tailwindcss').Config} */
export default {
    content: [
        "./src/**/*.{js,jsx,md,mdx,ts,tsx}"
    ],
    theme: {
        extend: {},
    },
    presets: [require("./suc.preset.js")]
}

