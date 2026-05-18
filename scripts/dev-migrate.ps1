. "$PSScriptRoot\dev-env.ps1"

$ErrorActionPreference = "Stop"

Require-Cmd dotnet

$dotnetToolsJson = Join-Path $RepoRoot ".config\dotnet-tools.json"

if (Test-Path $dotnetToolsJson) {
    Set-RepoRoot
    dotnet tool restore
    $efCmd = @("tool", "run", "dotnet-ef")
}
else {
    $efVersionOutput = & dotnet ef --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        $efCmd = @("ef")
    }
    else {
        Write-Error "dotnet-ef is required for migrations. Restore the local tool with: dotnet tool restore"
        exit 127
    }
}

Set-RepoRoot
& dotnet @efCmd database update `
    --project $InfrastructureProject `
    --startup-project $ApiProject `
    --context NodeControlDbContext
