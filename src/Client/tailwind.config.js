const colors = require('tailwindcss/colors')

/** @type {import('tailwindcss').Config} */
module.exports = {
    mode: "jit",
    content: [
        "./index.html",
        "./**/*.{fs,js,ts,jsx,tsx}",
    ],
    theme: {
        extend: {
            gridTemplateRows: {
                "index": "min-content min-content min-content auto"
            },

        },
    },
    plugins: [require("daisyui")],
    daisyui: {
        themes: [
            {
                winter: {
                    ...require("daisyui/src/theming/themes")["winter"],
                    primary: colors.teal["300"],
                },
            },
        ]
    }
}
