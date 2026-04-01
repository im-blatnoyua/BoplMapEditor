===== Bopl Battle Map Editor Mod =====

--- ТРЕБОВАНИЯ ---
- BepInEx 5.x (Mono) для Unity
- .NET Framework 4.6 (для компиляции)
- Visual Studio 2022 или Rider (Windows)

--- УСТАНОВКА BepInEx ---
1. Скачать BepInEx 5.x Mono x64: https://github.com/BepInEx/BepInEx/releases
2. Распаковать в папку с игрой:
   C:\...\steamapps\common\Bopl Battle\
3. Запустить игру один раз — BepInEx создаст папки (BepInEx\plugins, BepInEx\config)
4. Закрыть игру

--- КОМПИЛЯЦИЯ МОДА ---
1. Скопировать папку BoplMapEditor на Windows
2. Скопировать DLL-ки из папки игры в папку рядом с проектом:
   ../Managed/  ← Assembly-CSharp.dll, UnityEngine*.dll, com.rlabrecque.steamworks.net.dll
   ../BepInEx/core/  ← BepInEx.dll, 0Harmony.dll
3. Открыть BoplMapEditor.csproj в Visual Studio / Rider
4. Build → Release
5. Скопировать BoplMapEditor.dll в:
   C:\...\Bopl Battle\BepInEx\plugins\

--- ИСПОЛЬЗОВАНИЕ ---
1. Запустить игру с BepInEx
2. Зайти в лобби
3. Нажать F5 — откроется выбор кастомных карт
4. "+ Open Map Editor" — открыть редактор
5. В редакторе:
   - Select  — выделить/двигать/ресайзить платформу
   - Place   — кликнуть на канвас для добавления платформы
   - Delete  — кликнуть на платформу для удаления
   - Scroll  — зум
   - Средняя кнопка мыши — пан
6. Ввести имя карты, нажать Save
7. "Push to Lobby" — синхронизировать карту с друзьями
8. Нажать Start — все игроки с модом загрузят вашу карту

--- ФАЙЛЫ КАРТ ---
Сохраняются в: C:\Users\<user>\AppData\Roaming\BepInEx\config\CustomMaps\

--- МУЛЬТИПЛЕЕР ---
Все игроки должны иметь установленный мод.
Хост выбирает карту → автоматически синхронизируется через Steam lobby metadata.
