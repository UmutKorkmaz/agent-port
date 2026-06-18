"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";

export type ThemePreference = "light" | "dark";

const STORAGE_KEY = "agentport-theme";

/**
 * Theme toggle for the operator console.
 *
 * Sets `data-theme` on <html> and persists the choice to localStorage. The
 * actual token swap is driven by CSS ([data-theme="dark"] overrides in
 * globals.css); this component only owns the attribute + persistence, and
 * reflects the logo swap via the `.brand-logo-*` rules in globals.css.
 *
 * Three-state semantics are intentionally avoided: the console ships a real
 * light + dark pair, and system preference is honored at first paint via the
 * no-flash script in layout.tsx. Once the user picks, that choice wins.
 */
export function ThemeToggle() {
  const t = useTranslations("theme");
  // Render nothing theme-specific until mounted: the inline script in layout
  // applies the correct attribute before hydration, so we just sync to it.
  const [resolved, setResolved] = useState<ThemePreference>("light");

  useEffect(() => {
    const current = readResolvedTheme();
    setResolved(current);

    function onStorage(event: StorageEvent) {
      if (event.key === STORAGE_KEY && event.newValue) {
        setResolved(normalizeTheme(event.newValue));
      }
    }
    window.addEventListener("storage", onStorage);
    return () => window.removeEventListener("storage", onStorage);
  }, []);

  function apply(next: ThemePreference) {
    document.documentElement.setAttribute("data-theme", next);
    try {
      window.localStorage.setItem(STORAGE_KEY, next);
    } catch {
      // localStorage may be unavailable (private mode / sandbox); the
      // attribute is still applied for the current session.
    }
    setResolved(next);
  }

  const isDark = resolved === "dark";

  return (
    <button
      type="button"
      className="theme-toggle"
      aria-pressed={isDark}
      aria-label={isDark ? t("switchToLight") : t("switchToDark")}
      title={isDark ? t("switchToLight") : t("switchToDark")}
      onClick={() => apply(isDark ? "light" : "dark")}
    >
      <span className="theme-toggle-glyph theme-toggle-glyph-sun" aria-hidden="true" />
      <span className="theme-toggle-glyph theme-toggle-glyph-moon" aria-hidden="true" />
    </button>
  );
}

function readResolvedTheme(): ThemePreference {
  if (typeof document !== "undefined") {
    const attr = document.documentElement.getAttribute("data-theme");
    if (attr === "light" || attr === "dark") {
      return attr;
    }
  }
  return "light";
}

export function normalizeTheme(value: string): ThemePreference {
  return value === "dark" ? "dark" : "light";
}

export const THEME_STORAGE_KEY = STORAGE_KEY;
