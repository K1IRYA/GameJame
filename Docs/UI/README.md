# UI: главное меню, пауза и выбор построек

Стартовый набор, нарисованный кодом; это не собранные Canvas или работающие меню.
На каждой новой карте игрок строит новую базу. Иконки построек пока заменены
полностью белым непрозрачным квадратом. Иконки ресурсов не входят в текущий набор.

## Состав

- `Assets/Art/UI/Generated/panel_menu.png`, `panel_pause.png`: 256×256, border 28 px.
- `panel_build.png`: 256×128, border 28 px.
- `button_normal/hover/pressed/disabled.png`: 256×64, border 12 px.
- `build_slot_normal/hover/pressed/disabled.png`: 96×96, border 12 px.
- `building_placeholder.png`: 64×64, белый квадрат, border 0.
- `overlay_pause.png`: 8×8, однотонное полупрозрачное затемнение.
- `sprites.json`: размеры и симметричные границы спрайтов, в пикселях.

В PNG нет текста. Русские подписи в `ui_preview.png` — только пример компоновки,
не скриншот из Unity. Название игры на превью вымышленное, не утверждено.

## Использование в Unity 2022.3

`Assets/Editor/GeneratedUISpriteImporter.cs` при импорте только этой папки включает
Sprite (Single), alpha, Bilinear, Clamp, отключает mipmaps и compression, задаёт
границы 9-slice. Если PNG импортировались до компиляции скрипта, после окончания
компиляции выделить папку Generated в Project и выполнить Reimport.

Для панелей и кнопок использовать UI Image с Image Type = Sliced, Fill Center = on.
Не уменьшать панели меньше 56×56, кнопки и слоты — меньше 24×24.
Для надписей использовать отдельный TextMeshProUGUI с шрифтом с кириллицей.
Canvas Scaler: Scale With Screen Size, Reference Resolution 1920×1080 — рекомендуемая
отправная точка, не обязательная настройка проекта.

Для Button выбрать Transition = Sprite Swap: исходное изображение normal,
Highlighted Sprite = hover, Pressed Sprite = pressed, Selected Sprite = hover,
Disabled Sprite = disabled. Для постоянного отображения выбранной постройки
менять исходный спрайт на pressed из логики выбора: состояние Pressed у Button
само по себе не сохраняет выбор.

Внутри кнопки постройки добавить дочерний Image с building_placeholder,
цвет белый, размер, например, 48×48, Raycast Target = off.
Затемнение паузы: растянуть overlay_pause на весь Canvas под окном паузы,
Image Color оставить белым, иначе прозрачность будет дополнительно перемножена.
Raycast Target включить для блокирования мыши через overlay; блокировку игровых
команд и остановку игры реализовать отдельно — спрайт их не выполняет.

## Повторная генерация

Нужны Python 3 и Pillow. Из корня GameJame:

    python Tools/generate_ui_sprites.py

Скрипт обновляет только именованные PNG набора, sprites.json и превью. Существующие
Unity .meta не изменяются; GUID сохраняются при повторной генерации.
PNG имеют RGBA и сглаживание, не pixel art. Генерация детерминирована.
Проверки в скрипте повторно открывают PNG, проверяют размеры, RGBA, прозрачность
и полностью белый квадрат. Компиляция импортера и вид в Unity требуют проверки
в редакторе; Python-проверка не заменяет проверку сцены.
