===== Bopl Battle Map Editor Mod =====

--- ТРЕБОВАНИЯ ---
- Игра Bopl Battle (Steam)
- BepInEx 5.x (Mono) для Unity
- .NET SDK 4.7.1+ или Visual Studio 2022 (для компиляции)

--- БЫСТРАЯ УСТАНОВКА (готовый DLL) ---
1. Установить BepInEx (см. ниже)
2. Скопировать BoplMapEditor.dll в папку:
   C:\...\steamapps\common\Bopl Battle\BepInEx\plugins\
3. Запустить игру

--- УСТАНОВКА BepInEx ---
1. Скачать BepInEx 5.x (Mono) x64:
   https://github.com/BepInEx/BepInEx/releases
   (файл: BepInEx_win_x64_5.x.x.x.zip)
2. Распаковать содержимое архива прямо в папку игры:
   C:\...\steamapps\common\Bopl Battle\
   После распаковки там должны появиться папки BepInEx\ и файл winhttp.dll
3. Запустить игру один раз — BepInEx создаст нужные папки (BepInEx\plugins\ и др.)
4. Закрыть игру
5. Скопировать BoplMapEditor.dll в:
   C:\...\steamapps\common\Bopl Battle\BepInEx\plugins\

--- КОМПИЛЯЦИЯ ИЗ ИСХОДНИКОВ ---

Требования:
  - .NET SDK (https://dotnet.microsoft.com/download) — достаточно для сборки без IDE
  - ИЛИ Visual Studio 2022 / JetBrains Rider

Структура папок (относительно исходников мода):
  Папки с зависимостями должны лежать НА ОДИН УРОВЕНЬ ВЫШЕ папки BoplMapEditor:

  ParentFolder\
    BoplMapEditor\       <- папка с исходниками (эта папка)
    Managed\             <- DLL из игры
    BepInEx\
      core\              <- DLL из BepInEx

Откуда брать DLL:

  Managed\ — копировать из папки игры:
    C:\...\steamapps\common\Bopl Battle\BoplBattle_Data\Managed\
    Нужные файлы:
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

  BepInEx\core\ — копировать из установленного BepInEx:
    C:\...\steamapps\common\Bopl Battle\BepInEx\core\
    Нужные файлы:
      BepInEx.dll
      0Harmony.dll

Сборка через командную строку (Windows, Linux, macOS):
  cd BoplMapEditor
  dotnet build -c Release
  Готовый DLL: bin\Release\net471\BoplMapEditor.dll

Сборка через Visual Studio / Rider:
  1. Открыть BoplMapEditor.csproj
  2. Выбрать конфигурацию Release
  3. Build -> Build Solution
  4. Готовый DLL: bin\Release\net471\BoplMapEditor.dll

После сборки скопировать DLL в:
  C:\...\steamapps\common\Bopl Battle\BepInEx\plugins\

--- ИСПОЛЬЗОВАНИЕ ---
1. Запустить игру с установленным BepInEx и модом
2. Зайти в лобби (локальное или онлайн)
3. Нажать кнопку "Map Editor" в нижнем левом углу экрана
4. Откроется браузер карт:
   - Дефолтные карты: Classic, Bridge, Space Arena, Snow Ladder, Chaos
   - Кнопка "+ New Map" — создать новую карту (ввести название)
   - Кнопка "Import Level" — захватить текущий загруженный уровень игры
   - Клик на карточку — открыть карту в редакторе
5. В редакторе:
   - Вкладка "Platforms": размещение и настройка платформ
   - Вкладка "Environment": физика, гравитация, вода, трение
   - Инструменты: Select / Place / Delete
   - Типы платформ: Grass / Snow / Ice / Space / Robot / Slime
   - Движение платформы: Linear / Circle / Path (настройка в боковой панели)
   - Колесо мыши — зум, правая кнопка мыши — пан камеры
6. Сохранить карту, нажать "Push to Lobby"
7. Нажать Start — все игроки с установленным модом загрузят карту

--- ФАЙЛЫ КАРТ ---
Карты хранятся в:
  C:\Users\<user>\AppData\Roaming\BepInEx\config\CustomMaps\
Формат: JSON (.json)

--- МУЛЬТИПЛЕЕР ---
- Все игроки в лобби должны иметь установленный мод
- Хост выбирает карту и нажимает "Push to Lobby"
- Карта синхронизируется через метаданные Steam-лобби автоматически
- Игроки без мода не смогут зайти в игру с кастомной картой
