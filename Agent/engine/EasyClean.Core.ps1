param(
    [string]$RootPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $RootPath) {
    $RootPath = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$global:EasyPaths = [ordered]@{
    Root       = $RootPath
    ConfigFile = Join-Path $RootPath '..\config\settings.json'
    LogDir     = Join-Path $RootPath '..\logs'
    LogFile    = Join-Path $RootPath '..\logs\easy.log'
    Progress   = Join-Path $RootPath '..\logs\progress.json'
}

if (-not (Test-Path $global:EasyPaths.LogDir)) {
    New-Item -Path $global:EasyPaths.LogDir -ItemType Directory -Force | Out-Null
}

function Write-EasyLog {
    param(
        [string]$Message,
        [string]$Level = 'INFO'
    )
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $line = "[$timestamp] [$Level] $Message"
    Add-Content -Path $global:EasyPaths.LogFile -Value $line
}

function Initialize-EasyConfig {
    if (-not (Test-Path $global:EasyPaths.ConfigFile)) {
        $default = @{
            SimulationMode = $true
            Targets        = @(
                @{
                    Id            = 'TempUser'
                    Name          = '%TEMP%'
                    Path          = $env:TEMP
                    RequiresAdmin = $false
                    Enabled       = $true
                },
                @{
                    Id            = 'WindowsTemp'
                    Name          = 'C:\Windows\Temp'
                    Path          = 'C:\Windows\Temp'
                    RequiresAdmin = $true
                    Enabled       = $true
                },
                @{
                    Id            = 'LocalAppDataTemp'
                    Name          = '%AppData%\Local\Temp'
                    Path          = (Join-Path $env:LOCALAPPDATA 'Temp')
                    RequiresAdmin = $false
                    Enabled       = $true
                },
                @{
                    Id            = 'Prefetch'
                    Name          = 'C:\Windows\Prefetch'
                    Path          = 'C:\Windows\Prefetch'
                    RequiresAdmin = $true
                    Enabled       = $true
                    Policy        = @{
                        Mode         = 'OlderThanDays'
                        Days         = 7
                        ExcludeFiles = @('Layout.ini')
                    }
                },
                @{
                    Id            = 'SoftwareDistribution'
                    Name          = 'SoftwareDistribution\Download'
                    Path          = 'C:\Windows\SoftwareDistribution\Download'
                    RequiresAdmin = $true
                    Enabled       = $true
                }
            )
            Schedule       = @{
                Enabled = $false
                Time    = '03:00'
            }
        }
        $default | ConvertTo-Json -Depth 6 | Set-Content -Path $global:EasyPaths.ConfigFile -Encoding UTF8
        Write-EasyLog "Configuracao padrao criada em $($global:EasyPaths.ConfigFile)"
    }
}

function Get-EasyConfig {
    Initialize-EasyConfig
    (Get-Content -Path $global:EasyPaths.ConfigFile -Raw) | ConvertFrom-Json
}

function Save-EasyConfig {
    param(
        [Parameter(Mandatory)]
        $Config
    )
    $Config | ConvertTo-Json -Depth 6 | Set-Content -Path $global:EasyPaths.ConfigFile -Encoding UTF8
    Write-EasyLog "Configuracao atualizada."
}

function Get-IsAdmin {
    $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Initialize-EasyProgress {
    param(
        [string]$Status = 'idle'
    )
    $progress = @{
        Status        = $Status
        Phase         = 'idle'
        CurrentTarget = $null
        Processed     = 0
        Total         = 0
        Percent       = 0
        Simulation    = $true
        FreedBytes    = 0
        Timestamp     = (Get-Date).ToString('o')
    }
    Set-EasyProgressFile -ProgressObject $progress
}

function Update-EasyProgress {
    param(
        [string]$Status,
        [string]$Phase = $null,
        [string]$CurrentTarget,
        [int]$Processed,
        [int]$Total,
        [long]$FreedBytes,
        [bool]$Simulation
    )
    $percent = 0
    if ($Total -gt 0) {
        $percent = [math]::Round(($Processed / $Total) * 100, 1)
    }
    $progress = @{
        Status        = $Status
        Phase         = if ($null -ne $Phase) { $Phase } else { 'cleaning' }
        CurrentTarget = $CurrentTarget
        Processed     = $Processed
        Total         = $Total
        Percent       = $percent
        Simulation    = $Simulation
        FreedBytes    = $FreedBytes
        Timestamp     = (Get-Date).ToString('o')
    }
    Set-EasyProgressFile -ProgressObject $progress
}

function Set-EasyProgressFile {
    param(
        [Parameter(Mandatory)]
        $ProgressObject
    )
    $json = $ProgressObject | ConvertTo-Json -Depth 4
    $temp = "$($global:EasyPaths.Progress).tmp"
    [System.IO.File]::WriteAllText($temp, $json, [System.Text.Encoding]::UTF8)
    Move-Item -Path $temp -Destination $global:EasyPaths.Progress -Force
}

function Read-EasyProgressFile {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )
    $fs = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    try {
        $sr = New-Object System.IO.StreamReader($fs, [System.Text.Encoding]::UTF8, $true)
        try {
            return $sr.ReadToEnd()
        }
        finally {
            $sr.Close()
        }
    }
    finally {
        $fs.Close()
    }
}

function Get-EasyProgress {
    if (Test-Path $global:EasyPaths.Progress) {
        for ($i = 0; $i -lt 5; $i++) {
            try {
                $raw = Read-EasyProgressFile -Path $global:EasyPaths.Progress
                if ($raw) {
                    return $raw | ConvertFrom-Json
                }
            }
            catch {
                Start-Sleep -Milliseconds 40
            }
        }
        return [PSCustomObject]@{
            Status        = 'running'
            Phase         = 'enumerating'
            CurrentTarget = 'Aguardando acesso ao progresso...'
            Processed     = 0
            Total         = 0
            Percent       = 0
            Simulation    = $true
            FreedBytes    = 0
            Timestamp     = (Get-Date).ToString('o')
        }
    }
    else {
        Initialize-EasyProgress
        return Get-EasyProgress
    }
}

function Get-TargetItems {
    param(
        [Parameter(Mandatory)]
        $Target
    )

    # Expande variaveis de ambiente para suportar valores como %TEMP% e %LOCALAPPDATA%
    $path = [Environment]::ExpandEnvironmentVariables($Target.Path)
    if (-not (Test-Path $path)) {
        Write-EasyLog "Pasta nao encontrada: $path"
        return @()
    }

    $items = Get-ChildItem -Path $path -Recurse -Force -ErrorAction SilentlyContinue

    if ($Target.Id -eq 'Prefetch' -and $Target.Policy) {
        $cutoff = (Get-Date).AddDays( - [int]$Target.Policy.Days)
        $exclude = @($Target.Policy.ExcludeFiles)
        $items = $items | Where-Object {
            $_.LastWriteTime -lt $cutoff -and
            ($exclude -notcontains $_.Name)
        }
    }

    return $items
}

function Invoke-EasyClean {
    param(
        [bool]$Simulation = $true,
        [string[]]$SelectedTargetIds = $null
    )

    Initialize-EasyConfig
    $config = Get-EasyConfig
    $isAdmin = Get-IsAdmin

    $targets = $config.Targets | Where-Object { $_.Enabled }
    if ($SelectedTargetIds) {
        $targets = $targets | Where-Object { $SelectedTargetIds -contains $_.Id }
    }

    [long]$freedBytes = 0
    $processed = 0
    $total = 0

    Write-EasyLog "EasyClean iniciado. Modo simulacao: $Simulation"
    Update-EasyProgress -Status 'running' -Phase 'enumerating' -CurrentTarget 'Preparando lista de arquivos...' -Processed 0 -Total 0 -FreedBytes 0 -Simulation $Simulation

    foreach ($t in $targets) {
        if ($t.RequiresAdmin -and -not $isAdmin) { continue }
        Update-EasyProgress -Status 'running' -Phase 'enumerating' -CurrentTarget "Listando: $($t.Name)" -Processed 0 -Total 0 -FreedBytes 0 -Simulation $Simulation
        $items = @(Get-TargetItems -Target $t)
        $total += $items.Count
    }

    Write-EasyLog "Total de itens a processar: $total"
    Update-EasyProgress -Status 'running' -Phase 'cleaning' -CurrentTarget '' -Processed 0 -Total $total -FreedBytes 0 -Simulation $Simulation

    foreach ($t in $targets) {
        if ($t.RequiresAdmin -and -not $isAdmin) { continue }
        $items = @(Get-TargetItems -Target $t)
        foreach ($item in $items) {
            $processed++
            Update-EasyProgress -Status 'running' -Phase 'cleaning' -CurrentTarget $t.Name -Processed $processed -Total $total -FreedBytes $freedBytes -Simulation $Simulation
            try {
                if ($item.PSIsContainer) { continue }
                $size = $item.Length
                if ($Simulation) {
                    $freedBytes += $size
                }
                else {
                    try {
                        $freedBytes += $size
                        Remove-Item -LiteralPath $item.FullName -Force -ErrorAction Stop
                        Write-EasyLog "Removido: $($item.FullName) ($size bytes)"
                    }
                    catch [System.IO.IOException] {
                        Write-EasyLog "Arquivo em uso, ignorado: $($item.FullName)" 'WARN'
                    }
                    catch [System.UnauthorizedAccessException] {
                        Write-EasyLog "Sem permissao: $($item.FullName)" 'WARN'
                    }
                }
            }
            catch {
                Write-EasyLog "Erro: $($item.FullName) - $($_.Exception.Message)" 'ERROR'
            }
        }
    }

    Update-EasyProgress -Status 'completed' -Phase 'idle' -CurrentTarget '' -Processed $processed -Total $total -FreedBytes $freedBytes -Simulation $Simulation
    $freedMB = [math]::Round($freedBytes / 1MB, 2)
    Write-EasyLog "EasyClean concluido. Itens processados: $processed. Espaco liberado (estimado): $freedMB MB. Simulacao: $Simulation"

    return [PSCustomObject]@{
        ProcessedItems = $processed
        FreedBytes     = $freedBytes
        Simulation     = $Simulation
    }
}

function Get-EasyDiskInfo {
    $drive = Get-PSDrive -Name C -ErrorAction SilentlyContinue
    if (-not $drive) {
        return $null
    }
    return [PSCustomObject]@{
        Name    = $drive.Name
        Used    = $drive.Used
        Free    = $drive.Free
        Total   = $drive.Used + $drive.Free
        UsedPct = [math]::Round(($drive.Used / ($drive.Used + $drive.Free)) * 100, 1)
        FreePct = [math]::Round(($drive.Free / ($drive.Used + $drive.Free)) * 100, 1)
    }
}

function Get-EasyLogs {
    param(
        [int]$Last = 200
    )
    if (-not (Test-Path $global:EasyPaths.LogFile)) {
        return @()
    }
    Get-Content -Path $global:EasyPaths.LogFile -Tail $Last
}

function Set-EasySchedule {
    param(
        [bool]$Enabled,
        [string]$Time = '03:00'
    )

    $taskName = 'EasyCleanEnterprise'
    $launcher = Join-Path $global:EasyPaths.Root '..\launcher.bat'

    if (-not (Test-Path $launcher)) {
        Write-EasyLog "Launcher nao encontrado para agendamento: $launcher" 'WARN'
        return
    }

    if (-not (Get-IsAdmin)) {
        Write-EasyLog 'Tentativa de configurar agendamento sem privilegios administrativos.' 'WARN'
        return
    }

    if (-not $Enabled) {
        schtasks /Delete /TN $taskName /F 2>&1 | Out-Null
        Write-EasyLog "Agendamento desabilitado (tarefa removida) se existente."
        return
    }

    $timePart = $Time
    schtasks /Create /SC DAILY /TN $taskName /TR "`"$launcher`"" /ST $timePart /F 2>&1 | Out-Null
    Write-EasyLog "Agendamento diario configurado para $timePart."
}
