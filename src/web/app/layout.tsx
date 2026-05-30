import "./globals.css";

import type { ReactNode } from "react";

import { OperatorNavigation } from "./operator-navigation";

export const metadata = {
  title: "AgentPort",
  description: "AI systems creation, training, testing, deployment, and operations",
};

export default function RootLayout({
  children,
}: {
  children: ReactNode;
}) {
  return (
    <html lang="en">
      <body>
        <div className="app-shell">
          <OperatorNavigation />
          <main className="workspace" id="workspace">
            {children}
          </main>
        </div>
      </body>
    </html>
  );
}
