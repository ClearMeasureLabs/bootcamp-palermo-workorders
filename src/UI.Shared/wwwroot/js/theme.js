const THEME_STORAGE_KEY = 'churchbulletin-theme';
const COMPACT_TABLES_STORAGE_KEY = 'churchbulletin-compact-tables';

/**
 * @param {string} theme 'dark' | 'light'
 */
export function syncDomFromTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    document.documentElement.setAttribute('data-bs-theme', theme);
}

/**
 * @returns {'dark'|'light'} Stored preference if set; otherwise returns 'light'.
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
    return 'light';
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

/**
 * @returns {boolean} Whether compact table density is enabled.
 */
export function getCompactTables() {
    try {
        return localStorage.getItem(COMPACT_TABLES_STORAGE_KEY) === 'true';
    } catch {
        return false;
    }
}

/**
 * @param {boolean} enabled
 */
export function setCompactTables(enabled) {
    try {
        localStorage.setItem(COMPACT_TABLES_STORAGE_KEY, enabled ? 'true' : 'false');
    } catch {
        // ignore
    }
}
