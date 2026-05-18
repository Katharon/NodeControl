. "$PSScriptRoot\dev-env.ps1"

$ErrorActionPreference = "Stop"

Require-Cmd dotnet
Require-Cmd npm

Set-RepoRoot

Write-Host "Checking that no .sln file exists..."
$slnFiles = Get-ChildItem -Path . -Recurse -Filter "*.sln" -File | Select-Object -ExpandProperty FullName

if ($slnFiles) {
    $slnFiles | ForEach-Object { Write-Error $_ }
    Write-Error "Unexpected .sln file found. NodeControl uses src/backend/NodeControl.slnx."
    exit 1
}

Write-Host "Restoring backend..."
dotnet restore $BackendSolution

Write-Host "Building backend..."
dotnet build $BackendSolution

Write-Host "Testing backend..."
dotnet test $BackendSolution

Write-Host "Linting frontend..."
Push-Location $FrontendDir
try {
    npm run lint
}
finally {
    Pop-Location
}

Write-Host "Building frontend..."
Push-Location $FrontendDir
try {
    npm run build
}
finally {
    Pop-Location
}

Write-Host "Checking API process execution boundary..."
$apiDir = Join-Path $RepoRoot "src\backend\NodeControl.Api"
$patterns = "ProcessStartInfo|Process.Start|ansible-playbook"

$apiProcessMatches = Get-ChildItem -Path $apiDir -Recurse -File |
    Where-Object { $_.FullName -notmatch "\\bin\\" -and $_.FullName -notmatch "\\obj\\" } |
    Select-String -Pattern $patterns

if ($apiProcessMatches) {
    $apiProcessMatches | ForEach-Object { Write-Error "$($_.Path):$($_.LineNumber):$($_.Line)" }
    Write-Error "Forbidden process execution reference found in NodeControl.Api."
    exit 1
}

if (Get-Command curl.exe -ErrorAction SilentlyContinue) {
    & curl.exe -fsS "$($env:NODECONTROL_API_URL)/api/v1/me" *> $null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "API smoke check passed: $($env:NODECONTROL_API_URL)/api/v1/me"
    }
    else {
        Write-Host "API smoke check skipped: start the API with scripts/dev-run-api.ps1"
    }

    & curl.exe -fsSI "$($env:NODECONTROL_FRONTEND_URL)/dashboard" *> $null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Frontend smoke check passed: $($env:NODECONTROL_FRONTEND_URL)/dashboard"
    }
    else {
        Write-Host "Frontend smoke check skipped: start the frontend with scripts/dev-run-frontend.ps1"
    }
}
else {
    Write-Host "curl.exe not found; local HTTP smoke checks skipped."
}

Write-Host "Smoke checks completed."
