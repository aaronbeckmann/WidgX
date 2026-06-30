@echo off
REM Builds the WidgX self-contained app and packages it into a single per-user
REM MSI installer (no administrator rights required to install).
REM
REM Requires the free WiX v5 toolset (and its UI extension) as .NET global tools:
REM     dotnet tool install --global wix --version 5.0.2
REM     wix extension add -g WixToolset.UI.wixext/5.0.2
setlocal
set "ROOT=%~dp0.."
set "VERSION=1.0.0"
set "OUT=%ROOT%\dist\WidgX-%VERSION%-win-x64.msi"

echo Building self-contained app ...
call "%ROOT%\rebuild.bat" || exit /b 1

echo.
echo Packaging installer -^> %OUT% ...
if not exist "%ROOT%\dist" mkdir "%ROOT%\dist"
wix build "%ROOT%\installer\WidgX.wxs" -arch x64 -ext WixToolset.UI.wixext ^
    -d "PublishDir=%ROOT%\build" -d "LicenseRtf=%ROOT%\installer\license.rtf" -o "%OUT%"
if errorlevel 1 ( echo. & echo INSTALLER BUILD FAILED. & exit /b 1 )

echo.
echo Done. Installer: %OUT%
endlocal
