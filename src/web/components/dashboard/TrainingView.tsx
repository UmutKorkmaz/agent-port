"use client";

import { useTranslations } from "next-intl";

import { useDashboard } from "./dashboard-context";

export function TrainingView() {
  const t = useTranslations("training");
  const {
    busy,
    setBusy,
    trainingJobs,
    modelVersions,
    trainingTargetColumn,
    setTrainingTargetColumn,
    trainingTask,
    setTrainingTask,
    activeDataset,
    ensureBootstrap,
    refreshTrainingJobs,
    refreshModelVersions,
    setError,
  } = useDashboard();

  return (
    <div className="section-grid">
      <section className="section-card">
        <h2>{t("trainingJobsTitle")}</h2>
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
                name: t("jobNamePrefix", { timestamp: new Date().toISOString().slice(0, 19) }),
                kind: trainingTask,
                targetColumn: trainingTargetColumn,
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
              setError(err instanceof Error ? err.message : t("createFailed"));
            } finally {
              setBusy(null);
            }
          }}
        >
          <div className="field-row">
            <label>{t("targetColumn")}</label>
            <input
              className="text-input"
              value={trainingTargetColumn}
              onChange={(e) => setTrainingTargetColumn(e.target.value)}
              placeholder={t("targetColumnPlaceholder")}
            />
          </div>
          <div className="field-row">
            <label>{t("task")}</label>
            <select
              className="text-input"
              value={trainingTask}
              onChange={(e) => setTrainingTask(e.target.value as "classification" | "regression")}
            >
              <option value="classification">{t("classification")}</option>
              <option value="regression">{t("regression")}</option>
            </select>
          </div>
          <button className="control-button" type="submit" disabled={busy === "create-training"}>
            {busy === "create-training" ? t("creating") : t("createTrainingJob")}
          </button>
        </form>
        <div className="asset-grid">
          {trainingJobs.map((job) => (
            <article className="asset-card" key={job.id}>
              <div className="asset-head">
                <p className="asset-label">{job.name}</p>
                <span className={`status-pill ${job.status}`}>{job.status}</span>
              </div>
              <p className="asset-detail">{t("kindAndCost", { kind: job.kind, cost: job.estimatedCost })}</p>
              <p className="asset-detail">{t("metrics", { metrics: job.metricsJson })}</p>
            </article>
          ))}
        </div>
      </section>
      <aside className="section-card">
        <h2>{t("modelVersionsTitle")}</h2>
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
          {t("refresh")}
        </button>
        <ul>
          {modelVersions.length === 0 ? <li>{t("noModelVersions")}</li> : null}
          {modelVersions.map((mv) => (
            <li key={mv.id}>
              {t("modelVersionSummary", { name: mv.name, kind: mv.kind, status: mv.status, artifact: mv.artifactUri ?? t("noArtifact") })}
            </li>
          ))}
        </ul>
      </aside>
    </div>
  );
}
