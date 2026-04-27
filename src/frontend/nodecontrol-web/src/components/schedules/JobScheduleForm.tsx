"use client";

import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import { Alert, Button, CircularProgress, MenuItem, Stack, TextField } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
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
    handleSubmit,
    register,
  } = useForm<JobScheduleFormValues>({
    resolver: zodResolver(scheduleSchema),
    defaultValues: {
      name: schedule?.name ?? "",
      slug: schedule?.slug ?? "",
      description: schedule?.description ?? "",
      jobId: schedule?.jobId ?? "",
      cronExpression: schedule?.cronExpression ?? "0 * * * *",
      timeZoneId: schedule?.timeZoneId ?? "UTC",
    },
  });

  if (jobsQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (jobsQuery.isError) {
    return <Alert severity="error">Schedule form data could not be loaded.</Alert>;
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
      <TextField error={Boolean(errors.jobId)} helperText={errors.jobId?.message} label="Job" select {...register("jobId")}>
        {jobsQuery.data.map((job) => <MenuItem key={job.id} value={job.id}>{job.name}</MenuItem>)}
      </TextField>
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
