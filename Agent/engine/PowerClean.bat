@echo off
setlocal enabledelayedexpansion

echo =======================================================
echo          EASYCLEAN - MANUTENCAO PROFUNDA E AGRESSIVA
echo =======================================================
echo Data: %date% %time%
echo Usuario: %username%
echo Drive do Sistema: %systemdrive%
echo.

echo [1/5] Limpeza de arquivos temporarios...
:: Tenta limpar mas reporta se falhar
del /s /f /q "%temp%\*.*"
del /s /f /q "%systemdrive%\Windows\Temp\*.*"
rd /s /q %systemdrive%\$Recycle.bin 2>nul

echo.
echo [2/5] Cache Windows Update e DNS...
ipconfig /flushdns
net stop wuauserv 2>nul
del /f /s /q %systemdrive%\Windows\SoftwareDistribution\Download\*.* 2>nul
net start wuauserv 2>nul

echo.
echo [3/5] Configurando Limpeza de Disco Nativa (Cleanmgr)...
:: Habilita opcoes de limpeza no registro
set "regPath=HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches"
for /f "tokens=*" %%a in ('reg query "%regPath%"') do (
    reg add "%%a" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
)

echo.
echo [4/5] Executando Cleanmgr silencioso...
cleanmgr /sagerun:10

echo.
echo [5/5] Otimizacao de Unidade (%systemdrive%)...
:: Verifica se o usuário tem privilégios de Admin para o defrag
openfiles >nul 2>&1
if %errorlevel% neq 0 (
    echo ERRO: O comando DEFRAG requer privilegios de Administrador elevados.
) else (
    echo Analisando e Otimizando unidade...
    defrag %systemdrive% /O /U /V
    if %errorlevel% neq 0 (
        echo AVISO: O comando defrag retornou codigo %errorlevel%. 
        echo Verifique se o servico 'Otimizar Unidades' esta ativado.
    )
)

echo.
echo SUCESSO: Otimizacao estrutural concluida!
exit /b 0
