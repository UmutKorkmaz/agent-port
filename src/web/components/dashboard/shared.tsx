"use client";

import type { ReactNode } from "react";
import Image from "next/image";
import { useTranslations } from "next-intl";

import type { SectionDetail } from "../../app/operator-data";
import type { ControlStatus, LocalModelStatus, ModelRoute, ServiceState } from "@/lib/types";
import { formatRouteLabel, getRouteProviderName, summarizeLocalModelStatus } from "./helpers";

export function SectionHeader({
  title,
  subtitle,
  actions,
  status,
}: {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
  status?: ReactNode;
}) {
  return (
    <div className="section-header">
      <div>
        <h2>{title}</h2>
        {subtitle ? <p className="panel-copy">{subtitle}</p> : null}
      </div>
      {(actions || status) && (
        <div className="section-header-actions">
          {status}
          {actions}
        </div>
      )}
    </div>
  );
}

export function ControlStatusCard({ status }: { status: ControlStatus }) {
  return (
    <article className="check-card">
      <div className="check-head">
        <p className="check-label">{status.label}</p>
        <span className={`status-pill ${status.tone}`}>{status.tone}</span>
      </div>
      <p className="check-detail">{status.detail}</p>
    </article>
  );
}

export function StatusCard({ label, state, detail }: { label: string; state: ServiceState; detail: string }) {
  const tone = state === "ok" ? "ready" : state === "error" ? "pending" : "placeholder";
  return (
    <article className="check-card">
      <div className="check-head">
        <p className="check-label">{label}</p>
        <span className={`status-pill ${tone}`}>{state}</span>
      </div>
      <p className="check-detail">{detail}</p>
    </article>
  );
}

export function Metric({ label, value, detail }: { label: string; value: string; detail: string }) {
  return (
    <article className="metric-card">
      <p className="metric-label">{label}</p>
      <p className="metric-value compact">{value}</p>
      <p className="metric-detail">{detail}</p>
    </article>
  );
}

export function RouteSelector({
  label,
  routes,
  value,
  providerNameById,
  bootstrapProviderId,
  onChange,
}: {
  label: string;
  routes: ModelRoute[];
  value: string;
  providerNameById: Map<string, string>;
  bootstrapProviderId?: string;
  onChange: (value: string) => void;
}) {
  const t = useTranslations("routeStatus");
  return (
    <label className="field-row route-selector">
      <span>{label}</span>
      <select className="text-input" value={value} onChange={(event) => onChange(event.target.value)} disabled={routes.length === 0}>
        {routes.length === 0 ? <option value="">{t("noRoutesLoaded")}</option> : null}
        {routes.map((route) => (
          <option value={route.id} key={route.id}>
            {formatRouteLabel(route, getRouteProviderName(route, providerNameById, bootstrapProviderId))}
          </option>
        ))}
      </select>
    </label>
  );
}

export function RouteStatusSummary({
  route,
  providerName,
  status,
  localModelStatus,
}: {
  route: ModelRoute | null;
  providerName: string;
  status: ControlStatus;
  localModelStatus: LocalModelStatus | null;
}) {
  const t = useTranslations("routeStatus");
  const tCommon = useTranslations("common");
  return (
    <article className="route-status-panel">
      <div className="asset-head">
        <p className="asset-label">{t("selectedRoute")}</p>
        <span className={`status-pill ${status.tone}`}>{status.tone}</span>
      </div>
      <p className="asset-detail">{status.detail}</p>
      <dl className="detail-list">
        <div>
          <dt>{t("provider")}</dt>
          <dd>{providerName || tCommon("notSelected")}</dd>
        </div>
        <div>
          <dt>{t("route")}</dt>
          <dd className="font-mono">{route?.name ?? t("noRouteSelected")}</dd>
        </div>
        <div>
          <dt>{t("model")}</dt>
          <dd className="font-mono">{route?.modelName ?? localModelStatus?.modelName ?? localModelStatus?.model ?? tCommon("notLoaded")}</dd>
        </div>
        <div>
          <dt>{t("localHealth")}</dt>
          <dd>{summarizeLocalModelStatus(localModelStatus)}</dd>
        </div>
      </dl>
    </article>
  );
}

export function SectionView({ section }: { section: SectionDetail }) {
  const t = useTranslations("sectionView");
  const tSection = useTranslations(`sections.${section.slug}`);
  const primaryItems = tSection.raw("primaryItems") as string[];
  const secondaryItems = tSection.raw("secondaryItems") as string[];

  return (
    <div className="section-grid">
      <section className="section-card">
        <SectionHeader title={t("operatorSurface")} subtitle={t("operatorSurfaceCopy")} />
        <ul>
          {primaryItems.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
      </section>

      <aside className="empty-state" aria-label={t("currentState", { title: tSection("title") })}>
        <p className="empty-title">{tSection("emptyTitle")}</p>
        <p className="empty-copy">{tSection("emptyCopy")}</p>
        <span className="status-pill placeholder">{tSection("status")}</span>
      </aside>

      <section className="section-card">
        <SectionHeader title={t("integrationQueue")} subtitle={t("integrationQueueCopy")} />
        <ul>
          {secondaryItems.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
      </section>

      <section className="section-card">
        <SectionHeader title={t("serviceLinks")} subtitle={t("serviceLinksCopy")} />
        <ul>
          <li>
            <a href="/api/platform/status">{t("platformApiStatus")}</a>
          </li>
          <li>
            <a href="/api/ai/health">{t("aiServiceHealth")}</a>
          </li>
        </ul>
      </section>
    </div>
  );
}

/*
 * Branded empty state — a quiet AgentPort logo watermark anchors the surface
 * while a single primary action moves the operator forward. The watermark is
 * decorative (alt="") and theme-swapped by .brand-watermark-mark in brand.css.
 * Reused by DatasetsView (no documents) and TracesView (no runs) so the empty
 * voice is consistent and intentional rather than a generic flat placeholder.
 */
export function EmptyStateBrand({
  title,
  copy,
  watermarkLabel,
  action,
  className,
}: {
  title: string;
  copy: string;
  watermarkLabel: string;
  action?: ReactNode;
  className?: string;
}) {
  return (
    <article className={`asset-card empty-inline brand-watermark${className ? ` ${className}` : ""}`} aria-label={title}>
      <Image
        src="/agentport-logo.png"
        alt=""
        width={56}
        height={56}
        aria-hidden="true"
        className="brand-watermark-mark brand-watermark-mark-light"
      />
      <Image
        src="/agentport-logo-dark.png"
        alt=""
        width={56}
        height={56}
        aria-hidden="true"
        className="brand-watermark-mark brand-watermark-mark-dark"
      />
      <div className="brand-watermark-body">
        <span className="asset-label brand-watermark-kicker">{watermarkLabel}</span>
        <p className="empty-title">{title}</p>
        <p className="empty-copy">{copy}</p>
        {action ? <div className="brand-watermark-action">{action}</div> : null}
      </div>
    </article>
  );
}
