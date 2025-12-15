window.InactivityTimer = {
    timeoutHandle: null,
    warningHandle: null,
    dotNetReference: null,
    timeoutSeconds: 60,
    warningSeconds: 50,

    initialize: function (dotNetRef, timeoutSeconds, warningSeconds) {
        this.dotNetReference = dotNetRef;
        this.timeoutSeconds = timeoutSeconds || 60;
        this.warningSeconds = warningSeconds || 50;
        
        this.resetTimer();
        this.attachEventListeners();
    },

    attachEventListeners: function () {
        const events = ['mousemove', 'keydown', 'click', 'touchstart', 'scroll', 'wheel'];
        const resetTimer = this.resetTimer.bind(this);
        
        events.forEach(event => {
            document.addEventListener(event, resetTimer, { passive: true });
        });
    },

    resetTimer: function () {
        this.clearTimers();
        
        const warningDelay = this.warningSeconds * 1000;
        const timeoutDelay = this.timeoutSeconds * 1000;
        
        this.warningHandle = setTimeout(() => {
            if (this.dotNetReference) {
                this.dotNetReference.invokeMethodAsync('ShowWarning');
            }
        }, warningDelay);
        
        this.timeoutHandle = setTimeout(() => {
            if (this.dotNetReference) {
                this.dotNetReference.invokeMethodAsync('TriggerLogout');
            }
        }, timeoutDelay);
    },

    clearTimers: function () {
        if (this.warningHandle) {
            clearTimeout(this.warningHandle);
            this.warningHandle = null;
        }
        if (this.timeoutHandle) {
            clearTimeout(this.timeoutHandle);
            this.timeoutHandle = null;
        }
    },

    dispose: function () {
        this.clearTimers();
        this.dotNetReference = null;
    }
};
