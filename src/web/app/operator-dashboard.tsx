import { getTranslations } from "next-intl/server";

import { OperatorDashboardClient } from "./operator-dashboard-client";
import { sectionDetails, SectionSlug } from "./operator-data";

export async function OperatorDashboard({ sectionSlug }: { sectionSlug: SectionSlug }) {
  const section = sectionDetails[sectionSlug];
  const t = await getTranslations();
  const sectionKey = `sections.${sectionSlug}` as const;

  return (
    <div className="operator-page">
      <header className="page-header">
        <div>
          <p className="eyebrow">{t(`${sectionKey}.eyebrow`)}</p>
          <h1 className="page-title">{t(`${sectionKey}.title`)}</h1>
          <p className="page-copy">{t(`${sectionKey}.summary`)}</p>
        </div>
        <aside className="environment-panel" aria-label={t("shell.workspaceStatus")}>
          <div className="environment-row">
            <span className="environment-label">{t("shell.environment")}</span>
            <span className="environment-value">{t("shell.environmentValue")}</span>
          </div>
          <div className="environment-row">
            <span className="environment-label">{t("shell.phase")}</span>
            <span className="environment-value">{t("shell.phaseValue")}</span>
          </div>
          <div className="environment-row">
            <span className="environment-label">{t("shell.viewStatus")}</span>
            <span className="environment-value">{t(`${sectionKey}.status`)}</span>
          </div>
        </aside>
      </header>

      <OperatorDashboardClient section={section} sectionSlug={sectionSlug} />
    </div>
  );
}
