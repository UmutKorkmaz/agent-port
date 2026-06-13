"use client";

import { useTranslations } from "next-intl";

import { ControlStatusCard, RouteSelector, RouteStatusSummary } from "./shared";
import { clampNumber, formatCost, formatRouteLabel, formatScore } from "./helpers";
import { useDashboard } from "./dashboard-context";

export function PlaygroundView() {
  const t = useTranslations("playground");
  const tChat = useTranslations("chat");
  const {
    chatResult,
    qualityBlocked,
    askAgent,
    question,
    setQuestion,
    ragTopK,
    setRagTopK,
    scoreThreshold,
    setScoreThreshold,
    modelRoutes,
    selectedModelRoute,
    providerNameById,
    bootstrap,
    setSelectedModelRouteId,
    apiKeyInput,
    setApiKeyInput,
    showNoAnswer,
    setShowNoAnswer,
    busy,
    fallbackMode,
    selectedRouteProviderName,
    bestScore,
    qualityChunks,
    retrievedChunks,
    answerText,
    runId,
    traceId,
    chatRun,
    estimatedCost,
    loadTrace,
    routeStatus,
    localModelStatus,
    traceStatus,
    selectedTrace,
  } = useDashboard();

  return (
    <div className="section-grid">
      <section className="section-card">
        <div className="panel-header compact">
          <div>
            <h2>{t("askTitle")}</h2>
            <p className="panel-copy">{t("askCopy")}</p>
          </div>
          <span className={`status-pill ${qualityBlocked ? "pending" : chatResult ? "ready" : "placeholder"}`}>
            {chatResult ? (qualityBlocked ? t("belowThreshold") : t("qualityPass")) : t("notRun")}
          </span>
        </div>
        <form className="control-form stacked" onSubmit={askAgent}>
          <textarea className="question-input" value={question} onChange={(event) => setQuestion(event.target.value)} rows={4} />
          <div className="control-grid">
            <label className="field-row">
              <span>{t("topK")}</span>
              <input
                className="text-input"
                type="number"
                min="1"
                max="12"
                value={ragTopK}
                onChange={(event) => setRagTopK(clampNumber(event.target.valueAsNumber, 1, 12, 4))}
              />
            </label>
            <label className="field-row">
              <span>{t("scoreThreshold")}</span>
              <input
                className="text-input"
                type="number"
                min="0"
                max="1"
                step="0.05"
                value={scoreThreshold}
                onChange={(event) => setScoreThreshold(clampNumber(event.target.valueAsNumber, 0, 1, 0.2))}
              />
            </label>
            <RouteSelector
              label={t("modelRoute")}
              routes={modelRoutes}
              value={selectedModelRoute?.id ?? ""}
              providerNameById={providerNameById}
              bootstrapProviderId={bootstrap?.modelProviderId}
              onChange={setSelectedModelRouteId}
            />
            <label className="field-row">
              <span>{t("apiKey")}</span>
              <input
                className="text-input"
                value={apiKeyInput}
                onChange={(event) => setApiKeyInput(event.target.value)}
                placeholder={bootstrap?.apiKey ? t("bootstrapKeyLoaded") : t("pasteLocalApiKey")}
              />
            </label>
            <label className="toggle-row">
              <input
                type="checkbox"
                checked={showNoAnswer}
                onChange={(event) => setShowNoAnswer(event.target.checked)}
              />
              <span>{t("showNoAnswer")}</span>
            </label>
          </div>
          <button className="control-button" type="submit" disabled={busy === "chat"}>
            {busy === "chat" ? t("asking") : t("ask")}
          </button>
        </form>
        {chatResult ? (
          <div className="answer-block">
            <div className="result-head">
              <p className="answer-label">{t("answerLabel")}</p>
              <div className="meta-row">
                <span className="status-pill ready">{fallbackMode}</span>
                <span>{t("routeLabel", { route: formatRouteLabel(selectedModelRoute, selectedRouteProviderName) })}</span>
                <span>{t("bestScore", { score: formatScore(bestScore) })}</span>
                <span>{t("visibleChunks", { visible: qualityChunks.length, total: retrievedChunks.length })}</span>
              </div>
            </div>
            <p className="answer-text">{answerText}</p>
            <div className="meta-row">
              <span>{t("run", { value: runId ?? t("notCaptured") })}</span>
              <span>{t("trace", { value: traceId ?? t("notCaptured") })}</span>
              <span>{t("latency", { value: chatRun ? t("latencyMs", { ms: chatRun.latencyMs }) : t("pendingRefresh") })}</span>
              <span>{t("costEstimate", { value: formatCost(estimatedCost) })}</span>
              {traceId ? (
                <button className="inline-button" type="button" onClick={() => void loadTrace(traceId)}>
                  {t("loadTrace")}
                </button>
              ) : null}
            </div>
          </div>
        ) : null}
      </section>
      <aside className="section-card">
        <h2>{t("routeStatusTitle")}</h2>
        <RouteStatusSummary
          route={selectedModelRoute}
          providerName={selectedRouteProviderName}
          status={routeStatus}
          localModelStatus={localModelStatus}
        />
      </aside>
      <aside className="section-card">
        <div className="panel-header compact">
          <div>
            <h2>{t("citationsTitle")}</h2>
            <p className="panel-copy">{t("citationsCopy")}</p>
          </div>
        </div>
        <div className="chunk-list">
          {(chatResult?.citations ?? []).map((citation, index) => (
            <article className="asset-card" key={`${citation.citation_id ?? citation.citationId}-${index}`}>
              <div className="asset-head">
                <p className="asset-label">{citation.citation_id ?? citation.citationId ?? t("citationFallback")}</p>
                <span className={`status-pill ${(citation.score ?? 0) >= scoreThreshold ? "ready" : "pending"}`}>
                  {formatScore(citation.score)}
                </span>
              </div>
              <p className="asset-detail">
                <span className="citation-source-label">{tChat("citationLabel")}: </span>
                {citation.file_name ?? citation.fileName ?? t("unknownFile")}
              </p>
            </article>
          ))}
          {qualityChunks.map((chunk) => (
            <article className="asset-card" key={chunk.id}>
              <div className="asset-head">
                <p className="asset-label">{(chunk.citation_id ?? chunk.citationId) || chunk.id}</p>
                <span className="status-pill ready">{formatScore(chunk.score)}</span>
              </div>
              <p className="asset-detail">{chunk.text.slice(0, 240)}</p>
            </article>
          ))}
          {chatResult && qualityChunks.length === 0 ? (
            <article className="asset-card empty-inline">
              <p className="asset-label">{t("noChunksTitle")}</p>
              <p className="asset-detail">{t("noChunksCopy")}</p>
            </article>
          ) : null}
        </div>
      </aside>
      <aside className="section-card">
        <h2>{t("tracePreviewTitle")}</h2>
        <ControlStatusCard status={traceStatus} />
        {selectedTrace ? (
          <dl className="detail-list">
            <div>
              <dt>{t("trace2")}</dt>
              <dd>{selectedTrace.id}</dd>
            </div>
            <div>
              <dt>{t("tokens")}</dt>
              <dd>{t("tokensSummary", { input: selectedTrace.inputTokens, output: selectedTrace.outputTokens })}</dd>
            </div>
            <div>
              <dt>{t("cost")}</dt>
              <dd>{formatCost(selectedTrace.costAmount)}</dd>
            </div>
          </dl>
        ) : null}
      </aside>
    </div>
  );
}
