"use client";

import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useTransition } from "react";

const SUPPORTED_LOCALES = ["en", "tr"] as const;
type AppLocale = (typeof SUPPORTED_LOCALES)[number];

// One year, in seconds — keeps the chosen locale sticky across sessions.
const COOKIE_MAX_AGE = 60 * 60 * 24 * 365;

export function LocaleToggle() {
  const t = useTranslations("locale");
  const activeLocale = useLocale();
  const router = useRouter();
  const [isPending, startTransition] = useTransition();

  function selectLocale(next: AppLocale) {
    if (next === activeLocale) {
      return;
    }
    document.cookie = `NEXT_LOCALE=${next}; path=/; max-age=${COOKIE_MAX_AGE}; samesite=lax`;
    startTransition(() => {
      router.refresh();
    });
  }

  return (
    <div className="locale-toggle" role="group" aria-label={t("label")}>
      {SUPPORTED_LOCALES.map((locale) => {
        const isActive = locale === activeLocale;
        return (
          <button
            key={locale}
            type="button"
            className={`locale-toggle-option${isActive ? " is-active" : ""}`}
            aria-pressed={isActive}
            disabled={isPending}
            onClick={() => selectLocale(locale)}
          >
            {locale.toUpperCase()}
          </button>
        );
      })}
    </div>
  );
}
