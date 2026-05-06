# Desktop installer

Для WPF desktop-клиента подготовлен скрипт сборки `.exe` и простого установщика.

## Сборка

Команда выполняется из корня проекта:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-desktop-installer.ps1
```

## Результат

Обычный self-contained exe:

```text
artifacts/desktop/publish/OnlineCourses.Desktop.exe
```

Installer:

```text
artifacts/desktop/OnlineCourses.Desktop.Setup.exe
```

## Что делает installer

Installer:

- копирует приложение в `%LOCALAPPDATA%\OnlineCourses.Desktop`;
- копирует `desktopsettings.json`;
- создает ярлык `OnlineCourses Desktop` на рабочем столе;
- запускает приложение после установки.

Папка `artifacts/` не коммитится в репозиторий, потому что содержит собранные бинарные файлы.
