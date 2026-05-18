. "$PSScriptRoot\dev-env.ps1"

$ErrorActionPreference = "Stop"

Require-Cmd node

Set-RepoRoot
node (Join-Path $RepoRoot "scripts\seed-demo.mjs") @args
