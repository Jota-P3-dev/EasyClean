# Script de Limpeza Direta EasyClean
$ErrorActionPreference = 'Continue'

$targets = @(
    "$env:TEMP\*",
    "C:\Windows\Temp\*",
    "$env:LOCALAPPDATA\Temp\*",
    "C:\Windows\Prefetch\*"
)

Write-Host "Iniciando limpeza profunda..."
$totalFreed = 0

foreach ($path in $targets) {
    try {
        $files = Get-ChildItem -Path $path -Recurse -File -ErrorAction SilentlyContinue
        foreach ($file in $files) {
            try {
                $size = $file.Length
                Remove-Item -LiteralPath $file.FullName -Force -ErrorAction Stop
                $totalFreed += $size
                Write-Host "Removido: $($file.Name)"
            }
            catch {
                # Arquivo em uso ou sem permissão - ignora silenciosamente
            }
        }
    }
    catch { }
}

$freedMB = [math]::Round($totalFreed / 1MB, 2)
Write-Host "Limpeza concluída! Liberado: $freedMB MB"
