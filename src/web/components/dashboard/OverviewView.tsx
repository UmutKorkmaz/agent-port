"use client";

import { useTranslations } from "next-intl";

import { overviewMetrics, readinessChecks, setupQueue } from "../../app/operator-data";
import { ControlStatusCard, Metric, StatusCard } from "./shared";
import { useDashboard } from "./dashboard-context";

export function OverviewView() {
  const t = useTranslations("overview");
  const tReadiness = useTranslations("readiness");
  const tMetrics = useTranslations("overviewMetrics");
  const tQueue = useTranslations("setupQueue");
  const {
    bootstrap,
    activeDataset,
    platformState,
    aiState,
    busy,
    devStatuses,
    bootstrapLocal,
    runDevAction,
  } = useDashboard();

  return (
    <div className="overview-grid">
      <section className="panel">
        <div className="panel-header">
          <div>
            <h2 className="panel-title">{t("firstRunTitle")}</h2>
            <p className="panel-copy">{t("firstRunCopy")}</p>
          </div>
          <div className="button-row">
            <button className="control-button" type="button" onClick={bootstrapLocal} disabled={busy === "bootstrap"}>
              {busy === "bootstrap" ? t("creating") : t("runOnboarding")}
            </button>
            <button className="control-button secondary" type="button" onClick={() => runDevAction("seed")} disabled={busy === "dev-seed"}>
              {busy === "dev-seed" ? t("seeding") : t("seed")}
            </button>
            <button className="control-button danger" type="button" onClick={() => runDevAction("reset")} disabled={busy === "dev-reset"}>
              {busy === "dev-reset" ? t("resetting") : t("reset")}
            </button>
          </div>
        </div>
        <div className="check-grid">
          <StatusCard label={t("platformApi")} state={platformState} detail="/api/platform/status" />
          <StatusCard label={t("aiService")} state={aiState} detail="/api/ai/health" />
          <StatusCard label={t("workspace")} state={bootstrap ? "ok" : "checking"} detail={bootstrap?.workspaceId ?? t("runOnboardingDetail")} />
          <StatusCard
            label={t("dataset")}
            state={activeDataset ? "ok" : "checking"}
            detail={activeDataset ? t("docsChunks", { documents: activeDataset.documentCount, chunks: activeDataset.chunkCount }) : t("noDatasetLoaded")}
          />
          <ControlStatusCard status={devStatuses.seed} />
          <ControlStatusCard status={devStatuses.reset} />
        </div>
      </section>

      <aside className="panel">
        <div className="panel-header">
          <div>
            <h2 className="panel-title">{t("localFlowTitle")}</h2>
            <p className="panel-copy">{t("localFlowCopy")}</p>
          </div>
        </div>
        <div className="queue-grid">
          {setupQueue.map((item) => (
            <article className="queue-card" key={item.key}>
              <div className="queue-head">
                <p className="queue-label">{tQueue(`${item.key}.label`)}</p>
                <span className={`mini-badge ${item.tone}`}>{item.tone}</span>
              </div>
              <p className="queue-detail">{tQueue(`${item.key}.detail`)}</p>
            </article>
          ))}
        </div>
      </aside>

      <section className="panel">
        <div className="panel-header">
          <div>
            <h2 className="panel-title">{t("workspaceSnapshotTitle")}</h2>
            <p className="panel-copy">{t("workspaceSnapshotCopy")}</p>
          </div>
        </div>
        <div className="metric-grid">
          {overviewMetrics.map((metric) => (
            <article className="metric-card" key={metric.key}>
              <p className="metric-label">{tMetrics(`${metric.key}.label`)}</p>
              <p className="metric-value">{tMetrics(`${metric.key}.value`)}</p>
              <p className="metric-detail">{tMetrics(`${metric.key}.detail`)}</p>
            </article>
          ))}
          {bootstrap ? (
            <>
              <Metric label={t("agentLabel")} value={bootstrap.agentDefinitionId} detail={t("agentDetail")} />
              <Metric label={t("knowledgeBaseLabel")} value={bootstrap.knowledgeBaseId} detail={t("knowledgeBaseDetail")} />
            </>
          ) : null}
        </div>
      </section>

      <section className="panel">
        <div className="panel-header">
          <div>
            <h2 className="panel-title">{t("readinessTitle")}</h2>
            <p className="panel-copy">{t("readinessCopy")}</p>
          </div>
        </div>
        <div className="check-grid">
          {readinessChecks.map((item) => (
            <article className="check-card" key={item.key}>
              <div className="check-head">
                <p className="check-label">{tReadiness(`${item.key}.label`)}</p>
                <span className={`status-pill ${item.tone}`}>{tReadiness(`${item.key}.state`)}</span>
              </div>
              <p className="check-detail">{tReadiness(`${item.key}.detail`)}</p>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
}
