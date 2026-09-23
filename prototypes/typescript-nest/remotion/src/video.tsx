import { AbsoluteFill, OffthreadVideo, interpolate, useCurrentFrame } from 'remotion';

export const AcceptanceDemo = () => {
  const frame = useCurrentFrame();
  const opacity = interpolate(frame, [0, 24, 426, 449], [1, 0, 0, 1]);
  return <AbsoluteFill style={{ backgroundColor: '#0b1220', fontFamily: 'Arial, sans-serif' }}>
    <OffthreadVideo src="/acceptance-video.webm" style={{ width: '100%', height: '100%', objectFit: 'contain' }} />
    <AbsoluteFill style={{ opacity, background: 'linear-gradient(120deg,#063b60,#0f172a)', color: 'white', justifyContent: 'center', alignItems: 'center' }}>
      <h1 style={{ fontSize: 56 }}>Palermo Work Orders</h1><p style={{ fontSize: 28 }}>NestJS acceptance: create · assign · complete</p>
    </AbsoluteFill>
  </AbsoluteFill>;
};
