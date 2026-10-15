import { Composition } from 'remotion';
import { AcceptanceDemo } from './video';

export const Root = () => <Composition id="AcceptanceDemo" component={AcceptanceDemo} durationInFrames={450} fps={30} width={1280} height={720} />;
