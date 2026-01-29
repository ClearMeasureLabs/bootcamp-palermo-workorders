window.InactivityTimer = {
    timeoutHandle: null,
    warningHandle: null,
    dotNetReference: null,
    timeoutSeconds: 60,
    warningSeconds: 50,
    boundResetTimer: null,
    attachedEvents: [],

    initialize: function (dotNetRef, timeoutSeconds, warningSeconds) {
        this.dotNetReference = dotNetRef;
        this.timeoutSeconds = timeoutSeconds || 60;
        this.warningSeconds = warningSeconds || 50;
        
        this.resetTimer();
        this.attachEventListeners();
    },

    attachEventListeners: function () {
        const events = ['mousemove', 'keydown', 'click', 'touchstart', 'scroll', 'wheel'];
        this.boundResetTimer = this.resetTimer.bind(this);
        
        events.forEach(event => {
            document.addEventListener(event, this.boundResetTimer, { passive: true });
            this.attachedEvents.push(event);
        });
    },

    removeEventListeners: function () {
        if (this.boundResetTimer) {
            this.attachedEvents.forEach(event => {
                document.removeEventListener(event, this.boundResetTimer, { passive: true });
            });
            this.attachedEvents = [];
            this.boundResetTimer = null;
        }
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
        this.removeEventListeners();
        this.dotNetReference = null;
    }
};
