@echo off
setlocal

set PROJECT=src\Host\MonolitoModular.API\MonolitoModular.API.csproj
set SWAGGER_URL=http://localhost:5119/swagger

echo.
echo  Monolito Modular
echo  ----------------------------------------
echo  Swagger UI: %SWAGGER_URL%
echo  ----------------------------------------
echo.

:: Abrir browser con un pequeño delay para que el servidor levante
start "" timeout /t 3 >nul & start "" "%SWAGGER_URL%"

dotnet run --project "%~dp0%PROJECT%" --launch-profile http

endlocal
