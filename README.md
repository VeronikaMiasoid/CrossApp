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
    │   ├── SampleData.cs       # 16 демо-замовлень для --store та звітів тижня 7
    │   ├── Dto/                # record-типи: ProductDto, CustomerDto, OrderDto, OrderLineDto, ImportResult<T>, MixedImportResult
    │   ├── Import/             # імпортери CSV / JSON / змішаний CSV
    │   ├── Domain/             # тиждень 4: сутності з поведінкою та інваріантами
    │   │   ├── Order.cs        # агрегат: Create, AddLine, Confirm, ToDto/FromDto
    │   │   └── OrderLine.cs    # значення (value object) в межах Order
    │   ├── Abstractions/
    │   │   └── IOrderStore.cs  # тиждень 5: контракт сховища замовлень
    │   ├── Storage/            # тиждень 5: дві реалізації IOrderStore
    │   │   ├── InMemoryOrderStore.cs
    │   │   └── FileOrderStore.cs
    │   └── Services/
    │       └── OrderService.cs # тиждень 5: бізнес-операції поверх IOrderStore
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
- `Core/Abstractions/` — контракти сховищ, інтерфейси (тиждень 5)
- `Core/Storage/` — реалізації сховищ (тиждень 5)
- `Core/Services/` — бізнес-операції поверх абстракції сховища (тиждень 5)

## Доменна модель: Order + OrderLine (тиждень 4)

`Order` — сутність-агрегат (звичайний `class`, не `record`): приватний конструктор,
створення лише через фабричний метод `Order.Create(id, customerId)`, зміна стану — лише
через методи з бізнес-іменами `AddLine` і `Confirm`. Жодного публічного `set` на
критичних полях. `OrderLine` — проста незмінна сутність-значення всередині замовлення.

Інваріанти:

| № | Правило | Тип винятку | Де перевіряється |
|---|---|---|---|
| 1 | `Id` та `CustomerId` не порожні | `ArgumentException` | `Order.Create` |
| 2 | Кількість у рядку > 0, ціна рядка ≥ 0 | `ArgumentOutOfRangeException` | `Order.AddLine` |
| 3 | Не можна додати рядок до підтвердженого замовлення | `InvalidOperationException` | `Order.AddLine` |
| 4 | Не можна підтвердити замовлення без жодного рядка | `InvalidOperationException` | `Order.Confirm` |

`Order.ToDto()` / `Order.FromDto(OrderDto)` — мапінг сутності у формат зберігання тижня 3
(`OrderDto`, `OrderLineDto`) і назад; `FromDto` проходить ті самі перевірки, що й `Create`.

## Сервісний шар: IOrderStore + OrderService (тиждень 5)

`IOrderStore` — абстракція сховища (`List`, `GetById`, `Add`, `Update`, `Remove`), від якої
залежить `OrderService`, а не від конкретного класу. Дві взаємозамінні реалізації:

- **`InMemoryOrderStore`** — `Dictionary<string, Order>` у пам'яті; дані зникають після завершення процесу.
- **`FileOrderStore`** — той самий контракт, зберігання у JSON-файлі (`data/orders.json`):
  кеш у пам'яті, дозавантаження при першому зверненні, запис на диск після кожної зміни.

Вибір реалізації відбувається вручну, без DI-контейнера, в єдиній точці входу
застосунку (composition root) — `src/Cli/Program.cs`:

```csharp
IOrderStore store = useFile
    ? new FileOrderStore(dataPath)
    : new InMemoryOrderStore(SampleData.Orders());
var service = new OrderService(store);
```

## Події та делегати: OrderService.Changed (тиждень 6)

`OrderService` оголошує `public event EventHandler<OrderChangedEventArgs>? Changed;` і викликає приватний `OnChanged` через `Changed?.Invoke(this, args)` **лише після успішної зміни стану**.

| Зміна | Подія? | Чому |
|---|---|---|
| `CreateOrder` | так (`OrderCreated`) | стан сховища змінився |
| `AddLine` | так (`LineAdded`, із `TotalDelta`) | змінилась сума замовлення |
| `Confirm` | так (`OrderConfirmed`) | змінився статус |
| `Find`, `All`, `FindAll` | ні | читання, стан не змінюється |
| відмова (виняток) | ні | зміни не було, тому й сповіщати нема про що |

Підписники зараз: локальний метод у `Cli` (консоль + `logs/app.log`) і лямбда-лічильник із замиканням. На тижні 10 підписником стане логер Api, на тижні 12 — компонент Blazor. `Core` не знає ні про `Console`, ні про `logs`.

Додатково: `INotificationSink` (`ConsoleSink`, `FileSink`, `CompositeSink` у `Cli/Sinks`) передається в конструктор сервісу (`--sink`); `Action<string>` у `ProductCsvImporter.Load(path, onSkipped)` (`--skipped`).
**Подія проти приймача:** подій може бути скільки завгодно підписників, і сервіс про них не знає (підписка/відписка в будь-який момент, `+=`/`-=`); приймач один, відомий наперед, його обов'язково передати при створенні (чітка залежність, зручніше тестувати, але менш гнучко).

## Звіти LINQ (тиждень 7)

Звіти — чисті статичні функції в `Core/Reports/ReportService.cs`: приймають `IEnumerable<Order>`, повертають `IReadOnlyList<DTO>`. `Program.cs` не містить `Where`/`GroupBy`.

| Підкоманда | Запит | Оператори |
|---|---|---|
| `report-top` | топ-5 підтверджених замовлень | Where, Select, TopBy |
| `report-top-orders` | топ-5 товарів за виручкою | SelectMany, GroupBy, Select, TopBy |
| `report-expensive` | замовлення дорожчі за поріг | Where, Select, OrderByDescending, ThenBy |
| `report-groups` | статистика по клієнтах | GroupBy, Count, Sum, Max |
| `report-sum` (`--empty`) | агрегати за статусом | Where, Select, Sum, Average, Max |
| `report-join` | замовлення + клієнти | Join, GroupJoin |
| `report-query` | той самий звіт у query syntax | from/group/into/orderby/select |
| `report-lazy` | демонстрація відкладеного виконання | Where, Count, ToList |
| `report-bench` | 100 000 записів, ToList у середині чи без | Stopwatch |

**Коли виконується запит.** `source.Where(...)` лише *описує* запит; перебір відбувається при `Count()`, `ToList()`, `foreach`. Тому в `report-lazy` перший `Count()` дає 11, а після додавання елемента в джерело другий `Count()` того ж запиту дає 12: запит виконався знову на новому стані. `ToList()` робить знімок, і `List.Count` — це властивість без повторного виконання.

**Порожнє джерело.** `Sum` порожньої послідовності = 0, а `Average` і `Max` кидають `InvalidOperationException`, тому в `Summarize` перед ними стоїть перевірка `Count == 0`.

**Другий ключ сортування.** `ThenBy(..., StringComparer.Ordinal)` робить порядок однаковим на всіх ОС при рівних сумах. У query syntax `orderby ..., g.Key` використовує порівняння за замовчуванням; для ключів виду `C-01` результат той самий (`report-query` перевіряє `SequenceEqual`).

**Query syntax проти method syntax.** Запит читається ближче до SQL, а `group ... into` зручно читати; зате частина операторів (`TopBy`, `Take`) і так залишається методами. Результат ідентичний.

**Benchmark** (`report-bench`, моя машина): _вставте свої числа_. Відмінність мала, бо обидва варіанти проходять дані лінійно; `ToList()` у середині додає виділення пам'яті.

## Команди

```
dotnet build
dotnet run --project src/Cli                          # імпорт data/sample.csv
dotnet run --project src/Cli -- data/sample.json      # JSON: імпортер обирається за розширенням
dotnet run --project src/Cli -- data/nope.csv         # неіснуючий файл: повідомлення + код виходу 1
dotnet run --project src/Cli -- --mixed               # data/mixed.csv: рядки P; і C;
dotnet run --project src/Cli -- --env                 # інформація про середовище
dotnet run --project src/Cli -- --env --json          # те саме у форматі JSON
dotnet run --project src/Cli -- --orders              # тиждень 4: сценарії Order напряму (успіх + порушення інваріантів)
dotnet run --project src/Cli -- --store               # тиждень 5: сервіс + InMemoryOrderStore (демо-дані SampleData)
dotnet run --project src/Cli -- --store --file        # тиждень 5: сервіс + FileOrderStore (data/orders.json)
dotnet run --project src/Cli -- --events              # тиждень 6: подія Changed, підписники, відписка, відмови
dotnet run --project src/Cli -- --sink                # тиждень 6: INotificationSink через конструктор
dotnet run --project src/Cli -- data/sample.csv --skipped   # тиждень 6: Action<string> для пропущених рядків
dotnet run --project src/Cli -- report-top            # тиждень 7: звіти (report-top-orders, -expensive, -groups,
                                                      #   -sum [--empty], -join, -query, -lazy, -bench)
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

Trimming (`-p:PublishTrimmed=true`) дає 4 попередження IL2026 (JSON-серіалізація на рефлексії в
`Program.cs` і `ProductJsonImporter.cs`). Збірка проходить, текстовий вивід працює, але режим
`--env --json` у trimmed-збірці падає з `InvalidOperationException: Reflection-based serialization
has been disabled`. Тому trimmed-пакет треба перевіряти запуском усіх режимів; виправлення —
source generator для System.Text.Json (`JsonSerializerContext`).

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