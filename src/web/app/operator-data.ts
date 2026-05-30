export const navigationSections = [
  { slug: "overview", label: "Overview", href: "/" },
  { slug: "stack-chooser", label: "Stack Chooser", href: "/stack-chooser" },
  { slug: "builder", label: "Builder", href: "/builder" },
  { slug: "model-catalog", label: "Model Catalog", href: "/model-catalog" },
  { slug: "playground", label: "Playground", href: "/playground" },
  { slug: "datasets", label: "Datasets", href: "/datasets" },
  { slug: "training", label: "Training", href: "/training" },
  { slug: "evals", label: "Evals", href: "/evals" },
  { slug: "traces", label: "Traces", href: "/traces" },
  { slug: "deployments", label: "Deployments", href: "/deployments" },
  { slug: "wallet", label: "Wallet", href: "/wallet" },
  { slug: "billing", label: "Billing", href: "/billing" },
  { slug: "settings", label: "Settings", href: "/settings" },
] as const;

export type SectionSlug = (typeof navigationSections)[number]["slug"];

export type ReadinessTone = "ready" | "pending" | "placeholder";

export type SectionDetail = {
  slug: SectionSlug;
  eyebrow: string;
  title: string;
  summary: string;
  status: string;
  primaryItems: string[];
  secondaryItems: string[];
  emptyTitle: string;
  emptyCopy: string;
};

export const readinessChecks: {
  label: string;
  state: string;
  tone: ReadinessTone;
  detail: string;
  href?: string;
}[] = [
  {
    label: "Platform API",
    state: "Proxy wired",
    tone: "ready",
    detail: "Web requests under /api/platform route to the local platform service.",
    href: "/api/platform/status",
  },
  {
    label: "AI service",
    state: "Proxy wired",
    tone: "ready",
    detail: "Web requests under /api/ai route to the local AI service.",
    href: "/api/ai/health",
  },
  {
    label: "Bootstrap",
    state: "Available",
    tone: "ready",
    detail: "Creates the local workspace, project, route, dataset, knowledge base, agent, wallet, and API key.",
  },
  {
    label: "Dev seed/reset",
    state: "Defensive UI",
    tone: "placeholder",
    detail: "Seed falls back to local bootstrap; reset reports backend availability when the dev endpoint is missing.",
  },
  {
    label: "Model route",
    state: "Local default",
    tone: "ready",
    detail: "Ollama is seeded as the default route, with extractive fallback when no model is reachable.",
  },
  {
    label: "Agent definition",
    state: "Created",
    tone: "ready",
    detail: "The bootstrap flow creates a document-QA agent linked to the default knowledge base.",
  },
  {
    label: "Billing config placeholders",
    state: "Placeholder",
    tone: "placeholder",
    detail: "Wallet and billing views reserve plan, wallet, and payment settings.",
  },
  {
    label: "Docs and examples",
    state: "Sample ready",
    tone: "ready",
    detail: "A support policy sample document is available for smoke testing the document-QA flow.",
  },
  {
    label: "RAG quality controls",
    state: "Client active",
    tone: "ready",
    detail: "Top K, score threshold, and no-answer visibility are exposed in the playground.",
  },
];

export const overviewMetrics = [
  {
    label: "Workspace mode",
    value: "Operator shell",
    detail: "Document upload, RAG chat, training, evals, traces, and snippets are available locally.",
  },
  {
    label: "Service proxies",
    value: "2 configured",
    detail: "/api/platform and /api/ai are routed by Next rewrites.",
  },
  {
    label: "Launch scope",
    value: "Phase 1.2",
    detail: "Document-QA operations, model catalog UI, route status, defensive dev controls, and trace polish.",
  },
  {
    label: "Billing state",
    value: "Placeholders",
    detail: "Wallet and billing paths exist without payment execution.",
  },
];

export const setupQueue = [
  {
    label: "Run onboarding",
    detail: "Create or confirm the local workspace, route, dataset, knowledge base, agent, wallet, and API key.",
    tone: "ready" as ReadinessTone,
  },
  {
    label: "Upload documents",
    detail: "Add .txt, .md, or .pdf documents and track inferred document assets from ingestion jobs.",
    tone: "pending" as ReadinessTone,
  },
  {
    label: "Ask in playground",
    detail: "Use cited extractive RAG locally, or Ollama when it is enabled and reachable.",
    tone: "ready" as ReadinessTone,
  },
  {
    label: "Review traces",
    detail: "Check answer, retrieved chunks, citations, run id, trace id, latency, cost, and trace metadata.",
    tone: "ready" as ReadinessTone,
  },
  {
    label: "Train a classifier",
    detail: "Upload a CSV dataset and train a scikit-learn classifier from the Training Lab.",
    tone: "ready" as ReadinessTone,
  },
  {
    label: "Register model version",
    detail: "Save trained artifacts to the model registry with candidate/staging/production aliases.",
    tone: "ready" as ReadinessTone,
  },
];

export const sectionDetails: Record<SectionSlug, SectionDetail> = {
  overview: {
    slug: "overview",
    eyebrow: "AgentPort / Phase 1.2",
    title: "Operator overview",
    summary:
      "Control surface for local document-QA operations, dev setup, RAG quality checks, trace review, and placeholders.",
    status: "Phase 1.2 ready",
    primaryItems: [],
    secondaryItems: [],
    emptyTitle: "No live incidents",
    emptyCopy: "Trace, eval, deployment, and billing events will appear here after service contracts are connected.",
  },
  "stack-chooser": {
    slug: "stack-chooser",
    eyebrow: "Design",
    title: "Stack chooser",
    summary:
      "Provider, runtime, storage, vector, evaluation, and deployment choices grouped for an agent workspace.",
    status: "Catalog placeholder",
    primaryItems: [
      "Provider family selection",
      "Runtime and orchestration options",
      "Storage, vector, and telemetry choices",
      "Deployment target profile",
    ],
    secondaryItems: [
      "Needs provider catalog API",
      "Needs compatibility rules",
      "Needs saved stack template contract",
    ],
    emptyTitle: "No stack templates saved",
    emptyCopy: "Templates will be listed once the platform API can persist workspace stack choices.",
  },
  builder: {
    slug: "builder",
    eyebrow: "Create",
    title: "Agent builder",
    summary:
      "Document-QA agent configuration linked to the local route, dataset, and knowledge base.",
    status: "Document QA ready",
    primaryItems: [
      "Identity and purpose block",
      "Tools and permissions",
      "Model route binding",
      "Dataset and eval links",
    ],
    secondaryItems: [
      "Needs agent definition schema",
      "Needs validation endpoint",
      "Needs draft save and publish actions",
    ],
    emptyTitle: "No draft agent selected",
    emptyCopy: "Draft summaries will appear here after the builder save contract is available.",
  },
  "model-catalog": {
    slug: "model-catalog",
    eyebrow: "Models",
    title: "Model catalog",
    summary:
      "Provider comparison, route status, local Ollama readiness, price snapshots, and endpoint health.",
    status: "Catalog UI ready",
    primaryItems: [
      "Provider cards with tradeoffs",
      "Route selector and status",
      "Local Ollama readiness",
      "Price and health sections",
    ],
    secondaryItems: [
      "Provider endpoint can replace fallback cards",
      "Price snapshots can replace qualitative costs",
      "Local model status endpoint can replace inferred readiness",
    ],
    emptyTitle: "Catalog fallback active",
    emptyCopy: "Provider reference cards are shown even when catalog endpoints are not available.",
  },
  playground: {
    slug: "playground",
    eyebrow: "Test",
    title: "Playground",
    summary:
      "Prompt and RAG quality test surface for validating local document-QA behavior before promotion.",
    status: "RAG controls ready",
    primaryItems: [
      "Prompt input",
      "Model route selector",
      "Tool-call preview",
      "Response and token summary",
    ],
    secondaryItems: [
      "Needs invoke endpoint",
      "Needs streaming response adapter",
      "Needs trace capture binding",
    ],
    emptyTitle: "No playground run loaded",
    emptyCopy: "Runs will appear after the AI service invocation path is available.",
  },
  datasets: {
    slug: "datasets",
    eyebrow: "Data",
    title: "Datasets",
    summary:
      "Workspace for document upload, inferred document inventory, delete/reingest controls, and ingestion status.",
    status: "Document ops ready",
    primaryItems: [
      "Dataset registry",
      "Source and consent metadata",
      "Version lineage",
      "Training and eval usage",
    ],
    secondaryItems: [
      "Needs dataset API",
      "Needs upload and import jobs",
      "Needs retention policy display",
    ],
    emptyTitle: "No datasets registered",
    emptyCopy: "Data assets will appear here after import and catalog endpoints are connected.",
  },
  evals: {
    slug: "evals",
    eyebrow: "Quality",
    title: "Evals",
    summary:
      "Evaluation suite list for acceptance gates, regression checks, scoring, and release readiness.",
    status: "Gate placeholder",
    primaryItems: [
      "Eval suite registry",
      "Scenario and rubric status",
      "Latest score summary",
      "Promotion gate outcome",
    ],
    secondaryItems: [
      "Needs eval suite contract",
      "Needs run scheduler",
      "Needs score persistence",
    ],
    emptyTitle: "No eval suites configured",
    emptyCopy: "Suites and latest scores will appear once evaluation contracts are available.",
  },
  traces: {
    slug: "traces",
    eyebrow: "Observe",
    title: "Traces",
    summary:
      "Operational trace stream for prompts, tools, model routing, identity, policy, costs, and failures.",
    status: "Run traces ready",
    primaryItems: [
      "Trace search",
      "Identity and policy columns",
      "Model and tool timeline",
      "Cost and latency summary",
    ],
    secondaryItems: [
      "Needs trace ingest schema",
      "Needs search endpoint",
      "Needs redaction policy",
    ],
    emptyTitle: "No traces captured",
    emptyCopy: "Trace records will appear after playground and deployed agents emit telemetry.",
  },
  deployments: {
    slug: "deployments",
    eyebrow: "Release",
    title: "Deployments",
    summary:
      "Local invocation, API key, and embeddable widget configuration placeholders.",
    status: "Config placeholder",
    primaryItems: [
      "Environment targets",
      "Version and promotion state",
      "Health and rollback signal",
      "Release approval status",
    ],
    secondaryItems: [
      "Needs deploy target contract",
      "Needs rollout history",
      "Needs health checks",
    ],
    emptyTitle: "No deployments active",
    emptyCopy: "Deployment cards will appear once agents can be promoted to a target runtime.",
  },
  wallet: {
    slug: "wallet",
    eyebrow: "Spend",
    title: "Wallet",
    summary:
      "Reserved operator view for credits, budgets, provider spend limits, and prepaid balance status.",
    status: "Billing placeholder",
    primaryItems: [
      "Workspace balance",
      "Provider spend limits",
      "Budget alerts",
      "Usage reservation state",
    ],
    secondaryItems: [
      "Needs wallet ledger contract",
      "Needs payment provider keys",
      "Needs spend reservation policy",
    ],
    emptyTitle: "No wallet ledger entries",
    emptyCopy: "Ledger activity will appear after billing configuration and usage metering are connected.",
  },
  billing: {
    slug: "billing",
    eyebrow: "Commercial",
    title: "Billing",
    summary:
      "Plan, invoice, payment, tax, and usage metering placeholder for the commercial control path.",
    status: "Config placeholder",
    primaryItems: [
      "Plan and subscription state",
      "Invoices and payment status",
      "Usage metering windows",
      "Tax and receipt profile",
    ],
    secondaryItems: [
      "Needs billing provider config",
      "Needs invoice webhook handling",
      "Needs metering reconciliation",
    ],
    emptyTitle: "No billing account linked",
    emptyCopy: "Billing records will appear after payment provider configuration is completed.",
  },
  training: {
    slug: "training",
    eyebrow: "Train",
    title: "Training Lab",
    summary:
      "Create training jobs for classifiers and regressors, monitor progress, and register model versions.",
    status: "CPU training ready",
    primaryItems: [
      "Training job registry",
      "CSV classifier/regression trainer",
      "Job logs, metrics, and artifacts",
      "Model version registration",
    ],
    secondaryItems: [
      "Needs GPU training for LoRA/fine-tuning",
      "Needs experiment tracking integration",
      "Needs wallet reservation before jobs",
    ],
    emptyTitle: "No training jobs yet",
    emptyCopy: "Training jobs will appear after you create a dataset and start a classifier training job.",
  },
  settings: {
    slug: "settings",
    eyebrow: "Admin",
    title: "Settings",
    summary:
      "Workspace administration placeholder for dev controls, identity, policy, keys, retention, and environment settings.",
    status: "Dev controls ready",
    primaryItems: [
      "Workspace identity",
      "Policy and permission slots",
      "Provider key references",
      "Retention and audit settings",
    ],
    secondaryItems: [
      "Needs identity contract",
      "Needs policy enforcement API",
      "Needs secret reference display",
    ],
    emptyTitle: "No settings loaded",
    emptyCopy: "Workspace settings will appear after identity and policy APIs are connected.",
  },
};

export function getSectionBySlug(slug: string): SectionDetail | undefined {
  return sectionDetails[slug as SectionSlug];
}
