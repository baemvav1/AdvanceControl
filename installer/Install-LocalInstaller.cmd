@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
echo.
echo Ejecutando instalacion local...
echo Log: "%SCRIPT_DIR%Install-LocalInstaller.log"
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Install-LocalInstaller.ps1" %*
set "EXITCODE=%errorlevel%"
echo.
if not "%EXITCODE%"=="0" (
    echo La instalacion termino con error. Revisa el log indicado arriba.
) else (
    echo La instalacion termino sin errores en el launcher.
)
pause
exit /b %EXITCODE%
