@echo off
setlocal

set "SRC=%~dp0"
set "WORK=%TEMP%\VideoClientWindowsSetup_%RANDOM%%RANDOM%"
mkdir "%WORK%" >nul 2>nul
xcopy "%SRC%*" "%WORK%\" /E /I /Y >nul

set "PS=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if exist "%SystemRoot%\Sysnative\WindowsPowerShell\v1.0\powershell.exe" set "PS=%SystemRoot%\Sysnative\WindowsPowerShell\v1.0\powershell.exe"

"%PS%" -NoProfile -ExecutionPolicy Bypass -Command "$p = Start-Process -FilePath '%PS%' -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File','%WORK%\install-elevated.ps1') -WorkingDirectory '%WORK%' -Verb RunAs -PassThru -Wait; exit $p.ExitCode"
set "RC=%ERRORLEVEL%"
exit /b %RC%
