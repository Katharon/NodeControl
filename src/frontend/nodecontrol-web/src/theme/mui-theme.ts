import { createTheme } from "@mui/material/styles";

export const muiTheme = createTheme({
  palette: {
    primary: {
      main: "#245b54",
    },
    secondary: {
      main: "#3f5f8a",
    },
    background: {
      default: "#f7f8fa",
    },
  },
  typography: {
    fontFamily: "Arial, Helvetica, sans-serif",
  },
  shape: {
    borderRadius: 8,
  },
});
