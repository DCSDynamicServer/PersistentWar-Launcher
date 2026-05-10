@echo off
setlocal EnableExtensions EnableDelayedExpansion

title DCS War Launcher Update

set "APP_PROCESS=DcsWarLauncher.exe"
set "START_DIR=%~dp0"
set "REPO_DIR=%START_DIR%"
set "DOTNET_EXE=dotnet"

echo.
echo === DCS War Launcher Update ===
echo Script: %~f0
echo.

where git >nul 2>nul
if errorlevel 1 (
    echo ERROR: git was not found in PATH.
    goto failed
)

where dotnet >nul 2>nul
if errorlevel 1 (
    if exist "%ProgramFiles%\dotnet\dotnet.exe" (
        set "DOTNET_EXE=%ProgramFiles%\dotnet\dotnet.exe"
    ) else (
        echo ERROR: dotnet was not found in PATH.
        goto failed
    )
)

:find_repo
if exist "%REPO_DIR%.git\" goto repo_found
for %%I in ("%REPO_DIR%..") do set "NEXT_DIR=%%~fI\"
if /I "%NEXT_DIR%"=="%REPO_DIR%" (
    echo ERROR: Could not find .git folder above "%START_DIR%".
    echo Run this from a checkout of the launcher repository.
    goto failed
)
set "REPO_DIR=%NEXT_DIR%"
goto find_repo

:repo_found
set "PROJECT_FILE=%REPO_DIR%src\DcsWarLauncher\DcsWarLauncher.csproj"
if not exist "%PROJECT_FILE%" (
    echo ERROR: Project file not found:
    echo %PROJECT_FILE%
    goto failed
)

pushd "%REPO_DIR%" >nul
if errorlevel 1 (
    echo ERROR: Could not enter repository folder:
    echo %REPO_DIR%
    goto failed
)

for /f "delims=" %%B in ('git rev-parse --abbrev-ref HEAD') do set "BRANCH=%%B"
if not "%~1"=="" set "BRANCH=%~1"

echo Repository: %REPO_DIR%
echo Branch:     %BRANCH%
echo.

echo Fetching latest changes...
git fetch origin
if errorlevel 1 goto failed

echo Switching branch...
git switch "%BRANCH%"
if errorlevel 1 goto failed

echo Pulling latest version...
git pull --ff-only origin "%BRANCH%"
if errorlevel 1 goto failed

tasklist /FI "IMAGENAME eq %APP_PROCESS%" 2>nul | find /I "%APP_PROCESS%" >nul
if not errorlevel 1 (
    echo Stopping running launcher...
    taskkill /IM "%APP_PROCESS%" /F
    if errorlevel 1 goto failed
)

echo Building launcher...
"%DOTNET_EXE%" build "%PROJECT_FILE%" -c Release
if errorlevel 1 goto failed

set "APP_EXE=%REPO_DIR%src\DcsWarLauncher\bin\Release\net8.0\DcsWarLauncher.exe"
set "APP_DIR=%REPO_DIR%src\DcsWarLauncher\bin\Release\net8.0"
if not exist "%APP_EXE%" (
    echo ERROR: Built launcher executable not found:
    echo %APP_EXE%
    goto failed
)

echo Starting launcher...
start "" /D "%APP_DIR%" "%APP_EXE%"

echo.
echo Update complete.
echo Launcher started from:
echo %APP_EXE%
echo Working directory:
echo %APP_DIR%
echo.
popd >nul
pause
exit /b 0

:failed
echo.
echo Update failed.
echo Check the message above and try again.
echo.
popd >nul 2>nul
pause
exit /b 1
