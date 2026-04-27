"use client";

import AutorenewIcon from "@mui/icons-material/Autorenew";
import { zodResolver } from "@hookform/resolvers/zod";
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, TextField } from "@mui/material";
import { useForm } from "react-hook-form";
import { z } from "zod";

const rotateSchema = z.object({
  value: z.string().min(1, "Secret value is required.").max(100000),
});

type RotateValues = z.infer<typeof rotateSchema>;

type RotateSecretDialogProps = {
  open: boolean;
  onClose: () => void;
  onRotate: (value: string) => Promise<void>;
};

export function RotateSecretDialog({ open, onClose, onRotate }: RotateSecretDialogProps) {
  const {
    formState: { errors, isSubmitting },
    handleSubmit,
    register,
    reset,
  } = useForm<RotateValues>({
    resolver: zodResolver(rotateSchema),
    defaultValues: { value: "" },
  });

  function close() {
    reset();
    onClose();
  }

  return (
    <Dialog fullWidth maxWidth="sm" onClose={close} open={open}>
      <DialogTitle>Rotate Secret</DialogTitle>
      <DialogContent>
        <Stack
          component="form"
          id="rotate-secret-form"
          onSubmit={handleSubmit(async (values) => {
            await onRotate(values.value);
            close();
          })}
          sx={{ gap: 2, pt: 1 }}
        >
          <Alert severity="warning">The new secret value cannot be viewed again after saving.</Alert>
          <TextField
            error={Boolean(errors.value)}
            helperText={errors.value?.message}
            label="New secret value"
            minRows={4}
            multiline
            type="password"
            {...register("value")}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={close}>Cancel</Button>
        <Button disabled={isSubmitting} form="rotate-secret-form" startIcon={<AutorenewIcon />} type="submit" variant="contained">
          Rotate
        </Button>
      </DialogActions>
    </Dialog>
  );
}
