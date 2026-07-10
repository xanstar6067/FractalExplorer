# Темы в новых и переносимых модулях

`ThemeManager.Initialize` регистрирует class-handler для `Window`, поэтому каждое новое окно автоматически получает фон и текст текущей темы. Регистрировать окно вручную не нужно.

Правила переноса интерфейса:

1. Не задавать UI-цвета литералами (`#...`, `White`, `SystemColors`), использовать `{DynamicResource Theme.*Brush}`.
2. Локальные неявные стили объявлять с `BasedOn="{StaticResource {x:Type Button}}"` (с нужным типом), иначе они перекроют общий шаблон.
3. Намеренные цветовые поверхности — холст рендера, цветовой образец, превью изображения — помечать `theme:ThemeContract.IgnoreAudit="True"`.
4. В Debug `ThemeContract` обходит визуальное дерево загруженного окна и пишет в Output локальные цвета, которые не будут обновляться при смене темы.
5. Новые общие цвета добавлять как токены в `ThemeDefinition`, назначать в `ThemeManager.ApplyResources` и использовать в `ThemeStyles.xaml`.

Основные токены: `Theme.BaseBackgroundBrush`, `Theme.PanelBackgroundBrush`, `Theme.ControlBackgroundBrush`, `Theme.PrimaryTextBrush`, `Theme.SecondaryTextBrush`, `Theme.AccentPrimaryBrush`, `Theme.BorderBrush`, `Theme.InputBorderBrush`, `Theme.FocusBrush`.
