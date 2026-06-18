"use client";

import {
  createContext,
  FormEvent,
  ReactNode,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";

import { useTranslations } from "next-intl";

import type { SectionDetail, SectionSlug } from "../../app/operator-data";
import { useWorkspaceStateValue, WorkspaceState } from "@/lib/workspace-context";
import type {
  BootstrapState,
  CatalogApiStatuses,
  ChatResponse,
  ControlStatus,
  Dataset,
  DevAction,
  DocumentAsset,
  ModelRoute,
  ProviderCardModel,
  RetrievedChunk,
  Run,
  ServiceState,
  TraceRecord,
} from "@/lib/types";
import {
  buildProviderCards,
  getRouteControlStatus,
  getRouteProviderName,
  inferDocumentsFromJobs,
} from "./helpers";
import { createDashboardActions } from "./dashboard-actions";

export type DashboardController = WorkspaceState & {
  section: SectionDetail;
  sectionSlug: SectionSlug;
  platformState: ServiceState;
  aiState: ServiceState;
  selectedFile: File | null;
  setSelectedFile: (file: File | null) => void;
  question: string;
  setQuestion: (value: string) => void;
  chatResult: ChatResponse | null;
  busy: string | null;
  setBusy: (value: string | null) => void;
  error: string | null;
  setError: (value: string | null) => void;
  trainingTargetColumn: string;
  setTrainingTargetColumn: (value: string) => void;
  trainingTask: "classification" | "regression";
  setTrainingTask: (value: "classification" | "regression") => void;
  documentStatus: ControlStatus;
  devStatuses: Record<DevAction, ControlStatus>;
  ragTopK: number;
  setRagTopK: (value: number) => void;
  scoreThreshold: number;
  setScoreThreshold: (value: number) => void;
  showNoAnswer: boolean;
  setShowNoAnswer: (value: boolean) => void;
  apiKeyInput: string;
  setApiKeyInput: (value: string) => void;
  apiKeyMode: "header" | "bearer";
  setApiKeyMode: (value: "header" | "bearer") => void;
  widgetOrigin: string;
  setWidgetOrigin: (value: string) => void;
  widgetTitle: string;
  setWidgetTitle: (value: string) => void;
  widgetTheme: "system" | "light" | "dark";
  setWidgetTheme: (value: "system" | "light" | "dark") => void;
  widgetMode: "embedded" | "floating";
  setWidgetMode: (value: "embedded" | "floating") => void;
  selectedTrace: TraceRecord | null;
  traceStatus: ControlStatus;
  selectedModelRouteId: string;
  setSelectedModelRouteId: (value: string) => void;
  catalogStatuses: CatalogApiStatuses;
  // Derived values
  activeDataset: Dataset | null;
  providerNameById: Map<string, string>;
  providerCards: ProviderCardModel[];
  selectedModelRoute: ModelRoute | null;
  selectedRouteProviderName: string;
  routeStatus: ControlStatus;
  retrievedChunks: RetrievedChunk[];
  fallbackMode: string;
  runId?: string;
  traceId?: string;
  estimatedCost: number;
  bestScore: number;
  qualityChunks: RetrievedChunk[];
  qualityBlocked: boolean;
  answerText?: string;
  documentRows: DocumentAsset[];
  chatRun: Run | null | undefined;
  apiKeyValue: string;
  apiKeyHeader: string;
  apiKeyHeaderValue: string;
  agentIdForSnippet: string;
  widgetUrl: string;
  // Handlers
  refreshModelCatalog: (workspaceId?: string) => Promise<void>;
  refreshJobs: (datasetId?: string) => Promise<void>;
  refreshDocuments: (datasetId?: string) => Promise<void>;
  bootstrapLocal: () => Promise<void>;
  ensureBootstrap: () => Promise<BootstrapState>;
  runDevAction: (action: DevAction) => Promise<void>;
  uploadDocument: (event: FormEvent<HTMLFormElement>) => Promise<void>;
  deleteDocument: (document: DocumentAsset) => Promise<void>;
  reingestDocument: (document: DocumentAsset) => Promise<void>;
  askAgent: (event: FormEvent<HTMLFormElement>) => Promise<void>;
  loadTrace: (targetTraceId?: string | null) => Promise<void>;
  refreshRuns: (agentId?: string) => Promise<void>;
  refreshTrainingJobs: (workspaceId?: string) => Promise<void>;
  refreshModelVersions: (workspaceId?: string) => Promise<void>;
  refreshEvalRuns: (workspaceId?: string) => Promise<void>;
  refreshHumanReviews: (workspaceId?: string) => Promise<void>;
};

function useDashboardController(section: SectionDetail, sectionSlug: SectionSlug): DashboardController {
  const tChat = useTranslations("chat");
  const tPlayground = useTranslations("playground");
  const workspace = useWorkspaceStateValue();
  const {
    bootstrap,
    setBootstrap,
    datasets,
    setDatasets,
    jobs,
    setJobs,
    runs,
    setRuns,
    setTrainingJobs,
    setModelVersions,
    setEvalRuns,
    setHumanReviews,
    documents,
    setDocuments,
    modelProviders,
    setModelProviders,
    modelRoutes,
    setModelRoutes,
    setPriceSnapshots,
    localModelStatus,
    setLocalModelStatus,
  } = workspace;
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
  const fallbackMode = chatResult?.fallback_mode ?? chatResult?.fallbackMode ?? tPlayground("notRun");
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
    ? tChat("noAnswer")
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

  const actions = createDashboardActions({
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
  });
  const {
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
  } = actions;

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

  return {
    ...workspace,
    section,
    sectionSlug,
    platformState,
    aiState,
    selectedFile,
    setSelectedFile,
    question,
    setQuestion,
    chatResult,
    busy,
    setBusy,
    error,
    setError,
    trainingTargetColumn,
    setTrainingTargetColumn,
    trainingTask,
    setTrainingTask,
    documentStatus,
    devStatuses,
    ragTopK,
    setRagTopK,
    scoreThreshold,
    setScoreThreshold,
    showNoAnswer,
    setShowNoAnswer,
    apiKeyInput,
    setApiKeyInput,
    apiKeyMode,
    setApiKeyMode,
    widgetOrigin,
    setWidgetOrigin,
    widgetTitle,
    setWidgetTitle,
    widgetTheme,
    setWidgetTheme,
    widgetMode,
    setWidgetMode,
    selectedTrace,
    traceStatus,
    selectedModelRouteId,
    setSelectedModelRouteId,
    catalogStatuses,
    activeDataset,
    providerNameById,
    providerCards,
    selectedModelRoute,
    selectedRouteProviderName,
    routeStatus,
    retrievedChunks,
    fallbackMode,
    runId,
    traceId,
    estimatedCost,
    bestScore,
    qualityChunks,
    qualityBlocked,
    answerText,
    documentRows,
    chatRun,
    apiKeyValue,
    apiKeyHeader,
    apiKeyHeaderValue,
    agentIdForSnippet,
    widgetUrl,
    refreshModelCatalog,
    refreshJobs,
    refreshDocuments,
    bootstrapLocal,
    ensureBootstrap,
    runDevAction,
    uploadDocument,
    deleteDocument,
    reingestDocument,
    askAgent,
    loadTrace,
    refreshRuns,
    refreshTrainingJobs,
    refreshModelVersions,
    refreshEvalRuns,
    refreshHumanReviews,
  };
}

const DashboardContext = createContext<DashboardController | null>(null);

export function DashboardProvider({
  section,
  sectionSlug,
  children,
}: {
  section: SectionDetail;
  sectionSlug: SectionSlug;
  children: ReactNode;
}) {
  const value = useDashboardController(section, sectionSlug);
  return <DashboardContext.Provider value={value}>{children}</DashboardContext.Provider>;
}

export function useDashboard(): DashboardController {
  const context = useContext(DashboardContext);
  if (!context) {
    throw new Error("useDashboard must be used within a DashboardProvider");
  }
  return context;
}
