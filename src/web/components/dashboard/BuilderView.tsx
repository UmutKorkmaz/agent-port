"use client";

import { useTranslations } from "next-intl";

import { RouteSelector, RouteStatusSummary } from "./shared";
import { formatRouteLabel } from "./helpers";
import { useDashboard } from "./dashboard-context";

export function BuilderView() {
  const t = useTranslations("builder");
  const {
    routeStatus,
    modelRoutes,
    selectedModelRoute,
    providerNameById,
    bootstrap,
    setSelectedModelRouteId,
    activeDataset,
    selectedRouteProviderName,
    localModelStatus,
  } = useDashboard();

  return (
    <div className="section-grid">
      <section className="section-card">
        <div className="panel-header compact">
          <div>
            <h2>{t("documentQaTitle")}</h2>
            <p className="panel-copy">{t("documentQaCopy")}</p>
          </div>
          <span className={`status-pill ${routeStatus.tone}`}>{routeStatus.label}</span>
        </div>
        <RouteSelector
          label={t("builderRoute")}
          routes={modelRoutes}
          value={selectedModelRoute?.id ?? ""}
          providerNameById={providerNameById}
          bootstrapProviderId={bootstrap?.modelProviderId}
          onChange={setSelectedModelRouteId}
        />
        <ul>
          <li>{t("agentId", { id: bootstrap?.agentDefinitionId ?? t("agentIdFallback") })}</li>
          <li>{t("datasetId", { id: bootstrap?.datasetId ?? activeDataset?.id ?? t("datasetIdFallback") })}</li>
          <li>{t("knowledgeBase", { id: bootstrap?.knowledgeBaseId ?? activeDataset?.knowledgeBaseId ?? t("knowledgeBaseFallback") })}</li>
          <li>{t("selectedRoute", { route: formatRouteLabel(selectedModelRoute, selectedRouteProviderName) })}</li>
          <li>{t("retrieval")}</li>
          <li>{t("answerMode")}</li>
        </ul>
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
      <aside className="empty-state">
        <p className="empty-title">{t("trainingLabTitle")}</p>
        <p className="empty-copy">{t("trainingLabCopy")}</p>
        <span className="status-pill ready">{t("trainingActive")}</span>
      </aside>
    </div>
  );
}
