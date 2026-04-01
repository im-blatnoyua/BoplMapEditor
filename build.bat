@echo off
setlocal enabledelayedexpansion

echo ===== Bopl Battle Map Editor - Build =====
echo.

:: 1. Check dotnet
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] dotnet SDK not found.
    echo Download and install: https://dotnet.microsoft.com/download
    pause & exit /b 1
)
echo [OK] dotnet found

:: 2. Find game folder (needed before build to populate libs/)
set "GAME_DIR="

:: Try Steam path from registry first (most reliable)
for /f "tokens=2*" %%a in ('reg query "HKLM\SOFTWARE\WOW6432Node\Valve\Steam" /v "InstallPath" 2^>nul') do set "STEAM_REG=%%b"
if not defined STEAM_REG (
    for /f "tokens=2*" %%a in ('reg query "HKLM\SOFTWARE\Valve\Steam" /v "InstallPath" 2^>nul') do set "STEAM_REG=%%b"
)
if defined STEAM_REG (
    if exist "!STEAM_REG!\steamapps\common\Bopl Battle\BoplBattle.exe" (
        set "GAME_DIR=!STEAM_REG!\steamapps\common\Bopl Battle"
    )
)

:: If not found via registry, check Steam library folders config
if not defined GAME_DIR (
    if defined STEAM_REG (
        if exist "!STEAM_REG!\steamapps\libraryfolders.vdf" (
            for /f "tokens=2 delims=	" %%L in ('findstr /i "\"path\"" "!STEAM_REG!\steamapps\libraryfolders.vdf"') do (
                set "LIB_PATH=%%~L"
                set "LIB_PATH=!LIB_PATH:\\=\!"
                if exist "!LIB_PATH!\steamapps\common\Bopl Battle\BoplBattle.exe" (
                    if not defined GAME_DIR set "GAME_DIR=!LIB_PATH!\steamapps\common\Bopl Battle"
                )
            )
        )
    )
)

:: Fallback: common hardcoded paths
if not defined GAME_DIR (
    for %%D in (C D E F G) do (
        if not defined GAME_DIR (
            if exist "%%D:\Steam\steamapps\common\Bopl Battle\BoplBattle.exe" (
                set "GAME_DIR=%%D:\Steam\steamapps\common\Bopl Battle"
            )
        )
        if not defined GAME_DIR (
            if exist "%%D:\Program Files (x86)\Steam\steamapps\common\Bopl Battle\BoplBattle.exe" (
                set "GAME_DIR=%%D:\Program Files (x86)\Steam\steamapps\common\Bopl Battle"
            )
        )
        if not defined GAME_DIR (
            if exist "%%D:\Program Files\Steam\steamapps\common\Bopl Battle\BoplBattle.exe" (
                set "GAME_DIR=%%D:\Program Files\Steam\steamapps\common\Bopl Battle"
            )
        )
    )
)

:: Manual input if still not found
if not defined GAME_DIR (
    echo.
    echo Game not found automatically. Enter path manually.
    echo Example: C:\Program Files (x86)\Steam\steamapps\common\Bopl Battle
    echo.
    set /p "GAME_DIR=Path to game folder: "
    if not exist "!GAME_DIR!\BoplBattle.exe" (
        echo [ERROR] BoplBattle.exe not found at the specified path.
        pause & exit /b 1
    )
)

echo [OK] Game found: %GAME_DIR%

:: 3. Check BepInEx
if not exist "%GAME_DIR%\BepInEx\core" (
    echo.
    echo [ERROR] BepInEx is not installed or was not run yet.
    echo 1. Download BepInEx 5.x Mono x64 from:
    echo    https://github.com/BepInEx/BepInEx/releases
    echo 2. Extract to: %GAME_DIR%
    echo 3. Run the game once, then close it
    echo 4. Run this script again
    pause & exit /b 1
)

:: 4. Populate libs/ from game and BepInEx
echo Copying DLLs to libs/...
cd /d "%~dp0"
if not exist "libs" mkdir libs

set "MANAGED=%GAME_DIR%\BoplBattle_Data\Managed"
set "BEPINEX_CORE=%GAME_DIR%\BepInEx\core"

set "ALL_OK=1"
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
    if exist "%MANAGED%\%%F" (
        copy /y "%MANAGED%\%%F" "libs\%%F" >nul
    ) else (
        echo [WARN] Not found in Managed: %%F
        set "ALL_OK=0"
    )
)

for %%F in (BepInEx.dll 0Harmony.dll) do (
    if exist "%BEPINEX_CORE%\%%F" (
        copy /y "%BEPINEX_CORE%\%%F" "libs\%%F" >nul
    ) else (
        echo [WARN] Not found in BepInEx\core: %%F
        set "ALL_OK=0"
    )
)

if "!ALL_OK!"=="0" (
    echo.
    echo [ERROR] Some DLLs were not found. Check warnings above.
    pause & exit /b 1
)
echo [OK] libs/ populated

:: 5. Build
echo Building mod...
dotnet build -c Release
if errorlevel 1 (
    echo.
    echo [ERROR] Build failed. See errors above.
    pause & exit /b 1
)

:: 6. Ensure plugins folder exists
if not exist "%GAME_DIR%\BepInEx\plugins" mkdir "%GAME_DIR%\BepInEx\plugins"

:: 7. Copy DLL to plugins
copy /y "%~dp0bin\Release\net471\BoplMapEditor.dll" "%GAME_DIR%\BepInEx\plugins\" >nul
if errorlevel 1 (
    echo [ERROR] Failed to copy DLL to plugins folder.
    pause & exit /b 1
)

echo.
echo ================================================
echo  Done! Mod installed:
echo  %GAME_DIR%\BepInEx\plugins\BoplMapEditor.dll
echo ================================================
echo.
pause
