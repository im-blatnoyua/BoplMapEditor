===== Bopl Battle Map Editor Mod =====

--- ТРЕБОВАНИЯ ---
- BepInEx 5.x (Mono) для Unity
- .NET Framework 4.6 (для компиляции)
- Visual Studio 2022 или Rider (Windows)

--- УСТАНОВКА BepInEx ---
1. Скачать BepInEx 5.x Mono x64: https://github.com/BepInEx/BepInEx/releases
2. Распаковать в папку с игрой:
   C:\...\steamapps\common\Bopl Battle\
3. Запустить игру один раз — BepInEx создаст папки
4. Закрыть игру

--- КОМПИЛЯЦИЯ МОДА ---
1. Скопировать папку BoplMapEditor на Windows
2. Скопировать DLL из папки игры:
   ../Managed/  <- Assembly-CSharp.dll, UnityEngine*.dll, TMPro.dll,
                   com.rlabrecque.steamworks.net.dll
   ../BepInEx/core/  <- BepInEx.dll, 0Harmony.dll
3. Открыть BoplMapEditor.csproj в Visual Studio / Rider
4. Build -> Release
5. Скопировать BoplMapEditor.dll в:
   C:\...\Bopl Battle\BepInEx\plugins\

--- ИСПОЛЬЗОВАНИЕ ---
1. Запустить игру с BepInEx
2. Зайти в лобби (локальное или онлайн)
3. Нажать кнопку "Map Editor" в нижнем левом углу экрана
4. Откроется браузер карт:
   - Дефолтные карты: Classic, Bridge, Space Arena, Snow Ladder, Chaos
   - Кнопка "+ New Map" — создать новую карту (введи название)
   - Кнопка "Import Level" — захватить текущий загруженный уровень
   - Клик на карточку — открыть карту в редакторе
5. В редакторе:
   - Вкладка "Platforms": размещение и настройка платформ
   - Вкладка "Environment": физика, гравитация, вода, трение
   - Инструменты: Select / Place / Delete
   - Типы блоков: Grass / Snow / Ice / Space / Robot / Slime
   - Движение платформы: Linear / Circle / Path (в боковой панели)
   - Скролл — зум, правая/средняя кнопка мыши — пан
6. Сохранить карту, нажать "Push to Lobby"
7. Start — все игроки с модом загрузят твою карту

--- ФАЙЛЫ КАРТ ---
C:\Users\<user>\AppData\Roaming\BepInEx\config\CustomMaps\

--- МУЛЬТИПЛЕЕР ---
Все игроки должны иметь установленный мод.
Хост выбирает карту -> синхронизируется через Steam lobby metadata.
