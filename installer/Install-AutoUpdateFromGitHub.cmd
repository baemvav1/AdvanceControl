@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
echo.
echo Ejecutando instalacion autoactualizable desde GitHub...
echo Log: "%SCRIPT_DIR%Install-AutoUpdateFromGitHub.log"
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Install-AutoUpdateFromGitHub.ps1" %*
set "EXITCODE=%errorlevel%"
echo.
if not "%EXITCODE%"=="0" (
    echo La instalacion autoactualizable termino con error. Revisa el log indicado arriba.
) else (
    echo La instalacion autoactualizable termino sin errores en el launcher.
)
pause
exit /b %EXITCODE%
