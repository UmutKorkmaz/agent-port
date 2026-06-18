"use client";

import { useTranslations } from "next-intl";

import { SectionHeader } from "./shared";
import { useDashboard } from "./dashboard-context";

export function WalletView() {
  const t = useTranslations("wallet");
  const { section, bootstrap, estimatedCost } = useDashboard();
  const tSection = useTranslations(`sections.${section.slug}`);

  return (
    <div className="section-grid">
      <section className="section-card">
        <SectionHeader title={tSection("title")} subtitle={t("chargingCopy")} />
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
