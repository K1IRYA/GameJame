# Архив реализации Ropfire_develop до слияния main

Исходная ветка: Ropfire_develop, коммит 26cbd7e.
Слияние: main 5ede188, выполнено по явному запросу пользователя.

Старые скрипты дополнительно сохранены здесь как `.cs.txt`, вне Assets и компиляции Unity. Для классов mainmenu/pause/UnitsController активны только новые компоненты main, чтобы не было дубликатов. Совместимые BuildingMenu/BuildSetting/Building/BuildManager/ButtonBuildSetting сохранены в Assets со своими исходными GUID: они нужны прежним префабам и кнопкам HUD. Сходство метаданных в git rename detection не означает коллизию GUID; проверка реальных GUID дубликатов не выявила.

Для конфликтующих MainScene, модели tower1, метаданных, TagManager, EditorBuildSettings и package-lock выбран согласованный комплект main. Исправление камеры Ropfire_develop (не двигать камеру при pause.isPaused) сохранено автоматическим слиянием.

Полная предыдущая сцена и ассеты доступны без потери в истории Git: `git show 26cbd7e:Assets/Scenes/MainScene.unity`. Старая MainScene не добавляется второй активной копией: её размещение и зависимости остаются в истории. Схема слоёв старой ветки Ground=6/Building=7 отличается от main (Unit=6/Ground=7/Building=9/Enemy=10). Старые prefab-only слои мигрируются отдельно, современная MainScene берётся целиком из main.

В relocated-asset-paths.txt перечислены убранные старые пути ассетов с повторяющимися GUID. Их канонические копии из main остаются в Assets/Models/Art/UI, Assets/Prefabs/Flora, Assets/Prefabs/Builds и Assets/Materials. GUID ссылок сохранены; дубли удалены до запуска импорта Unity.

Старый незакоммиченный WIP Enemy_System на Ropfire_develop хранится в отдельном git stash с сообщением `Ropfire_develop: enemy AI WIP (auto-stash before main)`. Он не применён поверх новой Battle-системы.
