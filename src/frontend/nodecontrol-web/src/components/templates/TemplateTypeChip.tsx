import { Chip } from "@mui/material";
import type { TemplateType } from "@/lib/api/templates";

const labels: Record<TemplateType, string> = {
  GenericText: "Generic Text",
  Jinja2: "Jinja2",
  AnsibleVars: "Ansible Vars",
  ShellScript: "Shell Script",
  ConfigFile: "Config File",
};

type TemplateTypeChipProps = {
  templateType: TemplateType;
};

export function TemplateTypeChip({ templateType }: TemplateTypeChipProps) {
  return <Chip label={labels[templateType]} size="small" variant="outlined" />;
}
