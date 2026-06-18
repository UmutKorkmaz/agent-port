"use client";

import { useTranslations } from "next-intl";

import type {
  CatalogApiStatuses,
  ControlStatus,
  LocalModelStatus,
  ModelProvider,
  ModelRoute,
  ProviderCardModel,
  ProviderPriceSnapshot,
  ServiceState,
} from "@/lib/types";
import { ControlStatusCard, Metric, RouteSelector, RouteStatusSummary, StatusCard } from "./shared";
import {
  formatDate,
  formatPriceValue,
  formatRouteLabel,
  getLocalModelTone,
  getRouteProviderName,
  lookupProviderName,
} from "./helpers";
import { useDashboard } from "./dashboard-context";

export function ModelCatalogView() {
  const {
    providerCards,
    modelProviders,
    modelRoutes,
    priceSnapshots,
    localModelStatus,
    catalogStatuses,
    platformState,
    aiState,
    selectedModelRoute,
    selectedRouteProviderName,
    routeStatus,
    providerNameById,
    bootstrap,
    busy,
    setBusy,
    refreshModelCatalog,
    setSelectedModelRouteId,
  } = useDashboard();

  return (
    <ModelCatalogContent
      providerCards={providerCards}
      providers={modelProviders}
      routes={modelRoutes}
      prices={priceSnapshots}
      localModelStatus={localModelStatus}
      catalogStatuses={catalogStatuses}
      platformState={platformState}
      aiState={aiState}
      selectedRoute={selectedModelRoute}
      selectedRouteProviderName={selectedRouteProviderName}
      routeStatus={routeStatus}
      providerNameById={providerNameById}
      bootstrapProviderId={bootstrap?.modelProviderId}
      busy={busy === "refresh-model-catalog"}
      onRefreshStart={async () => {
        setBusy("refresh-model-catalog");
        await refreshModelCatalog(bootstrap?.workspaceId);
        setBusy(null);
      }}
      onSelectRoute={setSelectedModelRouteId}
    />
  );
}

function ModelCatalogContent({
  providerCards,
  providers,
  routes,
  prices,
  localModelStatus,
  catalogStatuses,
  platformState,
  aiState,
  selectedRoute,
  selectedRouteProviderName,
  routeStatus,
  providerNameById,
  bootstrapProviderId,
  busy,
  onRefreshStart,
  onSelectRoute,
}: {
  providerCards: ProviderCardModel[];
  providers: ModelProvider[];
  routes: ModelRoute[];
  prices: ProviderPriceSnapshot[];
  localModelStatus: LocalModelStatus | null;
  catalogStatuses: CatalogApiStatuses;
  platformState: ServiceState;
  aiState: ServiceState;
  selectedRoute: ModelRoute | null;
  selectedRouteProviderName: string;
  routeStatus: ControlStatus;
  providerNameById: Map<string, string>;
  bootstrapProviderId?: string;
  busy: boolean;
  onRefreshStart: () => Promise<void>;
  onSelectRoute: (value: string) => void;
}) {
  const t = useTranslations("modelCatalog");
  const tCommon = useTranslations("common");
  const platformProviderCount = providers.length;
  const enabledProviderCount = providerCards.filter((provider) => provider.isEnabled).length;
  const fallbackCount = providerCards.filter((provider) => provider.source === "fallback").length;
  const routeCount = routes.length;

  return (
    <div className="catalog-page">
      <nav className="catalog-tabs" aria-label={t("sectionsAriaLabel")}>
        <a className="catalog-tab" href="#catalog-providers">{t("tabProviders")}</a>
        <a className="catalog-tab" href="#catalog-routes">{t("tabRoutes")}</a>
        <a className="catalog-tab" href="#catalog-local-ollama">{t("tabLocalOllama")}</a>
        <a className="catalog-tab" href="#catalog-prices">{t("tabPrices")}</a>
        <a className="catalog-tab" href="#catalog-health">{t("tabHealth")}</a>
      </nav>

      <section className="catalog-summary" aria-label={t("summaryAriaLabel")}>
        <div>
          <p className="eyebrow">{t("eyebrow")}</p>
          <h2 className="panel-title">{t("summaryTitle")}</h2>
          <p className="panel-copy">
            {t("summaryCopy")}
          </p>
        </div>
        <div className="button-row">
          <button className="control-button secondary" type="button" onClick={() => void onRefreshStart()} disabled={busy}>
            {busy ? t("refreshing") : t("refreshCatalog")}
          </button>
        </div>
      </section>

      <div className="metric-grid">
        <Metric label={t("providerCards")} value={`${providerCards.length}`} detail={t("providerCardsDetail", { platform: platformProviderCount, fallback: fallbackCount })} />
        <Metric label={t("enabledProviders")} value={`${enabledProviderCount}`} detail={t("enabledProvidersDetail")} />
        <Metric label={t("routes")} value={`${routeCount}`} detail={routeCount > 0 ? t("routesLoadedDetail") : t("routesFallbackDetail")} />
        <Metric label={t("selectedRoute")} value={formatRouteLabel(selectedRoute, selectedRouteProviderName)} detail={routeStatus.detail} />
      </div>

      <section className="catalog-section" id="catalog-providers">
        <div className="panel-header compact">
          <div>
            <h2>{t("providersTitle")}</h2>
            <p className="panel-copy">{t("providersCopy")}</p>
          </div>
          <span className={`status-pill ${catalogStatuses.providers.tone}`}>{catalogStatuses.providers.label}</span>
        </div>
        <div className="provider-grid">
          {providerCards.map((provider) => (
            <article className="provider-card" key={provider.name}>
              <div className="provider-card-head">
                <div>
                  <h3>{provider.label}</h3>
                  <p>{provider.kind}</p>
                </div>
                <div className="provider-badges">
                  <span className={`status-pill ${provider.isEnabled ? "ready" : provider.source === "fallback" ? "placeholder" : "pending"}`}>
                    {provider.isEnabled ? t("enabled") : provider.source === "fallback" ? t("reference") : t("disabled")}
                  </span>
                  <span className="mini-badge placeholder">{provider.source}</span>
                </div>
              </div>
              <p className="provider-use-case">{provider.useCase}</p>
              <dl className="provider-field-grid">
                <div>
                  <dt>{t("advantage")}</dt>
                  <dd>{provider.advantage}</dd>
                </div>
                <div>
                  <dt>{t("disadvantage")}</dt>
                  <dd>{provider.disadvantage}</dd>
                </div>
                <div>
                  <dt>{t("cost")}</dt>
                  <dd>{provider.cost}</dd>
                </div>
                <div>
                  <dt>{t("privacy")}</dt>
                  <dd>{provider.privacy}</dd>
                </div>
                <div>
                  <dt>{t("lockIn")}</dt>
                  <dd>{provider.lockIn}</dd>
                </div>
                <div>
                  <dt>{t("keyMode")}</dt>
                  <dd>{provider.keyMode}</dd>
                </div>
              </dl>
              {provider.baseUrl ? <p className="asset-detail">{t("baseUrl", { url: provider.baseUrl })}</p> : null}
              <div className="capability-row">
                {provider.capabilities.map((capability) => (
                  <span className="mini-badge ready" key={`${provider.name}-${capability}`}>{capability}</span>
                ))}
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className="catalog-section" id="catalog-routes">
        <div className="panel-header compact">
          <div>
            <h2>{t("routesTitle")}</h2>
            <p className="panel-copy">{t("routesCopy")}</p>
          </div>
          <span className={`status-pill ${catalogStatuses.routes.tone}`}>{catalogStatuses.routes.label}</span>
        </div>
        <div className="catalog-control-row">
          <RouteSelector
            label={t("activeRoute")}
            routes={routes}
            value={selectedRoute?.id ?? ""}
            providerNameById={providerNameById}
            bootstrapProviderId={bootstrapProviderId}
            onChange={onSelectRoute}
          />
          <RouteStatusSummary
            route={selectedRoute}
            providerName={selectedRouteProviderName}
            status={routeStatus}
            localModelStatus={localModelStatus}
          />
        </div>
        <div className="route-grid">
          {routes.length === 0 ? (
            <article className="empty-state">
              <p className="empty-title">{t("noRoutesTitle")}</p>
              <p className="empty-copy">{catalogStatuses.routes.detail}</p>
              <span className={`status-pill ${catalogStatuses.routes.tone}`}>{catalogStatuses.routes.tone}</span>
            </article>
          ) : null}
          {routes.map((route) => {
            const providerName = getRouteProviderName(route, providerNameById, bootstrapProviderId);
            return (
              <article className="route-card" key={route.id}>
                <div className="asset-head">
                  <p className="asset-label">{route.name}</p>
                  <span className={`status-pill ${route.isEnabled === false ? "pending" : "ready"}`}>
                    {route.isEnabled === false ? t("disabled") : route.isDefault ? t("default") : t("enabled")}
                  </span>
                </div>
                <dl className="detail-list">
                  <div>
                    <dt>{t("provider")}</dt>
                    <dd>{providerName}</dd>
                  </div>
                  <div>
                    <dt>{t("model")}</dt>
                    <dd>{route.modelName}</dd>
                  </div>
                  <div>
                    <dt>{t("type")}</dt>
                    <dd>{route.routeType ?? t("chat")}</dd>
                  </div>
                  <div>
                    <dt>{t("priority")}</dt>
                    <dd>{route.priority ?? 100}</dd>
                  </div>
                </dl>
              </article>
            );
          })}
        </div>
      </section>

      <section className="catalog-section" id="catalog-local-ollama">
        <div className="panel-header compact">
          <div>
            <h2>{t("localOllamaTitle")}</h2>
            <p className="panel-copy">{t("localOllamaCopy")}</p>
          </div>
          <span className={`status-pill ${catalogStatuses.localModel.tone}`}>{catalogStatuses.localModel.label}</span>
        </div>
        <div className="local-ollama-grid">
          <RouteStatusSummary
            route={selectedRoute}
            providerName={selectedRouteProviderName}
            status={routeStatus}
            localModelStatus={localModelStatus}
          />
          <article className="route-card">
            <div className="asset-head">
              <p className="asset-label">{t("ollamaRuntime")}</p>
              <span className={`status-pill ${getLocalModelTone(localModelStatus)}`}>{localModelStatus?.status ?? localModelStatus?.routeHealth ?? tCommon("notReported")}</span>
            </div>
            <dl className="detail-list">
              <div>
                <dt>{t("model")}</dt>
                <dd>{localModelStatus?.modelName ?? localModelStatus?.model ?? selectedRoute?.modelName ?? "llama3.2"}</dd>
              </div>
              <div>
                <dt>{t("baseUrlLabel")}</dt>
                <dd>{localModelStatus?.baseUrl ?? "http://localhost:11434"}</dd>
              </div>
              <div>
                <dt>{t("endpoint")}</dt>
                <dd>{catalogStatuses.localModel.detail}</dd>
              </div>
              <div>
                <dt>{t("installedModels")}</dt>
                <dd>{localModelStatus?.models?.length ? localModelStatus.models.join(", ") : tCommon("notReported")}</dd>
              </div>
            </dl>
          </article>
        </div>
      </section>

      <section className="catalog-section" id="catalog-prices">
        <div className="panel-header compact">
          <div>
            <h2>{t("pricesTitle")}</h2>
            <p className="panel-copy">{t("pricesCopy")}</p>
          </div>
          <span className={`status-pill ${catalogStatuses.prices.tone}`}>{catalogStatuses.prices.label}</span>
        </div>
        {prices.length > 0 ? (
          <>
            {/* Wide screen: horizontally scrollable table inside .price-table-wrap. */}
            <div className="table-scroll price-table-wrap">
              <table className="price-table">
                <thead>
                  <tr>
                    <th>{t("priceProvider")}</th>
                    <th>{t("priceModel")}</th>
                    <th>{t("inputPerMillion")}</th>
                    <th>{t("outputPerMillion")}</th>
                    <th>{t("request")}</th>
                    <th>{t("captured")}</th>
                  </tr>
                </thead>
                <tbody>
                  {prices.map((price, index) => (
                    <tr key={price.id ?? `${price.providerId}-${price.modelName}-${index}`}>
                      <td>{price.providerName ?? lookupProviderName(price.providerId, providerNameById)}</td>
                      <td>{price.modelName ?? tCommon("unknown")}</td>
                      <td>{formatPriceValue(price.inputTokenPricePerMillion, price.currency)}</td>
                      <td>{formatPriceValue(price.outputTokenPricePerMillion, price.currency)}</td>
                      <td>{formatPriceValue(price.requestPrice, price.currency)}</td>
                      <td>{formatDate(price.capturedAt ?? price.createdAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {/* Narrow screen: stacked price cards (hidden at >=768px via CSS). */}
            <ul className="price-card-list" aria-label={t("pricesTitle")}>
              {prices.map((price, index) => (
                <li className="price-card" key={price.id ?? `${price.providerId}-${price.modelName}-${index}-card`}>
                  <div className="price-card-head">
                    <div>
                      <p className="price-card-title">{price.providerName ?? lookupProviderName(price.providerId, providerNameById)}</p>
                      <p className="price-card-subtitle">{price.modelName ?? tCommon("unknown")}</p>
                    </div>
                  </div>
                  <div className="price-card-grid">
                    <div className="price-card-cell is-cost">
                      <span className="price-card-cell-label">{t("inputPerMillion")}</span>
                      <span className="price-card-cell-value">{formatPriceValue(price.inputTokenPricePerMillion, price.currency)}</span>
                    </div>
                    <div className="price-card-cell is-cost">
                      <span className="price-card-cell-label">{t("outputPerMillion")}</span>
                      <span className="price-card-cell-value">{formatPriceValue(price.outputTokenPricePerMillion, price.currency)}</span>
                    </div>
                    <div className="price-card-cell">
                      <span className="price-card-cell-label">{t("request")}</span>
                      <span className="price-card-cell-value">{formatPriceValue(price.requestPrice, price.currency)}</span>
                    </div>
                    <div className="price-card-cell">
                      <span className="price-card-cell-label">{t("captured")}</span>
                      <span className="price-card-cell-value">{formatDate(price.capturedAt ?? price.createdAt)}</span>
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          </>
        ) : (
          <div className="price-fallback-grid">
            {providerCards.slice(0, 8).map((provider) => (
              <article className="route-card" key={`price-${provider.name}`}>
                <p className="asset-label">{provider.label}</p>
                <p className="asset-detail">{provider.cost}</p>
              </article>
            ))}
          </div>
        )}
      </section>

      <section className="catalog-section" id="catalog-health">
        <div className="panel-header compact">
          <div>
            <h2>{t("healthTitle")}</h2>
            <p className="panel-copy">{t("healthCopy")}</p>
          </div>
        </div>
        <div className="check-grid">
          <StatusCard label={t("platformApi")} state={platformState} detail="/api/platform/status" />
          <StatusCard label={t("aiService")} state={aiState} detail="/api/ai/health" />
          <ControlStatusCard status={catalogStatuses.status} />
          <ControlStatusCard status={catalogStatuses.catalog} />
          <ControlStatusCard status={catalogStatuses.providers} />
          <ControlStatusCard status={catalogStatuses.routes} />
          <ControlStatusCard status={catalogStatuses.prices} />
          <ControlStatusCard status={catalogStatuses.localModel} />
        </div>
      </section>
    </div>
  );
}
