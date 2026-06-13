"use client";

import {
  createContext,
  Dispatch,
  ReactNode,
  SetStateAction,
  useContext,
  useState,
} from "react";

import {
  BootstrapState,
  Dataset,
  EvalRun,
  DocumentAsset,
  HumanReviewRecord,
  IngestionJob,
  LocalModelStatus,
  ModelProvider,
  ModelRoute,
  ModelVersion,
  ProviderPriceSnapshot,
  Run,
  TrainingJob,
} from "./types";

/**
 * Shared workspace state extracted from the operator dashboard's `useState`
 * cluster. The provider owns the workspace artifacts that multiple console
 * sections read and mutate (bootstrap context plus the loaded collections).
 *
 * Behavior is preserved: every `useState` call has the same initial value and
 * runs in the same order it did inside the dashboard component.
 */
export type WorkspaceState = {
  bootstrap: BootstrapState | null;
  setBootstrap: Dispatch<SetStateAction<BootstrapState | null>>;
  datasets: Dataset[];
  setDatasets: Dispatch<SetStateAction<Dataset[]>>;
  jobs: IngestionJob[];
  setJobs: Dispatch<SetStateAction<IngestionJob[]>>;
  runs: Run[];
  setRuns: Dispatch<SetStateAction<Run[]>>;
  trainingJobs: TrainingJob[];
  setTrainingJobs: Dispatch<SetStateAction<TrainingJob[]>>;
  modelVersions: ModelVersion[];
  setModelVersions: Dispatch<SetStateAction<ModelVersion[]>>;
  evalRuns: EvalRun[];
  setEvalRuns: Dispatch<SetStateAction<EvalRun[]>>;
  humanReviews: HumanReviewRecord[];
  setHumanReviews: Dispatch<SetStateAction<HumanReviewRecord[]>>;
  documents: DocumentAsset[];
  setDocuments: Dispatch<SetStateAction<DocumentAsset[]>>;
  modelProviders: ModelProvider[];
  setModelProviders: Dispatch<SetStateAction<ModelProvider[]>>;
  modelRoutes: ModelRoute[];
  setModelRoutes: Dispatch<SetStateAction<ModelRoute[]>>;
  priceSnapshots: ProviderPriceSnapshot[];
  setPriceSnapshots: Dispatch<SetStateAction<ProviderPriceSnapshot[]>>;
  localModelStatus: LocalModelStatus | null;
  setLocalModelStatus: Dispatch<SetStateAction<LocalModelStatus | null>>;
};

export function useWorkspaceStateValue(): WorkspaceState {
  const [bootstrap, setBootstrap] = useState<BootstrapState | null>(null);
  const [datasets, setDatasets] = useState<Dataset[]>([]);
  const [jobs, setJobs] = useState<IngestionJob[]>([]);
  const [runs, setRuns] = useState<Run[]>([]);
  const [trainingJobs, setTrainingJobs] = useState<TrainingJob[]>([]);
  const [modelVersions, setModelVersions] = useState<ModelVersion[]>([]);
  const [evalRuns, setEvalRuns] = useState<EvalRun[]>([]);
  const [humanReviews, setHumanReviews] = useState<HumanReviewRecord[]>([]);
  const [documents, setDocuments] = useState<DocumentAsset[]>([]);
  const [modelProviders, setModelProviders] = useState<ModelProvider[]>([]);
  const [modelRoutes, setModelRoutes] = useState<ModelRoute[]>([]);
  const [priceSnapshots, setPriceSnapshots] = useState<ProviderPriceSnapshot[]>([]);
  const [localModelStatus, setLocalModelStatus] = useState<LocalModelStatus | null>(null);

  return {
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
  };
}

const WorkspaceContext = createContext<WorkspaceState | null>(null);

export function WorkspaceProvider({ children }: { children: ReactNode }) {
  const value = useWorkspaceStateValue();
  return <WorkspaceContext.Provider value={value}>{children}</WorkspaceContext.Provider>;
}

export function useWorkspace(): WorkspaceState {
  const context = useContext(WorkspaceContext);
  if (!context) {
    throw new Error("useWorkspace must be used within a WorkspaceProvider");
  }
  return context;
}
