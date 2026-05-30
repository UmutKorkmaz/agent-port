import { OperatorDashboardClient } from "./operator-dashboard-client";
import { sectionDetails, SectionSlug } from "./operator-data";

export function OperatorDashboard({ sectionSlug }: { sectionSlug: SectionSlug }) {
  const section = sectionDetails[sectionSlug];

  return (
    <div className="operator-page">
      <header className="page-header">
        <div>
          <p className="eyebrow">{section.eyebrow}</p>
          <h1 className="page-title">{section.title}</h1>
          <p className="page-copy">{section.summary}</p>
        </div>
        <aside className="environment-panel" aria-label="Workspace status">
          <div className="environment-row">
            <span className="environment-label">Environment</span>
            <span className="environment-value">Local integrated workspace</span>
          </div>
          <div className="environment-row">
            <span className="environment-label">Phase</span>
            <span className="environment-value">Phase 1.2</span>
          </div>
          <div className="environment-row">
            <span className="environment-label">View status</span>
            <span className="environment-value">{section.status}</span>
          </div>
        </aside>
      </header>

      <OperatorDashboardClient section={section} sectionSlug={sectionSlug} />
    </div>
  );
}
