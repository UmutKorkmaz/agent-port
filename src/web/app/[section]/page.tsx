import { notFound } from "next/navigation";

import { OperatorDashboard } from "../operator-dashboard";
import { getSectionBySlug, navigationSections } from "../operator-data";

type SectionPageProps = {
  params: Promise<{
    section: string;
  }>;
};

export function generateStaticParams() {
  return navigationSections.map((section) => ({
    section: section.slug,
  }));
}

export default async function SectionPage({ params }: SectionPageProps) {
  const { section } = await params;
  const sectionDetail = getSectionBySlug(section);

  if (!sectionDetail) {
    notFound();
  }

  return <OperatorDashboard sectionSlug={sectionDetail.slug} />;
}
