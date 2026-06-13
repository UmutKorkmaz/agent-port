import type { ReadinessTone } from "../../app/operator-data";
import { fallbackProviderCatalog, getProviderCatalogDetail } from "../../app/model-catalog-data";
import { isPendingEndpoint } from "@/lib/api";
import type {
  BootstrapState,
  ControlStatus,
  DocumentAsset,
  IngestionJob,
  LocalModelStatus,
  ModelProvider,
  ModelRoute,
  ProviderCardModel,
} from "@/lib/types";

export function resultToControlStatus(
  result: { ok: boolean; status: number; text: string },
  loadedLabel: string,
  missingLabel: string,
  loadedDetail: string,
): ControlStatus {
  if (result.ok) {
    return {
      label: loadedLabel,
      detail: loadedDetail,
      tone: "ready",
    };
  }

  const pending = isPendingEndpoint(result.status);
  return {
    label: pending ? missingLabel : `${loadedLabel} error`,
    detail: pending ? "Endpoint is missing or not enabled yet; UI fallback is active." : shortenText(result.text || "Request failed.", 240),
    tone: pending ? "placeholder" : "pending",
  };
}

export function buildProviderCards(
  providers: ModelProvider[],
  routes: ModelRoute[],
  providerNameById: Map<string, string>,
  bootstrapProviderId?: string,
): ProviderCardModel[] {
  const cards = new Map<string, ProviderCardModel>();

  for (const provider of fallbackProviderCatalog) {
    cards.set(provider.name, {
      ...provider,
      source: "fallback",
    });
  }

  for (const provider of providers) {
    const detail = getProviderCatalogDetail(provider.name ?? provider.label);
    const normalizedName = detail.name;
    cards.set(normalizedName, {
      ...detail,
      label: provider.label ?? detail.label,
      kind: provider.kind ?? detail.kind,
      useCase: provider.useCase ?? detail.useCase,
      advantage: provider.advantage ?? detail.advantage,
      disadvantage: provider.disadvantage ?? detail.disadvantage,
      cost: provider.cost ?? detail.cost,
      privacy: provider.privacy ?? detail.privacy,
      lockIn: provider.lockIn ?? detail.lockIn,
      keyMode: provider.keyMode ?? detail.keyMode,
      capabilities: stringArray(provider.capabilities, detail.capabilities),
      id: provider.id,
      baseUrl: provider.baseUrl,
      isEnabled: provider.isEnabled,
      source: "platform",
    });
  }

  for (const route of routes) {
    const providerName = getRouteProviderName(route, providerNameById, bootstrapProviderId);
    const detail = getProviderCatalogDetail(providerName);
    const existing = cards.get(detail.name);
    if (existing) {
      cards.set(detail.name, {
        ...existing,
        isEnabled: existing.isEnabled ?? route.isEnabled ?? true,
        source: existing.source === "fallback" ? "route" : existing.source,
      });
      continue;
    }

    cards.set(detail.name, {
      ...detail,
      id: route.providerId,
      isEnabled: route.isEnabled,
      source: "route",
    });
  }

  return Array.from(cards.values()).sort((a, b) => {
    if (a.name === "ollama") return -1;
    if (b.name === "ollama") return 1;
    if (Boolean(a.isEnabled) !== Boolean(b.isEnabled)) return a.isEnabled ? -1 : 1;
    if (a.source !== b.source) return sourceWeight(a.source) - sourceWeight(b.source);
    return a.label.localeCompare(b.label);
  });
}

function sourceWeight(source: ProviderCardModel["source"]) {
  if (source === "platform") return 0;
  if (source === "route") return 1;
  return 2;
}

function stringArray(value: unknown, fallback: string[]) {
  return Array.isArray(value) && value.every((item) => typeof item === "string") ? value : fallback;
}

export function getRouteProviderName(route: ModelRoute | null, providerNameById: Map<string, string>, bootstrapProviderId?: string) {
  if (!route) {
    return "";
  }
  if (route.providerName) {
    return route.providerName;
  }
  if (route.providerId && providerNameById.has(route.providerId)) {
    return providerNameById.get(route.providerId) ?? "";
  }
  if (route.providerId && bootstrapProviderId && route.providerId === bootstrapProviderId) {
    return "ollama";
  }
  return route.providerId ? `provider ${route.providerId.slice(0, 8)}` : "unknown provider";
}

export function getRouteControlStatus(route: ModelRoute | null, routesStatus: ControlStatus): ControlStatus {
  if (!route) {
    return {
      label: "No route selected",
      detail: routesStatus.detail,
      tone: routesStatus.tone,
    };
  }

  if (route.isEnabled === false) {
    return {
      label: "Route disabled",
      detail: `${route.name} is loaded but marked disabled.`,
      tone: "pending",
    };
  }

  return {
    label: route.isDefault ? "Default route" : "Route enabled",
    detail: `${route.name} routes ${route.routeType ?? "chat"} traffic to ${route.modelName}.`,
    tone: "ready",
  };
}

export function formatRouteLabel(route: ModelRoute | null, providerName: string) {
  if (!route) {
    return "No route selected";
  }
  return `${route.name} / ${providerName || "unknown"} / ${route.modelName}`;
}

export function lookupProviderName(providerId: string | undefined, providerNameById: Map<string, string>) {
  if (!providerId) {
    return "unknown";
  }
  return providerNameById.get(providerId) ?? providerId.slice(0, 8);
}

export function summarizeLocalModelStatus(status: LocalModelStatus | null | undefined) {
  if (!status) {
    return "No local-model status endpoint response yet.";
  }
  const health = status.routeHealth ?? status.status ?? "reported";
  const model = status.modelName ?? status.model ?? "model not reported";
  const enabled = typeof status.enabled === "boolean" ? (status.enabled ? "enabled" : "disabled") : "enabled unknown";
  return `${health} - ${model} - ${enabled}`;
}

export function getLocalModelTone(status: LocalModelStatus | null | undefined): ReadinessTone {
  if (!status) {
    return "placeholder";
  }
  const health = (status.routeHealth ?? status.status ?? "").toLowerCase();
  if (health.includes("ok") || health.includes("ready") || health.includes("healthy") || health.includes("operational")) {
    return "ready";
  }
  if (health.includes("error") || health.includes("fail") || status.errorType) {
    return "pending";
  }
  return status.enabled === false ? "placeholder" : "ready";
}

export function formatPriceValue(value: number | string | undefined, currency?: string) {
  if (value === undefined || value === null || value === "") {
    return "-";
  }
  const numeric = typeof value === "number" ? value : Number(value);
  if (!Number.isFinite(numeric)) {
    return String(value);
  }
  return `${currency ?? "USD"} ${numeric.toFixed(6)}`;
}

function shortenText(value: string, length: number) {
  return value.length > length ? `${value.slice(0, length - 3)}...` : value;
}

export function looksLikeBootstrap(data: unknown): data is BootstrapState {
  if (!data || typeof data !== "object") {
    return false;
  }
  const candidate = data as Partial<BootstrapState>;
  return Boolean(candidate.workspaceId && candidate.projectId && candidate.datasetId && candidate.agentDefinitionId);
}

export function extractBootstrapSeed(data: unknown): BootstrapState | null {
  if (!data || typeof data !== "object") {
    return null;
  }
  const candidate = data as { seed?: unknown; Seed?: unknown };
  const seed = candidate.seed ?? candidate.Seed;
  return looksLikeBootstrap(seed) ? seed : null;
}

export function inferDocumentsFromJobs(jobs: IngestionJob[], datasetId?: string): DocumentAsset[] {
  const documents = new Map<string, DocumentAsset>();
  for (const job of jobs) {
    if (datasetId && job.datasetId !== datasetId) {
      continue;
    }
    const existing = documents.get(job.documentAssetId);
    if (existing) {
      documents.set(job.documentAssetId, {
        ...existing,
        status: job.status,
        chunkCount: Math.max(existing.chunkCount ?? 0, job.chunkCount),
        latestIngestionJobId: job.id,
        updatedAt: job.createdAt,
      });
      continue;
    }
    documents.set(job.documentAssetId, {
      documentAssetId: job.documentAssetId,
      datasetId: job.datasetId,
      status: job.status,
      chunkCount: job.chunkCount,
      latestIngestionJobId: job.id,
      createdAt: job.createdAt,
      updatedAt: job.createdAt,
      source: "ingestion-job",
    });
  }
  return Array.from(documents.values());
}

export function getDocumentId(document: DocumentAsset) {
  return document.id ?? document.documentAssetId ?? document.latestIngestionJobId ?? "unknown-document";
}

export function getDocumentName(document: DocumentAsset) {
  return document.fileName ?? document.name ?? `Document ${getDocumentId(document).slice(0, 8)}`;
}

export function getDocumentChunks(document: DocumentAsset) {
  return document.chunkCount ?? 0;
}

export function parseJsonArray<T>(value: string): T[] {
  try {
    const parsed = JSON.parse(value);
    return Array.isArray(parsed) ? (parsed as T[]) : [];
  } catch {
    return [];
  }
}

export function formatDate(value?: string | null) {
  if (!value) {
    return "not dated";
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return new Intl.DateTimeFormat("en", {
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}

export function formatScore(value?: number | null) {
  return Number(value ?? 0).toFixed(3);
}

export function formatCost(value?: number | null) {
  return `$${Number(value ?? 0).toFixed(6)}`;
}

export function clampNumber(value: number, min: number, max: number, fallback: number) {
  if (!Number.isFinite(value)) {
    return fallback;
  }
  return Math.min(max, Math.max(min, value));
}

export function escapeHtmlAttribute(value: string) {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("\"", "&quot;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;");
}
