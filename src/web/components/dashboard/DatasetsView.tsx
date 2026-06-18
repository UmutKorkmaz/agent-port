"use client";

import { useRef } from "react";
import { useTranslations } from "next-intl";

import { ControlStatusCard, EmptyStateBrand } from "./shared";
import { formatDate, getDocumentChunks, getDocumentId, getDocumentName } from "./helpers";
import { useDashboard } from "./dashboard-context";

export function DatasetsView() {
  const t = useTranslations("datasets");
  const {
    activeDataset,
    busy,
    setBusy,
    datasets,
    jobs,
    documentRows,
    documentStatus,
    refreshJobs,
    refreshDocuments,
    uploadDocument,
    setSelectedFile,
    reingestDocument,
    deleteDocument,
  } = useDashboard();

  const fileInputRef = useRef<HTMLInputElement>(null);

  return (
    <div className="section-grid">
      <section className="section-card">
        <div className="panel-header compact">
          <div>
            <h2>{t("documentSourcesTitle")}</h2>
            <p className="panel-copy">{t("documentSourcesCopy")}</p>
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
            {t("refresh")}
          </button>
        </div>
        <form className="control-form" onSubmit={uploadDocument}>
          <input
            ref={fileInputRef}
            className="file-input"
            type="file"
            accept=".txt,.md,.pdf,text/plain,text/markdown,application/pdf"
            onChange={(event) => setSelectedFile(event.target.files?.[0] ?? null)}
          />
          <button className="control-button" type="submit" disabled={busy === "upload"}>
            {busy === "upload" ? t("ingesting") : t("uploadDocument")}
          </button>
        </form>
        <ControlStatusCard status={documentStatus} />
        <div className="document-list">
          {documentRows.length === 0 ? (
            <EmptyStateBrand
              title={t("noDocumentsTitle")}
              copy={t("noDocumentsCopy")}
              watermarkLabel={t("noDocumentsWatermark")}
              action={
                <button
                  className="control-button"
                  type="button"
                  onClick={() => fileInputRef.current?.click()}
                  disabled={busy === "upload"}
                >
                  {t("noDocumentsAction")}
                </button>
              }
            />
          ) : null}
          {documentRows.map((document) => {
            const documentId = getDocumentId(document);
            return (
              <article className="asset-card" key={documentId}>
                <div className="asset-head">
                  <p className="asset-label">{getDocumentName(document)}</p>
                  <span className={`status-pill ${document.source === "api" ? "ready" : "placeholder"}`}>
                    {document.status ?? document.source ?? t("document")}
                  </span>
                </div>
                <p className="asset-detail font-mono">{t("chunksAndId", { chunks: getDocumentChunks(document), id: documentId })}</p>
                <p className="asset-detail">{t("updated", { date: formatDate(document.updatedAt ?? document.createdAt) })}</p>
                <div className="button-row compact">
                  <button
                    className="control-button secondary"
                    type="button"
                    onClick={() => void reingestDocument(document)}
                    disabled={busy === `reingest-${documentId}`}
                  >
                    {busy === `reingest-${documentId}` ? t("reingesting") : t("reingest")}
                  </button>
                  <button
                    className="control-button danger"
                    type="button"
                    onClick={() => void deleteDocument(document)}
                    disabled={busy === `delete-${documentId}`}
                  >
                    {busy === `delete-${documentId}` ? t("deleting") : t("delete")}
                  </button>
                </div>
              </article>
            );
          })}
        </div>
      </section>
      <aside className="section-card">
        <h2>{t("datasetsTitle")}</h2>
        <div className="asset-grid">
          {datasets.map((dataset) => (
            <article className="asset-card" key={dataset.id}>
              <div className="asset-head">
                <p className="asset-label">{dataset.name}</p>
                <span className="status-pill ready">{dataset.kind}</span>
              </div>
              <p className="asset-detail">{t("datasetSummary", { documents: dataset.documentCount, chunks: dataset.chunkCount })}</p>
              <p className="asset-detail font-mono">{dataset.id}</p>
            </article>
          ))}
        </div>
      </aside>
      <aside className="section-card">
        <h2>{t("ingestionJobsTitle")}</h2>
        <ul>
          {jobs.length === 0 ? <li>{t("noIngestionJobs")}</li> : null}
          {jobs.map((job) => (
            <li key={job.id}>
              {t("jobSummary", { status: job.status, chunks: job.chunkCount, documentId: job.documentAssetId, date: formatDate(job.createdAt) })}
            </li>
          ))}
        </ul>
      </aside>
    </div>
  );
}
