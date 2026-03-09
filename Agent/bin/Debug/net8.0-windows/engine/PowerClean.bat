@echo off
echo =======================================================
echo          EASYCLEAN - MANUTENCAO PROFUNDA E AGRESSIVA
echo =======================================================
echo.
echo [1/5] Limpeza de arquivos temporarios imediatos...
del /s /f /q %temp%\*.* >nul 2>&1
del /s /f /q C:\Windows\Temp\*.* >nul 2>&1
rd /s /q %systemdrive%\$Recycle.bin >nul 2>&1

echo [2/5] Cache Windows Update e DNS...
ipconfig /flushdns >nul 2>&1

echo [3/5] Configurando Limpeza de Disco Nativa (Cleanmgr)...
:: Habilita TODAS as opcoes agressivas de limpeza para o flag 10.
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Active Setup Temp Folders" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\BranchCache" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Downloaded Program Files" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Internet Cache Files" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Memory Dump Files" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Old ChkDsk Files" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Previous Installations" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Recycle Bin" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Setup Log Files" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\System error memory dump files" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\System error minidump files" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Files" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Setup Files" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Thumbnail Cache" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Upgrade Discarded Files" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Defender" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Error Reporting Archive Files" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Delivery Optimization Files" /v StateFlags0010 /t REG_DWORD /d 2 /f >nul 2>&1

echo [4/5] Executando Cleanmgr silencioso (pode demorar varios minutos)...
cleanmgr /sagerun:10 >nul 2>&1

echo [5/5] Analise e Desfragmentacao Basica SSD/HDD...
defrag C: /O /U >nul 2>&1

echo SUCESSO: Otimizacao estrutural concluida!
