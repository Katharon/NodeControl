import LoginIcon from "@mui/icons-material/Login";
import { Button } from "@mui/material";

export function LoginButton() {
  return (
    <Button href="/auth/login" startIcon={<LoginIcon />} variant="contained">
      Sign in
    </Button>
  );
}
