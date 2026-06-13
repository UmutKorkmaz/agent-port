"use client";

import { useTranslations } from "next-intl";

import { useDashboard } from "./dashboard-context";

export function EvalsView() {
  const t = useTranslations("evals");
  const { busy, setBusy, evalRuns, ensureBootstrap, refreshEvalRuns } = useDashboard();

  return (
    <div className="section-grid">
      <section className="section-card">
        <h2>{t("evalRunsTitle")}</h2>
        <button
          className="control-button"
          type="button"
          disabled={busy === "refresh-evals"}
          onClick={async () => {
            setBusy("refresh-evals");
            const context = await ensureBootstrap();
            await refreshEvalRuns(context.workspaceId);
            setBusy(null);
          }}
        >
          {t("refresh")}
        </button>
        <ul>
          {evalRuns.length === 0 ? <li>{t("noEvalRuns")}</li> : null}
          {evalRuns.map((er) => (
            <li key={er.id}>
              {t("evalRunSummary", { status: er.status, score: er.score ?? "-", threshold: er.threshold ?? "-", outcome: er.passed ? t("passed") : t("failed") })}
            </li>
          ))}
        </ul>
      </section>
      <aside className="section-card">
        <h2>{t("evalGatesTitle")}</h2>
        <p className="asset-detail">{t("evalGatesCopy")}</p>
        <ul>
          <li>{t("gateExactMatch")}</li>
          <li>{t("gateCitation")}</li>
          <li>{t("gateLatency")}</li>
          <li>{t("gateCost")}</li>
          <li>{t("gateSafety")}</li>
        </ul>
      </aside>
    </div>
  );
}
