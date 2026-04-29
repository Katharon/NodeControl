"use client";

import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Tooltip,
} from "@mui/material";
import type { ButtonProps } from "@mui/material";
import type { ReactNode } from "react";
import { useState } from "react";

type ConfirmActionButtonProps = {
  actionLabel: string;
  cancelLabel?: string;
  children: ReactNode;
  color?: ButtonProps["color"];
  confirmLabel?: string;
  disabled?: boolean;
  disabledReason?: string;
  message: string;
  onConfirm: () => unknown | Promise<unknown>;
  pending?: boolean;
  startIcon?: ReactNode;
  title: string;
  variant?: ButtonProps["variant"];
  warning?: string;
};

export function ConfirmActionButton({
  actionLabel,
  cancelLabel = "Cancel",
  children,
  color = "warning",
  confirmLabel,
  disabled = false,
  disabledReason,
  message,
  onConfirm,
  pending = false,
  startIcon,
  title,
  variant = "outlined",
  warning,
}: ConfirmActionButtonProps) {
  const [open, setOpen] = useState(false);
  const isDisabled = disabled || pending;
  const button = (
    <span>
      <Button
        color={color}
        disabled={isDisabled}
        onClick={() => setOpen(true)}
        startIcon={startIcon}
        variant={variant}
      >
        {children}
      </Button>
    </span>
  );

  return (
    <>
      {disabledReason && isDisabled ? <Tooltip title={disabledReason}>{button}</Tooltip> : button}
      <Dialog
        fullWidth
        maxWidth="sm"
        onClose={() => {
          if (!pending) {
            setOpen(false);
          }
        }}
        open={open}
      >
        <DialogTitle>{title}</DialogTitle>
        <DialogContent>
          <DialogContentText>{message}</DialogContentText>
          {warning ? <Alert severity="warning" sx={{ mt: 2 }}>{warning}</Alert> : null}
        </DialogContent>
        <DialogActions>
          <Button disabled={pending} onClick={() => setOpen(false)}>{cancelLabel}</Button>
          <Button
            color={color}
            disabled={pending}
            onClick={async () => {
              await onConfirm();
              setOpen(false);
            }}
            variant="contained"
          >
            {confirmLabel ?? actionLabel}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
