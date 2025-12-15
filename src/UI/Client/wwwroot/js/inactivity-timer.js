window.InactivityTimer = {
    timeoutId: null,
    warningId: null,
    timeoutSeconds: 60,
    warningSeconds: 50,
    dotNetRef: null,
    events: ['mousemove', 'keydown', 'click', 'touchstart', 'scroll', 'wheel'],

    initialize: function (dotNetReference, timeoutSeconds, warningSeconds) {
        this.dotNetRef = dotNetReference;
        this.timeoutSeconds = timeoutSeconds;
        this.warningSeconds = warningSeconds;
        this.startTimer();
        this.addEventListeners();
    },

    startTimer: function () {
        this.clearTimers();
        
        // Set warning timer
        this.warningId = setTimeout(() => {
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('ShowWarning');
            }
        }, this.warningSeconds * 1000);

        // Set logout timer
        this.timeoutId = setTimeout(() => {
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('HandleTimeout');
            }
        }, this.timeoutSeconds * 1000);
    },

    resetTimer: function () {
        this.startTimer();
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('HideWarning');
        }
    },

    clearTimers: function () {
        if (this.timeoutId) {
            clearTimeout(this.timeoutId);
            this.timeoutId = null;
        }
        if (this.warningId) {
            clearTimeout(this.warningId);
            this.warningId = null;
        }
    },

    handleActivity: function () {
        this.resetTimer();
    },

    addEventListeners: function () {
        const self = this;
        this.events.forEach(event => {
            document.addEventListener(event, () => self.handleActivity(), { passive: true });
        });
    },

    removeEventListeners: function () {
        const self = this;
        this.events.forEach(event => {
            document.removeEventListener(event, () => self.handleActivity(), { passive: true });
        });
    },

    dispose: function () {
        this.clearTimers();
        this.removeEventListeners();
        this.dotNetRef = null;
    }
};
