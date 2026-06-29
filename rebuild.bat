@echo off
REM Rebuild WidgX from scratch into the build\ directory as a SELF-CONTAINED
REM distribution: the .NET runtime is downloaded and bundled, so build\WidgX.exe
REM runs on any 64-bit Windows machine without .NET installed.

setlocal
set "ROOT=%~dp0"
set "PROJECT=%ROOT%src\WidgX\WidgX.csproj"
set "OUTDIR=%ROOT%build"

echo Stopping any running WidgX instance ...
taskkill /im WidgX.exe /f >nul 2>&1

echo Cleaning %OUTDIR% ...
if exist "%OUTDIR%" rmdir /s /q "%OUTDIR%"

echo Publishing WidgX (Release, self-contained win-x64) -^> %OUTDIR% ...
echo (First run downloads the .NET runtime packs; this can take a few minutes.)
dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true -o "%OUTDIR%"
if errorlevel 1 (
    echo.
    echo PUBLISH FAILED.
    exit /b 1
)

echo.
echo Done. Self-contained distribution is in %OUTDIR%
echo Run the app with:
echo     "%OUTDIR%\WidgX.exe"
endlocal
