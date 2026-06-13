"use client";

import { SectionDetail, SectionSlug } from "./operator-data";
import { DashboardProvider, useDashboard } from "@/components/dashboard/dashboard-context";
import { BuilderView } from "@/components/dashboard/BuilderView";
import { DatasetsView } from "@/components/dashboard/DatasetsView";
import { DeploymentsView } from "@/components/dashboard/DeploymentsView";
import { EvalsView } from "@/components/dashboard/EvalsView";
import { ModelCatalogView } from "@/components/dashboard/ModelCatalogView";
import { OverviewView } from "@/components/dashboard/OverviewView";
import { PlaygroundView } from "@/components/dashboard/PlaygroundView";
import { SettingsView } from "@/components/dashboard/SettingsView";
import { StackChooserView } from "@/components/dashboard/StackChooserView";
import { TracesView } from "@/components/dashboard/TracesView";
import { TrainingView } from "@/components/dashboard/TrainingView";
import { WalletView } from "@/components/dashboard/WalletView";

export function OperatorDashboardClient({
  section,
  sectionSlug,
}: {
  section: SectionDetail;
  sectionSlug: SectionSlug;
}) {
  return (
    <DashboardProvider section={section} sectionSlug={sectionSlug}>
      <DashboardShell />
    </DashboardProvider>
  );
}

function DashboardShell() {
  const { error } = useDashboard();
  return (
    <>
      {error ? <div className="error-banner">{error}</div> : null}
      <ActiveView />
    </>
  );
}

function ActiveView() {
  const { sectionSlug } = useDashboard();
  switch (sectionSlug) {
    case "overview":
      return <OverviewView />;
    case "datasets":
      return <DatasetsView />;
    case "model-catalog":
      return <ModelCatalogView />;
    case "training":
      return <TrainingView />;
    case "builder":
      return <BuilderView />;
    case "playground":
      return <PlaygroundView />;
    case "evals":
      return <EvalsView />;
    case "traces":
      return <TracesView />;
    case "deployments":
      return <DeploymentsView />;
    case "settings":
      return <SettingsView />;
    case "wallet":
    case "billing":
      return <WalletView />;
    default:
      return <StackChooserView />;
  }
}
