"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";

import {
  overviewMetrics,
  readinessChecks,
  ReadinessTone,
  SectionDetail,
  SectionSlug,
  setupQueue,
} from "./operator-data";
import {
  fallbackProviderCatalog,
  getProviderCatalogDetail,
  ProviderCatalogDetail,
} from "./model-catalog-data";
import {
  extractCollection,
  extractNestedObject,
  isPendingEndpoint,
  requestFirstAvailableCollection,
  requestFirstAvailableJson,
  requestOptionalCollection,
  requestOptionalJson,
} from "@/lib/api";
import { useWorkspaceStateValue } from "@/lib/workspace-context";
import type {
  BootstrapState,
  CatalogApiStatuses,
  ChatResponse,
  Citation,
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
  ProviderCardModel,
  ProviderPriceSnapshot,
  RetrievedChunk,
  Run,
  ServiceState,
  TraceRecord,
  TrainingJob,
} from "@/lib/types";

export function OperatorDashboardClient({
  section,
  sectionSlug,
}: {
  section: SectionDetail;
  sectionSlug: SectionSlug;
}) {
  const {
    bootstrap,
    setBootstrap,
    datasets,
    setDatasets,
    jobs,
    setJobs,
    runs,
    setRuns,
    trainingJobs,
    setTrainingJobs,
    modelVersions,
    setModelVersions,
    evalRuns,
    setEvalRuns,
    humanReviews,
    setHumanReviews,
    documents,
    setDocuments,
    modelProviders,
    setModelProviders,
    modelRoutes,
    setModelRoutes,
    priceSnapshots,
    setPriceSnapshots,
    localModelStatus,
    setLocalModelStatus,
  } = useWorkspaceStateValue();
  const [platformState, setPlatformState] = useState<ServiceState>("checking");
  const [aiState, setAiState] = useState<ServiceState>("checking");
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [question, setQuestion] = useState("What does this document say about refunds?");
  const [chatResult, setChatResult] = useState<ChatResponse | null>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [trainingTargetColumn, setTrainingTargetColumn] = useState("label");
  const [trainingTask, setTrainingTask] = useState<"classification" | "regression">("classification");
  const [documentStatus, setDocumentStatus] = useState<ControlStatus>({
    label: "Document endpoint",
    detail: "Not checked yet.",
    tone: "placeholder",
  });
  const [devStatuses, setDevStatuses] = useState<Record<DevAction, ControlStatus>>({
    reset: {
      label: "Reset endpoint",
      detail: "Not checked yet.",
      tone: "placeholder",
    },
    seed: {
      label: "Seed endpoint",
      detail: "Not checked yet.",
      tone: "placeholder",
    },
  });
  const [ragTopK, setRagTopK] = useState(4);
  const [scoreThreshold, setScoreThreshold] = useState(0);
  const [showNoAnswer, setShowNoAnswer] = useState(true);
  const [apiKeyInput, setApiKeyInput] = useState("");
  const [apiKeyMode, setApiKeyMode] = useState<"header" | "bearer">("header");
  const [widgetOrigin, setWidgetOrigin] = useState("http://127.0.0.1:3002");
  const [widgetTitle, setWidgetTitle] = useState("AgentPort assistant");
  const [widgetTheme, setWidgetTheme] = useState<"system" | "light" | "dark">("system");
  const [widgetMode, setWidgetMode] = useState<"embedded" | "floating">("embedded");
  const [selectedTrace, setSelectedTrace] = useState<TraceRecord | null>(null);
  const [traceStatus, setTraceStatus] = useState<ControlStatus>({
    label: "Trace detail",
    detail: "Select a run to load trace metadata.",
    tone: "placeholder",
  });
  const [selectedModelRouteId, setSelectedModelRouteId] = useState("");
  const [catalogStatuses, setCatalogStatuses] = useState<CatalogApiStatuses>({
    status: {
      label: "Platform status",
      detail: "Not checked yet.",
      tone: "placeholder",
    },
    catalog: {
      label: "Catalog endpoint",
      detail: "Not checked yet.",
      tone: "placeholder",
    },
    providers: {
      label: "Provider endpoint",
      detail: "Not checked yet.",
      tone: "placeholder",
    },
    routes: {
      label: "Route endpoint",
      detail: "Not checked yet.",
      tone: "placeholder",
    },
    prices: {
      label: "Price endpoint",
      detail: "Not checked yet.",
      tone: "placeholder",
    },
    localModel: {
      label: "Local model endpoint",
      detail: "Not checked yet.",
      tone: "placeholder",
    },
  });

  const activeDataset = useMemo(() => {
    if (!bootstrap) {
      return datasets[0] ?? null;
    }
    return datasets.find((item) => item.id === bootstrap.datasetId) ?? datasets[0] ?? null;
  }, [bootstrap, datasets]);
  const providerNameById = useMemo(() => {
    const names = new Map<string, string>();
    for (const provider of modelProviders) {
      if (provider.id && provider.name) {
        names.set(provider.id, provider.name);
      }
    }
    if (bootstrap?.modelProviderId) {
      names.set(bootstrap.modelProviderId, names.get(bootstrap.modelProviderId) ?? "ollama");
    }
    return names;
  }, [bootstrap?.modelProviderId, modelProviders]);
  const providerCards = useMemo(
    () => buildProviderCards(modelProviders, modelRoutes, providerNameById, bootstrap?.modelProviderId),
    [bootstrap?.modelProviderId, modelProviders, modelRoutes, providerNameById],
  );
  const selectedModelRoute = useMemo(() => {
    const selected = modelRoutes.find((route) => route.id === selectedModelRouteId);
    return selected ?? modelRoutes.find((route) => route.isDefault) ?? modelRoutes[0] ?? null;
  }, [modelRoutes, selectedModelRouteId]);
  const selectedRouteProviderName = getRouteProviderName(selectedModelRoute, providerNameById, bootstrap?.modelProviderId);
  const routeStatus = getRouteControlStatus(selectedModelRoute, catalogStatuses.routes);

  const retrievedChunks = chatResult?.retrieved_chunks ?? chatResult?.retrievedChunks ?? [];
  const fallbackMode = chatResult?.fallback_mode ?? chatResult?.fallbackMode ?? "not run";
  const runId = chatResult?.run_id ?? chatResult?.runId;
  const traceId = chatResult?.trace_id_record ?? chatResult?.traceIdRecord;
  const estimatedCost = chatResult?.estimated_cost ?? chatResult?.estimatedCost ?? 0;
  const bestScore = useMemo(
    () => retrievedChunks.reduce((best, chunk) => Math.max(best, chunk.score ?? 0), 0),
    [retrievedChunks],
  );
  const qualityChunks = useMemo(
    () => retrievedChunks.filter((chunk) => (chunk.score ?? 0) >= scoreThreshold),
    [retrievedChunks, scoreThreshold],
  );
  const qualityBlocked = Boolean(chatResult) && (retrievedChunks.length === 0 || bestScore < scoreThreshold);
  const answerText = showNoAnswer && qualityBlocked
    ? "No answer is shown because the best retrieved chunk is below the configured score threshold."
    : chatResult?.answer;
  const documentRows = useMemo(() => {
    if (documents.length > 0) {
      return documents;
    }
    return inferDocumentsFromJobs(jobs, activeDataset?.id);
  }, [activeDataset?.id, documents, jobs]);
  const chatRun = runId ? runs.find((run) => run.id === runId) : null;
  const apiKeyValue = apiKeyInput.trim() || bootstrap?.apiKey || "";
  const apiKeyHeader = apiKeyMode === "bearer" ? "authorization" : "x-agentport-api-key";
  const apiKeyHeaderValue = apiKeyMode === "bearer" ? `Bearer ${apiKeyValue || "{apiKey}"}` : apiKeyValue || "{apiKey}";
  const agentIdForSnippet = bootstrap?.agentDefinitionId ?? "{agentId}";
  const widgetUrl = `${widgetOrigin.replace(/\/$/, "")}/playground?agentId=${agentIdForSnippet}&mode=${widgetMode}&theme=${widgetTheme}`;

  useEffect(() => {
    void refreshStatus();
    void refreshDatasets();
    void refreshRuns();
    void refreshModelCatalog();
  }, []);

  useEffect(() => {
    if (!activeDataset?.id) {
      return;
    }
    void refreshJobs(activeDataset.id);
    void refreshDocuments(activeDataset.id);
  }, [activeDataset?.id]);

  useEffect(() => {
    if (bootstrap?.modelRouteId) {
      setSelectedModelRouteId(bootstrap.modelRouteId);
    }
  }, [bootstrap?.modelRouteId]);

  useEffect(() => {
    if (selectedModelRouteId && modelRoutes.some((route) => route.id === selectedModelRouteId)) {
      return;
    }
    const fallbackRoute = modelRoutes.find((route) => route.isDefault) ?? modelRoutes[0];
    if (fallbackRoute) {
      setSelectedModelRouteId(fallbackRoute.id);
    }
  }, [modelRoutes, selectedModelRouteId]);

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

  function renderMainView() {
    if (sectionSlug === "overview") {
      return (
        <div className="overview-grid">
          <section className="panel">
            <div className="panel-header">
              <div>
                <h2 className="panel-title">First run</h2>
                <p className="panel-copy">Bring the local workspace, dataset, knowledge base, model route, and document-QA agent online.</p>
              </div>
              <div className="button-row">
                <button className="control-button" type="button" onClick={bootstrapLocal} disabled={busy === "bootstrap"}>
                  {busy === "bootstrap" ? "Creating..." : "Run onboarding"}
                </button>
                <button className="control-button secondary" type="button" onClick={() => runDevAction("seed")} disabled={busy === "dev-seed"}>
                  {busy === "dev-seed" ? "Seeding..." : "Seed"}
                </button>
                <button className="control-button danger" type="button" onClick={() => runDevAction("reset")} disabled={busy === "dev-reset"}>
                  {busy === "dev-reset" ? "Resetting..." : "Reset"}
                </button>
              </div>
            </div>
            <div className="check-grid">
              <StatusCard label="Platform API" state={platformState} detail="/api/platform/status" />
              <StatusCard label="AI service" state={aiState} detail="/api/ai/health" />
              <StatusCard label="Workspace" state={bootstrap ? "ok" : "checking"} detail={bootstrap?.workspaceId ?? "Run onboarding"} />
              <StatusCard label="Dataset" state={activeDataset ? "ok" : "checking"} detail={activeDataset ? `${activeDataset.documentCount} docs, ${activeDataset.chunkCount} chunks` : "No dataset loaded"} />
              <ControlStatusCard status={devStatuses.seed} />
              <ControlStatusCard status={devStatuses.reset} />
            </div>
          </section>

          <aside className="panel">
            <div className="panel-header">
              <div>
                <h2 className="panel-title">Local flow</h2>
                <p className="panel-copy">Phase 1.2 focuses on document-QA operations, model catalog visibility, route status, trace review, and configuration placeholders.</p>
              </div>
            </div>
            <div className="queue-grid">
              {setupQueue.map((item) => (
                <article className="queue-card" key={item.label}>
                  <div className="queue-head">
                    <p className="queue-label">{item.label}</p>
                    <span className={`mini-badge ${item.tone}`}>{item.tone}</span>
                  </div>
                  <p className="queue-detail">{item.detail}</p>
                </article>
              ))}
            </div>
          </aside>

          <section className="panel">
            <div className="panel-header">
              <div>
                <h2 className="panel-title">Workspace snapshot</h2>
                <p className="panel-copy">Live identifiers from bootstrap are shown after onboarding.</p>
              </div>
            </div>
            <div className="metric-grid">
              {overviewMetrics.map((metric) => (
                <article className="metric-card" key={metric.label}>
                  <p className="metric-label">{metric.label}</p>
                  <p className="metric-value">{metric.value}</p>
                  <p className="metric-detail">{metric.detail}</p>
                </article>
              ))}
              {bootstrap ? (
                <>
                  <Metric label="Agent" value={bootstrap.agentDefinitionId} detail="Document-QA agent definition" />
                  <Metric label="Knowledge base" value={bootstrap.knowledgeBaseId} detail="pgvector-backed local store" />
                </>
              ) : null}
            </div>
          </section>

          <section className="panel">
            <div className="panel-header">
              <div>
                <h2 className="panel-title">Readiness checklist</h2>
                <p className="panel-copy">Phase 1 status and remaining placeholders.</p>
              </div>
            </div>
            <div className="check-grid">
              {readinessChecks.map((item) => (
                <article className="check-card" key={item.label}>
                  <div className="check-head">
                    <p className="check-label">{item.label}</p>
                    <span className={`status-pill ${item.tone}`}>{item.state}</span>
                  </div>
                  <p className="check-detail">{item.detail}</p>
                </article>
              ))}
            </div>
          </section>
        </div>
      );
    }

    if (sectionSlug === "datasets") {
      return (
        <div className="section-grid">
          <section className="section-card">
            <div className="panel-header compact">
              <div>
                <h2>Document sources</h2>
                <p className="panel-copy">Upload, inspect ingestion status, and manage document assets when backend controls are present.</p>
              </div>
              <button
                className="control-button secondary"
                type="button"
                onClick={async () => {
                  if (activeDataset?.id) {
                    setBusy("refresh-documents");
                    await Promise.all([refreshJobs(activeDataset.id), refreshDocuments(activeDataset.id)]);
                    setBusy(null);
                  }
                }}
                disabled={!activeDataset || busy === "refresh-documents"}
              >
                Refresh
              </button>
            </div>
            <form className="control-form" onSubmit={uploadDocument}>
              <input
                className="file-input"
                type="file"
                accept=".txt,.md,.pdf,text/plain,text/markdown,application/pdf"
                onChange={(event) => setSelectedFile(event.target.files?.[0] ?? null)}
              />
              <button className="control-button" type="submit" disabled={busy === "upload"}>
                {busy === "upload" ? "Ingesting..." : "Upload document"}
              </button>
            </form>
            <ControlStatusCard status={documentStatus} />
            <div className="document-list">
              {documentRows.length === 0 ? (
                <article className="asset-card empty-inline">
                  <p className="asset-label">No documents loaded</p>
                  <p className="asset-detail">Upload a document or refresh after a backend document list endpoint is added.</p>
                </article>
              ) : null}
              {documentRows.map((document) => {
                const documentId = getDocumentId(document);
                return (
                  <article className="asset-card" key={documentId}>
                    <div className="asset-head">
                      <p className="asset-label">{getDocumentName(document)}</p>
                      <span className={`status-pill ${document.source === "api" ? "ready" : "placeholder"}`}>
                        {document.status ?? document.source ?? "document"}
                      </span>
                    </div>
                    <p className="asset-detail">{getDocumentChunks(document)} chunks - {documentId}</p>
                    <p className="asset-detail">Updated {formatDate(document.updatedAt ?? document.createdAt)}</p>
                    <div className="button-row compact">
                      <button
                        className="control-button secondary"
                        type="button"
                        onClick={() => void reingestDocument(document)}
                        disabled={busy === `reingest-${documentId}`}
                      >
                        {busy === `reingest-${documentId}` ? "Reingesting..." : "Reingest"}
                      </button>
                      <button
                        className="control-button danger"
                        type="button"
                        onClick={() => void deleteDocument(document)}
                        disabled={busy === `delete-${documentId}`}
                      >
                        {busy === `delete-${documentId}` ? "Deleting..." : "Delete"}
                      </button>
                    </div>
                  </article>
                );
              })}
            </div>
          </section>
          <aside className="section-card">
            <h2>Datasets</h2>
            <div className="asset-grid">
              {datasets.map((dataset) => (
                <article className="asset-card" key={dataset.id}>
                  <div className="asset-head">
                    <p className="asset-label">{dataset.name}</p>
                    <span className="status-pill ready">{dataset.kind}</span>
                  </div>
                  <p className="asset-detail">{dataset.documentCount} documents, {dataset.chunkCount} chunks</p>
                  <p className="asset-detail">{dataset.id}</p>
                </article>
              ))}
            </div>
          </aside>
          <aside className="section-card">
            <h2>Ingestion jobs</h2>
            <ul>
              {jobs.length === 0 ? <li>No ingestion jobs yet.</li> : null}
              {jobs.map((job) => (
                <li key={job.id}>
                  {job.status} - {job.chunkCount} chunks - document {job.documentAssetId} - {formatDate(job.createdAt)}
                </li>
              ))}
            </ul>
          </aside>
        </div>
      );
    }

    if (sectionSlug === "model-catalog") {
      return (
        <ModelCatalogView
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

    if (sectionSlug === "training") {
      return (
        <div className="section-grid">
          <section className="section-card">
            <h2>Training jobs</h2>
            <form
              className="control-form stacked"
              onSubmit={async (event) => {
                event.preventDefault();
                setBusy("create-training");
                setError(null);
                try {
                  const context = await ensureBootstrap();
                  const payload = {
                    workspaceId: context.workspaceId,
                    projectId: context.projectId,
                    datasetId: activeDataset?.id,
                    name: `Classifier ${new Date().toISOString().slice(0, 19)}`,
                    kind: "classification",
                    estimatedCost: 0,
                  };
                  const response = await fetch("/api/platform/training-jobs", {
                    method: "POST",
                    headers: { "content-type": "application/json" },
                    body: JSON.stringify(payload),
                  });
                  if (!response.ok) throw new Error(await response.text());
                  await refreshTrainingJobs(context.workspaceId);
                } catch (err) {
                  setError(err instanceof Error ? err.message : "Failed to create training job.");
                } finally {
                  setBusy(null);
                }
              }}
            >
              <div className="field-row">
                <label>Target column</label>
                <input
                  className="text-input"
                  value={trainingTargetColumn}
                  onChange={(e) => setTrainingTargetColumn(e.target.value)}
                  placeholder="e.g. label, category, price"
                />
              </div>
              <div className="field-row">
                <label>Task</label>
                <select
                  className="text-input"
                  value={trainingTask}
                  onChange={(e) => setTrainingTask(e.target.value as "classification" | "regression")}
                >
                  <option value="classification">Classification</option>
                  <option value="regression">Regression</option>
                </select>
              </div>
              <button className="control-button" type="submit" disabled={busy === "create-training"}>
                {busy === "create-training" ? "Creating..." : "Create training job"}
              </button>
            </form>
            <div className="asset-grid">
              {trainingJobs.map((job) => (
                <article className="asset-card" key={job.id}>
                  <div className="asset-head">
                    <p className="asset-label">{job.name}</p>
                    <span className={`status-pill ${job.status}`}>{job.status}</span>
                  </div>
                  <p className="asset-detail">Kind: {job.kind} | Est. cost: ${job.estimatedCost}</p>
                  <p className="asset-detail">Metrics: {job.metricsJson}</p>
                </article>
              ))}
            </div>
          </section>
          <aside className="section-card">
            <h2>Model versions</h2>
            <button
              className="control-button"
              type="button"
              disabled={busy === "refresh-models"}
              onClick={async () => {
                setBusy("refresh-models");
                const context = await ensureBootstrap();
                await refreshModelVersions(context.workspaceId);
                setBusy(null);
              }}
            >
              Refresh
            </button>
            <ul>
              {modelVersions.length === 0 ? <li>No model versions yet.</li> : null}
              {modelVersions.map((mv) => (
                <li key={mv.id}>
                  {mv.name} ({mv.kind}) - {mv.status} - {mv.artifactUri ?? "no artifact"}
                </li>
              ))}
            </ul>
          </aside>
        </div>
      );
    }

    if (sectionSlug === "builder") {
      return (
        <div className="section-grid">
          <section className="section-card">
            <div className="panel-header compact">
              <div>
                <h2>Document QA agent</h2>
                <p className="panel-copy">Builder route selection is displayed without changing the saved demo agent contract.</p>
              </div>
              <span className={`status-pill ${routeStatus.tone}`}>{routeStatus.label}</span>
            </div>
            <RouteSelector
              label="Builder route"
              routes={modelRoutes}
              value={selectedModelRoute?.id ?? ""}
              providerNameById={providerNameById}
              bootstrapProviderId={bootstrap?.modelProviderId}
              onChange={setSelectedModelRouteId}
            />
            <ul>
              <li>Agent id: {bootstrap?.agentDefinitionId ?? "Run onboarding to create the local agent."}</li>
              <li>Dataset id: {bootstrap?.datasetId ?? activeDataset?.id ?? "No dataset loaded."}</li>
              <li>Knowledge base: {bootstrap?.knowledgeBaseId ?? activeDataset?.knowledgeBaseId ?? "No knowledge base loaded."}</li>
              <li>Selected route: {formatRouteLabel(selectedModelRoute, selectedRouteProviderName)}</li>
              <li>Retrieval: pgvector top-k with deterministic local hash embeddings.</li>
              <li>Answer mode: Ollama when enabled, otherwise extractive fallback with citations.</li>
            </ul>
          </section>
          <aside className="section-card">
            <h2>Route status</h2>
            <RouteStatusSummary
              route={selectedModelRoute}
              providerName={selectedRouteProviderName}
              status={routeStatus}
              localModelStatus={localModelStatus}
            />
          </aside>
          <aside className="empty-state">
            <p className="empty-title">Training lab available</p>
            <p className="empty-copy">Classifiers and regressors can be trained from CSV datasets in the Training Lab.</p>
            <span className="status-pill ready">Training active</span>
          </aside>
        </div>
      );
    }

    if (sectionSlug === "playground") {
      return (
        <div className="section-grid">
          <section className="section-card">
            <div className="panel-header compact">
              <div>
                <h2>Ask the document agent</h2>
                <p className="panel-copy">Tune retrieval depth and answer visibility before asking the local document-QA agent.</p>
              </div>
              <span className={`status-pill ${qualityBlocked ? "pending" : chatResult ? "ready" : "placeholder"}`}>
                {chatResult ? (qualityBlocked ? "below threshold" : "quality pass") : "not run"}
              </span>
            </div>
            <form className="control-form stacked" onSubmit={askAgent}>
              <textarea className="question-input" value={question} onChange={(event) => setQuestion(event.target.value)} rows={4} />
              <div className="control-grid">
                <label className="field-row">
                  <span>Top K</span>
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
                  <span>Score threshold</span>
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
                  label="Model route"
                  routes={modelRoutes}
                  value={selectedModelRoute?.id ?? ""}
                  providerNameById={providerNameById}
                  bootstrapProviderId={bootstrap?.modelProviderId}
                  onChange={setSelectedModelRouteId}
                />
                <label className="field-row">
                  <span>API key</span>
                  <input
                    className="text-input"
                    value={apiKeyInput}
                    onChange={(event) => setApiKeyInput(event.target.value)}
                    placeholder={bootstrap?.apiKey ? "Bootstrap key loaded" : "Paste local API key"}
                  />
                </label>
                <label className="toggle-row">
                  <input
                    type="checkbox"
                    checked={showNoAnswer}
                    onChange={(event) => setShowNoAnswer(event.target.checked)}
                  />
                  <span>Show no-answer below threshold</span>
                </label>
              </div>
              <button className="control-button" type="submit" disabled={busy === "chat"}>
                {busy === "chat" ? "Asking..." : "Ask"}
              </button>
            </form>
            {chatResult ? (
              <div className="answer-block">
                <div className="result-head">
                  <p className="answer-label">Answer</p>
                  <div className="meta-row">
                    <span className="status-pill ready">{fallbackMode}</span>
                    <span>Route: {formatRouteLabel(selectedModelRoute, selectedRouteProviderName)}</span>
                    <span>Best score: {formatScore(bestScore)}</span>
                    <span>Visible chunks: {qualityChunks.length}/{retrievedChunks.length}</span>
                  </div>
                </div>
                <p className="answer-text">{answerText}</p>
                <div className="meta-row">
                  <span>Run: {runId ?? "not captured"}</span>
                  <span>Trace: {traceId ?? "not captured"}</span>
                  <span>Latency: {chatRun ? `${chatRun.latencyMs} ms` : "pending refresh"}</span>
                  <span>Cost estimate: {formatCost(estimatedCost)}</span>
                  {traceId ? (
                    <button className="inline-button" type="button" onClick={() => void loadTrace(traceId)}>
                      Load trace
                    </button>
                  ) : null}
                </div>
              </div>
            ) : null}
          </section>
          <aside className="section-card">
            <h2>Route status</h2>
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
                <h2>Citations and chunks</h2>
                <p className="panel-copy">Client-side thresholding controls what counts as answerable evidence.</p>
              </div>
            </div>
            <div className="chunk-list">
              {(chatResult?.citations ?? []).map((citation, index) => (
                <article className="asset-card" key={`${citation.citation_id ?? citation.citationId}-${index}`}>
                  <div className="asset-head">
                    <p className="asset-label">{citation.citation_id ?? citation.citationId ?? "citation"}</p>
                    <span className={`status-pill ${(citation.score ?? 0) >= scoreThreshold ? "ready" : "pending"}`}>
                      {formatScore(citation.score)}
                    </span>
                  </div>
                  <p className="asset-detail">{citation.file_name ?? citation.fileName ?? "unknown file"}</p>
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
                  <p className="asset-label">No chunks above threshold</p>
                  <p className="asset-detail">Lower the score threshold or increase Top K to inspect more evidence.</p>
                </article>
              ) : null}
            </div>
          </aside>
          <aside className="section-card">
            <h2>Trace preview</h2>
            <ControlStatusCard status={traceStatus} />
            {selectedTrace ? (
              <dl className="detail-list">
                <div>
                  <dt>Trace</dt>
                  <dd>{selectedTrace.id}</dd>
                </div>
                <div>
                  <dt>Tokens</dt>
                  <dd>{selectedTrace.inputTokens} in / {selectedTrace.outputTokens} out</dd>
                </div>
                <div>
                  <dt>Cost</dt>
                  <dd>{formatCost(selectedTrace.costAmount)}</dd>
                </div>
              </dl>
            ) : null}
          </aside>
        </div>
      );
    }

    if (sectionSlug === "evals") {
      return (
        <div className="section-grid">
          <section className="section-card">
            <h2>Eval runs</h2>
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
              Refresh
            </button>
            <ul>
              {evalRuns.length === 0 ? <li>No eval runs yet.</li> : null}
              {evalRuns.map((er) => (
                <li key={er.id}>
                  {er.status} - score {er.score ?? "-"} / threshold {er.threshold ?? "-"} - {er.passed ? "PASSED" : "FAILED"}
                </li>
              ))}
            </ul>
          </section>
          <aside className="section-card">
            <h2>Eval gates</h2>
            <p className="asset-detail">Eval gates block promotion until configured thresholds are met.</p>
            <ul>
              <li>Exact match / contains checks</li>
              <li>Citation presence</li>
              <li>Latency threshold</li>
              <li>Cost threshold</li>
              <li>Safety checks</li>
            </ul>
          </aside>
        </div>
      );
    }

    if (sectionSlug === "traces") {
      return (
        <div className="section-grid">
          <section className="section-card">
            <div className="panel-header compact">
              <div>
                <h2>Recent runs</h2>
                <p className="panel-copy">Playground and agent invocations with trace ids, latency, fallback mode, and citation counts.</p>
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
                Refresh
              </button>
            </div>
            <div className="trace-list">
              {runs.length === 0 ? (
                <article className="asset-card empty-inline">
                  <p className="asset-label">No runs captured yet</p>
                  <p className="asset-detail">Ask a question in the playground to create the first traceable run.</p>
                </article>
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
                    <div className="meta-row">
                      <span>{run.latencyMs} ms</span>
                      <span>{formatCost(run.estimatedCost)}</span>
                      <span>{citations.length} citations</span>
                      <span>{chunks.length} chunks</span>
                      <span>{formatDate(run.createdAt)}</span>
                    </div>
                    <div className="button-row compact">
                      <button
                        className="control-button secondary"
                        type="button"
                        onClick={() => void loadTrace(run.traceRecordId)}
                        disabled={!run.traceRecordId || busy === `trace-${run.traceRecordId}`}
                      >
                        {busy === `trace-${run.traceRecordId}` ? "Loading..." : "Load trace"}
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
                <h2>Trace detail</h2>
                <p className="panel-copy">Raw trace metadata stays collapsed into dense operational fields.</p>
              </div>
            </div>
            <ControlStatusCard status={traceStatus} />
            {selectedTrace ? (
              <>
                <dl className="detail-list">
                  <div>
                    <dt>Trace id</dt>
                    <dd>{selectedTrace.id}</dd>
                  </div>
                  <div>
                    <dt>Correlation</dt>
                    <dd>{selectedTrace.correlationId ?? "none"}</dd>
                  </div>
                  <div>
                    <dt>Model route</dt>
                    <dd>{selectedTrace.modelRouteId ?? "none"}</dd>
                  </div>
                  <div>
                    <dt>Tokens</dt>
                    <dd>{selectedTrace.inputTokens} / {selectedTrace.outputTokens}</dd>
                  </div>
                </dl>
                <pre className="code-block">{selectedTrace.metadataJson}</pre>
              </>
            ) : null}
          </aside>
          <aside className="section-card">
            <h2>Human review queue</h2>
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
              Refresh
            </button>
            <ul>
              {humanReviews.length === 0 ? <li>No review records yet.</li> : null}
              {humanReviews.map((r) => (
                <li key={r.id}>
                  {r.queue} - {r.status} - {r.label ?? "no label"} - reviewer {r.reviewerName ?? "unassigned"}
                </li>
              ))}
            </ul>
          </aside>
        </div>
      );
    }

    if (sectionSlug === "deployments") {
      return (
        <div className="section-grid">
          <section className="section-card">
            <div className="panel-header compact">
              <div>
                <h2>Local API</h2>
                <p className="panel-copy">Invocation snippet with optional API key header placeholder.</p>
              </div>
              <span className={`status-pill ${apiKeyValue ? "ready" : "placeholder"}`}>
                {apiKeyValue ? "key staged" : "key optional"}
              </span>
            </div>
            <div className="control-grid">
              <label className="field-row">
                <span>API key</span>
                <input
                  className="text-input"
                  value={apiKeyInput}
                  onChange={(event) => setApiKeyInput(event.target.value)}
                  placeholder={bootstrap?.apiKey ? "Bootstrap key loaded" : "Paste local key placeholder"}
                />
              </label>
              <label className="field-row">
                <span>Header mode</span>
                <select className="text-input" value={apiKeyMode} onChange={(event) => setApiKeyMode(event.target.value as "header" | "bearer")}>
                  <option value="header">x-agentport-api-key</option>
                  <option value="bearer">Bearer</option>
                </select>
              </label>
            </div>
            <p className="asset-detail">{bootstrap?.apiKeyMessage ?? "Run onboarding to create or confirm the local bootstrap key placeholder."}</p>
            <pre className="code-block">{`curl -X POST http://localhost:5001/api/v1/agent-definitions/${agentIdForSnippet}/chat \\
  -H "content-type: application/json" \\
  -H "${apiKeyHeader}: ${apiKeyHeaderValue}" \\
  -d '{"question":"What does this document say about refunds?","topK":${ragTopK},"scoreThreshold":${scoreThreshold}}'`}</pre>
          </section>
          <aside className="section-card">
            <div className="panel-header compact">
              <div>
                <h2>Widget config</h2>
                <p className="panel-copy">Local embed placeholder values for future hosted widget contracts.</p>
              </div>
              <span className="status-pill placeholder">placeholder</span>
            </div>
            <div className="control-grid">
              <label className="field-row">
                <span>Origin</span>
                <input className="text-input" value={widgetOrigin} onChange={(event) => setWidgetOrigin(event.target.value)} />
              </label>
              <label className="field-row">
                <span>Title</span>
                <input className="text-input" value={widgetTitle} onChange={(event) => setWidgetTitle(event.target.value)} />
              </label>
              <label className="field-row">
                <span>Mode</span>
                <select className="text-input" value={widgetMode} onChange={(event) => setWidgetMode(event.target.value as "embedded" | "floating")}>
                  <option value="embedded">Embedded</option>
                  <option value="floating">Floating</option>
                </select>
              </label>
              <label className="field-row">
                <span>Theme</span>
                <select className="text-input" value={widgetTheme} onChange={(event) => setWidgetTheme(event.target.value as "system" | "light" | "dark")}>
                  <option value="system">System</option>
                  <option value="light">Light</option>
                  <option value="dark">Dark</option>
                </select>
              </label>
            </div>
            <pre className="code-block">{`<iframe
  title="${escapeHtmlAttribute(widgetTitle)}"
  src="${widgetUrl}"
  width="420"
  height="640"
></iframe>`}</pre>
            <p className="asset-detail">Allowed-origin allowlist and rate-limit policy placeholders remain required before hosted widget activation.</p>
          </aside>
        </div>
      );
    }

    if (sectionSlug === "settings") {
      return (
        <div className="section-grid">
          <section className="section-card">
            <div className="panel-header compact">
              <div>
                <h2>Dev controls</h2>
                <p className="panel-copy">Local seed/reset controls report backend availability without requiring new routes.</p>
              </div>
              <div className="button-row">
                <button className="control-button secondary" type="button" onClick={() => runDevAction("seed")} disabled={busy === "dev-seed"}>
                  {busy === "dev-seed" ? "Seeding..." : "Seed"}
                </button>
                <button className="control-button danger" type="button" onClick={() => runDevAction("reset")} disabled={busy === "dev-reset"}>
                  {busy === "dev-reset" ? "Resetting..." : "Reset"}
                </button>
              </div>
            </div>
            <div className="check-grid">
              <ControlStatusCard status={devStatuses.seed} />
              <ControlStatusCard status={devStatuses.reset} />
              <Metric label="Workspace" value={bootstrap?.workspaceId ?? "not bootstrapped"} detail="Local bootstrap workspace" />
              <Metric label="API key" value={bootstrap?.apiKey ? "shown once" : "not returned"} detail={bootstrap?.apiKeyMessage ?? "Bootstrap key status appears after onboarding."} />
            </div>
          </section>
          <aside className="section-card">
            <h2>Widget placeholder</h2>
            <dl className="detail-list">
              <div>
                <dt>Agent</dt>
                <dd>{agentIdForSnippet}</dd>
              </div>
              <div>
                <dt>Mode</dt>
                <dd>{widgetMode}</dd>
              </div>
              <div>
                <dt>Theme</dt>
                <dd>{widgetTheme}</dd>
              </div>
            </dl>
          </aside>
        </div>
      );
    }

    if (sectionSlug === "wallet" || sectionSlug === "billing") {
      return (
        <div className="section-grid">
          <section className="section-card">
            <h2>{section.title}</h2>
            <ul>
              <li>Charging is disabled in Phase 1.2.</li>
              <li>Wallet id: {bootstrap?.walletAccountId ?? "Run onboarding to create the local wallet placeholder."}</li>
              <li>Latest estimated run cost: ${estimatedCost.toFixed(6)}</li>
              <li>Provider and Turkish payment methods are configuration placeholders only.</li>
            </ul>
          </section>
          <aside className="empty-state">
            <p className="empty-title">Read-only placeholder</p>
            <p className="empty-copy">No payment capture, credits, reservations, or real provider charges are executed in this phase.</p>
            <span className="status-pill placeholder">No charging</span>
          </aside>
        </div>
      );
    }

    return <SectionView section={section} />;
  }

  return (
    <>
      {error ? <div className="error-banner">{error}</div> : null}
      {renderMainView()}
    </>
  );
}

function ControlStatusCard({ status }: { status: ControlStatus }) {
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

function StatusCard({ label, state, detail }: { label: string; state: ServiceState; detail: string }) {
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

function Metric({ label, value, detail }: { label: string; value: string; detail: string }) {
  return (
    <article className="metric-card">
      <p className="metric-label">{label}</p>
      <p className="metric-value compact">{value}</p>
      <p className="metric-detail">{detail}</p>
    </article>
  );
}

function ModelCatalogView({
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
  const platformProviderCount = providers.length;
  const enabledProviderCount = providerCards.filter((provider) => provider.isEnabled).length;
  const fallbackCount = providerCards.filter((provider) => provider.source === "fallback").length;
  const routeCount = routes.length;

  return (
    <div className="catalog-page">
      <nav className="catalog-tabs" aria-label="Model catalog sections">
        <a className="catalog-tab" href="#catalog-providers">Providers</a>
        <a className="catalog-tab" href="#catalog-routes">Routes</a>
        <a className="catalog-tab" href="#catalog-local-ollama">Local Ollama</a>
        <a className="catalog-tab" href="#catalog-prices">Prices</a>
        <a className="catalog-tab" href="#catalog-health">Health</a>
      </nav>

      <section className="catalog-summary" aria-label="Catalog summary">
        <div>
          <p className="eyebrow">Phase 1.2 catalog slice</p>
          <h2 className="panel-title">Provider comparison and route readiness</h2>
          <p className="panel-copy">
            The UI reads platform catalog contracts when available, then falls back to the seeded provider reference so the page remains useful before backend endpoints land.
          </p>
        </div>
        <div className="button-row">
          <button className="control-button secondary" type="button" onClick={() => void onRefreshStart()} disabled={busy}>
            {busy ? "Refreshing..." : "Refresh catalog"}
          </button>
        </div>
      </section>

      <div className="metric-grid">
        <Metric label="Provider cards" value={`${providerCards.length}`} detail={`${platformProviderCount} platform rows, ${fallbackCount} fallback references`} />
        <Metric label="Enabled providers" value={`${enabledProviderCount}`} detail="Enabled flag is shown when returned by the provider or route API." />
        <Metric label="Routes" value={`${routeCount}`} detail={routeCount > 0 ? "Loaded from model-routes or catalog payload." : "Route fallback is waiting for the platform API."} />
        <Metric label="Selected route" value={formatRouteLabel(selectedRoute, selectedRouteProviderName)} detail={routeStatus.detail} />
      </div>

      <section className="catalog-section" id="catalog-providers">
        <div className="panel-header compact">
          <div>
            <h2>Providers</h2>
            <p className="panel-copy">Use case, tradeoff, cost, privacy, lock-in, key mode, and capabilities per provider.</p>
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
                    {provider.isEnabled ? "enabled" : provider.source === "fallback" ? "reference" : "disabled"}
                  </span>
                  <span className="mini-badge placeholder">{provider.source}</span>
                </div>
              </div>
              <p className="provider-use-case">{provider.useCase}</p>
              <dl className="provider-field-grid">
                <div>
                  <dt>Advantage</dt>
                  <dd>{provider.advantage}</dd>
                </div>
                <div>
                  <dt>Disadvantage</dt>
                  <dd>{provider.disadvantage}</dd>
                </div>
                <div>
                  <dt>Cost</dt>
                  <dd>{provider.cost}</dd>
                </div>
                <div>
                  <dt>Privacy</dt>
                  <dd>{provider.privacy}</dd>
                </div>
                <div>
                  <dt>Lock-in</dt>
                  <dd>{provider.lockIn}</dd>
                </div>
                <div>
                  <dt>Key mode</dt>
                  <dd>{provider.keyMode}</dd>
                </div>
              </dl>
              {provider.baseUrl ? <p className="asset-detail">Base URL: {provider.baseUrl}</p> : null}
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
            <h2>Routes</h2>
            <p className="panel-copy">Route selector is shared with Builder and Playground status displays.</p>
          </div>
          <span className={`status-pill ${catalogStatuses.routes.tone}`}>{catalogStatuses.routes.label}</span>
        </div>
        <div className="catalog-control-row">
          <RouteSelector
            label="Active route"
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
              <p className="empty-title">No routes loaded</p>
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
                    {route.isEnabled === false ? "disabled" : route.isDefault ? "default" : "enabled"}
                  </span>
                </div>
                <dl className="detail-list">
                  <div>
                    <dt>Provider</dt>
                    <dd>{providerName}</dd>
                  </div>
                  <div>
                    <dt>Model</dt>
                    <dd>{route.modelName}</dd>
                  </div>
                  <div>
                    <dt>Type</dt>
                    <dd>{route.routeType ?? "chat"}</dd>
                  </div>
                  <div>
                    <dt>Priority</dt>
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
            <h2>Local Ollama</h2>
            <p className="panel-copy">Local route availability is explicit even when the dedicated backend status endpoint is missing.</p>
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
              <p className="asset-label">Ollama runtime</p>
              <span className={`status-pill ${getLocalModelTone(localModelStatus)}`}>{localModelStatus?.status ?? localModelStatus?.routeHealth ?? "not reported"}</span>
            </div>
            <dl className="detail-list">
              <div>
                <dt>Model</dt>
                <dd>{localModelStatus?.modelName ?? localModelStatus?.model ?? selectedRoute?.modelName ?? "llama3.2"}</dd>
              </div>
              <div>
                <dt>Base URL</dt>
                <dd>{localModelStatus?.baseUrl ?? "http://localhost:11434"}</dd>
              </div>
              <div>
                <dt>Endpoint</dt>
                <dd>{catalogStatuses.localModel.detail}</dd>
              </div>
              <div>
                <dt>Installed models</dt>
                <dd>{localModelStatus?.models?.length ? localModelStatus.models.join(", ") : "not reported"}</dd>
              </div>
            </dl>
          </article>
        </div>
      </section>

      <section className="catalog-section" id="catalog-prices">
        <div className="panel-header compact">
          <div>
            <h2>Prices</h2>
            <p className="panel-copy">Exact prices come from platform snapshots. Qualitative cost is shown while that endpoint is pending.</p>
          </div>
          <span className={`status-pill ${catalogStatuses.prices.tone}`}>{catalogStatuses.prices.label}</span>
        </div>
        {prices.length > 0 ? (
          <div className="table-scroll">
            <table className="price-table">
              <thead>
                <tr>
                  <th>Provider</th>
                  <th>Model</th>
                  <th>Input / 1M</th>
                  <th>Output / 1M</th>
                  <th>Request</th>
                  <th>Captured</th>
                </tr>
              </thead>
              <tbody>
                {prices.map((price, index) => (
                  <tr key={price.id ?? `${price.providerId}-${price.modelName}-${index}`}>
                    <td>{price.providerName ?? lookupProviderName(price.providerId, providerNameById)}</td>
                    <td>{price.modelName ?? "unknown"}</td>
                    <td>{formatPriceValue(price.inputTokenPricePerMillion, price.currency)}</td>
                    <td>{formatPriceValue(price.outputTokenPricePerMillion, price.currency)}</td>
                    <td>{formatPriceValue(price.requestPrice, price.currency)}</td>
                    <td>{formatDate(price.capturedAt ?? price.createdAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
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
            <h2>Health</h2>
            <p className="panel-copy">Endpoint status is separated from fallback catalog rendering so missing APIs are visible but non-blocking.</p>
          </div>
        </div>
        <div className="check-grid">
          <StatusCard label="Platform API" state={platformState} detail="/api/platform/status" />
          <StatusCard label="AI service" state={aiState} detail="/api/ai/health" />
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

function RouteSelector({
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
  return (
    <label className="field-row route-selector">
      <span>{label}</span>
      <select className="text-input" value={value} onChange={(event) => onChange(event.target.value)} disabled={routes.length === 0}>
        {routes.length === 0 ? <option value="">No routes loaded</option> : null}
        {routes.map((route) => (
          <option value={route.id} key={route.id}>
            {formatRouteLabel(route, getRouteProviderName(route, providerNameById, bootstrapProviderId))}
          </option>
        ))}
      </select>
    </label>
  );
}

function RouteStatusSummary({
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
  return (
    <article className="route-status-panel">
      <div className="asset-head">
        <p className="asset-label">Selected route</p>
        <span className={`status-pill ${status.tone}`}>{status.tone}</span>
      </div>
      <p className="asset-detail">{status.detail}</p>
      <dl className="detail-list">
        <div>
          <dt>Provider</dt>
          <dd>{providerName || "not selected"}</dd>
        </div>
        <div>
          <dt>Route</dt>
          <dd>{route?.name ?? "No route selected"}</dd>
        </div>
        <div>
          <dt>Model</dt>
          <dd>{route?.modelName ?? localModelStatus?.modelName ?? localModelStatus?.model ?? "not loaded"}</dd>
        </div>
        <div>
          <dt>Local health</dt>
          <dd>{summarizeLocalModelStatus(localModelStatus)}</dd>
        </div>
      </dl>
    </article>
  );
}

function SectionView({ section }: { section: SectionDetail }) {
  return (
    <div className="section-grid">
      <section className="section-card">
        <h2>Operator surface</h2>
        <ul>
          {section.primaryItems.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
      </section>

      <aside className="empty-state" aria-label={`${section.title} current state`}>
        <p className="empty-title">{section.emptyTitle}</p>
        <p className="empty-copy">{section.emptyCopy}</p>
        <span className="status-pill placeholder">{section.status}</span>
      </aside>

      <section className="section-card">
        <h2>Integration queue</h2>
        <ul>
          {section.secondaryItems.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
      </section>

      <section className="section-card">
        <h2>Service links</h2>
        <ul>
          <li>
            <a href="/api/platform/status">Platform API status</a>
          </li>
          <li>
            <a href="/api/ai/health">AI service health</a>
          </li>
        </ul>
      </section>
    </div>
  );
}

function resultToControlStatus(
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

function buildProviderCards(
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

function getRouteProviderName(route: ModelRoute | null, providerNameById: Map<string, string>, bootstrapProviderId?: string) {
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

function getRouteControlStatus(route: ModelRoute | null, routesStatus: ControlStatus): ControlStatus {
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

function formatRouteLabel(route: ModelRoute | null, providerName: string) {
  if (!route) {
    return "No route selected";
  }
  return `${route.name} / ${providerName || "unknown"} / ${route.modelName}`;
}

function lookupProviderName(providerId: string | undefined, providerNameById: Map<string, string>) {
  if (!providerId) {
    return "unknown";
  }
  return providerNameById.get(providerId) ?? providerId.slice(0, 8);
}

function summarizeLocalModelStatus(status: LocalModelStatus | null | undefined) {
  if (!status) {
    return "No local-model status endpoint response yet.";
  }
  const health = status.routeHealth ?? status.status ?? "reported";
  const model = status.modelName ?? status.model ?? "model not reported";
  const enabled = typeof status.enabled === "boolean" ? (status.enabled ? "enabled" : "disabled") : "enabled unknown";
  return `${health} - ${model} - ${enabled}`;
}

function getLocalModelTone(status: LocalModelStatus | null | undefined): ReadinessTone {
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

function formatPriceValue(value: number | string | undefined, currency?: string) {
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

function looksLikeBootstrap(data: unknown): data is BootstrapState {
  if (!data || typeof data !== "object") {
    return false;
  }
  const candidate = data as Partial<BootstrapState>;
  return Boolean(candidate.workspaceId && candidate.projectId && candidate.datasetId && candidate.agentDefinitionId);
}

function extractBootstrapSeed(data: unknown): BootstrapState | null {
  if (!data || typeof data !== "object") {
    return null;
  }
  const candidate = data as { seed?: unknown; Seed?: unknown };
  const seed = candidate.seed ?? candidate.Seed;
  return looksLikeBootstrap(seed) ? seed : null;
}

function inferDocumentsFromJobs(jobs: IngestionJob[], datasetId?: string): DocumentAsset[] {
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

function getDocumentId(document: DocumentAsset) {
  return document.id ?? document.documentAssetId ?? document.latestIngestionJobId ?? "unknown-document";
}

function getDocumentName(document: DocumentAsset) {
  return document.fileName ?? document.name ?? `Document ${getDocumentId(document).slice(0, 8)}`;
}

function getDocumentChunks(document: DocumentAsset) {
  return document.chunkCount ?? 0;
}

function parseJsonArray<T>(value: string): T[] {
  try {
    const parsed = JSON.parse(value);
    return Array.isArray(parsed) ? (parsed as T[]) : [];
  } catch {
    return [];
  }
}

function formatDate(value?: string | null) {
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

function formatScore(value?: number | null) {
  return Number(value ?? 0).toFixed(3);
}

function formatCost(value?: number | null) {
  return `$${Number(value ?? 0).toFixed(6)}`;
}

function clampNumber(value: number, min: number, max: number, fallback: number) {
  if (!Number.isFinite(value)) {
    return fallback;
  }
  return Math.min(max, Math.max(min, value));
}

function escapeHtmlAttribute(value: string) {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("\"", "&quot;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;");
}
