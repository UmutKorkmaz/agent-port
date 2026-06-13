import { Dispatch, FormEvent, SetStateAction } from "react";

import {
  extractCollection,
  extractNestedObject,
  isPendingEndpoint,
  requestFirstAvailableCollection,
  requestFirstAvailableJson,
  requestOptionalCollection,
  requestOptionalJson,
} from "@/lib/api";
import type { WorkspaceState } from "@/lib/workspace-context";
import type {
  BootstrapState,
  CatalogApiStatuses,
  ChatResponse,
  ControlStatus,
  Dataset,
  DevAction,
  DocumentAsset,
  EvalRun,
  HumanReviewRecord,
  IngestionJob,
  LocalModelStatus,
  ModelProvider,
  ModelRoute,
  ModelVersion,
  ProviderPriceSnapshot,
  Run,
  ServiceState,
  TraceRecord,
  TrainingJob,
} from "@/lib/types";
import {
  extractBootstrapSeed,
  getDocumentId,
  getDocumentName,
  getLocalModelTone,
  looksLikeBootstrap,
  resultToControlStatus,
  summarizeLocalModelStatus,
} from "./helpers";

/**
 * Render-scoped dependency bundle for the dashboard action handlers.
 *
 * The factory is re-invoked on every render of the controller hook, so the
 * captured values (`bootstrap`, `activeDataset`, form inputs, etc.) stay as
 * fresh as the closures that lived inline in the original component. Behavior
 * is preserved exactly — only the location of the function bodies changed.
 */
export type DashboardActionDeps = Pick<
  WorkspaceState,
  | "setBootstrap"
  | "setDatasets"
  | "setJobs"
  | "setRuns"
  | "setTrainingJobs"
  | "setModelVersions"
  | "setEvalRuns"
  | "setHumanReviews"
  | "setDocuments"
  | "setModelProviders"
  | "setModelRoutes"
  | "setPriceSnapshots"
  | "setLocalModelStatus"
> & {
  bootstrap: BootstrapState | null;
  activeDataset: Dataset | null;
  question: string;
  selectedFile: File | null;
  apiKeyInput: string;
  apiKeyMode: "header" | "bearer";
  apiKeyHeader: string;
  ragTopK: number;
  scoreThreshold: number;
  selectedModelRoute: ModelRoute | null;
  setPlatformState: Dispatch<SetStateAction<ServiceState>>;
  setAiState: Dispatch<SetStateAction<ServiceState>>;
  setBusy: Dispatch<SetStateAction<string | null>>;
  setError: Dispatch<SetStateAction<string | null>>;
  setChatResult: Dispatch<SetStateAction<ChatResponse | null>>;
  setSelectedTrace: Dispatch<SetStateAction<TraceRecord | null>>;
  setDocumentStatus: Dispatch<SetStateAction<ControlStatus>>;
  setDevStatuses: Dispatch<SetStateAction<Record<DevAction, ControlStatus>>>;
  setTraceStatus: Dispatch<SetStateAction<ControlStatus>>;
  setCatalogStatuses: Dispatch<SetStateAction<CatalogApiStatuses>>;
};

export type DashboardActions = {
  refreshStatus: () => Promise<void>;
  refreshModelCatalog: (workspaceId?: string) => Promise<void>;
  refreshDatasets: (workspaceId?: string) => Promise<void>;
  refreshJobs: (datasetId?: string) => Promise<void>;
  refreshDocuments: (datasetId?: string) => Promise<void>;
  refreshRuns: (agentId?: string) => Promise<void>;
  refreshTrainingJobs: (workspaceId?: string) => Promise<void>;
  refreshModelVersions: (workspaceId?: string) => Promise<void>;
  refreshEvalRuns: (workspaceId?: string) => Promise<void>;
  refreshHumanReviews: (workspaceId?: string) => Promise<void>;
  bootstrapLocal: () => Promise<void>;
  ensureBootstrap: () => Promise<BootstrapState>;
  runDevAction: (action: DevAction) => Promise<void>;
  uploadDocument: (event: FormEvent<HTMLFormElement>) => Promise<void>;
  deleteDocument: (document: DocumentAsset) => Promise<void>;
  reingestDocument: (document: DocumentAsset) => Promise<void>;
  askAgent: (event: FormEvent<HTMLFormElement>) => Promise<void>;
  loadTrace: (targetTraceId?: string | null) => Promise<void>;
};

export function createDashboardActions(deps: DashboardActionDeps): DashboardActions {
  const {
    setBootstrap,
    setDatasets,
    setJobs,
    setRuns,
    setTrainingJobs,
    setModelVersions,
    setEvalRuns,
    setHumanReviews,
    setDocuments,
    setModelProviders,
    setModelRoutes,
    setPriceSnapshots,
    setLocalModelStatus,
    bootstrap,
    activeDataset,
    question,
    selectedFile,
    apiKeyInput,
    apiKeyMode,
    apiKeyHeader,
    ragTopK,
    scoreThreshold,
    selectedModelRoute,
    setPlatformState,
    setAiState,
    setBusy,
    setError,
    setChatResult,
    setSelectedTrace,
    setDocumentStatus,
    setDevStatuses,
    setTraceStatus,
    setCatalogStatuses,
  } = deps;

  async function refreshStatus() {
    const [platform, ai] = await Promise.allSettled([
      fetch("/api/platform/status", { cache: "no-store" }),
      fetch("/api/ai/health", { cache: "no-store" }),
    ]);
    setPlatformState(platform.status === "fulfilled" && platform.value.ok ? "ok" : "error");
    setAiState(ai.status === "fulfilled" && ai.value.ok ? "ok" : "error");
  }

  async function refreshModelCatalog(workspaceId?: string) {
    const workspaceQuery = workspaceId ? `?workspaceId=${workspaceId}` : "";
    const [
      statusResult,
      catalogResult,
      providerResult,
      routeResult,
      priceResult,
      localModelResult,
    ] = await Promise.all([
      requestOptionalJson<Record<string, unknown>>("/api/platform/status", { cache: "no-store" }),
      requestFirstAvailableJson<Record<string, unknown>>([
        `/api/platform/model-catalog${workspaceQuery}`,
        `/api/platform/catalog/models${workspaceQuery}`,
      ]),
      requestFirstAvailableCollection<ModelProvider>([
        `/api/platform/model-providers${workspaceQuery}`,
        `/api/platform/providers${workspaceQuery}`,
        `/api/platform/model-catalog/providers${workspaceQuery}`,
      ], ["providers", "items", "data"]),
      requestOptionalCollection<ModelRoute>(`/api/platform/model-routes${workspaceQuery}`, ["routes", "items", "data"]),
      requestFirstAvailableCollection<ProviderPriceSnapshot>([
        `/api/platform/provider-price-snapshots${workspaceQuery}`,
        `/api/platform/model-prices${workspaceQuery}`,
        `/api/platform/model-catalog/prices${workspaceQuery}`,
      ], ["prices", "items", "data", "snapshots"]),
      requestFirstAvailableJson<LocalModelStatus>([
        `/api/platform/local-model/status${workspaceQuery}`,
        `/api/platform/model-catalog/local-ollama${workspaceQuery}`,
        `/api/platform/model-catalog/local-model${workspaceQuery}`,
      ]),
    ]);

    const catalogProviders = catalogResult.ok ? extractCollection<ModelProvider>(catalogResult.data, ["providers", "items", "data"]) : [];
    const catalogRoutes = catalogResult.ok ? extractCollection<ModelRoute>(catalogResult.data, ["routes", "modelRoutes", "items"]) : [];
    const catalogPrices = catalogResult.ok
      ? extractCollection<ProviderPriceSnapshot>(catalogResult.data, ["prices", "priceSnapshots", "snapshots"])
      : [];
    const catalogLocalModel = catalogResult.ok ? extractNestedObject<LocalModelStatus>(catalogResult.data, ["localModel", "localOllama", "ollama"]) : null;

    const nextProviders = providerResult.ok ? providerResult.data : catalogProviders;
    const nextRoutes = routeResult.ok ? routeResult.data : catalogRoutes;
    const nextPrices = priceResult.ok ? priceResult.data : catalogPrices;
    const nextLocalModel = localModelResult.ok ? localModelResult.data : catalogLocalModel;

    setModelProviders(nextProviders ?? []);
    setModelRoutes(nextRoutes ?? []);
    setPriceSnapshots(nextPrices ?? []);
    setLocalModelStatus(nextLocalModel ?? null);
    setCatalogStatuses({
      status: statusResult.ok
        ? {
            label: "Platform status",
            detail: statusResult.text || "Platform status loaded.",
            tone: "ready",
          }
        : {
            label: isPendingEndpoint(statusResult.status) ? "Platform status pending" : "Platform status error",
            detail: statusResult.text || "Platform status could not be loaded.",
            tone: isPendingEndpoint(statusResult.status) ? "placeholder" : "pending",
          },
      catalog: resultToControlStatus(catalogResult, "Catalog endpoint", "Catalog endpoint missing", "Catalog endpoint loaded."),
      providers: providerResult.ok || catalogProviders.length > 0
        ? {
            label: providerResult.ok ? "Provider endpoint" : "Provider fallback",
            detail: providerResult.ok
              ? `${providerResult.data.length} providers loaded from the platform API.`
              : `${catalogProviders.length} providers loaded from the catalog payload.`,
            tone: "ready",
          }
        : resultToControlStatus(providerResult, "Provider endpoint", "Provider endpoint missing", "Provider endpoint loaded."),
      routes: routeResult.ok || catalogRoutes.length > 0
        ? {
            label: routeResult.ok ? "Route endpoint" : "Route fallback",
            detail: routeResult.ok
              ? `${routeResult.data.length} routes loaded from the platform API.`
              : `${catalogRoutes.length} routes loaded from the catalog payload.`,
            tone: "ready",
          }
        : resultToControlStatus(routeResult, "Route endpoint", "Route endpoint missing", "Route endpoint loaded."),
      prices: priceResult.ok || catalogPrices.length > 0
        ? {
            label: priceResult.ok ? "Price endpoint" : "Price fallback",
            detail: priceResult.ok
              ? `${priceResult.data.length} price snapshots loaded from the platform API.`
              : `${catalogPrices.length} price snapshots loaded from the catalog payload.`,
            tone: "ready",
          }
        : resultToControlStatus(priceResult, "Price endpoint", "Price endpoint missing", "Price endpoint loaded."),
      localModel: localModelResult.ok || nextLocalModel
        ? {
            label: localModelResult.ok ? "Local model endpoint" : "Local model fallback",
            detail: summarizeLocalModelStatus(nextLocalModel),
            tone: getLocalModelTone(nextLocalModel),
          }
        : resultToControlStatus(localModelResult, "Local model endpoint", "Local model endpoint missing", "Local model endpoint loaded."),
    });
  }

  async function refreshDatasets(workspaceId?: string) {
    const suffix = workspaceId ? `?workspaceId=${workspaceId}` : "";
    const result = await requestOptionalJson<Dataset[]>(`/api/platform/datasets${suffix}`, { cache: "no-store" });
    if (result.ok && result.data) {
      setDatasets(result.data);
    }
  }

  async function refreshJobs(datasetId?: string) {
    const targetDataset = datasetId ?? activeDataset?.id;
    if (!targetDataset) {
      setJobs([]);
      return;
    }
    const result = await requestOptionalJson<IngestionJob[]>(`/api/platform/ingestion-jobs?datasetId=${targetDataset}`, {
      cache: "no-store",
    });
    if (result.ok && result.data) {
      setJobs(result.data);
    }
  }

  async function refreshDocuments(datasetId?: string) {
    const targetDataset = datasetId ?? activeDataset?.id;
    if (!targetDataset) {
      setDocuments([]);
      setDocumentStatus({
        label: "Document endpoint",
        detail: "Run onboarding to create a dataset first.",
        tone: "placeholder",
      });
      return;
    }

    const result = await requestOptionalJson<DocumentAsset[]>(`/api/platform/documents?datasetId=${targetDataset}`, {
      cache: "no-store",
    });
    if (result.ok && result.data) {
      setDocuments(result.data.map((document) => ({ ...document, source: "api" })));
      setDocumentStatus({
        label: "Document list",
        detail: `${result.data.length} document records loaded from the platform API.`,
        tone: "ready",
      });
      return;
    }

    if (isPendingEndpoint(result.status)) {
      setDocuments([]);
      setDocumentStatus({
        label: "Document list pending",
        detail: "Showing document asset IDs inferred from ingestion jobs until the list endpoint exists.",
        tone: "placeholder",
      });
      return;
    }

    setDocuments([]);
    setDocumentStatus({
      label: "Document list error",
      detail: result.text || "Document list could not be loaded.",
      tone: "pending",
    });
  }

  async function refreshWorkspaceArtifacts(context: BootstrapState) {
    await Promise.all([
      refreshDatasets(context.workspaceId),
      refreshJobs(context.datasetId),
      refreshDocuments(context.datasetId),
      refreshRuns(context.agentDefinitionId),
      refreshTrainingJobs(context.workspaceId),
      refreshModelVersions(context.workspaceId),
      refreshEvalRuns(context.workspaceId),
      refreshHumanReviews(context.workspaceId),
      refreshModelCatalog(context.workspaceId),
    ]);
  }

  async function bootstrapLocalContext() {
    const response = await fetch("/api/platform/bootstrap/local", { method: "POST" });
    if (!response.ok) {
      throw new Error(await response.text());
    }
    const result = (await response.json()) as BootstrapState;
    setBootstrap(result);
    await refreshWorkspaceArtifacts(result);
    return result;
  }

  async function refreshRuns(agentId?: string) {
    const suffix = agentId ? `?agentId=${agentId}` : "";
    const result = await requestOptionalJson<Run[]>(`/api/platform/runs${suffix}`, { cache: "no-store" });
    if (result.ok && result.data) {
      setRuns(result.data);
    }
  }

  async function refreshTrainingJobs(workspaceId?: string) {
    const suffix = workspaceId ? `?workspaceId=${workspaceId}` : "";
    const result = await requestOptionalJson<TrainingJob[]>(`/api/platform/training-jobs${suffix}`, {
      cache: "no-store",
    });
    if (result.ok && result.data) {
      setTrainingJobs(result.data);
    }
  }

  async function refreshModelVersions(workspaceId?: string) {
    const suffix = workspaceId ? `?workspaceId=${workspaceId}` : "";
    const result = await requestOptionalJson<ModelVersion[]>(`/api/platform/model-versions${suffix}`, {
      cache: "no-store",
    });
    if (result.ok && result.data) {
      setModelVersions(result.data);
    }
  }

  async function refreshEvalRuns(workspaceId?: string) {
    const suffix = workspaceId ? `?workspaceId=${workspaceId}` : "";
    const result = await requestOptionalJson<EvalRun[]>(`/api/platform/eval-runs${suffix}`, {
      cache: "no-store",
    });
    if (result.ok && result.data) {
      setEvalRuns(result.data);
    }
  }

  async function refreshHumanReviews(workspaceId?: string) {
    const suffix = workspaceId ? `?workspaceId=${workspaceId}` : "";
    const result = await requestOptionalJson<HumanReviewRecord[]>(`/api/platform/human-review-records${suffix}`, {
      cache: "no-store",
    });
    if (result.ok && result.data) {
      setHumanReviews(result.data);
    }
  }

  async function bootstrapLocal() {
    setBusy("bootstrap");
    setError(null);
    try {
      await bootstrapLocalContext();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Bootstrap failed.");
    } finally {
      setBusy(null);
    }
  }

  async function ensureBootstrap() {
    if (bootstrap) {
      return bootstrap;
    }
    return bootstrapLocalContext();
  }

  async function runDevAction(action: DevAction) {
    if (action === "reset" && !window.confirm("Reset local dev data if the backend endpoint is available?")) {
      return;
    }

    setBusy(`dev-${action}`);
    setError(null);
    setDevStatuses((current) => ({
      ...current,
      [action]: {
        label: action === "reset" ? "Reset running" : "Seed running",
        detail: "Waiting for the platform API.",
        tone: "placeholder",
      },
    }));

    try {
      const result = await requestOptionalJson<BootstrapState | Record<string, unknown>>(`/api/platform/dev/${action}`, {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: action === "reset" ? JSON.stringify({ reseed: true }) : undefined,
      });

      if (result.ok) {
        const reseed = extractBootstrapSeed(result.data);
        if (action === "reset") {
          setBootstrap(null);
          setDatasets([]);
          setJobs([]);
          setDocuments([]);
          setRuns([]);
          setChatResult(null);
          setSelectedTrace(null);
          if (reseed) {
            setBootstrap(reseed);
            await refreshWorkspaceArtifacts(reseed);
          }
        } else if (looksLikeBootstrap(result.data)) {
          setBootstrap(result.data);
          await refreshWorkspaceArtifacts(result.data);
        } else if (reseed) {
          setBootstrap(reseed);
          await refreshWorkspaceArtifacts(reseed);
        } else if (bootstrap) {
          await refreshWorkspaceArtifacts(bootstrap);
        }

        setDevStatuses((current) => ({
          ...current,
          [action]: {
            label: action === "reset" ? "Reset completed" : "Seed completed",
            detail: result.text || "Dev endpoint completed successfully.",
            tone: "ready",
          },
        }));
        return;
      }

      if (action === "seed" && isPendingEndpoint(result.status)) {
        const context = await bootstrapLocalContext();
        setDevStatuses((current) => ({
          ...current,
          seed: {
            label: "Seed via bootstrap",
            detail: `Used /bootstrap/local fallback for workspace ${context.workspaceId}.`,
            tone: "ready",
          },
        }));
        return;
      }

      setDevStatuses((current) => ({
        ...current,
        [action]: {
          label: action === "reset" ? "Reset pending" : "Seed pending",
          detail: isPendingEndpoint(result.status)
            ? `/api/platform/dev/${action} is not available yet.`
            : result.text || "Dev action failed.",
          tone: isPendingEndpoint(result.status) ? "placeholder" : "pending",
        },
      }));
    } catch (err) {
      setDevStatuses((current) => ({
        ...current,
        [action]: {
          label: action === "reset" ? "Reset error" : "Seed error",
          detail: err instanceof Error ? err.message : "Dev action failed.",
          tone: "pending",
        },
      }));
    } finally {
      setBusy(null);
    }
  }

  async function uploadDocument(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedFile) {
      setError("Choose a .txt, .md, or .pdf file first.");
      return;
    }
    setBusy("upload");
    setError(null);
    try {
      const context = await ensureBootstrap();
      const form = new FormData();
      form.append("file", selectedFile);
      const response = await fetch(`/api/platform/datasets/${context.datasetId}/documents`, {
        method: "POST",
        body: form,
      });
      if (!response.ok) {
        throw new Error(await response.text());
      }
      await Promise.all([
        refreshDatasets(context.workspaceId),
        refreshJobs(context.datasetId),
        refreshDocuments(context.datasetId),
      ]);
      setDocumentStatus({
        label: "Upload complete",
        detail: `${selectedFile.name} was sent to ingestion.`,
        tone: "ready",
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Upload failed.");
    } finally {
      setBusy(null);
    }
  }

  async function deleteDocument(document: DocumentAsset) {
    const targetDataset = document.datasetId ?? activeDataset?.id;
    const targetDocument = getDocumentId(document);
    if (!targetDataset || !targetDocument) {
      setDocumentStatus({
        label: "Delete unavailable",
        detail: "A dataset and document id are required.",
        tone: "pending",
      });
      return;
    }

    if (!window.confirm(`Delete ${getDocumentName(document)} if the backend endpoint is available?`)) {
      return;
    }

    setBusy(`delete-${targetDocument}`);
    setDocumentStatus({
      label: "Delete requested",
      detail: targetDocument,
      tone: "placeholder",
    });

    const result = await requestOptionalJson(`/api/platform/documents/${targetDocument}`, {
      method: "DELETE",
    });

    if (result.ok || result.status === 204) {
      setDocuments((current) => current.filter((item) => getDocumentId(item) !== targetDocument));
      await Promise.all([refreshDatasets(bootstrap?.workspaceId), refreshJobs(targetDataset), refreshDocuments(targetDataset)]);
      setDocumentStatus({
        label: "Document deleted",
        detail: targetDocument,
        tone: "ready",
      });
    } else {
      setDocumentStatus({
        label: isPendingEndpoint(result.status) ? "Delete endpoint pending" : "Delete failed",
        detail: isPendingEndpoint(result.status)
          ? "The UI is wired, but the platform delete endpoint is not available yet."
          : result.text || "Document delete failed.",
        tone: isPendingEndpoint(result.status) ? "placeholder" : "pending",
      });
    }
    setBusy(null);
  }

  async function reingestDocument(document: DocumentAsset) {
    const targetDataset = document.datasetId ?? activeDataset?.id;
    const targetDocument = getDocumentId(document);
    if (!targetDataset || !targetDocument) {
      setDocumentStatus({
        label: "Reingest unavailable",
        detail: "A dataset and document id are required.",
        tone: "pending",
      });
      return;
    }

    setBusy(`reingest-${targetDocument}`);
    setDocumentStatus({
      label: "Reingest requested",
      detail: targetDocument,
      tone: "placeholder",
    });

    const result = await requestOptionalJson(`/api/platform/documents/${targetDocument}/reingest`, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ force: true }),
    });

    if (result.ok) {
      await Promise.all([refreshDatasets(bootstrap?.workspaceId), refreshJobs(targetDataset), refreshDocuments(targetDataset)]);
      setDocumentStatus({
        label: "Reingest queued",
        detail: targetDocument,
        tone: "ready",
      });
    } else {
      setDocumentStatus({
        label: isPendingEndpoint(result.status) ? "Reingest endpoint pending" : "Reingest failed",
        detail: isPendingEndpoint(result.status)
          ? "The UI is wired, but the platform reingest endpoint is not available yet."
          : result.text || "Document reingest failed.",
        tone: isPendingEndpoint(result.status) ? "placeholder" : "pending",
      });
    }
    setBusy(null);
  }

  async function askAgent(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!question.trim()) {
      setError("Enter a question first.");
      return;
    }
    setBusy("chat");
    setError(null);
    try {
      const context = await ensureBootstrap();
      const requestApiKey = apiKeyInput.trim() || context.apiKey || "";
      if (!requestApiKey) {
        throw new Error("Run onboarding first or paste an API key before using the playground.");
      }
      const response = await fetch(`/api/platform/agent-definitions/${context.agentDefinitionId}/chat`, {
        method: "POST",
        headers: {
          "content-type": "application/json",
          [apiKeyHeader]: apiKeyMode === "bearer" ? `Bearer ${requestApiKey}` : requestApiKey,
        },
        body: JSON.stringify({
          question: question.trim(),
          topK: ragTopK,
          scoreThreshold,
          modelRouteId: selectedModelRoute?.id,
        }),
      });
      if (!response.ok) {
        throw new Error(await response.text());
      }
      const result = (await response.json()) as ChatResponse;
      setChatResult(result);
      await refreshRuns(context.agentDefinitionId);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Chat failed.");
    } finally {
      setBusy(null);
    }
  }

  async function loadTrace(targetTraceId?: string | null) {
    if (!targetTraceId) {
      setTraceStatus({
        label: "Trace unavailable",
        detail: "This run does not have a trace id.",
        tone: "pending",
      });
      return;
    }

    setBusy(`trace-${targetTraceId}`);
    const result = await requestOptionalJson<TraceRecord>(`/api/platform/traces/${targetTraceId}`, {
      cache: "no-store",
    });

    if (result.ok && result.data) {
      setSelectedTrace(result.data);
      setTraceStatus({
        label: "Trace loaded",
        detail: `${result.data.traceType} / ${result.data.status}`,
        tone: "ready",
      });
    } else {
      setTraceStatus({
        label: isPendingEndpoint(result.status) ? "Trace endpoint pending" : "Trace load failed",
        detail: result.text || "Trace detail could not be loaded.",
        tone: isPendingEndpoint(result.status) ? "placeholder" : "pending",
      });
    }
    setBusy(null);
  }

  return {
    refreshStatus,
    refreshModelCatalog,
    refreshDatasets,
    refreshJobs,
    refreshDocuments,
    refreshRuns,
    refreshTrainingJobs,
    refreshModelVersions,
    refreshEvalRuns,
    refreshHumanReviews,
    bootstrapLocal,
    ensureBootstrap,
    runDevAction,
    uploadDocument,
    deleteDocument,
    reingestDocument,
    askAgent,
    loadTrace,
  };
}
