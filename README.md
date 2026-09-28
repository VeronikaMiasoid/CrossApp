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
├── data/
│   ├── sample.csv              # 10 коректних рядків + 3 пошкоджені (тестові дані)
│   ├── sample.json
│   └── mixed.csv               # рядки різних типів: P; (товар) і C; (клієнт)
├── scripts/
│   └── publish-sizes.sh        # publish у 4 режимах + рядки таблиці розмірів
└── src/
    ├── Core/                   # class library (net8.0;net10.0), без точки входу
    │   ├── Core.csproj
    │   ├── EnvironmentInfo.cs  # EnvironmentReport (record) + EnvironmentInfo.Collect()
    │   ├── Dto/                # record-типи: ProductDto, CustomerDto, ImportResult<T>, MixedImportResult
    │   ├── Import/             # імпортери CSV / JSON / змішаний CSV
    │   ├── Domain/             # тиждень 4: сутності з поведінкою та інваріантами
    │   └── Storage/            # тиждень 5: реалізації сховищ
    └── Cli/                    # консольний застосунок: аргументи, виклик Core, вивід
        ├── Cli.csproj          # ProjectReference -> Core
        └── Program.cs
```

Напрямок залежності односторонній: **Cli → Core**. Core про Cli нічого не знає, тому його
можна буде підключити і до Api (тиждень 10), і до Blazor (тиждень 12).
Правило: у `Program.cs` немає бізнес-логіки, лише аргументи й вивід.

Домовленість про каталоги в Core на весь семестр:

- `Core/Dto/` — record-типи формату даних (тиждень 3)
- `Core/Domain/` — сутності з поведінкою та інваріантами (тиждень 4)
- `Core/Storage/` — реалізації сховищ (тиждень 5)

## Команди

```
dotnet build
dotnet run --project src/Cli                          # імпорт data/sample.csv
dotnet run --project src/Cli -- data/sample.json      # JSON: імпортер обирається за розширенням
dotnet run --project src/Cli -- data/nope.csv         # неіснуючий файл: повідомлення + код виходу 1
dotnet run --project src/Cli -- --mixed               # data/mixed.csv: рядки P; і C;
dotnet run --project src/Cli -- --env                 # інформація про середовище
dotnet run --project src/Cli -- --env --json          # те саме у форматі JSON
dotnet build src/Core/Core.csproj                     # Core збирається окремо (запустити його не можна)
```

## Публікація

RID — за вашою ОС (тут osx-arm64; для Windows win-x64, для Linux linux-x64):

```
dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true
dotnet publish src/Cli -c Release -r osx-arm64 --self-contained false
```

Усі режими й розміри одним скриптом (результат — у `artifacts/publish/`, каталог у `.gitignore`):

```
bash scripts/publish-sizes.sh osx-arm64
```

Запуск зібраного застосунку без `dotnet run`:

```
./artifacts/publish/osx-arm64-self-contained/Cli --env
```

### self-contained vs framework-dependent

- **self-contained** — у каталог publish кладеться код, залежності і копія .NET runtime для
  конкретної RID. Працює на машині без встановленого .NET, але каталог великий.
- **framework-dependent** — лише код і залежності. Каталог малий, але на машині користувача
  має бути встановлений сумісний .NET Runtime (тут .NET 10).

### Розміри publish

| RID | Режим | Розмір publish | Файлів | Потрібен runtime |
|---|---|---|---|---|
| osx-arm64 | self-contained | 83 МБ | 193 | ні |
| osx-arm64 | framework-dependent | 208 КБ | 7 | так (.NET 10) |
| osx-arm64 | self-contained + single-file | 76 МБ | 3 | ні |
| osx-arm64 | self-contained + single-file + trimmed | 13 МБ | 3 | ні |
| linux-x64 | self-contained | 79 МБ | 194 | ні |
| linux-x64 | framework-dependent | 164 КБ | 7 | так (.NET 10) |
| linux-x64 | self-contained + single-file | 70 МБ | 3 | ні |
| linux-x64 | self-contained + single-file + trimmed | 15 МБ | 3 | ні |

Trimming (`-p:PublishTrimmed=true`) дає 4 рядки попереджень збірки: тример може видалити код,
який знаходиться лише через рефлексію (зокрема JSON-серіалізація), тому такий пакет треба
перевіряти запуском усіх режимів.

## Формат даних

### `data/sample.csv`

- роздільник — крапка з комою `;` (константа `Separator` в `ProductCsvImporter`);
- перший рядок — заголовок `id;name;price` (розпізнається за початком `id`), також допустимий файл без заголовка;
- рядки, що починаються з `#`, і порожні рядки пропускаються;
- кодування UTF-8;
- ціна — десяткова **крапка** (`12.50`), розбір через `CultureInfo.InvariantCulture`;
  `12,50` вважається помилкою, а не числом 1250;
- у файлі навмисно 3 пошкоджені рядки (12: мало колонок, 13: нечислова ціна, 14: порожня назва) — це тестові дані.

`data/sample.json` — масив `{ "id", "name", "price" }`, регістр імен властивостей не важливий;
`data/mixed.csv` — рядки з префіксом типу: `P;id;назва;ціна`, `C;id;ім'я[;email]`.

Коди виходу Cli: `0` — успіх, `1` — файл не знайдено, `2` — непідтримуваний формат файлу.

## Середовище

.NET SDK 10.0, macOS (Apple Silicon, RID osx-arm64).