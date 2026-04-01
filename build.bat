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

:: ── 2. Сборка ───────────────────────────────────────────────────────────────
echo Собираю мод...
cd /d "%~dp0"
dotnet build -c Release
if errorlevel 1 (
    echo.
    echo [ОШИБКА] Сборка не удалась. Смотри ошибки выше.
    pause & exit /b 1
)

:: ── 3. Ищем папку игры для установки ────────────────────────────────────────
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
    echo Игра не найдена автоматически. Введи путь вручную.
    echo Пример: C:\Program Files (x86)\Steam\steamapps\common\Bopl Battle
    echo.
    set /p "GAME_DIR=Путь к папке игры: "
    if not exist "!GAME_DIR!\BoplBattle.exe" (
        echo [ОШИБКА] BoplBattle.exe не найден по указанному пути.
        pause & exit /b 1
    )
)

:: ── 4. Проверяем BepInEx ────────────────────────────────────────────────────
if not exist "%GAME_DIR%\BepInEx\plugins" (
    echo.
    echo [ОШИБКА] BepInEx не установлен.
    echo Скачай с https://github.com/BepInEx/BepInEx/releases
    echo Распакуй в: %GAME_DIR%
    echo Запусти игру один раз, закрой, и запусти этот скрипт снова.
    pause & exit /b 1
)

:: ── 5. Копируем DLL в plugins ───────────────────────────────────────────────
copy /y "%~dp0bin\Release\net471\BoplMapEditor.dll" "%GAME_DIR%\BepInEx\plugins\" >nul

echo.
echo ================================================
echo  Готово! Мод установлен:
echo  %GAME_DIR%\BepInEx\plugins\BoplMapEditor.dll
echo ================================================
echo.
pause
