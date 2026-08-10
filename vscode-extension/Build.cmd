@echo off
setlocal EnableExtensions EnableDelayedExpansion

pushd "%~dp0" || exit /b 1

echo Building HelpService VS Code extension...
if not exist "node_modules\.bin\vsce.cmd" (
    echo Installing build dependencies...
    call npm ci
    if errorlevel 1 (
        set "BUILD_EXIT_CODE=!ERRORLEVEL!"
        echo.
        echo Dependency installation failed with exit code !BUILD_EXIT_CODE!.
        popd
        exit /b !BUILD_EXIT_CODE!
    )
)

call npm run package
set "BUILD_EXIT_CODE=%ERRORLEVEL%"

if not "%BUILD_EXIT_CODE%"=="0" (
    echo.
    echo Build failed with exit code %BUILD_EXIT_CODE%.
    popd
    exit /b %BUILD_EXIT_CODE%
)

echo.
echo Build completed successfully:
for %%F in (peterspoenemann-helpservice-preview-*.vsix) do echo   %%~fF

popd
exit /b 0
