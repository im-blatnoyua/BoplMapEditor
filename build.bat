@echo off
setlocal enabledelayedexpansion

echo ===== Bopl Battle Map Editor - Build =====
echo.

:: 1. Check dotnet
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] dotnet SDK not found.
    pause
    exit /b 1
)
echo [OK] dotnet found

:: 2. Find game folder
set "GAME_DIR="

for /f "tokens=2*" %%a in ('reg query "HKLM\SOFTWARE\WOW6432Node\Valve\Steam" /v "InstallPath" 2^>nul') do set "STEAM_REG=%%b"

if defined STEAM_REG (
    if exist "!STEAM_REG!\steamapps\common\Bopl Battle\BoplBattle.exe" (
        set "GAME_DIR=!STEAM_REG!\steamapps\common\Bopl Battle"
    )
)

if not defined GAME_DIR (
    echo Game not found automatically.
    set /p "GAME_DIR=Enter full path to game folder: "
)

if not exist "!GAME_DIR!\BoplBattle.exe" (
    echo [ERROR] BoplBattle.exe not found at: !GAME_DIR!
    pause
    exit /b 1
)

echo [OK] Game found: !GAME_DIR!

:: 3. Check BepInEx
if not exist "!GAME_DIR!\BepInEx\core" (
    echo [ERROR] BepInEx not found. Install BepInEx 5.x and run the game once.
    pause
    exit /b 1
)

echo [OK] BepInEx found

:: 4. Copy DLLs to libs/
echo Copying DLLs to libs/...
cd /d "%~dp0"
if not exist "libs" mkdir libs

set "MANAGED=!GAME_DIR!\BoplBattle_Data\Managed"
set "BEPINEX_CORE=!GAME_DIR!\BepInEx\core"

for %%F in (
    UnityEngine.dll
    UnityEngine.CoreModule.dll
    UnityEngine.IMGUIModule.dll
    UnityEngine.InputLegacyModule.dll
    UnityEngine.PhysicsModule.dll
    UnityEngine.UI.dll
    UnityEngine.UIModule.dll
    UnityEngine.JSONSerializeModule.dll
    UnityEngine.TextRenderingModule.dll
    Unity.TextMeshPro.dll
    Assembly-CSharp.dll
    Facepunch.Steamworks.Win64.dll
    netstandard.dll
) do (
    if exist "!MANAGED!\%%F" (
        copy /y "!MANAGED!\%%F" "libs\%%F" >nul
    ) else (
        echo [WARN] Not found in Managed: %%F
    )
)

for %%F in (BepInEx.dll 0Harmony.dll) do (
    if exist "!BEPINEX_CORE!\%%F" (
        copy /y "!BEPINEX_CORE!\%%F" "libs\%%F" >nul
    ) else (
        echo [WARN] Not found in BepInEx\core: %%F
    )
)

echo [OK] libs/ populated

:: 5. Build
echo Building mod...
dotnet build -c Release
if errorlevel 1 (
    echo [ERROR] Build failed
    pause
    exit /b 1
)

:: 6. Install to plugins
if not exist "!GAME_DIR!\BepInEx\plugins" mkdir "!GAME_DIR!\BepInEx\plugins"
copy /y "%~dp0bin\Release\net471\BoplMapEditor.dll" "!GAME_DIR!\BepInEx\plugins\" >nul
if errorlevel 1 (
    echo [ERROR] Failed to copy DLL to plugins
    pause
    exit /b 1
)

echo.
echo [OK] Done! Mod installed to: !GAME_DIR!\BepInEx\plugins\BoplMapEditor.dll
echo.
pause
