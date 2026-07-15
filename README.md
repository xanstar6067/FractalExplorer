# Fractal Explorer WPF

Интерактивная лаборатория фракталов и динамических систем для Windows. Актуальная версия проекта построена на WPF и объединяет исследование комплексных множеств, стохастические визуализации, редакторы параметров, палитры, сохранения и экспорт изображений в одном приложении.

<p align="center">
  <img src="./FractalExplorerWPF/FractalExplorerWPF/FractalExplorerWPF/Assets/Screenshots/README/01-fractal-catalog.png" alt="Каталог фракталов Fractal Explorer WPF" width="900">
</p>

> WPF — основное направление разработки. Предыдущая реализация на Windows Forms сохранена в каталоге `FractalExplorer` как legacy-версия.

## Возможности

- **27 пунктов каталога:** 25 визуализаций и 2 галереи констант Julia.
- **Интерактивное исследование:** масштабирование колесом мыши, перемещение холста, сброс вида и полноэкранный режим.
- **Асинхронный рендеринг на CPU:** настройка числа потоков, отмена вычисления, индикаторы прогресса и восемь схем появления плиток.
- **Гибкое окрашивание:** встроенные и пользовательские палитры, плавные и дискретные режимы, Histogram, Orbit Trap и Stripe Average для семейства Mandelbrot.
- **Сохранение исследований:** параметры фрактала, превью и точки интереса хранятся в JSON и восстанавливаются через менеджер сохранений.
- **Экспорт изображений:** произвольное разрешение, пресеты вплоть до 8K, PNG/JPG/BMP, SSAA, Bicubic и Lanczos 3.
- **Настраиваемый WPF-интерфейс:** встроенные темы, системная тема Windows, редактор цветов и экранная пипетка.

## Каталог визуализаций

| Семейство | Доступные модули |
| --- | --- |
| Множество Мандельброта | Mandelbrot, Burning Ship, Tricorn (Mandelbar), Buffalo, Celtic Mandelbrot, Simonobrot, Generalized Mandelbrot |
| Множество Жюлиа | Julia, Julia Burning Ship и две галереи констант `C` |
| Итерируемые функции | Newton Pools+, Phoenix, Collatz, Nova Mandelbrot, Nova Julia, Buddhabrot / Anti-Buddhabrot, Fractal Flame |
| Геометрические фракталы | Треугольник Серпинского, IFS Барнсли / Хейуэя |
| Динамические системы | Lyapunov, Lorenz, Rössler, Logistic Map, Bifurcation, Hénon, Ikeda |

## Интерфейс

### Комплексные множества

Рабочие окна объединяют параметры формулы, управление качеством и потоками, палитры, сохранения и экспорт. Семейство Mandelbrot использует общий движок и единый интерфейс для классического множества и его вариантов.

<p align="center">
  <img src="./FractalExplorerWPF/FractalExplorerWPF/FractalExplorerWPF/Assets/Screenshots/README/02-mandelbrot-explorer.png" alt="Исследователь множества Мандельброта" width="1000">
</p>

### Итерируемые функции и IFS

Newton Pools+ поддерживает готовые и пользовательские формулы, методы Newton, Halley и Householder. IFS включает готовые наборы и редактор аффинных преобразований.

<table>
  <tr>
    <td width="50%"><img src="./FractalExplorerWPF/FractalExplorerWPF/FractalExplorerWPF/Assets/Screenshots/README/03-newton-pools.png" alt="Бассейны Ньютона"></td>
    <td width="50%"><img src="./FractalExplorerWPF/FractalExplorerWPF/FractalExplorerWPF/Assets/Screenshots/README/04-ifs-fractal.png" alt="IFS — папоротник Барнсли"></td>
  </tr>
  <tr>
    <td align="center"><sub>Newton Pools+</sub></td>
    <td align="center"><sub>IFS — папоротник Барнсли</sub></td>
  </tr>
</table>

### Стохастические фракталы и динамические системы

Fractal Flame использует накопление HDR-гистограммы и настраиваемые трансформации. Отдельное семейство модулей визуализирует непрерывные и дискретные динамические системы.

<table>
  <tr>
    <td width="50%"><img src="./FractalExplorerWPF/FractalExplorerWPF/FractalExplorerWPF/Assets/Screenshots/README/05-fractal-flame.png" alt="Fractal Flame"></td>
    <td width="50%"><img src="./FractalExplorerWPF/FractalExplorerWPF/FractalExplorerWPF/Assets/Screenshots/README/06-lorenz-system.png" alt="Аттрактор Лоренца"></td>
  </tr>
  <tr>
    <td align="center"><sub>Fractal Flame</sub></td>
    <td align="center"><sub>Аттрактор Лоренца</sub></td>
  </tr>
</table>

### Темы оформления

Редактор тем позволяет создавать и копировать темы, менять цвета элементов интерфейса и применять оформление Windows.

<p align="center">
  <img src="./FractalExplorerWPF/FractalExplorerWPF/FractalExplorerWPF/Assets/Screenshots/README/07-theme-editor.png" alt="Редактор тем Fractal Explorer WPF" width="900">
</p>

## Управление

| Действие | Управление |
| --- | --- |
| Масштабирование относительно курсора | Колесо мыши над холстом |
| Перемещение области просмотра | Перетаскивание левой кнопкой мыши |
| Полноэкранный режим | `F11` |
| Выход из полноэкранного режима | `Esc` |
| Скрытие панели параметров | Кнопка в левом верхнем углу холста |

Конкретные параметры, доступные режимы окрашивания и дополнительные интерактивные карты зависят от выбранного фрактала.

## Сохранения и экспорт

Менеджеры сохранений запоминают формулу, координаты, масштаб, качество рендера, палитру и точки интереса. Для записей создаются превью, а данные хранятся локально в JSON.

Менеджер экспорта позволяет задавать размер изображения вручную или выбрать готовый пресет, формат файла и способ финальной обработки:

- нативный рендер или SSAA;
- бикубическое масштабирование;
- фильтр Lanczos 3;
- PNG, JPG с настройкой качества и BMP.

## Сборка и запуск

Требуются Windows и [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
git clone <repository-url>
cd FractalExplorer
dotnet build .\FractalExplorerWPF\FractalExplorerWPF\FractalExplorerWPF.slnx
dotnet run --project .\FractalExplorerWPF\FractalExplorerWPF\FractalExplorerWPF\FractalExplorerWPF.csproj
```

Для разработки решение также можно открыть в Visual Studio с установленной рабочей нагрузкой .NET Desktop Development.

## Структура репозитория

```text
FractalExplorerWPF/   основная WPF-версия
FractalExplorer/      предыдущая WinForms-версия
Pictures/             архив изображений предыдущих версий
README.md             описание актуальной WPF-версии
```

README-скриншоты хранятся вместе с WPF-проектом в `FractalExplorerWPF/FractalExplorerWPF/FractalExplorerWPF/Assets/Screenshots/README`.

## Лицензия

Проект распространяется по лицензии [Apache License 2.0](./LICENSE).
