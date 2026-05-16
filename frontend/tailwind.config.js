/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        mayhem: {
          primary:   '#FF6B6B',
          secondary: '#4ECDC4',
          yellow:    '#FFE66D',
          purple:    '#A855F7',
          dark:      '#1A1A2E',
          card:      '#FFFFFF',
          surface:   '#F8F9FA',
        }
      },
      fontFamily: {
        display: ['Poppins', 'sans-serif'],
        body:    ['Inter', 'sans-serif'],
      },
    },
  },
  plugins: [],
}