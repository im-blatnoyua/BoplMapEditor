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

:: 2. Build
echo Building mod...
cd /d "%~dp0"
dotnet build -c Release
if errorlevel 1 (
    echo.
    echo [ERROR] Build failed. See errors above.
    pause & exit /b 1
)

:: 3. Find game folder
set "GAME_DIR="

if exist "C:\Program Files (x86)\Steam\steamapps\common\Bopl Battle\BoplBattle.exe" (
    set "GAME_DIR=C:\Program Files (x86)\Steam\steamapps\common\Bopl Battle"
)
if not defined GAME_DIR (
    if exist "D:\Steam\steamapps\common\Bopl Battle\BoplBattle.exe" (
        set "GAME_DIR=D:\Steam\steamapps\common\Bopl Battle"
    )
)
if not defined GAME_DIR (
    if exist "D:\Program Files (x86)\Steam\steamapps\common\Bopl Battle\BoplBattle.exe" (
        set "GAME_DIR=D:\Program Files (x86)\Steam\steamapps\common\Bopl Battle"
    )
)
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

:: 4. Check BepInEx
if not exist "%GAME_DIR%\BepInEx\plugins" (
    echo.
    echo [ERROR] BepInEx is not installed.
    echo Download from https://github.com/BepInEx/BepInEx/releases
    echo Extract to: %GAME_DIR%
    echo Run the game once, close it, then run this script again.
    pause & exit /b 1
)

:: 5. Copy DLL to plugins
copy /y "%~dp0bin\Release\net471\BoplMapEditor.dll" "%GAME_DIR%\BepInEx\plugins\" >nul

echo.
echo ================================================
echo  Done! Mod installed:
echo  %GAME_DIR%\BepInEx\plugins\BoplMapEditor.dll
echo ================================================
echo.
pause
