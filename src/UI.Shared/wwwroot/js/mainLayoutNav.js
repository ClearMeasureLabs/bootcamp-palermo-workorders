export function initNavToggle(dotNetRef, mediaQuery) {
	const mq = window.matchMedia(mediaQuery);
	const handler = () => {
		dotNetRef.invokeMethodAsync('OnViewportChanged', mq.matches);
	};
	mq.addEventListener('change', handler);
	handler();
	return {
		dispose: () => mq.removeEventListener('change', handler)
	};
}
