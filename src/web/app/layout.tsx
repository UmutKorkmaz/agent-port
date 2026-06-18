import "./globals.css";
import "./brand.css";

import type { ReactNode } from "react";
import type { Metadata, Viewport } from "next";

import { IBM_Plex_Mono, IBM_Plex_Sans } from "next/font/google";
import { NextIntlClientProvider } from "next-intl";
import { getLocale, getMessages } from "next-intl/server";

import { OperatorNavigation } from "./operator-navigation";

/*
 * Type pairing: IBM Plex Sans for body / UI and IBM Plex Mono for code-like
 * surfaces (route ids, trace ids, model names, prices, token counts). Plex
 * carries the "sovereign infrastructure" voice — quiet, precise, engineered —
 * and the sans/mono pairing reads as a deliberate system, not a Tailwind default.
 *
 * Exposed as --font-sans / --font-mono on <html>; brand.css binds <body> to the
 * sans variable and provides the .font-mono utility.
 */
const plexSans = IBM_Plex_Sans({
  subsets: ["latin", "latin-ext"],
  weight: ["400", "500", "600", "700"],
  variable: "--font-sans",
  display: "swap",
});

const plexMono = IBM_Plex_Mono({
  subsets: ["latin", "latin-ext"],
  weight: ["400", "500", "600"],
  variable: "--font-mono",
  display: "swap",
});

export const metadata: Metadata = {
  title: "AgentPort",
  description: "AI systems creation, training, testing, deployment, and operations",
  // Explicit metadata.icons (preferred over the App-Router icon.png convention)
  // so Next.js deterministically serves the right sized assets from /public:
  //   - favicon.ico as the legacy default
  //   - favicon-16.png / favicon-32.png for HiDPI <link rel="icon"> entries
  //   - apple-touch-icon.png for iOS home-screen
  // The full agentport-logo(-dark).png files are brand marks used in the UI
  // chrome, not favicons, so they are intentionally not referenced here.
  icons: {
    icon: [
      { url: "/favicon.ico", sizes: "any" },
      { url: "/favicon-16.png", sizes: "16x16", type: "image/png" },
      { url: "/favicon-32.png", sizes: "32x32", type: "image/png" },
    ],
    apple: [{ url: "/apple-touch-icon.png", sizes: "180x180", type: "image/png" }],
  },
};

export const viewport: Viewport = {
  themeColor: [
    { media: "(prefers-color-scheme: light)", color: "#f6f7f9" },
    { media: "(prefers-color-scheme: dark)", color: "#161d20" },
  ],
};

/**
 * Runs before hydration to apply the persisted color theme to <html>.
 *
 * Without this the server-rendered markup assumes light tokens and the page
 * would flash light-then-dark for users who chose dark (FOUC). localStorage is
 * the source of truth; we fall back to the OS preference, and finally to light.
 * Kept as a string blob so it executes synchronously in <head> before paint.
 */
const themeInitScript = `
(function () {
  try {
    var stored = window.localStorage.getItem('agentport-theme');
    if (stored === 'light' || stored === 'dark') {
      document.documentElement.setAttribute('data-theme', stored);
      return;
    }
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
      document.documentElement.setAttribute('data-theme', 'dark');
    }
  } catch (e) {
    /* localStorage unavailable; fall through to default tokens */
  }
})();
`;

export default async function RootLayout({
  children,
}: {
  children: ReactNode;
}) {
  // Locale stays dynamic (resolved from NEXT_LOCALE / Accept-Language); <html lang>
  // must not be hardcoded so SSR + client hydration agree on the document language.
  const locale = await getLocale();
  const messages = await getMessages();

  return (
    <html lang={locale} className={`${plexSans.variable} ${plexMono.variable}`} suppressHydrationWarning>
      <head>
        <script dangerouslySetInnerHTML={{ __html: themeInitScript }} />
      </head>
      <body>
        <NextIntlClientProvider locale={locale} messages={messages}>
          <div className="app-shell">
            <OperatorNavigation />
            <main className="workspace" id="workspace">
              {children}
            </main>
          </div>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
