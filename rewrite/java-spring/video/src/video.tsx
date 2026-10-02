import React from 'react';
import {OffthreadVideo, staticFile} from 'remotion';

export const AcceptanceRecording: React.FC = () => <div style={{width:'100%',height:'100%',background:'#0f172a'}}>
  <OffthreadVideo src={staticFile('acceptance.webm')} style={{width:'100%',height:'100%',objectFit:'contain'}} />
</div>;
