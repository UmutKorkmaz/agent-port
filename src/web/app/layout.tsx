import "./globals.css";

import type { ReactNode } from "react";

import { NextIntlClientProvider } from "next-intl";
import { getLocale, getMessages } from "next-intl/server";

import { OperatorNavigation } from "./operator-navigation";

export const metadata = {
  title: "AgentPort",
  description: "AI systems creation, training, testing, deployment, and operations",
};

export default async function RootLayout({
  children,
}: {
  children: ReactNode;
}) {
  const locale = await getLocale();
  const messages = await getMessages();

  return (
    <html lang={locale}>
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
