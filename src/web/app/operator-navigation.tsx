"use client";

import Image from "next/image";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useTranslations } from "next-intl";
import { useEffect, useState } from "react";

import { LocaleToggle } from "@/components/LocaleToggle";
import { ThemeToggle } from "@/components/ThemeToggle";

import { navigationSections } from "./operator-data";

const SIDEBAR_BREAKPOINT = 1024;

export function OperatorNavigation() {
  const pathname = usePathname();
  const t = useTranslations("nav");
  const [isOpen, setIsOpen] = useState(false);

  // Close the drawer whenever the route changes (covers link taps + browser nav).
  useEffect(() => {
    setIsOpen(false);
  }, [pathname]);

  // Close the drawer when the viewport grows past the sidebar breakpoint so the
  // persistent sidebar takes over and no scrim is left trapping the page.
  useEffect(() => {
    if (typeof window === "undefined") {
      return;
    }
    const mql = window.matchMedia(`(min-width: ${SIDEBAR_BREAKPOINT}px)`);
    const onChange = (event: MediaQueryListEvent) => {
      if (event.matches) {
        setIsOpen(false);
      }
    };
    mql.addEventListener("change", onChange);
    return () => mql.removeEventListener("change", onChange);
  }, []);

  // Escape closes the drawer; lock body scroll while it is open on mobile.
  useEffect(() => {
    if (!isOpen) {
      return;
    }
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setIsOpen(false);
      }
    };
    document.addEventListener("keydown", onKey);
    document.body.style.overflow = "hidden";
    return () => {
      document.removeEventListener("keydown", onKey);
      document.body.style.overflow = "";
    };
  }, [isOpen]);

  return (
    <nav className="shell-nav" aria-label={t("ariaLabel")}>
      {/* Mobile top bar: brand lockup + hamburger toggle. Hidden at >=1024px. */}
      <div className="nav-topbar">
        <div className="brand-lockup">
          <Image
            src="/agentport-logo.png"
            alt=""
            width={30}
            height={30}
            className="brand-logo brand-logo-light"
            priority
          />
          <Image
            src="/agentport-logo-dark.png"
            alt=""
            width={30}
            height={30}
            className="brand-logo brand-logo-dark"
            priority
          />
          <span className="brand-lockup-text">
            <span className="brand-name">{t("brandName")}</span>
            <span className="brand-meta">{t("brandMeta")}</span>
          </span>
        </div>
        <span className="phase-badge">v0.1</span>
        <div className="nav-topbar-controls">
          <ThemeToggle />
          <LocaleToggle />
        </div>
        <button
          type="button"
          className={`nav-toggle${isOpen ? " is-open" : ""}`}
          aria-label={isOpen ? t("toggleClose") : t("toggleOpen")}
          aria-expanded={isOpen}
          aria-controls="nav-drawer"
          onClick={() => setIsOpen((prev) => !prev)}
        >
          <span className="nav-toggle-bars" aria-hidden="true" />
        </button>
      </div>

      {/* Scrim behind the mobile drawer. */}
      <button
        type="button"
        className={`nav-scrim${isOpen ? " is-open" : ""}`}
        tabIndex={isOpen ? 0 : -1}
        aria-hidden={!isOpen}
        aria-label={t("toggleClose")}
        onClick={() => setIsOpen(false)}
      />

      {/* Drawer / sidebar body. Slides in on mobile; persistent column at >=1024px. */}
      <div
        id="nav-drawer"
        className={`nav-drawer${isOpen ? " is-open" : ""}`}
        aria-label={t("drawerLabel")}
      >
        <div className="nav-brand-sidebar">
          <div className="brand-lockup">
            <Image
              src="/agentport-logo.png"
              alt=""
              width={30}
              height={30}
              className="brand-logo brand-logo-light"
              priority
            />
            <Image
              src="/agentport-logo-dark.png"
              alt=""
              width={30}
              height={30}
              className="brand-logo brand-logo-dark"
              priority
            />
            <span className="brand-lockup-text">
              <span className="brand-name">{t("brandName")}</span>
              <span className="brand-meta">{t("brandMeta")}</span>
            </span>
          </div>
          <span className="phase-badge">v0.1</span>
        </div>

        <div className="nav-controls">
          <LocaleToggle />
          <ThemeToggle />
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
                {t(section.slug)}
              </Link>
            );
          })}
        </div>
      </div>
    </nav>
  );
}
