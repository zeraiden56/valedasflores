param([switch]$Testar)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
$previousRollForward = $env:DOTNET_ROLL_FORWARD
$previousCliHome = $env:DOTNET_CLI_HOME
$previousNuget = $env:NUGET_PACKAGES
try {
    $env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.tools/dotnet-home'
    $env:NUGET_PACKAGES = Join-Path $PSScriptRoot '.tools/nuget'
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'Instale o SDK .NET 8 ou superior (x64).'
    }
    $godot = $env:GODOT_BIN
    if (-not $godot) {
        $godot = Join-Path $PSScriptRoot '.tools/godot/Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64.exe'
    }
    if (-not (Test-Path -LiteralPath $godot)) {
        throw 'Godot .NET ausente. Veja a preparacao para Windows no README.md ou defina GODOT_BIN.'
    }
    foreach ($template in @('windows_debug_x86_64.exe', 'windows_release_x86_64.exe')) {
        if (-not (Test-Path ".tools/templates/$template")) {
            throw 'Templates .NET ausentes em .tools/templates. Veja README.md.'
        }
    }
    # Permite executar o editor .NET 8 em maquinas com runtime superior.
    $env:DOTNET_ROLL_FORWARD = 'LatestMajor'
    New-Item -ItemType Directory -Force 'build/windows' | Out-Null
    & $godot --headless --path $PSScriptRoot --editor --import | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao importar o projeto.' }
    & $godot --headless --path $PSScriptRoot --export-release 'Windows Desktop' 'build/windows/ValeDasFlores.exe' | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Falha na exportacao Windows.' }
    if ($Testar) {
        & './build/windows/ValeDasFlores.exe' --headless -- --smoke | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'Falha nos testes integrados.' }
        & './build/windows/ValeDasFlores.exe' --headless -- --gameplay-test | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'Falha nos testes de atividades e saves.' }
        & './build/windows/ValeDasFlores.exe' --headless -- --input-test | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'Falha nos testes de controle e interface.' }
        & './build/windows/ValeDasFlores.exe' --headless -- --vehicle-test | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'Falha nos testes de veiculos.' }
    }
    Write-Host 'Pronto: .\build\windows\ValeDasFlores.exe'
} finally {
    $env:DOTNET_ROLL_FORWARD = $previousRollForward
    $env:DOTNET_CLI_HOME = $previousCliHome
    $env:NUGET_PACKAGES = $previousNuget
    Pop-Location
}
