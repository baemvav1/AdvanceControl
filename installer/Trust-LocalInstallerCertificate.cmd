@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
echo.
echo Ejecutando confianza del certificado...
echo Log: "%SCRIPT_DIR%Trust-LocalInstallerCertificate.log"
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Trust-LocalInstallerCertificate.ps1" %*
set "EXITCODE=%errorlevel%"
echo.
if not "%EXITCODE%"=="0" (
    echo La importacion del certificado termino con error. Revisa el log indicado arriba.
) else (
    echo La importacion del certificado termino correctamente.
)
pause
exit /b %EXITCODE%
