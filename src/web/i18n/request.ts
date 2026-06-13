import { cookies, headers } from "next/headers";
import { getRequestConfig } from "next-intl/server";

// "Without i18n routing" mode: the active locale is derived from the
// NEXT_LOCALE cookie (set by the middleware / LocaleToggle) and falls back to
// Accept-Language negotiation, then to the default. No [locale] URL segment.
export const SUPPORTED_LOCALES = ["en", "tr"] as const;
export type AppLocale = (typeof SUPPORTED_LOCALES)[number];
export const DEFAULT_LOCALE: AppLocale = "en";

export function isSupportedLocale(value: string | undefined): value is AppLocale {
  return value !== undefined && (SUPPORTED_LOCALES as readonly string[]).includes(value);
}

// Turkish-preferred negotiation: only the first Accept-Language entry decides.
export function negotiateLocale(acceptLanguage: string | null): AppLocale {
  const first = (acceptLanguage ?? "").split(",")[0] ?? "";
  return /\btr\b/i.test(first) ? "tr" : DEFAULT_LOCALE;
}

async function resolveLocale(): Promise<AppLocale> {
  const cookieStore = await cookies();
  const cookieLocale = cookieStore.get("NEXT_LOCALE")?.value;
  if (isSupportedLocale(cookieLocale)) {
    return cookieLocale;
  }

  const headerStore = await headers();
  return negotiateLocale(headerStore.get("accept-language"));
}

export default getRequestConfig(async () => {
  const locale = await resolveLocale();
  const messages = (await import(`../messages/${locale}.json`)).default;
  return { locale, messages };
});
