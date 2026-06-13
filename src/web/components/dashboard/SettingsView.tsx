"use client";

import { useTranslations } from "next-intl";

import { ControlStatusCard, Metric } from "./shared";
import { useDashboard } from "./dashboard-context";

export function SettingsView() {
  const t = useTranslations("settings");
  const {
    busy,
    runDevAction,
    devStatuses,
    bootstrap,
    agentIdForSnippet,
    widgetMode,
    widgetTheme,
  } = useDashboard();

  return (
    <div className="section-grid">
      <section className="section-card">
        <div className="panel-header compact">
          <div>
            <h2>{t("devControlsTitle")}</h2>
            <p className="panel-copy">{t("devControlsCopy")}</p>
          </div>
          <div className="button-row">
            <button className="control-button secondary" type="button" onClick={() => runDevAction("seed")} disabled={busy === "dev-seed"}>
              {busy === "dev-seed" ? t("seeding") : t("seed")}
            </button>
            <button className="control-button danger" type="button" onClick={() => runDevAction("reset")} disabled={busy === "dev-reset"}>
              {busy === "dev-reset" ? t("resetting") : t("reset")}
            </button>
          </div>
        </div>
        <div className="check-grid">
          <ControlStatusCard status={devStatuses.seed} />
          <ControlStatusCard status={devStatuses.reset} />
          <Metric label={t("workspace")} value={bootstrap?.workspaceId ?? t("notBootstrapped")} detail={t("workspaceDetail")} />
          <Metric label={t("apiKey")} value={bootstrap?.apiKey ? t("shownOnce") : t("notReturned")} detail={bootstrap?.apiKeyMessage ?? t("apiKeyMessageFallback")} />
        </div>
      </section>
      <aside className="section-card">
        <h2>{t("widgetPlaceholderTitle")}</h2>
        <dl className="detail-list">
          <div>
            <dt>{t("agent")}</dt>
            <dd>{agentIdForSnippet}</dd>
          </div>
          <div>
            <dt>{t("mode")}</dt>
            <dd>{widgetMode}</dd>
          </div>
          <div>
            <dt>{t("theme")}</dt>
            <dd>{widgetTheme}</dd>
          </div>
        </dl>
      </aside>
    </div>
  );
}
