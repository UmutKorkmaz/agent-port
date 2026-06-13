"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useTranslations } from "next-intl";

import { LocaleToggle } from "@/components/LocaleToggle";

import { navigationSections } from "./operator-data";

export function OperatorNavigation() {
  const pathname = usePathname();
  const t = useTranslations("nav");

  return (
    <nav className="shell-nav" aria-label={t("ariaLabel")}>
      <div className="nav-header">
        <div className="brand-lockup">
          <p className="brand-name">AgentPort</p>
          <p className="brand-meta">{t("brandMeta")}</p>
        </div>
        <span className="phase-badge">v0.1</span>
      </div>
      <LocaleToggle />
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
              {t(section.slug)}
            </Link>
          );
        })}
      </div>
    </nav>
  );
}
