@echo off
setlocal
cd /d "%~dp0"
if exist "%~dp0bin\Debug\net8.0-windows\PassTypePro.exe" start "" "%~dp0bin\Debug\net8.0-windows\PassTypePro.exe" & exit /b 0
if exist "%~dp0bin\Release\net8.0-windows\win-x64\PassTypePro.exe" start "" "%~dp0bin\Release\net8.0-windows\win-x64\PassTypePro.exe" & exit /b 0
if exist "%~dp0artifacts\v0.2.3\publish\win-x64\PassTypePro.exe" start "" "%~dp0artifacts\v0.2.3\publish\win-x64\PassTypePro.exe" & exit /b 0
start "PassTypePro" dotnet run --project "%~dp0PassTypePro.csproj"
endlocal
