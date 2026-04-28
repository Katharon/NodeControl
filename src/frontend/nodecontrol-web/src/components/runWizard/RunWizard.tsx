"use client";

import AddIcon from "@mui/icons-material/Add";
import ArrowForwardIcon from "@mui/icons-material/ArrowForward";
import AssignmentTurnedInIcon from "@mui/icons-material/AssignmentTurnedIn";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import InventoryIcon from "@mui/icons-material/Inventory";
import NetworkCheckIcon from "@mui/icons-material/NetworkCheck";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import PlayArrowIcon from "@mui/icons-material/PlayArrow";
import RefreshIcon from "@mui/icons-material/Refresh";
import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  FormControlLabel,
  Paper,
  Radio,
  RadioGroup,
  Stack,
  Step,
  StepLabel,
  Stepper,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { ConnectionCheckButton } from "@/components/hostHealth/ConnectionCheckButton";
import { HostConnectionStatusChip } from "@/components/hostHealth/HostConnectionStatusChip";
import { JobRunStatusChip } from "@/components/jobRuns/JobRunStatusChip";
import { ApiError } from "@/lib/api/apiClient";
import { getControlNodes, type ControlNode } from "@/lib/api/controlNodes";
import { getCustomer, getMyCustomers, type Customer } from "@/lib/api/customers";
import { getHostHealth, type HostHealthTarget } from "@/lib/api/hostConnectionChecks";
import {
  getInventoryGroups,
  getInventoryPreview,
  type InventoryGroup,
  type InventoryPreview,
} from "@/lib/api/inventoryGroups";
import { getJobRun, type JobRun } from "@/lib/api/jobRuns";
import { createJob, getJobs, runJob, type Job, type JobInput } from "@/lib/api/jobs";
import { getManagedNodes, type ManagedNode } from "@/lib/api/managedNodes";
import { getPlaybooks, type Playbook } from "@/lib/api/playbooks";
import { getVariableSets, type VariableSet } from "@/lib/api/variableSets";
import { hasPermission } from "@/lib/auth/permissions";

type RunWizardProps = {
  initialCustomerId?: string;
};

type ActionMode = "select" | "create";

type Choice = {
  id: string;
  title: string;
  subtitle?: string;
  meta?: string;
  warning?: string | null;
};

const steps = [
  "Kunde",
  "Hosts",
  "Inventar",
  "Playbook",
  "Variablen",
  "Action",
  "Run starten",
  "Ergebnis",
] as const;

const demoPlaybookSnippet = `---
- hosts: all
  gather_facts: false
  tasks:
    - name: Ping host
      ansible.builtin.ping:`;

const slugPattern = /^[a-z0-9][a-z0-9-]{1,99}$/;

const actionCreateSchema = z.object({
  name: z.string().trim().min(1, "Name ist erforderlich.").max(200),
  slug: z.string().trim().regex(slugPattern, "Kleinbuchstaben, Zahlen und Bindestriche verwenden."),
  description: z.string().trim().max(1000).optional(),
  defaultTimeoutSeconds: z.number().int().min(30).max(86400),
});

type ActionCreateValues = z.infer<typeof actionCreateSchema>;

export function RunWizard({ initialCustomerId }: RunWizardProps) {
  const queryClient = useQueryClient();
  const [activeStep, setActiveStep] = useState(0);
  const [selectedCustomerId, setSelectedCustomerId] = useState<string | null>(initialCustomerId ?? null);
  const [selectedControlNodeId, setSelectedControlNodeId] = useState<string | null>(null);
  const [selectedInventoryGroupId, setSelectedInventoryGroupId] = useState<string | null>(null);
  const [selectedPlaybookId, setSelectedPlaybookId] = useState<string | null>(null);
  const [selectedVariableSetId, setSelectedVariableSetId] = useState<string>("");
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null);
  const [actionMode, setActionMode] = useState<ActionMode>("select");
  const [validationMessage, setValidationMessage] = useState<string | null>(null);
  const [inventoryPreview, setInventoryPreview] = useState<InventoryPreview | null>(null);
  const [copySuccess, setCopySuccess] = useState(false);
  const [startedRun, setStartedRun] = useState<JobRun | null>(null);

  const customersQuery = useQuery({ queryKey: ["my-customers"], queryFn: getMyCustomers });
  const customerQuery = useQuery({
    queryKey: ["customer", selectedCustomerId],
    queryFn: () => getCustomer(selectedCustomerId ?? ""),
    enabled: Boolean(selectedCustomerId),
  });

  const customer = customerQuery.data;
  const canViewNodes = hasPermission(customer?.permissions, "ViewNodes");
  const canManageNodes = hasPermission(customer?.permissions, "ManageNodes");
  const canViewPlaybooks = hasPermission(customer?.permissions, "ViewPlaybooks");
  const canManagePlaybooks = hasPermission(customer?.permissions, "ManagePlaybooks");
  const canRunJobs = hasPermission(customer?.permissions, "RunJobs");
  const canViewJobRuns = hasPermission(customer?.permissions, "ViewJobRuns");
  const canViewAuditLogs = hasPermission(customer?.permissions, "ViewAuditLogs");

  const controlNodesQuery = useQuery({
    queryKey: ["control-nodes", selectedCustomerId],
    queryFn: () => getControlNodes(selectedCustomerId ?? ""),
    enabled: Boolean(selectedCustomerId && canViewNodes),
  });
  const managedNodesQuery = useQuery({
    queryKey: ["managed-nodes", selectedCustomerId],
    queryFn: () => getManagedNodes(selectedCustomerId ?? ""),
    enabled: Boolean(selectedCustomerId && canViewNodes),
  });
  const hostHealthQuery = useQuery({
    queryKey: ["host-health", selectedCustomerId],
    queryFn: () => getHostHealth(selectedCustomerId ?? ""),
    enabled: Boolean(selectedCustomerId && canViewNodes),
  });
  const inventoryGroupsQuery = useQuery({
    queryKey: ["inventory-groups", selectedCustomerId],
    queryFn: () => getInventoryGroups(selectedCustomerId ?? ""),
    enabled: Boolean(selectedCustomerId && canViewNodes),
  });
  const playbooksQuery = useQuery({
    queryKey: ["playbooks", selectedCustomerId],
    queryFn: () => getPlaybooks(selectedCustomerId ?? ""),
    enabled: Boolean(selectedCustomerId && canViewPlaybooks),
  });
  const variableSetsQuery = useQuery({
    queryKey: ["variable-sets", selectedCustomerId],
    queryFn: () => getVariableSets(selectedCustomerId ?? ""),
    enabled: Boolean(selectedCustomerId && canViewPlaybooks),
  });
  const jobsQuery = useQuery({
    queryKey: ["jobs", selectedCustomerId],
    queryFn: () => getJobs(selectedCustomerId ?? ""),
    enabled: Boolean(selectedCustomerId && canViewPlaybooks),
  });
  const currentRunQuery = useQuery({
    queryKey: ["job-run", selectedCustomerId, startedRun?.id],
    queryFn: () => getJobRun(selectedCustomerId ?? "", startedRun?.id ?? ""),
    enabled: Boolean(selectedCustomerId && startedRun?.id && canViewJobRuns),
  });

  const inventoryPreviewMutation = useMutation({
    mutationFn: (inventoryGroupId: string) => getInventoryPreview(selectedCustomerId ?? "", inventoryGroupId),
    onSuccess: (preview) => {
      setInventoryPreview(preview);
    },
  });
  const createJobMutation = useMutation({
    mutationFn: (input: JobInput) => createJob(selectedCustomerId ?? "", input),
    onSuccess: async (job) => {
      setSelectedJobId(job.id);
      setActionMode("select");
      await queryClient.invalidateQueries({ queryKey: ["jobs", selectedCustomerId] });
      setValidationMessage(null);
    },
  });
  const runJobMutation = useMutation({
    mutationFn: (jobId: string) => runJob(selectedCustomerId ?? "", jobId),
    onSuccess: async (run) => {
      setStartedRun(run);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["job-runs", selectedCustomerId] }),
        queryClient.invalidateQueries({ queryKey: ["job-run", selectedCustomerId, run.id] }),
      ]);
      setActiveStep(7);
      setValidationMessage(null);
    },
  });

  const selectedCustomer = customersQuery.data?.find((item) => item.id === selectedCustomerId) ?? customer ?? null;
  const activeControlNodes = activeOnly(controlNodesQuery.data);
  const activeManagedNodes = activeOnly(managedNodesQuery.data);
  const activeInventoryGroups = activeOnly(inventoryGroupsQuery.data);
  const activePlaybooks = activeOnly(playbooksQuery.data);
  const activeVariableSets = activeOnly(variableSetsQuery.data);
  const activeJobs = activeOnly(jobsQuery.data);
  const selectedControlNode = activeControlNodes.find((item) => item.id === selectedControlNodeId) ?? null;
  const selectedInventoryGroup = activeInventoryGroups.find((item) => item.id === selectedInventoryGroupId) ?? null;
  const selectedPlaybook = activePlaybooks.find((item) => item.id === selectedPlaybookId) ?? null;
  const selectedVariableSet = activeVariableSets.find((item) => item.id === selectedVariableSetId) ?? null;
  const selectedJob =
    activeJobs.find((item) => item.id === selectedJobId) ??
    (createJobMutation.data?.id === selectedJobId ? createJobMutation.data : null);
  const visibleRun = currentRunQuery.data ?? startedRun;

  const healthByTargetId = useMemo(() => {
    const map = new Map<string, HostHealthTarget>();
    for (const target of hostHealthQuery.data?.targets ?? []) {
      map.set(target.targetId, target);
    }

    return map;
  }, [hostHealthQuery.data?.targets]);

  function changeCustomer(customerId: string) {
    setSelectedCustomerId(customerId);
    setSelectedControlNodeId(null);
    setSelectedInventoryGroupId(null);
    setSelectedPlaybookId(null);
    setSelectedVariableSetId("");
    setSelectedJobId(null);
    setActionMode("select");
    setInventoryPreview(null);
    setStartedRun(null);
    setValidationMessage(null);
  }

  function goNext() {
    const message = getStepValidation();
    if (message) {
      setValidationMessage(message);
      return;
    }

    setValidationMessage(null);
    setActiveStep((current) => Math.min(current + 1, steps.length - 1));
  }

  function goBack() {
    setValidationMessage(null);
    setActiveStep((current) => Math.max(current - 1, 0));
  }

  function getStepValidation() {
    if (activeStep === 0) {
      if (!selectedCustomerId) {
        return "Wähle zuerst einen Kunden aus.";
      }

      if (customerQuery.isError) {
        return forbiddenMessage(customerQuery.error, "Dieser Kunde konnte nicht geladen werden.");
      }
    }

    if (activeStep === 1) {
      if (!canViewNodes) {
        return "Du hast keine Berechtigung, Hosts für diesen Kunden anzusehen.";
      }

      if (controlNodesQuery.isPending || managedNodesQuery.isPending) {
        return "Hosts werden noch geladen.";
      }

      if (controlNodesQuery.isError || managedNodesQuery.isError) {
        return "Hosts konnten nicht geladen werden.";
      }

      if (activeControlNodes.length === 0) {
        return "Lege mindestens einen Control Host an.";
      }

      if (!selectedControlNode) {
        return "Wähle einen Control Host für die Action aus.";
      }

      if (activeManagedNodes.length === 0) {
        return "Lege mindestens einen Host an.";
      }
    }

    if (activeStep === 2) {
      if (!canViewNodes) {
        return "Du hast keine Berechtigung, Inventare für diesen Kunden anzusehen.";
      }

      if (inventoryGroupsQuery.isPending) {
        return "Inventare werden noch geladen.";
      }

      if (inventoryGroupsQuery.isError) {
        return "Inventare konnten nicht geladen werden.";
      }

      if (activeInventoryGroups.length === 0) {
        return "Lege mindestens ein Inventar an.";
      }

      if (!selectedInventoryGroup) {
        return "Wähle ein Inventar aus.";
      }

      if (selectedInventoryGroup.managedNodeIds.length === 0) {
        return "Dieses Inventar enthält noch keine Hosts.";
      }
    }

    if (activeStep === 3) {
      if (!canViewPlaybooks) {
        return "Du hast keine Berechtigung, Playbooks für diesen Kunden anzusehen.";
      }

      if (playbooksQuery.isPending) {
        return "Playbooks werden noch geladen.";
      }

      if (playbooksQuery.isError) {
        return "Playbooks konnten nicht geladen werden.";
      }

      if (activePlaybooks.length === 0) {
        return "Lege mindestens ein Playbook an.";
      }

      if (!selectedPlaybook) {
        return "Wähle ein Playbook aus.";
      }
    }

    if (activeStep === 5) {
      if (!canViewPlaybooks) {
        return "Du hast keine Berechtigung, Actions für diesen Kunden anzusehen.";
      }

      if (actionMode === "select" && !selectedJob) {
        return "Wähle eine Action aus oder lege eine neue Action an.";
      }
    }

    if (activeStep === 6) {
      if (!canRunJobs) {
        return "Du hast keine Berechtigung, Runs zu starten.";
      }

      if (!selectedJob) {
        return "Wähle oder erstelle zuerst eine Action.";
      }
    }

    return null;
  }

  const canContinue = activeStep < 6 || Boolean(selectedJob);

  return (
    <Stack sx={{ gap: 3 }}>
      <Stack
        direction={{ xs: "column", md: "row" }}
        sx={{ alignItems: { md: "flex-start" }, justifyContent: "space-between", gap: 2 }}
      >
        <Stack sx={{ gap: 0.75 }}>
          <Typography component="h1" variant="h4">
            Ausführungsassistent
          </Typography>
          <Typography color="text.secondary">
            Geführter Demo-Flow von Kunden- und Host-Readiness bis zum gestarteten Run.
          </Typography>
        </Stack>
        {selectedCustomer ? (
          <Chip color="primary" label={`Kunde: ${selectedCustomer.name}`} variant="outlined" />
        ) : null}
      </Stack>

      <Paper sx={{ p: { xs: 2, md: 3 } }}>
        <Stepper activeStep={activeStep} alternativeLabel sx={{ display: { xs: "none", md: "flex" } }}>
          {steps.map((label) => (
            <Step key={label}>
              <StepLabel>{label}</StepLabel>
            </Step>
          ))}
        </Stepper>
        <Typography color="text.secondary" sx={{ display: { md: "none" }, fontWeight: 700 }} variant="body2">
          Schritt {activeStep + 1} von {steps.length}: {steps[activeStep]}
        </Typography>
      </Paper>

      {validationMessage ? <Alert severity="warning">{validationMessage}</Alert> : null}
      {createJobMutation.isError ? (
        <Alert severity="error">
          Action konnte nicht angelegt werden. Prüfe die Eingaben und deine Berechtigung.
        </Alert>
      ) : null}
      {runJobMutation.isError ? (
        <Alert severity="error">
          Run konnte nicht gestartet werden. Die API hat nur den bestehenden Run-Endpunkt aufgerufen.
        </Alert>
      ) : null}

      <Paper sx={{ p: { xs: 2, md: 3 } }}>
        {activeStep === 0 ? (
          <CustomerStep
            customers={customersQuery.data ?? []}
            error={customersQuery.error}
            isError={customersQuery.isError}
            isLoading={customersQuery.isPending}
            onSelect={changeCustomer}
            selectedCustomerId={selectedCustomerId}
          />
        ) : null}
        {activeStep === 1 ? (
          <HostsStep
            canManageNodes={canManageNodes}
            canViewNodes={canViewNodes}
            controlNodes={activeControlNodes}
            controlNodesError={controlNodesQuery.error}
            customerId={selectedCustomerId}
            healthByTargetId={healthByTargetId}
            hostHealthError={hostHealthQuery.error}
            isControlNodesError={controlNodesQuery.isError}
            isHostHealthError={hostHealthQuery.isError}
            isLoading={controlNodesQuery.isPending || managedNodesQuery.isPending}
            isManagedNodesError={managedNodesQuery.isError}
            managedNodes={activeManagedNodes}
            managedNodesError={managedNodesQuery.error}
            onSelectControlNode={setSelectedControlNodeId}
            onRefreshHealth={() => void hostHealthQuery.refetch()}
            selectedControlNodeId={selectedControlNode?.id ?? null}
          />
        ) : null}
        {activeStep === 2 ? (
          <InventoryStep
            canManageNodes={canManageNodes}
            canViewNodes={canViewNodes}
            customerId={selectedCustomerId}
            inventoryGroups={activeInventoryGroups}
            inventoryPreview={inventoryPreview}
            isError={inventoryGroupsQuery.isError}
            isLoading={inventoryGroupsQuery.isPending}
            onPreview={(inventoryGroupId) => {
              setInventoryPreview(null);
              inventoryPreviewMutation.mutate(inventoryGroupId);
            }}
            onSelect={(inventoryGroupId) => {
              setSelectedInventoryGroupId(inventoryGroupId);
              setInventoryPreview(null);
            }}
            previewError={inventoryPreviewMutation.isError}
            previewLoading={inventoryPreviewMutation.isPending}
            selectedInventoryGroupId={selectedInventoryGroup?.id ?? null}
          />
        ) : null}
        {activeStep === 3 ? (
          <PlaybookStep
            canViewPlaybooks={canViewPlaybooks}
            copySuccess={copySuccess}
            isError={playbooksQuery.isError}
            isLoading={playbooksQuery.isPending}
            onCopySnippet={async () => {
              await navigator.clipboard.writeText(demoPlaybookSnippet);
              setCopySuccess(true);
              window.setTimeout(() => setCopySuccess(false), 1800);
            }}
            onSelect={setSelectedPlaybookId}
            playbooks={activePlaybooks}
            selectedPlaybookId={selectedPlaybook?.id ?? null}
            selectedCustomerId={selectedCustomerId}
          />
        ) : null}
        {activeStep === 4 ? (
          <VariablesStep
            canViewPlaybooks={canViewPlaybooks}
            isError={variableSetsQuery.isError}
            isLoading={variableSetsQuery.isPending}
            onSelect={setSelectedVariableSetId}
            selectedCustomerId={selectedCustomerId}
            selectedVariableSetId={selectedVariableSet?.id ?? ""}
            variableSets={activeVariableSets}
          />
        ) : null}
        {activeStep === 5 ? (
          <ActionStep
            actionMode={actionMode}
            canManagePlaybooks={canManagePlaybooks}
            canViewPlaybooks={canViewPlaybooks}
            createJobPending={createJobMutation.isPending}
            customerId={selectedCustomerId}
            isError={jobsQuery.isError}
            isLoading={jobsQuery.isPending}
            jobs={activeJobs}
            onCreateJob={async (values) => {
              if (!selectedControlNode || !selectedInventoryGroup || !selectedPlaybook) {
                setValidationMessage("Control Host, Inventar und Playbook müssen ausgewählt sein.");
                return;
              }

              await createJobMutation.mutateAsync({
                ...values,
                controlNodeId: selectedControlNode.id,
                inventoryGroupId: selectedInventoryGroup.id,
                playbookId: selectedPlaybook.id,
                variableSetId: selectedVariableSet?.id ?? null,
              });
            }}
            onModeChange={setActionMode}
            onSelectJob={setSelectedJobId}
            selectedControlNode={selectedControlNode}
            selectedInventoryGroup={selectedInventoryGroup}
            selectedJobId={selectedJob?.id ?? null}
            selectedPlaybook={selectedPlaybook}
            selectedVariableSet={selectedVariableSet}
          />
        ) : null}
        {activeStep === 6 ? (
          <RunStep
            canRunJobs={canRunJobs}
            customerId={selectedCustomerId}
            job={selectedJob}
            onStart={() => selectedJob ? runJobMutation.mutate(selectedJob.id) : setValidationMessage("Wähle zuerst eine Action aus.")}
            runPending={runJobMutation.isPending}
          />
        ) : null}
        {activeStep === 7 ? (
          <ResultStep
            canViewAuditLogs={canViewAuditLogs}
            canViewJobRuns={canViewJobRuns}
            controlNode={selectedControlNode}
            customer={selectedCustomer}
            inventoryGroup={selectedInventoryGroup}
            job={selectedJob}
            onRefreshRun={() => void currentRunQuery.refetch()}
            onStartAnother={() => {
              setStartedRun(null);
              setActiveStep(5);
            }}
            playbook={selectedPlaybook}
            refreshPending={currentRunQuery.isFetching}
            run={visibleRun}
            variableSet={selectedVariableSet}
          />
        ) : null}
      </Paper>

      <Stack direction={{ xs: "column", sm: "row" }} sx={{ justifyContent: "space-between", gap: 1.5 }}>
        <Button disabled={activeStep === 0 || runJobMutation.isPending} onClick={goBack} variant="outlined">
          Zurück
        </Button>
        {activeStep < 6 ? (
          <Button
            disabled={!canContinue}
            endIcon={<ArrowForwardIcon />}
            onClick={goNext}
            variant="contained"
          >
            Weiter
          </Button>
        ) : null}
        {activeStep === 6 ? (
          <Button
            disabled={runJobMutation.isPending || !selectedJob}
            onClick={() => selectedJob ? runJobMutation.mutate(selectedJob.id) : setValidationMessage("Wähle zuerst eine Action aus.")}
            startIcon={<PlayArrowIcon />}
            variant="contained"
          >
            Run starten
          </Button>
        ) : null}
        {activeStep === 7 ? (
          <Button onClick={() => setActiveStep(0)} variant="outlined">
            Assistent neu starten
          </Button>
        ) : null}
      </Stack>
    </Stack>
  );
}

function CustomerStep({
  customers,
  error,
  isError,
  isLoading,
  onSelect,
  selectedCustomerId,
}: {
  customers: Customer[];
  error: unknown;
  isError: boolean;
  isLoading: boolean;
  onSelect: (customerId: string) => void;
  selectedCustomerId: string | null;
}) {
  return (
    <Stack sx={{ gap: 2 }}>
      <StepHeading
        description="Der Assistent arbeitet bewusst nur mit lokalem Zustand. Jeder API-Aufruf bleibt kunden-scoped."
        title="Kunde auswählen"
      />
      {isLoading ? <LoadingState label="Kunden werden geladen" /> : null}
      {isError ? <Alert severity="error">{forbiddenMessage(error, "Kunden konnten nicht geladen werden.")}</Alert> : null}
      {!isLoading && !isError && customers.length === 0 ? (
        <Alert
          action={
            <Button component={Link} href="/customers" size="small" startIcon={<AddIcon />}>
              Kunden öffnen
            </Button>
          }
          severity="info"
        >
          Für deinen Account sind noch keine Kunden verfügbar.
        </Alert>
      ) : null}
      <ChoiceList
        choices={customers.map((customer) => ({
          id: customer.id,
          title: customer.name,
          subtitle: customer.slug,
          meta: customer.status,
        }))}
        emptyLabel="Keine Kunden gefunden."
        onSelect={onSelect}
        selectedId={selectedCustomerId}
      />
    </Stack>
  );
}

function HostsStep({
  canManageNodes,
  canViewNodes,
  controlNodes,
  controlNodesError,
  customerId,
  healthByTargetId,
  hostHealthError,
  isControlNodesError,
  isHostHealthError,
  isLoading,
  isManagedNodesError,
  managedNodes,
  managedNodesError,
  onRefreshHealth,
  onSelectControlNode,
  selectedControlNodeId,
}: {
  canManageNodes: boolean;
  canViewNodes: boolean;
  controlNodes: ControlNode[];
  controlNodesError: unknown;
  customerId: string | null;
  healthByTargetId: Map<string, HostHealthTarget>;
  hostHealthError: unknown;
  isControlNodesError: boolean;
  isHostHealthError: boolean;
  isLoading: boolean;
  isManagedNodesError: boolean;
  managedNodes: ManagedNode[];
  managedNodesError: unknown;
  onRefreshHealth: () => void;
  onSelectControlNode: (controlNodeId: string) => void;
  selectedControlNodeId: string | null;
}) {
  if (!canViewNodes) {
    return <Alert severity="warning">Du hast keine Berechtigung, Hosts für diesen Kunden anzusehen.</Alert>;
  }

  const hostHref = customerId ? `/customers/${customerId}/hosts` : "/hosts";

  return (
    <Stack sx={{ gap: 2 }}>
      <StepHeading
        description="Wähle den Control Host für die spätere Action. Verbindungstests sind hilfreich, aber keine harte Voraussetzung."
        title="Hosts prüfen"
      />
      {isLoading ? <LoadingState label="Hosts werden geladen" /> : null}
      {isControlNodesError ? <Alert severity="error">{forbiddenMessage(controlNodesError, "Control Hosts konnten nicht geladen werden.")}</Alert> : null}
      {isManagedNodesError ? <Alert severity="error">{forbiddenMessage(managedNodesError, "Hosts konnten nicht geladen werden.")}</Alert> : null}
      {isHostHealthError ? <Alert severity="warning">{forbiddenMessage(hostHealthError, "Hostzustand konnte nicht geladen werden.")}</Alert> : null}

      <ReadinessGrid
        items={[
          { label: "Control Hosts", ok: controlNodes.length > 0, value: controlNodes.length.toString() },
          { label: "Hosts", ok: managedNodes.length > 0, value: managedNodes.length.toString() },
          { label: "Checks", ok: [...healthByTargetId.values()].some((target) => target.latestCheck?.status === "Succeeded"), value: "optional" },
        ]}
      />

      {controlNodes.length === 0 || managedNodes.length === 0 ? (
        <Alert
          action={
            <Button component={Link} href={hostHref} size="small" startIcon={<AddIcon />}>
              Hosts öffnen
            </Button>
          }
          severity="info"
        >
          Lege die fehlenden Host-Ressourcen an, bevor du die Action erstellst.
        </Alert>
      ) : null}

      <Stack direction={{ xs: "column", sm: "row" }} sx={{ justifyContent: "space-between", gap: 1.5 }}>
        <Typography component="h2" variant="h6">
          Control Host
        </Typography>
        <Button disabled={isHostHealthError} onClick={onRefreshHealth} startIcon={<RefreshIcon />} variant="outlined">
          Hostzustand aktualisieren
        </Button>
      </Stack>
      <ChoiceList
        choices={controlNodes.map((node) => hostChoice(node, healthByTargetId.get(node.id)))}
        emptyLabel="Noch keine Control Hosts definiert."
        onSelect={onSelectControlNode}
        selectedId={selectedControlNodeId}
      />

      <Typography component="h2" variant="h6">
        Hosts
      </Typography>
      {managedNodes.length === 0 ? (
        <Alert severity="info">Noch keine Hosts definiert.</Alert>
      ) : (
        <Paper variant="outlined">
          <Stack divider={<Divider />}>
            {managedNodes.map((node) => {
              const health = healthByTargetId.get(node.id);
              return (
                <Stack
                  direction={{ xs: "column", sm: "row" }}
                  key={node.id}
                  sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 2, p: 2 }}
                >
                  <Stack sx={{ gap: 0.5 }}>
                    <Typography sx={{ fontWeight: 700 }}>{node.name}</Typography>
                    <Typography color="text.secondary" variant="body2">
                      {node.hostname}:{node.sshPort}
                    </Typography>
                    <HostConnectionStatusChip status={health?.latestCheck?.status} />
                  </Stack>
                  {customerId && canManageNodes ? (
                    <ConnectionCheckButton
                      customerId={customerId}
                      targetId={node.id}
                      targetType="ManagedNode"
                    />
                  ) : null}
                </Stack>
              );
            })}
          </Stack>
        </Paper>
      )}

      {customerId && canManageNodes && selectedControlNodeId ? (
        <Stack direction="row" sx={{ gap: 1 }}>
          <ConnectionCheckButton
            customerId={customerId}
            targetId={selectedControlNodeId}
            targetType="ControlNode"
          />
          <Button component={Link} href={`/customers/${customerId}/host-health`} startIcon={<NetworkCheckIcon />} variant="outlined">
            Hostzustand öffnen
          </Button>
        </Stack>
      ) : null}
    </Stack>
  );
}

function InventoryStep({
  canManageNodes,
  canViewNodes,
  customerId,
  inventoryGroups,
  inventoryPreview,
  isError,
  isLoading,
  onPreview,
  onSelect,
  previewError,
  previewLoading,
  selectedInventoryGroupId,
}: {
  canManageNodes: boolean;
  canViewNodes: boolean;
  customerId: string | null;
  inventoryGroups: InventoryGroup[];
  inventoryPreview: InventoryPreview | null;
  isError: boolean;
  isLoading: boolean;
  onPreview: (inventoryGroupId: string) => void;
  onSelect: (inventoryGroupId: string) => void;
  previewError: boolean;
  previewLoading: boolean;
  selectedInventoryGroupId: string | null;
}) {
  if (!canViewNodes) {
    return <Alert severity="warning">Du hast keine Berechtigung, Inventare für diesen Kunden anzusehen.</Alert>;
  }

  const href = customerId ? `/customers/${customerId}/inventories` : "/inventories";

  return (
    <Stack sx={{ gap: 2 }}>
      <StepHeading
        description="Das Inventar bestimmt die Zielhosts. Für einen sinnvollen Demo-Run sollte es mindestens einen Host enthalten."
        title="Inventar auswählen"
      />
      {isLoading ? <LoadingState label="Inventare werden geladen" /> : null}
      {isError ? <Alert severity="error">Inventare konnten nicht geladen werden.</Alert> : null}
      {inventoryGroups.length === 0 ? (
        <Alert
          action={
            <Button component={Link} href={href} size="small" startIcon={<AddIcon />}>
              Inventare öffnen
            </Button>
          }
          severity="info"
        >
          Noch keine Inventare definiert.
        </Alert>
      ) : null}
      <ChoiceList
        choices={inventoryGroups.map((group) => ({
          id: group.id,
          title: group.name,
          subtitle: group.description ?? undefined,
          meta: `${group.managedNodeIds.length} Hosts`,
          warning: group.managedNodeIds.length === 0 ? "Dieses Inventar enthält noch keine Hosts." : null,
        }))}
        emptyLabel="Keine Inventare gefunden."
        onSelect={(inventoryGroupId) => {
          onSelect(inventoryGroupId);
        }}
        selectedId={selectedInventoryGroupId}
      />
      {selectedInventoryGroupId ? (
        <Stack sx={{ gap: 1.5 }}>
          <Stack direction={{ xs: "column", sm: "row" }} sx={{ gap: 1 }}>
            <Button
              disabled={previewLoading}
              onClick={() => onPreview(selectedInventoryGroupId)}
              startIcon={previewLoading ? <CircularProgress color="inherit" size={16} /> : <InventoryIcon />}
              variant="outlined"
            >
              Preview
            </Button>
            {canManageNodes ? (
              <Button component={Link} href={href} startIcon={<OpenInNewIcon />} variant="outlined">
                Inventar bearbeiten
              </Button>
            ) : null}
          </Stack>
          {previewError ? <Alert severity="error">Inventarvorschau konnte nicht geladen werden.</Alert> : null}
          {inventoryPreview ? (
            <Box
              component="pre"
              sx={{
                bgcolor: "background.default",
                border: 1,
                borderColor: "divider",
                borderRadius: 1,
                fontFamily: "monospace",
                fontSize: 14,
                m: 0,
                overflow: "auto",
                p: 2,
                whiteSpace: "pre",
              }}
            >
              {inventoryPreview.content.trim() || "Inventarvorschau enthält keine Hosts."}
            </Box>
          ) : null}
        </Stack>
      ) : null}
    </Stack>
  );
}

function PlaybookStep({
  canViewPlaybooks,
  copySuccess,
  isError,
  isLoading,
  onCopySnippet,
  onSelect,
  playbooks,
  selectedCustomerId,
  selectedPlaybookId,
}: {
  canViewPlaybooks: boolean;
  copySuccess: boolean;
  isError: boolean;
  isLoading: boolean;
  onCopySnippet: () => Promise<void>;
  onSelect: (playbookId: string) => void;
  playbooks: Playbook[];
  selectedCustomerId: string | null;
  selectedPlaybookId: string | null;
}) {
  if (!canViewPlaybooks) {
    return <Alert severity="warning">Du hast keine Berechtigung, Playbooks für diesen Kunden anzusehen.</Alert>;
  }

  const href = selectedCustomerId ? `/customers/${selectedCustomerId}/playbooks` : "/playbooks";

  return (
    <Stack sx={{ gap: 2 }}>
      <StepHeading
        description="Wähle ein vorhandenes Playbook. Der Beispiel-Snippet ist nur Kopierhilfe und erzeugt keine versteckten Demo-Daten."
        title="Playbook auswählen"
      />
      {isLoading ? <LoadingState label="Playbooks werden geladen" /> : null}
      {isError ? <Alert severity="error">Playbooks konnten nicht geladen werden.</Alert> : null}
      {playbooks.length === 0 ? (
        <Alert
          action={
            <Button component={Link} href={href} size="small" startIcon={<AddIcon />}>
              Playbooks öffnen
            </Button>
          }
          severity="info"
        >
          Noch keine Playbooks definiert.
        </Alert>
      ) : null}
      <ChoiceList
        choices={playbooks.map((playbook) => ({
          id: playbook.id,
          title: playbook.name,
          subtitle: playbook.slug,
          meta: playbook.sourceType,
        }))}
        emptyLabel="Keine Playbooks gefunden."
        onSelect={onSelect}
        selectedId={selectedPlaybookId}
      />
      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack sx={{ gap: 1.5 }}>
          <Stack direction={{ xs: "column", sm: "row" }} sx={{ justifyContent: "space-between", gap: 1 }}>
            <Typography component="h2" variant="h6">
              Beispiel-Playbook
            </Typography>
            <Tooltip title="Snippet kopieren">
              <Button onClick={onCopySnippet} startIcon={<ContentCopyIcon />} variant="outlined">
                {copySuccess ? "Kopiert" : "Kopieren"}
              </Button>
            </Tooltip>
          </Stack>
          <Box
            component="pre"
            sx={{
              bgcolor: "background.default",
              borderRadius: 1,
              fontFamily: "monospace",
              fontSize: 14,
              m: 0,
              overflow: "auto",
              p: 2,
              whiteSpace: "pre",
            }}
          >
            {demoPlaybookSnippet}
          </Box>
        </Stack>
      </Paper>
    </Stack>
  );
}

function VariablesStep({
  canViewPlaybooks,
  isError,
  isLoading,
  onSelect,
  selectedCustomerId,
  selectedVariableSetId,
  variableSets,
}: {
  canViewPlaybooks: boolean;
  isError: boolean;
  isLoading: boolean;
  onSelect: (variableSetId: string) => void;
  selectedCustomerId: string | null;
  selectedVariableSetId: string;
  variableSets: VariableSet[];
}) {
  if (!canViewPlaybooks) {
    return <Alert severity="warning">Du hast keine Berechtigung, Variablen für diesen Kunden anzusehen.</Alert>;
  }

  const href = selectedCustomerId ? `/customers/${selectedCustomerId}/variables` : "/variables";

  return (
    <Stack sx={{ gap: 2 }}>
      <StepHeading
        description="Variablen sind optional. Secret-Werte werden hier nicht angezeigt."
        title="Variablen auswählen"
      />
      {isLoading ? <LoadingState label="Variablen werden geladen" /> : null}
      {isError ? <Alert severity="error">Variable Sets konnten nicht geladen werden.</Alert> : null}
      <Alert severity="info">Secret-Referenzen werden als `secret://my-secret` angegeben. Secret-Werte werden niemals angezeigt.</Alert>
      <RadioGroup onChange={(event) => onSelect(event.target.value)} value={selectedVariableSetId}>
        <Paper variant="outlined">
          <Stack divider={<Divider />}>
            <FormControlLabel
              control={<Radio />}
              label={
                <Stack>
                  <Typography sx={{ fontWeight: 700 }}>Keine Variablen</Typography>
                  <Typography color="text.secondary" variant="body2">Action ohne Variable Set ausführen.</Typography>
                </Stack>
              }
              sx={{ alignItems: "flex-start", m: 0, p: 2 }}
              value=""
            />
            {variableSets.map((variableSet) => (
              <FormControlLabel
                control={<Radio />}
                key={variableSet.id}
                label={
                  <Stack>
                    <Typography sx={{ fontWeight: 700 }}>{variableSet.name}</Typography>
                    <Typography color="text.secondary" variant="body2">{variableSet.slug}</Typography>
                    {variableSet.containsSensitiveValues ? <Chip color="warning" label="Sensitive" size="small" sx={{ alignSelf: "flex-start", mt: 0.5 }} /> : null}
                  </Stack>
                }
                sx={{ alignItems: "flex-start", m: 0, p: 2 }}
                value={variableSet.id}
              />
            ))}
          </Stack>
        </Paper>
      </RadioGroup>
      <Button component={Link} href={href} startIcon={<OpenInNewIcon />} sx={{ alignSelf: "flex-start" }} variant="outlined">
        Variablen öffnen
      </Button>
    </Stack>
  );
}

function ActionStep({
  actionMode,
  canManagePlaybooks,
  canViewPlaybooks,
  createJobPending,
  customerId,
  isError,
  isLoading,
  jobs,
  onCreateJob,
  onModeChange,
  onSelectJob,
  selectedControlNode,
  selectedInventoryGroup,
  selectedJobId,
  selectedPlaybook,
  selectedVariableSet,
}: {
  actionMode: ActionMode;
  canManagePlaybooks: boolean;
  canViewPlaybooks: boolean;
  createJobPending: boolean;
  customerId: string | null;
  isError: boolean;
  isLoading: boolean;
  jobs: Job[];
  onCreateJob: (input: Omit<JobInput, "controlNodeId" | "inventoryGroupId" | "playbookId" | "variableSetId">) => Promise<void>;
  onModeChange: (mode: ActionMode) => void;
  onSelectJob: (jobId: string) => void;
  selectedControlNode: ControlNode | null;
  selectedInventoryGroup: InventoryGroup | null;
  selectedJobId: string | null;
  selectedPlaybook: Playbook | null;
  selectedVariableSet: VariableSet | null;
}) {
  if (!canViewPlaybooks) {
    return <Alert severity="warning">Du hast keine Berechtigung, Actions für diesen Kunden anzusehen.</Alert>;
  }

  const href = customerId ? `/customers/${customerId}/actions` : "/actions";

  return (
    <Stack sx={{ gap: 2 }}>
      <StepHeading
        description="Verwende eine vorhandene Action oder lege explizit eine Action aus den ausgewählten Ressourcen an."
        title="Action auswählen oder erstellen"
      />
      <ReadinessGrid
        items={[
          { label: "Control Host", ok: Boolean(selectedControlNode), value: selectedControlNode?.name ?? "fehlt" },
          { label: "Inventar", ok: Boolean(selectedInventoryGroup), value: selectedInventoryGroup?.name ?? "fehlt" },
          { label: "Playbook", ok: Boolean(selectedPlaybook), value: selectedPlaybook?.name ?? "fehlt" },
          { label: "Variablen", ok: true, value: selectedVariableSet?.name ?? "keine" },
        ]}
      />
      {isLoading ? <LoadingState label="Actions werden geladen" /> : null}
      {isError ? <Alert severity="error">Actions konnten nicht geladen werden.</Alert> : null}
      <RadioGroup
        onChange={(event) => onModeChange(event.target.value as ActionMode)}
        row
        value={actionMode}
      >
        <FormControlLabel control={<Radio />} label="Bestehende Action" value="select" />
        <FormControlLabel control={<Radio />} disabled={!canManagePlaybooks} label="Neue Action" value="create" />
      </RadioGroup>

      {actionMode === "select" ? (
        <Stack sx={{ gap: 2 }}>
          {jobs.length === 0 ? (
            <Alert
              action={
                <Button component={Link} href={href} size="small" startIcon={<AddIcon />}>
                  Actions öffnen
                </Button>
              }
              severity="info"
            >
              Noch keine Actions definiert.
            </Alert>
          ) : null}
          <ChoiceList
            choices={jobs.map((job) => ({
              id: job.id,
              title: job.name,
              subtitle: job.slug,
              meta: `${job.defaultTimeoutSeconds}s Timeout`,
              warning: actionMismatchWarning(job, selectedControlNode, selectedInventoryGroup, selectedPlaybook, selectedVariableSet),
            }))}
            emptyLabel="Keine Actions gefunden."
            onSelect={onSelectJob}
            selectedId={selectedJobId}
          />
        </Stack>
      ) : (
        <ActionCreateForm
          disabled={!canManagePlaybooks || !selectedControlNode || !selectedInventoryGroup || !selectedPlaybook}
          onSubmit={onCreateJob}
          pending={createJobPending}
          seedName={selectedPlaybook ? `${selectedPlaybook.name} Run` : "Demo Run"}
        />
      )}
    </Stack>
  );
}

function RunStep({
  canRunJobs,
  customerId,
  job,
  onStart,
  runPending,
}: {
  canRunJobs: boolean;
  customerId: string | null;
  job: Job | null;
  onStart: () => void;
  runPending: boolean;
}) {
  if (!canRunJobs) {
    return <Alert severity="warning">Du hast keine Berechtigung, Runs zu starten.</Alert>;
  }

  return (
    <Stack sx={{ gap: 2 }}>
      <StepHeading
        description="Der Button ruft den vorhandenen Action-Run-Endpunkt auf. Ausführung und Prozessstart bleiben beim Worker."
        title="Run starten"
      />
      {job ? (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Stack direction={{ xs: "column", sm: "row" }} sx={{ justifyContent: "space-between", gap: 2 }}>
            <Stack>
              <Typography sx={{ fontWeight: 700 }}>{job.name}</Typography>
              <Typography color="text.secondary" variant="body2">{job.slug}</Typography>
            </Stack>
            <Button
              disabled={runPending}
              onClick={onStart}
              startIcon={runPending ? <CircularProgress color="inherit" size={16} /> : <PlayArrowIcon />}
              variant="contained"
            >
              Run starten
            </Button>
          </Stack>
        </Paper>
      ) : (
        <Alert severity="warning">Keine Action ausgewählt.</Alert>
      )}
      {customerId ? (
        <Button component={Link} href={`/customers/${customerId}/runs`} startIcon={<OpenInNewIcon />} sx={{ alignSelf: "flex-start" }} variant="outlined">
          Runs öffnen
        </Button>
      ) : null}
    </Stack>
  );
}

function ResultStep({
  canViewAuditLogs,
  canViewJobRuns,
  controlNode,
  customer,
  inventoryGroup,
  job,
  onRefreshRun,
  onStartAnother,
  playbook,
  refreshPending,
  run,
  variableSet,
}: {
  canViewAuditLogs: boolean;
  canViewJobRuns: boolean;
  controlNode: ControlNode | null;
  customer: Customer | null;
  inventoryGroup: InventoryGroup | null;
  job: Job | null;
  onRefreshRun: () => void;
  onStartAnother: () => void;
  playbook: Playbook | null;
  refreshPending: boolean;
  run: JobRun | null;
  variableSet: VariableSet | null;
}) {
  const customerId = customer?.id;
  const runHref = customerId && run ? `/customers/${customerId}/runs/${run.id}` : null;
  const logsHref = runHref ? `${runHref}#logs` : null;

  return (
    <Stack sx={{ gap: 2 }}>
      <StepHeading
        description="Der Run ist über die bestehende Run-Ansicht nachvollziehbar. Logs werden dort aus dem vorhandenen JobRun-Log-API geladen."
        title="Ergebnis"
      />
      {run ? (
        <Alert icon={<AssignmentTurnedInIcon />} severity="success">
          Run wurde angelegt: {run.id}
        </Alert>
      ) : (
        <Alert severity="warning">Noch kein Run gestartet.</Alert>
      )}
      {run ? (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Stack direction={{ xs: "column", sm: "row" }} sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 2 }}>
            <Stack sx={{ gap: 0.75 }}>
              <Typography sx={{ fontWeight: 700 }}>Run Status</Typography>
              <JobRunStatusChip status={run.status} />
              <Typography color="text.secondary" variant="body2">
                Queued: {new Date(run.queuedAt).toLocaleString()}
              </Typography>
            </Stack>
            <Button disabled={refreshPending} onClick={onRefreshRun} startIcon={<RefreshIcon />} variant="outlined">
              Aktualisieren
            </Button>
          </Stack>
        </Paper>
      ) : null}
      <SummaryGrid
        rows={[
          ["Kunde", customer?.name],
          ["Control Host", controlNode?.name],
          ["Inventar", inventoryGroup?.name],
          ["Playbook", playbook?.name],
          ["Variablen", variableSet?.name ?? "Keine"],
          ["Action", job?.name],
          ["Run", run?.id],
        ]}
      />
      <Stack direction={{ xs: "column", sm: "row" }} sx={{ gap: 1 }}>
        {runHref && canViewJobRuns ? (
          <Button component={Link} href={runHref} startIcon={<OpenInNewIcon />} variant="contained">
            Run öffnen
          </Button>
        ) : null}
        {logsHref && canViewJobRuns ? (
          <Button component={Link} href={logsHref} startIcon={<OpenInNewIcon />} variant="outlined">
            Logs öffnen
          </Button>
        ) : null}
        {customerId && canViewAuditLogs ? (
          <Button component={Link} href={`/customers/${customerId}/audit`} startIcon={<OpenInNewIcon />} variant="outlined">
            Audit öffnen
          </Button>
        ) : null}
        <Button onClick={onStartAnother} startIcon={<PlayArrowIcon />} variant="outlined">
          Weitere Action starten
        </Button>
      </Stack>
    </Stack>
  );
}

function ActionCreateForm({
  disabled,
  onSubmit,
  pending,
  seedName,
}: {
  disabled: boolean;
  onSubmit: (input: Omit<JobInput, "controlNodeId" | "inventoryGroupId" | "playbookId" | "variableSetId">) => Promise<void>;
  pending: boolean;
  seedName: string;
}) {
  const defaultSlug = slugify(seedName);
  const {
    formState: { errors, isSubmitting },
    handleSubmit,
    register,
  } = useForm<ActionCreateValues>({
    resolver: zodResolver(actionCreateSchema),
    defaultValues: {
      name: seedName,
      slug: defaultSlug,
      description: "Aus dem Ausführungsassistenten angelegte Demo-Action.",
      defaultTimeoutSeconds: 1800,
    },
  });

  return (
    <Stack
      component="form"
      onSubmit={handleSubmit(async (values) => {
        await onSubmit({
          name: values.name,
          slug: values.slug,
          description: values.description || null,
          defaultTimeoutSeconds: values.defaultTimeoutSeconds,
        });
      })}
      sx={{ gap: 2 }}
    >
      {disabled ? (
        <Alert severity="warning">
          Für eine neue Action müssen Control Host, Inventar und Playbook ausgewählt sein. Außerdem brauchst du die passende Berechtigung.
        </Alert>
      ) : null}
      <TextField disabled={disabled || pending || isSubmitting} error={Boolean(errors.name)} helperText={errors.name?.message} label="Name" {...register("name")} />
      <TextField disabled={disabled || pending || isSubmitting} error={Boolean(errors.slug)} helperText={errors.slug?.message} label="Slug" {...register("slug")} />
      <TextField disabled={disabled || pending || isSubmitting} label="Description" minRows={2} multiline {...register("description")} />
      <TextField
        disabled={disabled || pending || isSubmitting}
        error={Boolean(errors.defaultTimeoutSeconds)}
        helperText={errors.defaultTimeoutSeconds?.message}
        label="Default timeout seconds"
        type="number"
        {...register("defaultTimeoutSeconds", { valueAsNumber: true })}
      />
      <Button
        disabled={disabled || pending || isSubmitting}
        startIcon={pending || isSubmitting ? <CircularProgress color="inherit" size={16} /> : <SaveIcon />}
        sx={{ alignSelf: "flex-start" }}
        type="submit"
        variant="contained"
      >
        Action anlegen
      </Button>
    </Stack>
  );
}

function StepHeading({ description, title }: { description: string; title: string }) {
  return (
    <Stack sx={{ gap: 0.5 }}>
      <Typography component="h2" variant="h5">{title}</Typography>
      <Typography color="text.secondary">{description}</Typography>
    </Stack>
  );
}

function LoadingState({ label }: { label: string }) {
  return (
    <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
      <CircularProgress size={20} />
      <Typography color="text.secondary">{label}</Typography>
    </Stack>
  );
}

function ChoiceList({
  choices,
  emptyLabel,
  onSelect,
  selectedId,
}: {
  choices: Choice[];
  emptyLabel: string;
  onSelect: (id: string) => void;
  selectedId: string | null;
}) {
  if (choices.length === 0) {
    return <Alert severity="info">{emptyLabel}</Alert>;
  }

  return (
    <RadioGroup onChange={(event) => onSelect(event.target.value)} value={selectedId ?? ""}>
      <Paper variant="outlined">
        <Stack divider={<Divider />}>
          {choices.map((choice) => (
            <FormControlLabel
              control={<Radio />}
              key={choice.id}
              label={
                <Stack sx={{ minWidth: 0 }}>
                  <Stack direction="row" sx={{ alignItems: "center", flexWrap: "wrap", gap: 1 }}>
                    <Typography sx={{ fontWeight: 700 }}>{choice.title}</Typography>
                    {choice.meta ? <Chip label={choice.meta} size="small" variant="outlined" /> : null}
                  </Stack>
                  {choice.subtitle ? (
                    <Typography color="text.secondary" sx={{ overflowWrap: "anywhere" }} variant="body2">
                      {choice.subtitle}
                    </Typography>
                  ) : null}
                  {choice.warning ? (
                    <Typography color="warning.main" variant="body2">
                      {choice.warning}
                    </Typography>
                  ) : null}
                </Stack>
              }
              sx={{ alignItems: "flex-start", m: 0, p: 2 }}
              value={choice.id}
            />
          ))}
        </Stack>
      </Paper>
    </RadioGroup>
  );
}

function ReadinessGrid({ items }: { items: { label: string; ok: boolean; value: string }[] }) {
  return (
    <Stack direction={{ xs: "column", sm: "row" }} sx={{ gap: 1.5 }}>
      {items.map((item) => (
        <Paper key={item.label} variant="outlined" sx={{ flex: 1, p: 2 }}>
          <Stack sx={{ gap: 0.5 }}>
            <Typography color="text.secondary" variant="body2">{item.label}</Typography>
            <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", gap: 1 }}>
              <Typography sx={{ fontWeight: 700 }}>{item.value}</Typography>
              <Chip color={item.ok ? "success" : "warning"} label={item.ok ? "bereit" : "fehlt"} size="small" />
            </Stack>
          </Stack>
        </Paper>
      ))}
    </Stack>
  );
}

function SummaryGrid({ rows }: { rows: [string, string | undefined | null][] }) {
  return (
    <Paper variant="outlined">
      <Stack divider={<Divider />}>
        {rows.map(([label, value]) => (
          <Stack direction={{ xs: "column", sm: "row" }} key={label} sx={{ gap: 1, justifyContent: "space-between", p: 2 }}>
            <Typography color="text.secondary">{label}</Typography>
            <Typography sx={{ fontWeight: 700, overflowWrap: "anywhere" }}>{value ?? "n/a"}</Typography>
          </Stack>
        ))}
      </Stack>
    </Paper>
  );
}

function hostChoice(node: ControlNode, health: HostHealthTarget | undefined): Choice {
  const latestCheck = health?.latestCheck;
  const checkedAt = latestCheck?.finishedAtUtc ?? latestCheck?.queuedAtUtc;
  return {
    id: node.id,
    title: node.name,
    subtitle: `${node.hostname}:${node.sshPort}`,
    meta: latestCheck?.status ? `Check: ${latestCheck.status}` : "kein Check",
    warning: checkedAt ? `Zuletzt geprüft: ${new Date(checkedAt).toLocaleString()}` : "Noch keine Verbindung geprüft.",
  };
}

function activeOnly<T extends { archivedAt: string | null }>(items: T[] | undefined) {
  return (items ?? []).filter((item) => !item.archivedAt);
}

function forbiddenMessage(error: unknown, fallback: string) {
  if (error instanceof ApiError && error.status === 403) {
    return "Du hast keine Berechtigung für diese Daten.";
  }

  if (error instanceof ApiError && error.status === 401) {
    return "Bitte melde dich an, um diese Daten zu sehen.";
  }

  return fallback;
}

function actionMismatchWarning(
  job: Job,
  controlNode: ControlNode | null,
  inventoryGroup: InventoryGroup | null,
  playbook: Playbook | null,
  variableSet: VariableSet | null,
) {
  const mismatches = [
    controlNode && job.controlNodeId !== controlNode.id ? "Control Host" : null,
    inventoryGroup && job.inventoryGroupId !== inventoryGroup.id ? "Inventar" : null,
    playbook && job.playbookId !== playbook.id ? "Playbook" : null,
    (variableSet?.id ?? null) !== job.variableSetId ? "Variablen" : null,
  ].filter(Boolean);

  return mismatches.length > 0 ? `Weicht von Auswahl ab: ${mismatches.join(", ")}` : null;
}

function slugify(value: string) {
  const slug = value
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 80);

  return slug.length >= 2 ? slug : "demo-run";
}
