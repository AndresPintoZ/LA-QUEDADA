import type { Config } from 'tailwindcss';

// Sistema visual de PlanVibe.es — azul-cian + lima sobre tinta.
export default {
  content: ['./app/**/*.{ts,tsx}', './components/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        ink: '#0C1A22',
        brand: { DEFAULT: '#0B7C9B', dark: '#075E77', tint: '#E6F3F7' },
        lime: '#D8F45A',
        paper: '#F3F6F5',
        line: '#E7EDEB',
        muted: '#6E827D',
        body: '#3D4F4A',
        ok: { DEFAULT: '#1E8A5F', bg: '#E7F4EE' },
        warn: { DEFAULT: '#8A5A0B', bg: '#FCF1DC' },
        danger: { DEFAULT: '#C2413C', bg: '#FBEBEA' },
      },
      fontFamily: {
        display: ['var(--font-display)', 'system-ui', 'sans-serif'],
        sans: ['var(--font-sans)', 'system-ui', 'sans-serif'],
        mono: ['var(--font-mono)', 'monospace'],
      },
      borderRadius: { card: '20px', frame: '34px' },
      boxShadow: { card: '0 2px 10px rgba(12,26,34,0.06)', sheet: '0 -8px 24px rgba(12,26,34,0.12)' },
    },
  },
  plugins: [],
} satisfies Config;
