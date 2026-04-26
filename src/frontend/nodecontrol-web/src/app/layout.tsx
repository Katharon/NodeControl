import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "NodeControl",
  description: "Self-hosted Ansible control plane",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
