"use client";

import { useTranslations } from "next-intl";

import { useDashboard } from "./dashboard-context";

export function WalletView() {
  const t = useTranslations("wallet");
  const { section, bootstrap, estimatedCost } = useDashboard();
  const tSection = useTranslations(`sections.${section.slug}`);

  return (
    <div className="section-grid">
      <section className="section-card">
        <h2>{tSection("title")}</h2>
        <ul>
          <li>{t("chargingDisabled")}</li>
          <li>{t("walletId", { id: bootstrap?.walletAccountId ?? t("walletIdFallback") })}</li>
          <li>{t("latestRunCost", { cost: estimatedCost.toFixed(6) })}</li>
          <li>{t("paymentPlaceholder")}</li>
        </ul>
      </section>
      <aside className="empty-state">
        <p className="empty-title">{t("readOnlyTitle")}</p>
        <p className="empty-copy">{t("readOnlyCopy")}</p>
        <span className="status-pill placeholder">{t("noCharging")}</span>
      </aside>
    </div>
  );
}
