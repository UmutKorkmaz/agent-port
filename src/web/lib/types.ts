import { ReadinessTone } from "../app/operator-data";
import { ProviderCatalogDetail } from "../app/model-catalog-data";

export type ServiceState = "checking" | "ok" | "error";

export type BootstrapState = {
  workspaceId: string;
  projectId: string;
  modelRouteId: string;
  modelProviderId?: string;
  agentDefinitionId: string;
  datasetId: string;
  knowledgeBaseId: string;
  walletAccountId: string;
  apiKey?: string | null;
  apiKeyMessage?: string;
};

export type ModelProvider = {
  id?: string;
  workspaceId?: string | null;
  name?: string;
  label?: string;
  kind?: string;
  baseUrl?: string | null;
  isEnabled?: boolean;
  metadataJson?: string;
  capabilities?: string[];
  useCase?: string;
  advantage?: string;
  disadvantage?: string;
  cost?: string;
  privacy?: string;
  lockIn?: string;
  keyMode?: string;
  createdAt?: string;
  updatedAt?: string;
};

export type ModelRoute = {
  id: string;
  workspaceId?: string;
  projectId?: string | null;
  providerId?: string;
  providerName?: string;
  name: string;
  slug?: string;
  modelName: string;
  routeType?: string;
  priority?: number;
  isDefault?: boolean;
  isEnabled?: boolean;
  createdAt?: string;
  updatedAt?: string;
};

export type ProviderPriceSnapshot = {
  id?: string;
  providerId?: string;
  providerName?: string;
  modelName?: string;
  currency?: string;
  inputTokenPricePerMillion?: number | string;
  outputTokenPricePerMillion?: number | string;
  requestPrice?: number | string;
  capturedAt?: string;
  createdAt?: string;
};

export type LocalModelStatus = {
  status?: string;
  provider?: string;
  model?: string;
  modelName?: string;
  routeHealth?: string;
  enabled?: boolean;
  baseUrl?: string;
  errorType?: string | null;
  checkedAt?: string;
  models?: string[];
  metadata?: Record<string, unknown>;
};

export type CatalogApiStatuses = {
  status: ControlStatus;
  catalog: ControlStatus;
  providers: ControlStatus;
  routes: ControlStatus;
  prices: ControlStatus;
  localModel: ControlStatus;
};

export type ProviderCardModel = ProviderCatalogDetail & {
  id?: string;
  baseUrl?: string | null;
  isEnabled?: boolean;
  source: "platform" | "fallback" | "route";
};

export type Dataset = {
  id: string;
  workspaceId: string;
  projectId: string;
  knowledgeBaseId?: string | null;
  name: string;
  slug: string;
  kind: string;
  documentCount: number;
  chunkCount: number;
};

export type IngestionJob = {
  id: string;
  datasetId: string;
  documentAssetId: string;
  status: string;
  chunkCount: number;
  errorMessage?: string | null;
  createdAt: string;
};

export type Citation = {
  citation_id?: string;
  citationId?: string;
  file_name?: string;
  fileName?: string;
  score?: number;
};

export type RetrievedChunk = {
  id: string;
  citation_id?: string;
  citationId?: string;
  text: string;
  score: number;
  metadata?: Record<string, unknown>;
};

export type ChatResponse = {
  answer: string;
  citations: Citation[];
  retrieved_chunks?: RetrievedChunk[];
  retrievedChunks?: RetrievedChunk[];
  retrieval?: {
    top_k?: number;
    topK?: number;
    score_threshold?: number;
    scoreThreshold?: number;
    max_score?: number | null;
    maxScore?: number | null;
    no_answer?: boolean;
    noAnswer?: boolean;
  };
  provider_response?: Record<string, unknown>;
  providerResponse?: Record<string, unknown>;
  run_id?: string;
  runId?: string;
  trace_id_record?: string;
  traceIdRecord?: string;
  fallback_mode?: string;
  fallbackMode?: string;
  estimated_cost?: number;
  estimatedCost?: number;
};

export type Run = {
  id: string;
  agentDefinitionId: string;
  traceRecordId?: string | null;
  question: string;
  answer: string;
  fallbackMode: string;
  citationsJson: string;
  retrievedChunksJson: string;
  latencyMs: number;
  estimatedCost: number;
  createdAt: string;
};

export type TrainingJob = {
  id: string;
  workspaceId: string;
  projectId: string;
  datasetId?: string | null;
  name: string;
  slug: string;
  kind: string;
  status: string;
  configJson: string;
  hyperparametersJson: string;
  artifactsJson: string;
  metricsJson: string;
  estimatedCost: number;
  actualCost: number;
  startedAt?: string | null;
  completedAt?: string | null;
  failureReason?: string | null;
  createdAt: string;
  updatedAt: string;
};

export type ModelVersion = {
  id: string;
  workspaceId: string;
  projectId: string;
  trainingJobId?: string | null;
  name: string;
  slug: string;
  kind: string;
  status: string;
  artifactUri?: string | null;
  configJson: string;
  createdAt: string;
  updatedAt: string;
};

export type EvalRun = {
  id: string;
  workspaceId: string;
  projectId: string;
  evalSuiteId: string;
  modelVersionId?: string | null;
  status: string;
  score?: number | null;
  threshold?: number | null;
  passed: boolean;
  createdAt: string;
};

export type HumanReviewRecord = {
  id: string;
  workspaceId: string;
  queue: string;
  status: string;
  label?: string | null;
  severity?: string | null;
  reason?: string | null;
  reviewerName?: string | null;
  reviewedAt?: string | null;
  canReuseForTraining: boolean;
  createdAt: string;
};

export type DocumentAsset = {
  id?: string;
  documentAssetId?: string;
  datasetId?: string;
  fileName?: string;
  name?: string;
  status?: string;
  contentHash?: string | null;
  documentVersion?: number;
  isActive?: boolean;
  chunkCount?: number;
  sizeBytes?: number;
  latestIngestionJobId?: string;
  archivedAt?: string | null;
  deletedAt?: string | null;
  createdAt?: string;
  updatedAt?: string;
  source?: "api" | "ingestion-job";
};

export type TraceRecord = {
  id: string;
  workspaceId: string;
  projectId: string;
  agentDefinitionId?: string | null;
  modelRouteId?: string | null;
  correlationId?: string | null;
  traceType: string;
  status: string;
  inputTokens: number;
  outputTokens: number;
  costAmount: number;
  startedAt: string;
  endedAt?: string | null;
  metadataJson: string;
  createdAt: string;
};

export type ControlStatus = {
  label: string;
  detail: string;
  tone: ReadinessTone;
};

export type DevAction = "reset" | "seed";
