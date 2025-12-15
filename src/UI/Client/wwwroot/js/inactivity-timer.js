window.InactivityTimer = {
    timeoutId: null,
    warningId: null,
    timeoutSeconds: 60,
    warningSeconds: 50,
    dotNetRef: null,
    events: ['mousemove', 'keydown', 'click', 'touchstart', 'scroll', 'wheel'],
    activityHandler: null,

    initialize: function (dotNetReference, timeoutSeconds, warningSeconds) {
        this.dotNetRef = dotNetReference;
        this.timeoutSeconds = timeoutSeconds;
        this.warningSeconds = warningSeconds;
        this.activityHandler = () => this.handleActivity();
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
        if (this.activityHandler) {
            this.events.forEach(event => {
                document.addEventListener(event, this.activityHandler, { passive: true });
            });
        }
    },

    removeEventListeners: function () {
        if (this.activityHandler) {
            this.events.forEach(event => {
                document.removeEventListener(event, this.activityHandler, { passive: true });
            });
        }
    },

    dispose: function () {
        this.clearTimers();
        this.removeEventListeners();
        this.activityHandler = null;
        this.dotNetRef = null;
    }
};

