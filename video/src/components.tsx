import React from 'react';
import {interpolate, spring, useCurrentFrame, useVideoConfig, AbsoluteFill} from 'remotion';
import {theme} from './theme';

/** Fades and lifts children in, with an optional frame delay. */
export const Rise: React.FC<{
  delay?: number;
  distance?: number;
  children: React.ReactNode;
  style?: React.CSSProperties;
}> = ({delay = 0, distance = 28, children, style}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const s = spring({frame: frame - delay, fps, config: {damping: 200}});
  return (
    <div
      style={{
        ...style,
        opacity: s,
        transform: `translateY(${interpolate(s, [0, 1], [distance, 0])}px)`,
      }}
    >
      {children}
    </div>
  );
};

/** Full-frame scene background with a subtle vignette toward the page plane. */
export const Scene: React.FC<{children: React.ReactNode}> = ({children}) => (
  <AbsoluteFill
    style={{
      backgroundColor: theme.surface,
      backgroundImage: `radial-gradient(120% 90% at 50% 0%, ${theme.surface} 0%, ${theme.plane} 100%)`,
      fontFamily: theme.font,
      color: theme.ink,
      padding: '84px 110px',
      justifyContent: 'center',
    }}
  >
    {children}
  </AbsoluteFill>
);

export const Eyebrow: React.FC<{children: React.ReactNode; color?: string}> = ({
  children,
  color = theme.inkMuted,
}) => (
  <div
    style={{
      fontSize: 24,
      letterSpacing: 3.4,
      textTransform: 'uppercase',
      fontWeight: 600,
      color,
    }}
  >
    {children}
  </div>
);

export const Title: React.FC<{children: React.ReactNode; size?: number}> = ({
  children,
  size = 76,
}) => (
  <div style={{fontSize: size, fontWeight: 700, lineHeight: 1.08, letterSpacing: -1.6}}>
    {children}
  </div>
);

/**
 * A stat tile: a hero figure with a label. No plot, so no hover layer applies.
 * The value carries a status color only when paired with its written label.
 */
export const StatTile: React.FC<{
  value: React.ReactNode;
  label: string;
  sub?: string;
  accent?: string;
  delay?: number;
}> = ({value, label, sub, accent = theme.ink, delay = 0}) => (
  <Rise delay={delay}>
    <div
      style={{
        background: theme.surface,
        border: `1px solid ${theme.border}`,
        borderRadius: 18,
        padding: '30px 34px',
        minWidth: 268,
      }}
    >
      <div style={{fontSize: 82, fontWeight: 700, color: accent, lineHeight: 1}}>{value}</div>
      <div style={{fontSize: 26, fontWeight: 600, color: theme.ink, marginTop: 14}}>{label}</div>
      {sub ? (
        <div style={{fontSize: 21, color: theme.inkMuted, marginTop: 6}}>{sub}</div>
      ) : null}
    </div>
  </Rise>
);

/**
 * Horizontal stacked progress bar. Segments are separated by a 2px surface gap
 * and every segment is named in the legend, so identity is never color-alone.
 */
export const StackedBar: React.FC<{
  segments: {label: string; value: number; color: string}[];
  total: number;
  delay?: number;
}> = ({segments, total, delay = 0}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const grow = spring({frame: frame - delay, fps, config: {damping: 200, mass: 0.9}});
  return (
    <div>
      <div
        style={{
          display: 'flex',
          gap: 2,
          height: 46,
          width: '100%',
          background: theme.gridline,
          borderRadius: 8,
          overflow: 'hidden',
        }}
      >
        {segments.map((seg) => (
          <div
            key={seg.label}
            style={{
              width: `${(seg.value / total) * 100 * grow}%`,
              background: seg.color,
              borderRadius: 4,
            }}
          />
        ))}
      </div>
      <div style={{display: 'flex', gap: 40, marginTop: 22}}>
        {segments.map((seg) => (
          <div key={seg.label} style={{display: 'flex', alignItems: 'center', gap: 11}}>
            <div
              style={{width: 15, height: 15, borderRadius: 4, background: seg.color, flexShrink: 0}}
            />
            <span style={{fontSize: 23, color: theme.inkSecondary}}>
              {seg.label}
              <span style={{color: theme.inkMuted}}> · {seg.value}</span>
            </span>
          </div>
        ))}
      </div>
    </div>
  );
};

/** An issue chip: number + title, tinted by state and always carrying a glyph. */
export const IssueChip: React.FC<{
  number: number;
  title: string;
  state: 'done' | 'todo' | 'defect';
  delay?: number;
  width?: number;
}> = ({number, title, state, delay = 0, width}) => {
  const map = {
    done: {color: theme.good, glyph: '✓'},
    todo: {color: theme.inkMuted, glyph: '○'},
    defect: {color: theme.critical, glyph: '⚠'},
  }[state];
  return (
    <Rise delay={delay} distance={14}>
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 14,
          background: theme.surface,
          border: `1px solid ${theme.border}`,
          borderLeft: `4px solid ${map.color}`,
          borderRadius: 12,
          padding: '15px 20px',
          width,
        }}
      >
        <span style={{color: map.color, fontSize: 24, width: 24, flexShrink: 0}}>{map.glyph}</span>
        <span
          style={{
            fontSize: 24,
            fontWeight: 700,
            color: theme.inkSecondary,
            fontVariantNumeric: 'tabular-nums',
          }}
        >
          #{number}
        </span>
        <span
          style={{
            fontSize: 24,
            color: theme.ink,
            whiteSpace: 'nowrap',
            overflow: 'hidden',
            textOverflow: 'ellipsis',
          }}
        >
          {title}
        </span>
      </div>
    </Rise>
  );
};
