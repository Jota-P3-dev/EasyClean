param(
    [string]$Root,
    [string]$SimulationFlag = '1',
    [string]$SelectedIdsCsv = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$engineRoot = if ($Root) { $Root } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
. (Join-Path $engineRoot 'EasyClean.Core.ps1') -RootPath $engineRoot

$simulation = $SimulationFlag -eq '1'
$selectedIds = @()
if ($SelectedIdsCsv) {
    $selectedIds = @($SelectedIdsCsv -split ',' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

Write-EasyLog "Worker iniciado. Simulacao: $simulation. Alvos: $($selectedIds -join ',')"

try {
    Invoke-EasyClean -Simulation:$simulation -SelectedTargetIds $selectedIds | Out-Null
    Write-EasyLog 'Worker finalizado com sucesso.'
}
catch {
    Write-EasyLog "Worker falhou: $($_.Exception.Message)" 'ERROR'
    Initialize-EasyProgress -Status 'idle'
}
