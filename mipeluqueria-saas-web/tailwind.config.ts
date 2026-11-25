import type { Config } from 'tailwindcss';

const config: Config = {
  darkMode: 'class',
  content: [
    './src/app/**/*.{js,ts,jsx,tsx,mdx}',
    './src/components/**/*.{js,ts,jsx,tsx,mdx}',
    './src/context/**/*.{js,ts,jsx,tsx,mdx}',
    './src/lib/**/*.{js,ts,jsx,tsx,mdx}',
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['var(--font-sans)', 'system-ui', '-apple-system', 'sans-serif'],
        mono: ['var(--font-mono)', 'ui-monospace', 'SFMono-Regular', 'monospace'],
      },
      colors: {
        canvas: 'var(--bg-canvas)',
        surface: 'var(--bg-surface)',
        elevated: 'var(--bg-elevated)',
        sidebar: 'var(--bg-sidebar)',
        'border-subtle': 'var(--border-subtle)',
        'border-strong': 'var(--border-strong)',
        'ink-primary': 'var(--text-primary)',
        'ink-secondary': 'var(--text-secondary)',
        brand: {
          primary: 'var(--brand-primary)',
          'primary-hover': 'var(--brand-primary-hover)',
          accent: 'var(--brand-accent)',
        },
        semantic: {
          cash: 'var(--semantic-cash)',
          digital: 'var(--semantic-digital)',
          warning: 'var(--semantic-warning)',
          danger: 'var(--semantic-danger)',
          neutral: 'var(--semantic-neutral)',
          courtesy: 'var(--semantic-courtesy)',
        },
      },
    },
  },
  plugins: [],
};

export default config;
