/**
 * Design tokens for the video. Dark-mode instance of the validated reference
 * palette: surfaces, ink and status colors are taken unchanged so the status
 * hues never impersonate a categorical series.
 */
export const theme = {
  surface: '#1a1a19',
  plane: '#0d0d0d',
  ink: '#ffffff',
  inkSecondary: '#c3c2b7',
  inkMuted: '#898781',
  gridline: '#2c2c2a',
  baseline: '#383835',
  border: 'rgba(255,255,255,0.10)',
  // categorical slots 1-3 (all-pairs validated in dark mode)
  series1: '#3987e5',
  series2: '#d95926',
  series3: '#199e70',
  // status palette - fixed, always shipped with an icon + label
  good: '#0ca30c',
  warning: '#fab219',
  serious: '#ec835a',
  critical: '#d03b3b',
  font: 'system-ui, -apple-system, "Segoe UI", sans-serif',
} as const;

export const FPS = 30;
