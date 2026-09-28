# CrossApp

Наскрізний проєкт з крос-платформного програмування.

**Автор:** Мʼясоїд Вероніка, група ФЕІ-31
**Репозиторій:** https://github.com/VeronikaMiasoid/CrossApp

**Предметна область:** Замовлення.
**Сутності:** Customer (клієнт), Product (товар), Order (замовлення), OrderLine (рядок замовлення).
**Призначення:** оформлення замовлень клієнтів та підрахунок їхніх сум.

## Структура solution

```
CrossApp/
├── CrossApp.sln
├── README.md
├── .gitignore
├── scripts/
│   └── publish-sizes.sh        # publish у 4 режимах + рядки таблиці розмірів
└── src/
    ├── Core/                   # class library (net8.0;net10.0), без точки входу
    │   ├── Core.csproj
    │   ├── EnvironmentInfo.cs  # EnvironmentReport (record) + EnvironmentInfo.Collect()
    │   ├── Domain/             # тиждень 4: сутності з поведінкою та інваріантами
    │   └── Storage/            # тиждень 5: реалізації сховищ
    └── Cli/                    # консольний застосунок, лише форматує вивід
        ├── Cli.csproj          # ProjectReference -> Core
        └── Program.cs
```

Напрямок залежності одностороння: **Cli → Core**. Core про Cli нічого не знає, тому його
можна буде підключити і до Api (тиждень 10), і до Blazor (тиждень 12).
Правило: у `Program.cs` немає жодної логіки, крім виводу.

Домовленість про каталоги в Core на весь семестр:

- `Core/Dto/` — record-типи формату даних (тиждень 3): ProductDto, CustomerDto, ...
- `Core/Domain/` — сутності з поведінкою та інваріантами (тиждень 4)
- `Core/Storage/` — реалізації сховищ (тиждень 5)

## Команди

```
dotnet build
dotnet run --project src/Cli
dotnet run --project src/Cli -- --json
dotnet build src/Core/Core.csproj        # Core збирається окремо (запустити його не можна)
```

Publish (RID — за вашою ОС, тут osx-arm64):

```
dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true
dotnet publish src/Cli -c Release -r osx-arm64 --self-contained false
```

Усі режими й розміри одним скриптом: `bash scripts/publish-sizes.sh osx-arm64`

## self-contained vs framework-dependent

- **self-contained** — у каталог publish кладеться код, залежності і копія .NET runtime для
  конкретної RID. Працює на машині без встановленого .NET, але каталог великий.
- **framework-dependent** — лише код і залежності. Каталог малий, але на машині користувача
  має бути встановлений сумісний .NET Runtime (тут .NET 10).

## Розміри publish

| RID | Режим | Розмір publish | Потрібен runtime |
|---|---|---|---|
| osx-arm64 | self-contained | _підставити_ | ні |
| osx-arm64 | framework-dependent | _підставити_ | так (.NET 10) |
| linux-x64 | self-contained | _підставити_ | ні |

## Середовище

.NET SDK 10.0, macOS (Apple Silicon, RID osx-arm64).
