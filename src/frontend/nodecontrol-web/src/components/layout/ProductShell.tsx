"use client";

import AccountTreeIcon from "@mui/icons-material/AccountTree";
import ArticleIcon from "@mui/icons-material/Article";
import BusinessIcon from "@mui/icons-material/Business";
import CloudIcon from "@mui/icons-material/Cloud";
import DashboardIcon from "@mui/icons-material/Dashboard";
import DnsIcon from "@mui/icons-material/Dns";
import ExtensionIcon from "@mui/icons-material/Extension";
import GitHubIcon from "@mui/icons-material/GitHub";
import GroupsIcon from "@mui/icons-material/Groups";
import HealthAndSafetyIcon from "@mui/icons-material/HealthAndSafety";
import HistoryIcon from "@mui/icons-material/History";
import HubIcon from "@mui/icons-material/Hub";
import InventoryIcon from "@mui/icons-material/Inventory";
import KeyIcon from "@mui/icons-material/Key";
import LogoutIcon from "@mui/icons-material/Logout";
import NotificationsIcon from "@mui/icons-material/Notifications";
import ReceiptLongIcon from "@mui/icons-material/ReceiptLong";
import ScheduleIcon from "@mui/icons-material/Schedule";
import SecurityIcon from "@mui/icons-material/Security";
import SettingsIcon from "@mui/icons-material/Settings";
import StorageIcon from "@mui/icons-material/Storage";
import UploadFileIcon from "@mui/icons-material/UploadFile";
import VpnKeyIcon from "@mui/icons-material/VpnKey";
import WorkIcon from "@mui/icons-material/Work";
import {
  Box,
  Button,
  Chip,
  Divider,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import type { SvgIconComponent } from "@mui/icons-material";
import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import { getMyCustomers } from "@/lib/api/customers";
import { getCurrentUser } from "@/lib/auth/currentUser";

type ProductShellProps = {
  children: ReactNode;
};

type NavigationItem = {
  label: string;
  href: string;
  icon: SvgIconComponent;
  planned?: boolean;
  scopedSegment?: string;
};

type NavigationGroup = {
  label: string;
  items: NavigationItem[];
};

const navigationGroups: NavigationGroup[] = [
  {
    label: "Täglicher Betrieb",
    items: [
      { label: "Dashboard", href: "/dashboard", icon: DashboardIcon },
      { label: "Runs", href: "/runs", icon: ReceiptLongIcon, scopedSegment: "runs" },
      { label: "Hosts", href: "/hosts", icon: HubIcon, scopedSegment: "hosts" },
      { label: "Playbooks", href: "/playbooks", icon: ArticleIcon, scopedSegment: "playbooks" },
      { label: "Actions", href: "/actions", icon: WorkIcon, scopedSegment: "actions" },
      { label: "Schedules", href: "/schedules", icon: ScheduleIcon, scopedSegment: "schedules" },
    ],
  },
  {
    label: "Inventar & Assets",
    items: [
      { label: "Kunden", href: "/customers", icon: BusinessIcon },
      { label: "Hostzustand", href: "/host-health", icon: HealthAndSafetyIcon, scopedSegment: "host-health" },
      { label: "Variablen", href: "/variables", icon: StorageIcon, scopedSegment: "variables" },
      { label: "Cloud-Provider", href: "/cloud-providers", icon: CloudIcon, planned: true },
      { label: "Inventare", href: "/inventories", icon: InventoryIcon, scopedSegment: "inventories" },
      { label: "Templates", href: "/templates", icon: AccountTreeIcon, scopedSegment: "templates" },
    ],
  },
  {
    label: "Onboarding & Quellen",
    items: [
      { label: "Git-Repos", href: "/git-repos", icon: GitHubIcon, planned: true },
      { label: "Collections", href: "/collections", icon: ExtensionIcon, planned: true },
      { label: "Import", href: "/import", icon: UploadFileIcon, planned: true },
    ],
  },
  {
    label: "Geheimnisse & Benachrichtigungen",
    items: [
      { label: "Secrets", href: "/secrets", icon: VpnKeyIcon, scopedSegment: "secrets" },
      { label: "Benachrichtigungen", href: "/notifications", icon: NotificationsIcon, planned: true },
    ],
  },
  {
    label: "Administration",
    items: [
      { label: "Benutzer", href: "/users", icon: GroupsIcon },
      { label: "Audit", href: "/audit", icon: HistoryIcon, scopedSegment: "audit" },
      { label: "Master-Key", href: "/master-key", icon: KeyIcon, planned: true },
      { label: "Sicherheit", href: "/security", icon: SecurityIcon, planned: true },
      { label: "System", href: "/system", icon: SettingsIcon, planned: true },
    ],
  },
];

const scopedPathBySegment: Record<string, string> = {
  actions: "actions",
  audit: "audit",
  "host-health": "host-health",
  hosts: "hosts",
  inventories: "inventories",
  playbooks: "playbooks",
  runs: "runs",
  schedules: "schedules",
  secrets: "secrets",
  templates: "templates",
  variables: "variables",
};

export function ProductShell({ children }: ProductShellProps) {
  const pathname = usePathname();
  const currentUserQuery = useQuery({ queryKey: ["current-user"], queryFn: getCurrentUser });
  const customersQuery = useQuery({ queryKey: ["my-customers"], queryFn: getMyCustomers });
  const singleCustomerId = customersQuery.data?.length === 1 ? customersQuery.data[0]?.id : null;

  function itemHref(item: NavigationItem) {
    if (!singleCustomerId || !item.scopedSegment) {
      return item.href;
    }

    const scopedPath = scopedPathBySegment[item.scopedSegment];
    return scopedPath ? `/customers/${singleCustomerId}/${scopedPath}` : item.href;
  }

  function isActive(item: NavigationItem) {
    if (pathname === item.href || pathname.startsWith(`${item.href}/`)) {
      return true;
    }

    return item.scopedSegment
      ? pathname.includes(`/${item.scopedSegment}`) || legacySegmentActive(pathname, item.scopedSegment)
      : false;
  }

  return (
    <Box sx={{ bgcolor: "background.default", minHeight: "100vh" }}>
      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: { xs: "1fr", lg: "280px minmax(0, 1fr)" },
          minHeight: "100vh",
        }}
      >
        <Box
          component="aside"
          sx={{
            bgcolor: "background.paper",
            borderRight: { lg: 1 },
            borderBottom: { xs: 1, lg: 0 },
            borderColor: "divider",
            px: 2,
            py: 2,
          }}
        >
          <Stack sx={{ gap: 2 }}>
            <Stack direction="row" sx={{ alignItems: "center", gap: 1 }}>
              <DnsIcon color="primary" />
              <Box>
                <Typography sx={{ fontWeight: 800 }}>NodeControl</Typography>
                <Typography color="text.secondary" variant="caption">
                  Ansible Control Plane
                </Typography>
              </Box>
            </Stack>

            <Divider />

            <Stack sx={{ gap: 2 }}>
              {navigationGroups.map((group) => (
                <Box key={group.label}>
                  <Typography
                    color="text.secondary"
                    sx={{ fontSize: 12, fontWeight: 700, px: 1, py: 0.75, textTransform: "uppercase" }}
                  >
                    {group.label}
                  </Typography>
                  <List disablePadding dense>
                    {group.items.map((item) => {
                      const Icon = item.icon;
                      const active = isActive(item);
                      return (
                        <ListItemButton
                          component={Link}
                          href={itemHref(item)}
                          key={item.label}
                          selected={active}
                          sx={{ borderRadius: 1, minHeight: 38 }}
                        >
                          <ListItemIcon sx={{ minWidth: 34 }}>
                            <Icon color={active ? "primary" : "inherit"} fontSize="small" />
                          </ListItemIcon>
                          <ListItemText
                            primary={item.label}
                            secondary={item.planned ? "Noch nicht implementiert" : undefined}
                            slotProps={{
                              primary: { variant: "body2" },
                              secondary: { variant: "caption" },
                            }}
                          />
                        </ListItemButton>
                      );
                    })}
                  </List>
                </Box>
              ))}
            </Stack>
          </Stack>
        </Box>

        <Box sx={{ minWidth: 0 }}>
          <Paper
            component="header"
            square
            sx={{
              borderBottom: 1,
              borderColor: "divider",
              px: { xs: 2, md: 4 },
              py: 1.5,
            }}
          >
            <Stack
              direction={{ xs: "column", sm: "row" }}
              sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 1.5 }}
            >
              <Stack
                direction="row"
                sx={{ alignItems: "center", flexWrap: "wrap", gap: 1 }}
              >
                <Typography color="text.secondary" variant="body2">
                  {currentUserQuery.isPending
                    ? "Benutzer wird geladen..."
                    : currentUserQuery.isError
                      ? "Benutzer konnte nicht geladen werden"
                      : currentUserQuery.data
                        ? `${currentUserQuery.data.displayName} · ${currentUserQuery.data.email}`
                        : "Nicht angemeldet"}
                </Typography>
                {currentUserQuery.data?.authProvider === "fake" ? (
                  <Chip color="info" label="Fake Auth" size="small" variant="outlined" />
                ) : null}
              </Stack>
              {currentUserQuery.data ? (
                <Box component="form" action="/auth/logout" method="post">
                  <Button startIcon={<LogoutIcon />} type="submit" variant="outlined">
                    Sign out
                  </Button>
                </Box>
              ) : (
                <Button href="/auth/login" variant="contained">
                  Sign in
                </Button>
              )}
            </Stack>
          </Paper>

          {children}
        </Box>
      </Box>
    </Box>
  );
}

function legacySegmentActive(pathname: string, segment: string) {
  if (segment === "actions") {
    return pathname.includes("/jobs");
  }

  if (segment === "runs") {
    return pathname.includes("/job-runs");
  }

  if (segment === "hosts") {
    return pathname.includes("/nodes");
  }

  if (segment === "variables") {
    return pathname.includes("/variable-sets");
  }

  return false;
}
