. "$PSScriptRoot\dev-env.ps1"

$ErrorActionPreference = "Stop"

Require-Cmd npm

Set-Location $FrontendDir
npm run dev -- @args
