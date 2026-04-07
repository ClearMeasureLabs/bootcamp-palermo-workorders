const THEME_STORAGE_KEY = 'churchbulletin-theme';

/**
 * @param {string} theme 'dark' | 'light'
 */
export function syncDomFromTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    document.documentElement.setAttribute('data-bs-theme', theme);
}

/**
 * @returns {'dark'|'light'} Stored preference if set; otherwise follows prefers-color-scheme (does not write storage).
 */
export function getTheme() {
    try {
        const stored = localStorage.getItem(THEME_STORAGE_KEY);
        if (stored === 'dark' || stored === 'light') {
            return stored;
        }
    } catch {
        // ignore
    }
    const prefersDark = typeof window !== 'undefined'
        && window.matchMedia
        && window.matchMedia('(prefers-color-scheme: dark)').matches;
    return prefersDark ? 'dark' : 'light';
}

/**
 * @param {boolean} isDark
 */
export function setTheme(isDark) {
    const theme = isDark ? 'dark' : 'light';
    try {
        localStorage.setItem(THEME_STORAGE_KEY, theme);
    } catch {
        // ignore
    }
    syncDomFromTheme(theme);
}
