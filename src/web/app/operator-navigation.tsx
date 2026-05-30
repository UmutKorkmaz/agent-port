"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

import { navigationSections } from "./operator-data";

export function OperatorNavigation() {
  const pathname = usePathname();

  return (
    <nav className="shell-nav" aria-label="Operator navigation">
      <div className="nav-header">
        <div className="brand-lockup">
          <p className="brand-name">AgentPort</p>
          <p className="brand-meta">Phase 1.2 operator console</p>
        </div>
        <span className="phase-badge">v0.1</span>
      </div>
      <div className="nav-list">
        {navigationSections.map((section) => {
          const isOverview = section.slug === "overview";
          const isActive = isOverview
            ? pathname === "/" || pathname === "/overview"
            : pathname === section.href;

          return (
            <Link
              aria-current={isActive ? "page" : undefined}
              className={`nav-link${isActive ? " is-active" : ""}`}
              href={section.href}
              key={section.slug}
            >
              {section.label}
            </Link>
          );
        })}
      </div>
    </nav>
  );
}
