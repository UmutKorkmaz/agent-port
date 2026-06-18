import { SectionView } from "./shared";
import { useDashboard } from "./dashboard-context";

export function StackChooserView() {
  const { section } = useDashboard();
  return <SectionView section={section} />;
}
