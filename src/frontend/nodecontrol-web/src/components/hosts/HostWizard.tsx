"use client";

import CheckIcon from "@mui/icons-material/Check";
import NavigateBeforeIcon from "@mui/icons-material/NavigateBefore";
import NavigateNextIcon from "@mui/icons-material/NavigateNext";
import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  Alert,
  Button,
  MenuItem,
  Paper,
  Stack,
  Step,
  StepLabel,
  Stepper,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { z } from "zod";
import { createControlNode } from "@/lib/api/controlNodes";
import { createManagedNode } from "@/lib/api/managedNodes";
import { getSecrets } from "@/lib/api/secrets";

const hostTypes = ["Linux", "Windows", "Netzwerk", "SNMP", "Generic"] as const;
const inventoryName = /^[a-zA-Z][a-zA-Z0-9_-]{1,99}$/;
type HostWizardKind = "control" | "managed";

type HostWizardValues = {
  hostType: (typeof hostTypes)[number];
  name: string;
  hostname: string;
  sshUser?: string;
  sshPort: number;
  sshPrivateKeySecretId?: string;
};

function createHostWizardSchema(hostKind: HostWizardKind) {
  return z.object({
    hostType: z.enum(hostTypes),
    name: hostKind === "managed"
      ? z.string().trim().regex(inventoryName, "Use an inventory-safe hostname")
      : z.string().trim().min(1).max(200),
    hostname: z.string().trim().min(1).max(253).refine((value) => !/\s/.test(value), {
      message: "IP or DNS name must not contain whitespace",
    }),
    sshUser: z.string().trim().max(100).refine((value) => !/\s/.test(value), {
      message: "SSH username must not contain whitespace",
    }).optional(),
    sshPort: z.number().int().min(1).max(65535),
    sshPrivateKeySecretId: z.string().trim().optional(),
  });
}

type HostWizardProps = {
  customerId: string;
  customerName: string;
  canViewSecrets?: boolean;
  hostKind: HostWizardKind;
  onCreated: () => void;
};

const stepsByKind: Record<HostWizardKind, string[]> = {
  control: ["Basisdaten", "SSH-Verbindung", "Prüfen & Anlegen"],
  managed: ["Host-Typ", "Basisdaten", "SSH-Ziel", "Prüfen & Anlegen"],
};

export function HostWizard({ customerId, customerName, canViewSecrets = false, hostKind, onCreated }: HostWizardProps) {
  const queryClient = useQueryClient();
  const [activeStep, setActiveStep] = useState(0);
  const steps = stepsByKind[hostKind];
  const isControlFlow = hostKind === "control";
  const titlePrefix = isControlFlow ? "Neuer Control Host" : "Neuer Host";
  const submitLabel = isControlFlow ? "Control Host anlegen" : "Host anlegen";
  const secretsQuery = useQuery({
    enabled: !isControlFlow && canViewSecrets,
    queryKey: ["secrets", customerId],
    queryFn: () => getSecrets(customerId),
  });
  const sshPrivateKeySecrets = (secretsQuery.data ?? []).filter((secret) => secret.kind === "SshPrivateKey" && secret.status === "Active");
  const {
    control,
    formState: { errors, isSubmitting },
    getValues,
    handleSubmit,
    register,
    trigger,
  } = useForm<HostWizardValues>({
    resolver: zodResolver(createHostWizardSchema(hostKind)),
    defaultValues: {
      hostType: "Linux",
      name: "",
      hostname: "",
      sshUser: "",
      sshPort: 22,
      sshPrivateKeySecretId: "",
    },
  });

  const createManagedMutation = useMutation({
    mutationFn: (values: HostWizardValues) =>
      createManagedNode(customerId, {
        name: values.name,
        hostname: values.hostname,
        sshPort: values.sshPort,
        sshUsername: values.sshUser || null,
        sshPrivateKeySecretId: values.sshPrivateKeySecretId || null,
        operatingSystem: values.hostType === "Generic" ? null : values.hostType,
        environment: null,
        description: null,
      }),
  });
  const createControlMutation = useMutation({
    mutationFn: (values: HostWizardValues) =>
      createControlNode(customerId, {
        name: values.name,
        hostname: values.hostname,
        sshPort: values.sshPort,
        description: null,
      }),
  });

  async function invalidateHosts() {
    await queryClient.invalidateQueries({ queryKey: ["managed-nodes", customerId] });
    await queryClient.invalidateQueries({ queryKey: ["control-nodes", customerId] });
    await queryClient.invalidateQueries({ queryKey: ["inventory-groups", customerId] });
  }

  async function nextStep() {
    const fieldsByStep: Array<Array<keyof HostWizardValues>> = isControlFlow
      ? [["name", "hostname"], ["sshPort"], []]
      : [["hostType"], ["name", "hostname"], ["sshUser", "sshPort", "sshPrivateKeySecretId"], []];
    const valid = await trigger(fieldsByStep[activeStep]);
    if (valid) {
      setActiveStep((step) => Math.min(step + 1, steps.length - 1));
    }
  }

  async function submit(values: HostWizardValues) {
    if (isControlFlow) {
      await createControlMutation.mutateAsync(values);
    } else {
      await createManagedMutation.mutateAsync(values);
    }

    await invalidateHosts();
    onCreated();
  }

  const values = getValues();
  const showTypeStep = !isControlFlow && activeStep === 0;
  const showBasisStep = isControlFlow ? activeStep === 0 : activeStep === 1;
  const showConnectionStep = isControlFlow ? activeStep === 1 : activeStep === 2;
  const showSummaryStep = isControlFlow ? activeStep === 2 : activeStep === 3;

  return (
    <Stack component="form" onSubmit={handleSubmit(submit)} sx={{ gap: 3, pt: 1 }}>
      <Stepper activeStep={activeStep} alternativeLabel>
        {steps.map((label) => (
          <Step key={label}>
            <StepLabel>{label}</StepLabel>
          </Step>
        ))}
      </Stepper>

      {showTypeStep ? (
        <Stack sx={{ gap: 2 }}>
          <Typography component="h2" variant="h6">
            {titlePrefix} — Host-Typ
          </Typography>
          <Controller
            control={control}
            name="hostType"
            render={({ field }) => (
              <ToggleButtonGroup
                exclusive
                fullWidth
                onChange={(_, value: HostWizardValues["hostType"] | null) => {
                  if (value) {
                    field.onChange(value);
                  }
                }}
                value={field.value}
              >
                {hostTypes.map((hostType) => (
                  <ToggleButton key={hostType} value={hostType}>
                    {hostType}
                  </ToggleButton>
                ))}
              </ToggleButtonGroup>
            )}
          />
        </Stack>
      ) : null}

      {showBasisStep ? (
        <Stack sx={{ gap: 2 }}>
          <Typography component="h2" variant="h6">
            {titlePrefix} — Basisdaten
          </Typography>
          <TextField error={Boolean(errors.name)} helperText={errors.name?.message} label="Hostname" {...register("name")} />
          <TextField
            error={Boolean(errors.hostname)}
            helperText={errors.hostname?.message}
            label="IP oder DNS-Name"
            {...register("hostname")}
          />
          <TextField disabled label="Kunde" value={customerName} />
        </Stack>
      ) : null}

      {showConnectionStep ? (
        <Stack sx={{ gap: 2 }}>
          <Typography component="h2" variant="h6">
            {titlePrefix} — {isControlFlow ? "SSH-Verbindung" : "SSH-Ziel"}
          </Typography>
          {!isControlFlow ? (
            <TextField error={Boolean(errors.sshUser)} helperText={errors.sshUser?.message} label="SSH-User" {...register("sshUser")} />
          ) : null}
          <TextField
            error={Boolean(errors.sshPort)}
            helperText={errors.sshPort?.message}
            label="SSH-Port"
            type="number"
            {...register("sshPort", { valueAsNumber: true })}
          />
          {!isControlFlow ? (
            <>
              {canViewSecrets && sshPrivateKeySecrets.length === 0 ? (
                <Alert severity="info">Lege ein aktives SSH-Private-Key-Secret an, wenn dieser Host mit eigenem Key laufen soll.</Alert>
              ) : null}
              {!canViewSecrets ? (
                <Alert severity="info">SSH-Key-Secret-Zuordnung kann in der Host-Konfiguration gepflegt werden, wenn Secret-Leserechte vorhanden sind.</Alert>
              ) : null}
              <Controller
                control={control}
                name="sshPrivateKeySecretId"
                render={({ field }) => (
                  <TextField
                    disabled={!canViewSecrets}
                    error={Boolean(errors.sshPrivateKeySecretId)}
                    helperText={errors.sshPrivateKeySecretId?.message ?? "Nur die Secret-Referenz wird gespeichert; der Key-Wert wird nicht angezeigt."}
                    label="SSH Private Key Secret"
                    onBlur={field.onBlur}
                    onChange={field.onChange}
                    select
                    value={field.value ?? ""}
                  >
                    <MenuItem value="">Kein Key-Secret</MenuItem>
                    {sshPrivateKeySecrets.map((secret) => (
                      <MenuItem key={secret.id} value={secret.id}>
                        {secret.name} · secret://{secret.slug}
                      </MenuItem>
                    ))}
                  </TextField>
                )}
              />
            </>
          ) : null}
          {isControlFlow ? (
            <Alert severity="info">
              Remote-Dispatch-Details wie SSH-Key und Workspace-Pfad werden nach dem Anlegen in der Control-Host-Konfiguration gepflegt.
            </Alert>
          ) : null}
        </Stack>
      ) : null}

      {showSummaryStep ? (
        <Stack sx={{ gap: 2 }}>
          <Typography component="h2" variant="h6">
            {titlePrefix} — Prüfen & Anlegen
          </Typography>
          <Paper variant="outlined" sx={{ p: 2 }}>
            <Stack sx={{ gap: 1 }}>
              <SummaryRow label="Typ" value={isControlFlow ? "Control Host" : values.hostType} />
              <SummaryRow label="Hostname" value={values.name} />
              <SummaryRow label="IP oder DNS-Name" value={values.hostname} />
              <SummaryRow label="Kunde" value={customerName} />
              {!isControlFlow ? <SummaryRow label="SSH-User" value={values.sshUser || "Nicht gesetzt"} /> : null}
              <SummaryRow label="SSH-Port" value={values.sshPort.toString()} />
              {!isControlFlow ? <SummaryRow label="SSH-Key" value={values.sshPrivateKeySecretId ? "Secret-backed" : "Nicht gesetzt"} /> : null}
            </Stack>
          </Paper>
          {createManagedMutation.isError || createControlMutation.isError ? (
            <Alert severity="error">Host konnte nicht angelegt werden.</Alert>
          ) : null}
        </Stack>
      ) : null}

      <Stack direction="row" sx={{ justifyContent: "space-between", gap: 1 }}>
        <Button
          disabled={activeStep === 0 || isSubmitting}
          onClick={() => setActiveStep((step) => Math.max(step - 1, 0))}
          startIcon={<NavigateBeforeIcon />}
          variant="outlined"
        >
          Zurück
        </Button>
        {activeStep < steps.length - 1 ? (
          <Button endIcon={<NavigateNextIcon />} onClick={() => void nextStep()} variant="contained">
            Weiter
          </Button>
        ) : (
          <Button disabled={isSubmitting} startIcon={<SaveIcon />} type="submit" variant="contained">
            {submitLabel}
          </Button>
        )}
      </Stack>
    </Stack>
  );
}

function SummaryRow({ label, value }: { label: string; value: string }) {
  return (
    <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
      <CheckIcon color="primary" fontSize="small" />
      <Typography color="text.secondary" sx={{ minWidth: 150 }} variant="body2">
        {label}
      </Typography>
      <Typography sx={{ overflowWrap: "anywhere" }}>{value}</Typography>
    </Stack>
  );
}
