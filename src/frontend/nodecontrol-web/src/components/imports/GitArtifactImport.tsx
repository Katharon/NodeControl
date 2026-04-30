"use client";

import DownloadIcon from "@mui/icons-material/Download";
import { Alert, Button, CircularProgress, Divider, MenuItem, Paper, Stack, TextField, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { getGitRepositories, type GitRepository } from "@/lib/api/gitRepositories";
import { getPlaybook, getPlaybooks, updatePlaybook } from "@/lib/api/playbooks";
import { getTemplate, getTemplates, updateTemplate } from "@/lib/api/templates";

type GitArtifactImportProps = {
  customerId: string;
  canManageArtifacts: boolean;
};

type ImportTarget = "Playbook" | "Template";

export function GitArtifactImport({ customerId, canManageArtifacts }: GitArtifactImportProps) {
  const queryClient = useQueryClient();
  const [target, setTarget] = useState<ImportTarget>("Playbook");
  const [repositoryId, setRepositoryId] = useState("");
  const [refOverride, setRefOverride] = useState("");
  const [subpathOverride, setSubpathOverride] = useState("");
  const [playbookId, setPlaybookId] = useState("");
  const [templateId, setTemplateId] = useState("");
  const [entryFilePath, setEntryFilePath] = useState("site.yml");
  const [artifactPathsText, setArtifactPathsText] = useState("site.yml\nroles/app/tasks/main.yml");
  const [templatePath, setTemplatePath] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const repositoriesQuery = useQuery({ queryKey: ["git-repositories", customerId], queryFn: () => getGitRepositories(customerId) });
  const playbooksQuery = useQuery({ queryKey: ["playbooks", customerId], queryFn: () => getPlaybooks(customerId) });
  const templatesQuery = useQuery({ queryKey: ["templates", customerId], queryFn: () => getTemplates(customerId) });
  const selectedRepository = useMemo(
    () => repositoriesQuery.data?.find((repository) => repository.id === repositoryId) ?? null,
    [repositoriesQuery.data, repositoryId],
  );
  const importMutation = useMutation({
    mutationFn: importFromGit,
    onSuccess: async (summary) => {
      await queryClient.invalidateQueries({ queryKey: ["playbooks", customerId] });
      await queryClient.invalidateQueries({ queryKey: ["templates", customerId] });
      if (playbookId) {
        await queryClient.invalidateQueries({ queryKey: ["playbook", customerId, playbookId] });
      }

      if (templateId) {
        await queryClient.invalidateQueries({ queryKey: ["template", customerId, templateId] });
      }

      setMessage(summary);
      setError(null);
    },
    onError: (importError) => {
      setMessage(null);
      setError(importError instanceof Error ? importError.message : "Import failed.");
    },
  });

  if (repositoriesQuery.isPending || playbooksQuery.isPending || templatesQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (repositoriesQuery.isError || playbooksQuery.isError || templatesQuery.isError) {
    return <Alert severity="error">Import data could not be loaded.</Alert>;
  }

  async function importFromGit() {
    if (!canManageArtifacts) {
      throw new Error("You do not have permission to import artifacts.");
    }

    if (!selectedRepository) {
      throw new Error("Select a Git repository source.");
    }

    const ref = getImportRef(selectedRepository, refOverride);
    const subpath = normalizeOptionalPath(subpathOverride || selectedRepository.subpath || "");

    if (target === "Playbook") {
      if (!playbookId) {
        throw new Error("Select a playbook.");
      }

      const entry = normalizeArtifactPath(entryFilePath);
      const artifactPaths = parseArtifactPaths(artifactPathsText);
      if (!artifactPaths.includes(entry)) {
        throw new Error("The artifact paths must include the entry file.");
      }

      const files = await Promise.all(
        artifactPaths.map(async (path) => ({
          path,
          content: await fetchGitHubFile(selectedRepository, joinPath(subpath, path), ref),
        })),
      );
      const playbook = await getPlaybook(customerId, playbookId);
      await updatePlaybook(customerId, playbookId, {
        name: playbook.name,
        slug: playbook.slug,
        description: playbook.description,
        sourceType: "ArtifactDirectory",
        inlineContent: null,
        entryFilePath: entry,
        artifactFiles: files,
      });

      return `Imported ${files.length} Git files into playbook '${playbook.name}'.`;
    }

    if (!templateId) {
      throw new Error("Select a template.");
    }

    const path = normalizeArtifactPath(templatePath);
    const content = await fetchGitHubFile(selectedRepository, joinPath(subpath, path), ref);
    const template = await getTemplate(customerId, templateId);
    await updateTemplate(customerId, templateId, {
      name: template.name,
      slug: template.slug,
      description: template.description,
      templateType: template.templateType,
      language: template.language,
      content,
    });

    return `Imported ${path} into template '${template.name}'.`;
  }

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack>
        <Typography component="h1" variant="h4">Import</Typography>
        <Typography color="text.secondary">
          One-time import from a public GitHub repository into managed NodeControl artifact content.
        </Typography>
      </Stack>

      {!canManageArtifacts ? (
        <Alert severity="warning">You do not have permission to import playbook or template artifacts for this customer.</Alert>
      ) : null}
      {message ? <Alert severity="success">{message}</Alert> : null}
      {error ? <Alert severity="error">{error}</Alert> : null}

      <Paper variant="outlined" sx={{ p: 3 }}>
        <Stack sx={{ gap: 2 }}>
          <Alert severity="info">
            Imports copy file content into existing Playbook or Template records. Runs continue to use the managed artifact content; NodeControl does not sync or clone Git during execution.
          </Alert>
          <TextField label="Target" select value={target} onChange={(event) => setTarget(event.target.value as ImportTarget)}>
            <MenuItem value="Playbook">Playbook artifact files</MenuItem>
            <MenuItem value="Template">Template content</MenuItem>
          </TextField>
          <TextField label="Git repository" select value={repositoryId} onChange={(event) => setRepositoryId(event.target.value)}>
            <MenuItem value="">Select repository</MenuItem>
            {repositoriesQuery.data.map((repository) => (
              <MenuItem key={repository.id} value={repository.id}>{repository.name}</MenuItem>
            ))}
          </TextField>
          {selectedRepository ? (
            <Typography color="text.secondary" sx={{ overflowWrap: "anywhere" }} variant="body2">
              {selectedRepository.repositoryUrl}
            </Typography>
          ) : null}
          <Stack direction={{ xs: "column", md: "row" }} sx={{ gap: 2 }}>
            <TextField
              helperText="Optional; falls back to repository revision, branch, then main."
              label="Branch / revision for this import"
              onChange={(event) => setRefOverride(event.target.value)}
              sx={{ flex: 1 }}
              value={refOverride}
            />
            <TextField
              helperText="Optional; falls back to repository subpath."
              label="Subpath for this import"
              onChange={(event) => setSubpathOverride(event.target.value)}
              sx={{ flex: 1 }}
              value={subpathOverride}
            />
          </Stack>
          <Divider />
          {target === "Playbook" ? (
            <Stack sx={{ gap: 2 }}>
              <TextField label="Playbook" select value={playbookId} onChange={(event) => setPlaybookId(event.target.value)}>
                <MenuItem value="">Select playbook</MenuItem>
                {playbooksQuery.data.map((playbook) => (
                  <MenuItem key={playbook.id} value={playbook.id}>{playbook.name}</MenuItem>
                ))}
              </TextField>
              <TextField label="Entry file" onChange={(event) => setEntryFilePath(event.target.value)} value={entryFilePath} />
              <TextField
                helperText="One relative file path per line. Paths are fetched under the selected source subpath and stored as artifact paths."
                label="Artifact file paths"
                minRows={8}
                multiline
                onChange={(event) => setArtifactPathsText(event.target.value)}
                value={artifactPathsText}
              />
            </Stack>
          ) : (
            <Stack sx={{ gap: 2 }}>
              <TextField label="Template" select value={templateId} onChange={(event) => setTemplateId(event.target.value)}>
                <MenuItem value="">Select template</MenuItem>
                {templatesQuery.data.map((template) => (
                  <MenuItem key={template.id} value={template.id}>{template.name}</MenuItem>
                ))}
              </TextField>
              <TextField
                helperText="Relative file path under the selected source subpath."
                label="Template source file"
                onChange={(event) => setTemplatePath(event.target.value)}
                value={templatePath}
              />
            </Stack>
          )}
          <Button
            disabled={!canManageArtifacts || importMutation.isPending}
            onClick={() => importMutation.mutate()}
            startIcon={<DownloadIcon />}
            sx={{ alignSelf: "flex-start" }}
            variant="contained"
          >
            Import from Git
          </Button>
        </Stack>
      </Paper>
    </Stack>
  );
}

function getImportRef(repository: GitRepository, override: string) {
  const value = override.trim() || repository.revision || repository.branch || "main";
  if (value.includes("..") || value.startsWith("-") || /\s/.test(value)) {
    throw new Error("Git branch or revision is invalid.");
  }

  return value;
}

function parseArtifactPaths(value: string) {
  const paths = value
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean)
    .map(normalizeArtifactPath);
  if (paths.length === 0) {
    throw new Error("At least one artifact file path is required.");
  }

  if (new Set(paths).size !== paths.length) {
    throw new Error("Artifact file paths must be unique.");
  }

  return paths;
}

function normalizeOptionalPath(value: string) {
  return value.trim() ? normalizeArtifactPath(value) : "";
}

function normalizeArtifactPath(value: string) {
  const normalized = value.trim().replaceAll("\\", "/");
  if (
    !normalized
    || normalized.length > 500
    || normalized.startsWith("/")
    || normalized.endsWith("/")
    || /^[A-Za-z]:/.test(normalized)
    || normalized.split("/").some((part) => !part.trim() || part === "." || part === "..")
  ) {
    throw new Error("Artifact path is invalid.");
  }

  return normalized;
}

function joinPath(prefix: string, path: string) {
  return [prefix, path].filter(Boolean).join("/");
}

async function fetchGitHubFile(repository: GitRepository, path: string, ref: string) {
  const parsed = parseGitHubRepository(repository.repositoryUrl);
  if (!parsed) {
    throw new Error("Browser import currently supports public GitHub repositories only.");
  }

  const apiPath = path.split("/").map(encodeURIComponent).join("/");
  const url = `https://api.github.com/repos/${parsed.owner}/${parsed.repo}/contents/${apiPath}?ref=${encodeURIComponent(ref)}`;
  const response = await fetch(url, { headers: { Accept: "application/vnd.github+json" } });
  if (!response.ok) {
    throw new Error(`Could not fetch ${path} from GitHub.`);
  }

  const payload = await response.json() as { type?: string; content?: string; encoding?: string };
  if (payload.type !== "file" || !payload.content || payload.encoding !== "base64") {
    throw new Error(`${path} is not a fetchable text file.`);
  }

  return decodeBase64(payload.content);
}

function parseGitHubRepository(repositoryUrl: string): { owner: string; repo: string } | null {
  const trimmed = repositoryUrl.trim().replace(/\.git$/, "");
  const httpsMatch = /^https:\/\/github\.com\/([^/]+)\/([^/]+)$/i.exec(trimmed);
  if (httpsMatch) {
    return { owner: httpsMatch[1], repo: httpsMatch[2] };
  }

  const scpMatch = /^git@github\.com:([^/]+)\/([^/]+)$/i.exec(trimmed);
  if (scpMatch) {
    return { owner: scpMatch[1], repo: scpMatch[2] };
  }

  const sshMatch = /^ssh:\/\/git@github\.com\/([^/]+)\/([^/]+)$/i.exec(trimmed);
  if (sshMatch) {
    return { owner: sshMatch[1], repo: sshMatch[2] };
  }

  return null;
}

function decodeBase64(content: string) {
  const binary = atob(content.replaceAll(/\s/g, ""));
  const bytes = Uint8Array.from(binary, (character) => character.charCodeAt(0));
  return new TextDecoder().decode(bytes);
}
