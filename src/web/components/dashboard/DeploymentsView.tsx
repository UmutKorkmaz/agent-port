"use client";

import { useTranslations } from "next-intl";

import { escapeHtmlAttribute } from "./helpers";
import { useDashboard } from "./dashboard-context";

export function DeploymentsView() {
  const t = useTranslations("deployments");
  const {
    apiKeyValue,
    apiKeyInput,
    setApiKeyInput,
    bootstrap,
    apiKeyMode,
    setApiKeyMode,
    apiKeyHeader,
    apiKeyHeaderValue,
    agentIdForSnippet,
    ragTopK,
    scoreThreshold,
    widgetOrigin,
    setWidgetOrigin,
    widgetTitle,
    setWidgetTitle,
    widgetMode,
    setWidgetMode,
    widgetTheme,
    setWidgetTheme,
    widgetUrl,
  } = useDashboard();

  return (
    <div className="section-grid">
      <section className="section-card">
        <div className="panel-header compact">
          <div>
            <h2>{t("localApiTitle")}</h2>
            <p className="panel-copy">{t("localApiCopy")}</p>
          </div>
          <span className={`status-pill ${apiKeyValue ? "ready" : "placeholder"}`}>
            {apiKeyValue ? t("keyStaged") : t("keyOptional")}
          </span>
        </div>
        <div className="control-grid">
          <label className="field-row">
            <span>{t("apiKey")}</span>
            <input
              className="text-input"
              value={apiKeyInput}
              onChange={(event) => setApiKeyInput(event.target.value)}
              placeholder={bootstrap?.apiKey ? t("bootstrapKeyLoaded") : t("pasteLocalKey")}
            />
          </label>
          <label className="field-row">
            <span>{t("headerMode")}</span>
            <select className="text-input" value={apiKeyMode} onChange={(event) => setApiKeyMode(event.target.value as "header" | "bearer")}>
              <option value="header">x-agentport-api-key</option>
              <option value="bearer">{t("bearer")}</option>
            </select>
          </label>
        </div>
        <p className="asset-detail">{bootstrap?.apiKeyMessage ?? t("apiKeyMessageFallback")}</p>
        <pre className="code-block">{`curl -X POST http://localhost:5001/api/v1/agent-definitions/${agentIdForSnippet}/chat \\
  -H "content-type: application/json" \\
  -H "${apiKeyHeader}: ${apiKeyHeaderValue}" \\
  -d '{"question":"${t("sampleQuestion")}","topK":${ragTopK},"scoreThreshold":${scoreThreshold}}'`}</pre>
      </section>
      <aside className="section-card">
        <div className="panel-header compact">
          <div>
            <h2>{t("widgetConfigTitle")}</h2>
            <p className="panel-copy">{t("widgetConfigCopy")}</p>
          </div>
          <span className="status-pill placeholder">{t("placeholder")}</span>
        </div>
        <div className="control-grid">
          <label className="field-row">
            <span>{t("origin")}</span>
            <input className="text-input" value={widgetOrigin} onChange={(event) => setWidgetOrigin(event.target.value)} />
          </label>
          <label className="field-row">
            <span>{t("title")}</span>
            <input className="text-input" value={widgetTitle} onChange={(event) => setWidgetTitle(event.target.value)} />
          </label>
          <label className="field-row">
            <span>{t("mode")}</span>
            <select className="text-input" value={widgetMode} onChange={(event) => setWidgetMode(event.target.value as "embedded" | "floating")}>
              <option value="embedded">{t("embedded")}</option>
              <option value="floating">{t("floating")}</option>
            </select>
          </label>
          <label className="field-row">
            <span>{t("theme")}</span>
            <select className="text-input" value={widgetTheme} onChange={(event) => setWidgetTheme(event.target.value as "system" | "light" | "dark")}>
              <option value="system">{t("system")}</option>
              <option value="light">{t("light")}</option>
              <option value="dark">{t("dark")}</option>
            </select>
          </label>
        </div>
        <pre className="code-block">{`<iframe
  title="${escapeHtmlAttribute(widgetTitle)}"
  src="${widgetUrl}"
  width="420"
  height="640"
></iframe>`}</pre>
        <p className="asset-detail">{t("widgetFooter")}</p>
      </aside>
    </div>
  );
}
