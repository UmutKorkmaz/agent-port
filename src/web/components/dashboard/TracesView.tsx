"use client";

import Link from "next/link";
import { useTranslations } from "next-intl";

import type { Citation, RetrievedChunk } from "@/lib/types";
import { ControlStatusCard, EmptyStateBrand } from "./shared";
import { formatCost, formatDate, parseJsonArray } from "./helpers";
import { useDashboard } from "./dashboard-context";

export function TracesView() {
  const t = useTranslations("traces");
  const tCommon = useTranslations("common");
  const {
    runs,
    busy,
    setBusy,
    bootstrap,
    refreshRuns,
    loadTrace,
    traceStatus,
    selectedTrace,
    humanReviews,
    ensureBootstrap,
    refreshHumanReviews,
  } = useDashboard();

  return (
    <div className="section-grid">
      <section className="section-card">
        <div className="panel-header compact">
          <div>
            <h2>{t("recentRunsTitle")}</h2>
            <p className="panel-copy">{t("recentRunsCopy")}</p>
          </div>
          <button
            className="control-button secondary"
            type="button"
            onClick={async () => {
              setBusy("refresh-runs");
              await refreshRuns(bootstrap?.agentDefinitionId);
              setBusy(null);
            }}
            disabled={busy === "refresh-runs"}
          >
            {t("refresh")}
          </button>
        </div>
        <div className="trace-list">
          {runs.length === 0 ? (
            <EmptyStateBrand
              title={t("noRunsTitle")}
              copy={t("noRunsCopy")}
              watermarkLabel={t("noRunsWatermark")}
              action={
                <Link className="control-button" href="/playground">
                  {t("noRunsAction")}
                </Link>
              }
            />
          ) : null}
          {runs.map((run) => {
            const citations = parseJsonArray<Citation>(run.citationsJson);
            const chunks = parseJsonArray<RetrievedChunk>(run.retrievedChunksJson);
            return (
              <article className="asset-card trace-card" key={run.id}>
                <div className="asset-head">
                  <p className="asset-label">{run.question}</p>
                  <span className="status-pill ready">{run.fallbackMode}</span>
                </div>
                <p className="asset-detail">{run.answer.slice(0, 220)}</p>
                <div className="meta-row font-mono">
                  <span>{t("ms", { ms: run.latencyMs })}</span>
                  <span>{formatCost(run.estimatedCost)}</span>
                  <span>{t("citationsCount", { count: citations.length })}</span>
                  <span>{t("chunksCount", { count: chunks.length })}</span>
                  <span>{formatDate(run.createdAt)}</span>
                </div>
                <div className="button-row compact">
                  <button
                    className="control-button secondary"
                    type="button"
                    onClick={() => void loadTrace(run.traceRecordId)}
                    disabled={!run.traceRecordId || busy === `trace-${run.traceRecordId}`}
                  >
                    {busy === `trace-${run.traceRecordId}` ? t("loading") : t("loadTrace")}
                  </button>
                </div>
              </article>
            );
          })}
        </div>
      </section>
      <aside className="section-card">
        <div className="panel-header compact">
          <div>
            <h2>{t("traceDetailTitle")}</h2>
            <p className="panel-copy">{t("traceDetailCopy")}</p>
          </div>
        </div>
        <ControlStatusCard status={traceStatus} />
        {selectedTrace ? (
          <>
            <dl className="detail-list">
              <div>
                <dt>{t("traceId")}</dt>
                <dd className="font-mono">{selectedTrace.id}</dd>
              </div>
              <div>
                <dt>{t("correlation")}</dt>
                <dd className="font-mono">{selectedTrace.correlationId ?? tCommon("none")}</dd>
              </div>
              <div>
                <dt>{t("modelRoute")}</dt>
                <dd className="font-mono">{selectedTrace.modelRouteId ?? tCommon("none")}</dd>
              </div>
              <div>
                <dt>{t("tokens")}</dt>
                <dd>{t("tokensSummary", { input: selectedTrace.inputTokens, output: selectedTrace.outputTokens })}</dd>
              </div>
            </dl>
            <pre className="code-block">{selectedTrace.metadataJson}</pre>
          </>
        ) : null}
      </aside>
      <aside className="section-card">
        <h2>{t("humanReviewTitle")}</h2>
        <button
          className="control-button"
          type="button"
          disabled={busy === "refresh-reviews"}
          onClick={async () => {
            setBusy("refresh-reviews");
            const context = await ensureBootstrap();
            await refreshHumanReviews(context.workspaceId);
            setBusy(null);
          }}
        >
          {t("refresh")}
        </button>
        <ul>
          {humanReviews.length === 0 ? <li>{t("noReviews")}</li> : null}
          {humanReviews.map((r) => (
            <li key={r.id}>
              {t("reviewSummary", { queue: r.queue, status: r.status, label: r.label ?? t("noLabel"), reviewer: r.reviewerName ?? t("unassigned") })}
            </li>
          ))}
        </ul>
      </aside>
    </div>
  );
}
