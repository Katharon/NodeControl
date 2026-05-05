"use client";

import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import { Alert, Button, CircularProgress, MenuItem, Stack, TextField } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useEffect } from "react";
import { Controller, useForm } from "react-hook-form";
import { z } from "zod";
import { getJobs } from "@/lib/api/jobs";
import type { JobSchedule, JobScheduleInput } from "@/lib/api/schedules";

const slugPattern = /^[a-z0-9][a-z0-9-]{1,99}$/;

const scheduleSchema = z.object({
  name: z.string().trim().min(1).max(200),
  slug: z.string().trim().regex(slugPattern, "Use lowercase letters, numbers, and hyphens"),
  description: z.string().trim().max(1000).optional(),
  jobId: z.string().uuid(),
  cronExpression: z.string().trim().min(1).max(120),
  timeZoneId: z.string().trim().min(1).max(100),
});

type JobScheduleFormValues = z.infer<typeof scheduleSchema>;

type JobScheduleFormProps = {
  customerId: string;
  schedule?: JobSchedule;
  submitLabel: string;
  onSubmit: (input: JobScheduleInput) => Promise<void>;
};

export function JobScheduleForm({ customerId, schedule, submitLabel, onSubmit }: JobScheduleFormProps) {
  const jobsQuery = useQuery({ queryKey: ["jobs", customerId], queryFn: () => getJobs(customerId) });
  const {
    formState: { errors, isSubmitting },
    control,
    handleSubmit,
    register,
    reset,
  } = useForm<JobScheduleFormValues>({
    resolver: zodResolver(scheduleSchema),
    defaultValues: getScheduleFormDefaults(schedule),
  });

  useEffect(() => {
    reset(getScheduleFormDefaults(schedule));
  }, [schedule, reset]);

  if (jobsQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (jobsQuery.isError) {
    return <Alert severity="error">Schedule-Formulardaten konnten nicht geladen werden.</Alert>;
  }

  return (
    <Stack
      component="form"
      onSubmit={handleSubmit(async (values) =>
        onSubmit({
          name: values.name,
          slug: values.slug,
          description: values.description || null,
          jobId: values.jobId,
          cronExpression: values.cronExpression,
          timeZoneId: values.timeZoneId || "UTC",
        }),
      )}
      sx={{ gap: 2 }}
    >
      <TextField error={Boolean(errors.name)} helperText={errors.name?.message} label="Name" {...register("name")} />
      <TextField error={Boolean(errors.slug)} helperText={errors.slug?.message} label="Slug" {...register("slug")} />
      <TextField label="Description" minRows={2} multiline {...register("description")} />
      <Controller
        control={control}
        name="jobId"
        render={({ field }) => (
          <TextField
            error={Boolean(errors.jobId)}
            helperText={errors.jobId?.message}
            label="Action"
            onBlur={field.onBlur}
            onChange={field.onChange}
            select
            value={field.value ?? ""}
          >
            <MenuItem value="">Select Action</MenuItem>
            {schedule?.jobId && !jobsQuery.data.some((job) => job.id === schedule.jobId) ? (
              <MenuItem value={schedule.jobId}>Configured Action</MenuItem>
            ) : null}
            {jobsQuery.data.map((job) => <MenuItem key={job.id} value={job.id}>{job.name}</MenuItem>)}
          </TextField>
        )}
      />
      <TextField
        error={Boolean(errors.cronExpression)}
        helperText={errors.cronExpression?.message ?? "Five-field cron, for example */5 * * * *"}
        label="Cron expression"
        {...register("cronExpression")}
      />
      <TextField
        error={Boolean(errors.timeZoneId)}
        helperText={errors.timeZoneId?.message}
        label="Time zone"
        {...register("timeZoneId")}
      />
      <Button disabled={isSubmitting} startIcon={<SaveIcon />} sx={{ alignSelf: "flex-start" }} type="submit" variant="contained">
        {submitLabel}
      </Button>
    </Stack>
  );
}

function getScheduleFormDefaults(schedule?: JobSchedule): JobScheduleFormValues {
  return {
    name: schedule?.name ?? "",
    slug: schedule?.slug ?? "",
    description: schedule?.description ?? "",
    jobId: schedule?.jobId ?? "",
    cronExpression: schedule?.cronExpression ?? "0 * * * *",
    timeZoneId: schedule?.timeZoneId ?? "UTC",
  };
}
