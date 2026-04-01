===== Bopl Battle Map Editor Mod =====

--- ТРЕБОВАНИЯ ---
- Игра Bopl Battle (Steam)
- BepInEx 5.x (Mono) для Unity
- .NET SDK (https://dotnet.microsoft.com/download)

--- БЫСТРАЯ УСТАНОВКА (готовый DLL) ---
1. Установить BepInEx (см. ниже)
2. Скопировать BoplMapEditor.dll в папку:
   <папка игры>\BepInEx\plugins\
3. Запустить игру

--- УСТАНОВКА BepInEx ---
1. Скачать BepInEx 5.x (Mono) x64:
   https://github.com/BepInEx/BepInEx/releases
   (файл: BepInEx_win_x64_5.x.x.x.zip)
2. Распаковать содержимое архива прямо в папку игры:
   <папка игры>\
   После распаковки там должны появиться папки BepInEx\ и файл winhttp.dll
3. Запустить игру один раз — BepInEx создаст нужные папки
4. Закрыть игру

--- КОМПИЛЯЦИЯ ИЗ ИСХОДНИКОВ ---

Требования:
  - .NET SDK (https://dotnet.microsoft.com/download)
  - Установленная игра Bopl Battle
  - Установленный BepInEx (см. выше)

Сборка (Windows):
  1. Запустить build.bat
  Скрипт автоматически:
    - Найдёт папку игры (через реестр Steam или перебор дисков)
    - Скопирует нужные DLL из игры и BepInEx в папку libs\
    - Скомпилирует мод
    - Установит BoplMapEditor.dll в BepInEx\plugins\

  Если игра установлена в нестандартное место, скрипт попросит
  ввести путь вручную.

Сборка через командную строку (после того как libs\ заполнена):
  dotnet build -c Release
  Готовый DLL: bin\Release\net471\BoplMapEditor.dll

Сборка через Visual Studio / Rider:
  1. Открыть BoplMapEditor.csproj
  2. Сначала запустить build.bat один раз (он заполнит libs\)
  3. Выбрать конфигурацию Release
  4. Build -> Build Solution
  5. Вручную скопировать DLL в <папка игры>\BepInEx\plugins\

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
