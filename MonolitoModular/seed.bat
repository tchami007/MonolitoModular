@echo off
setlocal

set SEED_URL=http://localhost:5119/api/seed
set INFO_URL=http://localhost:5119/api/seed

echo.
echo  Monolito Modular - Seed de datos
echo  ----------------------------------------
echo  Endpoint: POST %SEED_URL%
echo  ----------------------------------------
echo.

:: Verificar que curl este disponible
where curl >nul 2>&1
if %errorlevel% neq 0 (
    echo  [ERROR] curl no encontrado. Instala curl o usa Windows 10/11 que lo incluye.
    pause
    exit /b 1
)

:: Verificar que la app este corriendo
echo  Verificando que la aplicacion este activa...
curl -s -o nul -w "%%{http_code}" %INFO_URL% > %TEMP%\statuscode.txt 2>&1
set /p STATUS=<%TEMP%\statuscode.txt

if "%STATUS%" neq "200" (
    echo.
    echo  [ERROR] La aplicacion no responde en %INFO_URL%
    echo  Asegurate de ejecutar run.bat primero.
    echo.
    pause
    exit /b 1
)

echo  Aplicacion activa. Disparando seed...
echo.

:: Ejecutar el seed
curl -s -X POST %SEED_URL% -H "Content-Type: application/json" | powershell -Command "$input | ConvertFrom-Json | ConvertTo-Json -Depth 5"

echo.
echo  ----------------------------------------
echo  Seed completado.
echo  Podés verificar los datos en:
echo    GET http://localhost:5119/api/clientes
echo    GET http://localhost:5119/api/creditos
echo    GET http://localhost:5119/api/cuentas-ahorro
echo  ----------------------------------------
echo.

pause
endlocal
