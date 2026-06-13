import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

import { isSupportedLocale, negotiateLocale } from "@/i18n/request";

// No [locale] routing: the middleware only ensures a NEXT_LOCALE cookie exists
// so subsequent requests (and the request-scoped getRequestConfig) resolve a
// stable locale. Resolution order: existing cookie -> Accept-Language
// (Turkish-preferred) -> default ("en").
export function middleware(request: NextRequest) {
  const response = NextResponse.next();

  const existing = request.cookies.get("NEXT_LOCALE")?.value;
  if (!isSupportedLocale(existing)) {
    const locale = negotiateLocale(request.headers.get("accept-language"));
    response.cookies.set("NEXT_LOCALE", locale, { path: "/", sameSite: "lax" });
  }

  return response;
}

export const config = {
  // Run on app routes only; skip Next internals and static asset requests.
  matcher: ["/((?!_next|.*\\..*).*)"],
};
