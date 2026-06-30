@echo off
REM Builds the WidgX self-contained app and packages it into a single per-user
REM MSI installer (no administrator rights required to install).
REM
REM Requires the free WiX v5 toolset as a .NET global tool:
REM     dotnet tool install --global wix --version 5.0.2
setlocal
set "ROOT=%~dp0.."
set "VERSION=1.0.0"
set "OUT=%ROOT%\dist\WidgX-%VERSION%-win-x64.msi"

echo Building self-contained app ...
call "%ROOT%\rebuild.bat" || exit /b 1

echo.
echo Packaging installer -^> %OUT% ...
if not exist "%ROOT%\dist" mkdir "%ROOT%\dist"
wix build "%ROOT%\installer\WidgX.wxs" -arch x64 -d "PublishDir=%ROOT%\build" -o "%OUT%"
if errorlevel 1 ( echo. & echo INSTALLER BUILD FAILED. & exit /b 1 )

echo.
echo Done. Installer: %OUT%
endlocal
