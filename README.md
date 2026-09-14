# CrossApp

Наскрізний проєкт з крос-платформного програмування.

**Автор:** Мʼясоїд Вероніка, група ФЕІ-31 
**Репозиторій:** https://github.com/VeronikaMiasoid/CrossApp

**Предметна область:** Замовлення.
**Сутності:** Customer (клієнт), Product (товар), Order (замовлення), OrderLine (рядок замовлення).
**Призначення:** оформлення замовлень клієнтів та підрахунок їхніх сум.

## Запуск

```
dotnet build
dotnet run --project src/Cli
```

Для виводу інформації у форматі JSON замість таблиці:

```
dotnet run --project src/Cli -- --json
```

## Середовище

.NET SDK 10.0 (net10.0), Windows / Linux / macOS — крос-платформний консольний застосунок.

## Публікація self-contained (додаткове завдання)

```
dotnet publish src/Cli -c Release -r win-x64 --self-contained true
dotnet publish src/Cli -c Release -r linux-x64 --self-contained true
```

Порівняння розміру каталогів `publish` для двох RID — див. звіт (розділ «Додаткове завдання»).

## Структура

```
CrossApp/
├── .git/
├── .gitignore
├── CrossApp.sln
├── README.md
└── src/
    └── Cli/
        ├── Cli.csproj
        └── Program.cs
```

Каталоги `Core`, `Api`, `Web`, `tests` буде додано на наступних тижнях (2, 10, 12, 8 відповідно).
