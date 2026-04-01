@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul

echo ===== Bopl Battle Map Editor — Сборка мода =====
echo.

:: ── 1. Проверяем dotnet ─────────────────────────────────────────────────────
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ОШИБКА] dotnet SDK не найден.
    echo Скачай и установи: https://dotnet.microsoft.com/download
    pause & exit /b 1
)
echo [OK] dotnet найден

:: ── 2. Ищем папку игры ──────────────────────────────────────────────────────
set "GAME_DIR="

:: Стандартный путь Steam (диск C)
if exist "C:\Program Files (x86)\Steam\steamapps\common\Bopl Battle\BoplBattle.exe" (
    set "GAME_DIR=C:\Program Files (x86)\Steam\steamapps\common\Bopl Battle"
)
:: Steam на диске D
if not defined GAME_DIR (
    if exist "D:\Steam\steamapps\common\Bopl Battle\BoplBattle.exe" (
        set "GAME_DIR=D:\Steam\steamapps\common\Bopl Battle"
    )
)
:: Steam на диске D (Program Files)
if not defined GAME_DIR (
    if exist "D:\Program Files (x86)\Steam\steamapps\common\Bopl Battle\BoplBattle.exe" (
        set "GAME_DIR=D:\Program Files (x86)\Steam\steamapps\common\Bopl Battle"
    )
)

:: Если не нашли — спрашиваем вручную
if not defined GAME_DIR (
    echo [ВНИМАНИЕ] Игра не найдена в стандартных папках Steam.
    echo Укажи путь к папке игры вручную.
    echo Пример: C:\Program Files (x86)\Steam\steamapps\common\Bopl Battle
    echo.
    set /p "GAME_DIR=Путь к папке игры: "
    if not exist "!GAME_DIR!\BoplBattle.exe" (
        echo [ОШИБКА] Файл BoplBattle.exe не найден в указанной папке.
        pause & exit /b 1
    )
)
echo [OK] Игра найдена: %GAME_DIR%

:: ── 3. Проверяем BepInEx ────────────────────────────────────────────────────
if not exist "%GAME_DIR%\BepInEx\core\BepInEx.dll" (
    echo.
    echo [ОШИБКА] BepInEx не установлен в папку игры.
    echo Скачай BepInEx 5.x Mono x64 с https://github.com/BepInEx/BepInEx/releases
    echo и распакуй в: %GAME_DIR%
    echo Затем запусти игру один раз, закрой её, и запусти этот скрипт снова.
    pause & exit /b 1
)
echo [OK] BepInEx найден

:: ── 4. Создаём папки с зависимостями ───────────────────────────────────────
set "SCRIPT_DIR=%~dp0"
set "PARENT_DIR=%SCRIPT_DIR%..\"
set "MANAGED_DST=%PARENT_DIR%Managed"
set "BEPINEX_DST=%PARENT_DIR%BepInEx\core"

if not exist "%MANAGED_DST%" mkdir "%MANAGED_DST%"
if not exist "%BEPINEX_DST%" mkdir "%BEPINEX_DST%"

:: ── 5. Копируем DLL из игры ─────────────────────────────────────────────────
echo Копирую DLL из игры...
set "MANAGED_SRC=%GAME_DIR%\BoplBattle_Data\Managed"

for %%f in (
    Assembly-CSharp.dll
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
    Facepunch.Steamworks.Win64.dll
    netstandard.dll
) do (
    if exist "%MANAGED_SRC%\%%f" (
        copy /y "%MANAGED_SRC%\%%f" "%MANAGED_DST%\" >nul
    ) else (
        echo   [ВНИМАНИЕ] Не найден: %%f
    )
)

:: ── 6. Копируем BepInEx DLL ─────────────────────────────────────────────────
copy /y "%GAME_DIR%\BepInEx\core\BepInEx.dll"  "%BEPINEX_DST%\" >nul
copy /y "%GAME_DIR%\BepInEx\core\0Harmony.dll" "%BEPINEX_DST%\" >nul
echo [OK] DLL скопированы

:: ── 7. Сборка мода ──────────────────────────────────────────────────────────
echo.
echo Собираю мод...
cd /d "%SCRIPT_DIR%"
dotnet build -c Release
if errorlevel 1 (
    echo.
    echo [ОШИБКА] Сборка не удалась. Смотри ошибки выше.
    pause & exit /b 1
)

:: ── 8. Копируем готовый DLL в plugins ───────────────────────────────────────
set "PLUGINS_DIR=%GAME_DIR%\BepInEx\plugins"
if not exist "%PLUGINS_DIR%" mkdir "%PLUGINS_DIR%"

copy /y "%SCRIPT_DIR%bin\Release\net471\BoplMapEditor.dll" "%PLUGINS_DIR%\" >nul
echo.
echo ================================================
echo  Готово! Мод установлен:
echo  %PLUGINS_DIR%\BoplMapEditor.dll
echo ================================================
echo.
pause
