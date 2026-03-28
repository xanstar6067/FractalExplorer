# Auto-remove unused `using` directives

The solution is configured to automatically remove unused `using` directives (`IDE0005`) before each build.

## How it works

- `Directory.Build.targets` runs `dotnet format <project> --diagnostics IDE0005 --severity info --no-restore` in `BeforeBuild`.
- `Directory.Build.props` enables this behavior by default (`AutoRemoveUnusedUsings=true`).
- `.editorconfig` sets `IDE0005` severity to `warning` for visible diagnostics in IDE.

## Disable temporarily

If needed, disable for one build:

```bash
dotnet build -p:AutoRemoveUnusedUsings=false
```

---

# Авто-удаление неиспользуемых `using`

Решение настроено так, чтобы автоматически удалять неиспользуемые `using` (`IDE0005`) перед каждой сборкой.

## Как это работает

- В `Directory.Build.targets` на этапе `BeforeBuild` запускается:
  `dotnet format <project> --diagnostics IDE0005 --severity info --no-restore`.
- В `Directory.Build.props` поведение включено по умолчанию (`AutoRemoveUnusedUsings=true`).
- В `.editorconfig` для `IDE0005` установлена `warning`, чтобы проблема была заметна в IDE.

## Как временно отключить

```bash
dotnet build -p:AutoRemoveUnusedUsings=false
```
