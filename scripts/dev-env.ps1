# Shared paths and defaults for local NodeControl dev/demo scripts.
# Usage from another script:
#   . "$PSScriptRoot\dev-env.ps1"

$Script:ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Script:RepoRoot = Resolve-Path (Join-Path $ScriptDir "..")

$Script:BackendSolution = Join-Path $RepoRoot "src\backend\NodeControl.slnx"
$Script:ApiProject = Join-Path $RepoRoot "src\backend\NodeControl.Api\NodeControl.Api.csproj"
$Script:WorkerProject = Join-Path $RepoRoot "src\backend\NodeControl.Worker\NodeControl.Worker.csproj"
$Script:InfrastructureProject = Join-Path $RepoRoot "src\backend\NodeControl.Infrastructure\NodeControl.Infrastructure.csproj"
$Script:FrontendDir = Join-Path $RepoRoot "src\frontend\nodecontrol-web"
$Script:DevComposeFile = Join-Path $RepoRoot "docker-compose.dev.yml"

$Script:DefaultConnectionString = "Host=localhost;Port=5432;Database=nodecontrol;Username=nodecontrol;Password=nodecontrol_dev_password"

if (-not $env:NODECONTROL_CONNECTION_STRING) {
    $env:NODECONTROL_CONNECTION_STRING = $DefaultConnectionString
}

if (-not $env:NODECONTROL_API_URL) {
    $env:NODECONTROL_API_URL = "http://localhost:5257"
}

if (-not $env:NODECONTROL_FRONTEND_URL) {
    $env:NODECONTROL_FRONTEND_URL = "http://localhost:3000"
}

if (-not $env:NODECONTROL_API_ORIGIN) {
    $env:NODECONTROL_API_ORIGIN = $env:NODECONTROL_API_URL
}

function Require-Cmd {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        Write-Error "Missing required command: $Name"
        exit 127
    }
}

function Set-RepoRoot {
    Set-Location $RepoRoot
}
