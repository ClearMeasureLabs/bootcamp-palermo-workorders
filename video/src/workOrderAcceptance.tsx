import React from 'react';
import {AbsoluteFill, OffthreadVideo, staticFile} from 'remotion';

export const WorkOrderAcceptance: React.FC = () => (
  <AbsoluteFill style={{backgroundColor: '#102f46', color: 'white', fontFamily: 'Arial, sans-serif', padding: 44}}>
    <div style={{fontSize: 38, fontWeight: 700, marginBottom: 24}}>Django Work Orders · Browser Acceptance</div>
    <div style={{flex: 1, border: '3px solid #79bce1', borderRadius: 10, overflow: 'hidden'}}>
      <OffthreadVideo src={staticFile('work-order-acceptance.webm')} style={{width: '100%', height: '100%', objectFit: 'contain'}} />
    </div>
    <div style={{fontSize: 20, marginTop: 18, color: '#c8deeb'}}>Create · Search · Assign · Record lifecycle history</div>
  </AbsoluteFill>
);
