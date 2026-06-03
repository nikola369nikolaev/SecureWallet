
# SecureWallet Development History

## Предназначение на файла

Този файл е централен инженеринг log за проекта `SecureWallet`.

Тук ще се описват:
- какво е добавено
- защо е добавено
- в кой слой е добавено
- как работи на високо ниво

Оттук нататък при всяка важна стъпка ще се добавя нов запис:
- нов модул
- нов entity/model
- нова функционалност
- база данни и migration промени
- API endpoints
- security механизми
- frontend страници, state, API интеграция
- refactor-и и архитектурни решения

## Архитектурна посока

Проектът е организиран като по-лек и по-чист вариант на multi-project .NET приложение с отделен React frontend.

Текущите слоеве са:
- `SecureWallet.Domain`
- `SecureWallet.Application`
- `SecureWallet.Infrastructure`
- `SecureWallet.API`
- `securewallet.frontend`

### Зависимости между проектите
- `SecureWallet.Domain` не зависи от друг проект
- `SecureWallet.Application` зависи от `SecureWallet.Domain`
- `SecureWallet.Infrastructure` зависи от `SecureWallet.Application` и `SecureWallet.Domain`
- `SecureWallet.API` зависи от `SecureWallet.Application` и `SecureWallet.Infrastructure`

## Как ще поддържаме този файл

При всяка съществена промяна ще описваме:

1. Какво е добавено
2. Къде е добавено
3. Защо е добавено
4. Как работи

Записите ще са по дата и по слой.

---

## 2026-06-02 - Initial project setup

### Общо

Създаден е начален skeleton на проекта `SecureWallet` с цел:
- чист старт
- ясна архитектура
- добра основа за дипломния проект
- по-малко сложност от `BillingServer`

Избрана е по-лека архитектура с отделен backend и отделен frontend.

### SecureWallet.Domain

Добавено:
- създаден е проект `SecureWallet.Domain`
- премахнат е template файлът `Class1.cs`
- създадени са начални папки:
    - `Entities`
    - `Enums`
    - `ValueObjects`
    - `Constants`
    - `Exceptions`

Защо:
- `Domain` ще държи основните бизнес обекти и правила
- искаме този слой да е най-чист и най-независим

Как работи:
- тук ще живеят основните модели като `User`, `Role`, `Wallet`, `Transaction`
- този слой няма да съдържа EF Core, HTTP, JWT, Serilog или controller логика

### SecureWallet.Application

Добавено:
- създаден е проект `SecureWallet.Application`
- добавен е reference към `SecureWallet.Domain`
- премахнат е template файлът `Class1.cs`
- създадени са начални папки:
    - `Interfaces/Repositories`
    - `Interfaces/Security`
    - `Interfaces/Services`
    - `Features/Auth`
    - `Features/Users`
    - `Features/Roles`
    - `Features/Wallets`
    - `Features/Transactions`
    - `Features/Audit`
    - `Behaviors`

Защо:
- `Application` ще държи use-case логиката
- тук ще описваме какво може системата, без да връзваме директно техническите имплементации

Как работи:
- тук ще има commands, queries, handlers, validators и DTO-та
- repository и security интерфейсите ще се дефинират тук
- реалните имплементации ще бъдат в `Infrastructure`

### SecureWallet.Infrastructure

Добавено:
- създаден е проект `SecureWallet.Infrastructure`
- добавени са references към `SecureWallet.Application` и `SecureWallet.Domain`
- премахнат е template файлът `Class1.cs`
- създадени са начални папки:
    - `Data`
    - `Data/Configurations`
    - `Data/Migrations`
    - `Data/Seed`
    - `Repositories`
    - `Security`
    - `Services`
    - `Logging`

Защо:
- този слой ще съдържа техническата имплементация
- тук ще стоят EF Core, repository класовете, JWT/TOTP имплементациите и помощните услуги

Как работи:
- `Data` ще държи `AppDbContext` и database конфигурациите
- `Repositories` ще имплементират интерфейсите от `Application`
- `Security` ще съдържа token, password hashing и 2FA логика

### SecureWallet.API

Добавено:
- създаден е проект `SecureWallet.API` като `Web API`
- добавени са references към `SecureWallet.Application` и `SecureWallet.Infrastructure`
- премахнати са template файловете:
    - `WeatherForecast.cs`
    - `WeatherForecastController.cs`
    - `SecureWallet.API.http`
- създадени са начални папки:
    - `Controllers`
    - `Requests`
    - `Responses`
    - `Middleware`
    - `Extensions`

Защо:
- искаме API-то да е тънък входен слой
- да приема HTTP заявки и да ги подава към `Application`

Как работи:
- тук ще има controller-и, API request/response модели и middleware
- не искаме controller-ите да съдържат бизнес логика

### securewallet.frontend

Добавено:
- създадена е папка `securewallet.frontend`
- създадена е начална структура:
    - `src/api`
    - `src/auth`
    - `src/features/login`
    - `src/features/register`
    - `src/features/dashboard`
    - `src/features/wallets`
    - `src/features/transactions`
    - `src/features/security`
    - `src/components`
    - `src/layouts`
    - `src/routes`
    - `src/utils`
    - `public`

Защо:
- frontend-ът ще е отделен React проект
- още не е създадено реално React приложение, но структурата е подготвена

Как работи:
- по-късно тук ще се добавят Vite/React/TypeScript файловете
- frontend-ът ще комуникира с `SecureWallet.API`

### Tooling и build среда

Добавено:
- проектът е фиксиран на `.NET 10`
- добавен е `global.json` със SDK версия `10.0.100`

Защо:
- първоначално имаше разминаване между `dotnet` в Rider и `dotnet` в терминала
- трябваше да уеднаквим build средата

Как работи:
- `global.json` фиксира коя SDK версия да се използва за solution-а
- Windows user `Path` беше коригиран така, че `dotnet` да сочи към `C:\Users\User\.dotnet`

### Git и GitHub

Добавено:
- създаден е `.gitignore`
- инициализирано е локално Git repo
- направен е първи commit
- създадено е GitHub repo `SecureWallet`
- локалният проект е push-нат към `origin/main` през SSH

Защо:
- искаме историята на проекта да се пази още от самия старт
- това дава добра отправна точка за всички следващи стъпки

Как работи:
- локалният branch е преименуван на `main`
- repo-то е вързано с `origin`
- следващите промени ще минават през нормален Git workflow

### Състояние след тази стъпка

Към момента проектът:
- има чиста многослойна структура
- build-ва успешно
- е качен в GitHub
- е готов за първите domain модели и реален код

---

## Следваща планирана стъпка

Следващата основна стъпка е създаване на първите domain модели:
- `User`
- `Role`
- `Wallet`
- `Transaction`

След това ще продължим с:
- enum-и
- repository интерфейси
- `AppDbContext`
- първите auth и transaction use-case-и

---

## 2026-06-02 - First domain entities: Role and User

### SecureWallet.Domain

Добавено:
- `Entities/Role.cs`
- `Entities/User.cs`

Защо:
- `Role` и `User` са най-основните модели за authentication и authorization частта
- почти всички следващи модули ще стъпят върху тях: login, роли, 2FA, audit, wallet ownership

Как работи:
- `Role` описва роля в системата с име, описание, активен статус и връзка към потребителите
- `User` описва потребител с username, email, password, basic profile полета, 2FA статус и връзка към роля
- и двата модела съдържат `CreatedAtUtc` и `UpdatedAtUtc`, за да имаме ясна база за audit и future persistence логика

Архитектурно решение:
- моделите са умишлено прости
- не е добавяна validation логика вътре в entity-тата на този етап
- не са добавяни base classes или shared abstractions твърде рано, за да не усложняваме Domain слоя още в началото

---

## 2026-06-02 - Added Wallet and Transaction entities

### SecureWallet.Domain

Добавено:
- `Entities/Wallet.cs`
- `Entities/Transaction.cs`

Защо:
- `Wallet` е моделът, който ще държи виртуалния баланс на потребителя
- `Transaction` е моделът, който ще описва превод между два wallet-а
- тези два модела са ядрото на финансовата част на системата

Как работи:
- `Wallet` е свързан с конкретен `User` чрез `UserId`
- `Wallet` държи текущ баланс, валута и активен статус
- `Wallet` има две навигационни колекции:
    - изпратени транзакции
    - получени транзакции
- `Transaction` пази подател, получател, сума, reference, описание и време на създаване

Архитектурно решение:
- оставихме моделите максимално прости
- все още не добавяме `TransactionStatus` и `TransactionType`, за да не натоварваме Domain слоя твърде рано
- това ще се добави в следваща стъпка, когато оформим enum-ите и use-case логиката

---

## 2026-06-02 - Added TransactionStatus enum

### SecureWallet.Domain

Добавено:
- `Enums/TransactionStatus.cs`
- `Transaction.Status`

Защо:
- транзакциите вече имат нужда от ясно състояние, за да не разчитаме само на това, че записът съществува
- това ще е полезно още при първия transfer use case, защото ще можем да следим жизнения цикъл на операцията

Как работи:
- добавен е enum `TransactionStatus`
- началната стойност е `Pending`
- следващите слоеве ще могат да маркират транзакцията като `Completed`, `Failed` или `Cancelled`

Архитектурно решение:
- добавихме само статуса, а не още няколко enum-а наведнъж
- така държим стъпката малка и ясна, без да усложняваме Domain модела твърде рано

---

## 2026-06-02 - Changed default wallet currency to EUR

### SecureWallet.Domain

Добавено/променено:
- `Wallet.Currency` вече е с default стойност `EUR`

Защо:
- към този етап на проекта по-логичната начална валута за системата е евро
- това отразява актуалната среда на проекта и избягва объркване с временен placeholder

Как работи:
- при създаване на нов wallet, ако не е подадена друга валута, началната стойност ще бъде `EUR`
- това е само default стойност и по-късно може да се надгради с по-строга валутна логика, ако решим

---

## 2026-06-02 - Added first repository interfaces

### SecureWallet.Application

Добавено:
- `Interfaces/Repositories/IUserRepository.cs`
- `Interfaces/Repositories/IRoleRepository.cs`
- `Interfaces/Repositories/IWalletRepository.cs`
- `Interfaces/Repositories/ITransactionRepository.cs`

Защо:
- `Application` слоят вече има нужда от ясни договори за достъп до основните domain модели
- това ни позволява да описваме use-case логика, без да зависим директно от EF Core или конкретна база

Как работи:
- `IUserRepository` описва базов достъп до потребители по `Id`, `Email` и `Username`
- `IRoleRepository` описва четене на роля по `Id`, по име и списък с роли
- `IWalletRepository` описва достъп до wallet по `Id` и по собственик
- `ITransactionRepository` описва достъп до транзакция по `Id` и извличане на транзакции за конкретен wallet

Архитектурно решение:
- методите са умишлено минимални
- на този етап не добавяме generic repository, unit of work или прекалено абстрактни базови интерфейси
- целта е договорите да са лесни за четене и директно свързани с реалните use cases на системата

---

## 2026-06-02 - Added AppDbContext and EF Core foundation

### SecureWallet.Infrastructure

Добавено:
- `Data/AppDbContext.cs`
- EF Core зависимости в `SecureWallet.Infrastructure.csproj`

Защо:
- инфраструктурният слой вече има нужда от централна входна точка към базата данни
- `AppDbContext` е основата, върху която ще стъпят EF Core configurations, repository имплементациите и бъдещите migration-и

Как работи:
- `AppDbContext` наследява `DbContext`
- приема `DbContextOptions<AppDbContext>` отвън, вместо сам да си знае connection string
- излага `DbSet<User>`, `DbSet<Role>`, `DbSet<Wallet>` и `DbSet<Transaction>`
- в `OnModelCreating` зарежда всички `IEntityTypeConfiguration<>` класове от assembly-то чрез `ApplyConfigurationsFromAssembly`

Архитектурно решение:
- умишлено не използваме `OnConfiguring` за connection string логика
- настройката на provider-а и connection string-а ще бъде през dependency injection в по-късен етап
- това държи `DbContext` чист и по-лесен за тестване и поддръжка

---

## 2026-06-02 - Added EF Core configurations and password validation rules

### SecureWallet.Infrastructure

Добавено:
- `Data/Configurations/UserConfiguration.cs`
- `Data/Configurations/RoleConfiguration.cs`
- `Data/Configurations/WalletConfiguration.cs`
- `Data/Configurations/TransactionConfiguration.cs`

Защо:
- `AppDbContext` вече има нужда от реални mapping правила за таблици, индекси, връзки и ограничения
- това е базата, върху която по-късно ще стъпят migration-ите и repository имплементациите

Как работи:
- `RoleConfiguration` прави `Role.Name` задължително и уникално
- `UserConfiguration` прави `Username`, `Email` и `Password` задължителни, с unique индекси за `Username` и `Email`
- `WalletConfiguration` прави `UserId` уникален, така че всеки потребител да има един wallet
- `TransactionConfiguration` задава precision за `Amount`, прави `Reference` задължителен и уникален и описва двете връзки към wallet-и

Архитектурно решение:
- password правилата не се слагат в EF configuration, а само структурните изисквания за колоната
- decimal стойностите са конфигурирани с precision, защото това е по-подходящо за финансови данни

### SecureWallet.Application

Добавено:
- `Features/Auth/Validators/PasswordValidator.cs`

Защо:
- правилата за сигурна парола са business validation, не database validation
- искаме изискванията за парола да се прилагат още на входа на регистрация или смяна на парола

Как работи:
- `PasswordValidator` изисква минимум 8 символа
- изисква поне една главна буква
- изисква поне една цифра
- връща списък от грешки или позволява бърза проверка чрез `IsValid`

Архитектурно решение:
- валидираме паролата в `Application`, защото там е правилното място за use-case логика
- колоната `Password` в базата е с по-голяма дължина, за да може по-късно да пази хеширана стойност, въпреки че property-то в момента се казва `Password`

---

## 2026-06-02 - Documented security and data integrity reasoning in history only

### SecureWallet.Infrastructure

Добавено:
- описание в history файла на ключовите security и data integrity решения

Защо:
- някои EF Core правила имат директно значение за сигурността и целостта на данните
- искаме обясненията да останат в инженеринг историята, без да натоварваме кода с допълнителни inline коментари

Как работи:
- тук остава описано защо използваме unique индекси, ownership ограничения и `DeleteBehavior` правила
- кодът остава по-чист, а мотивите се пазят в history файла

### SecureWallet.Application

Добавено:
- описание в history файла защо password правилата стоят в `Application`

Защо:
- правилата за парола са security-sensitive и е полезно да е ясно защо стоят в `Application`, а не в базата

Как работи:
- тук е записано, че слабите пароли се отхвърлят преди да стигнат до persistence слоя

---

## 2026-06-02 - Aligned EF Core package versions

### SecureWallet.Infrastructure

Добавено/променено:
- подравнени са версиите на `Microsoft.EntityFrameworkCore` и `Microsoft.EntityFrameworkCore.Design`

Защо:
- предишната комбинация водеше до warning за конфликт между различни версии на `Microsoft.EntityFrameworkCore.Relational`
- искаме dependency graph-ът да е чист преди да продължим към repository имплементации и migrations

Как работи:
- EF Core пакетите вече са подравнени към версията, която е съвместима с текущия `Npgsql.EntityFrameworkCore.PostgreSQL` provider
- така build-ът трябва да мине без version mismatch warning

---

## 2026-06-02 - Added first repository implementations

### SecureWallet.Infrastructure

Добавено:
- `Repositories/RoleRepository.cs`
- `Repositories/UserRepository.cs`

Защо:
- `Application` вече имаше договори за работа с роли и потребители, а сега `Infrastructure` започва да ги реализира чрез EF Core
- това е първата реална връзка между use-case слоя и `AppDbContext`

Как работи:
- `RoleRepository` чете роли по `Id`, по име и връща списък с роли
- `UserRepository` чете потребители по `Id`, `Email` и `Username`
- при read операции се използва `AsNoTracking`, защото на този етап искаме чисти read модели без излишно EF проследяване
- `AddAsync` и `UpdateAsync` в `UserRepository` записват веднага чрез `SaveChangesAsync`

Архитектурно решение:
- за този етап е избран по-прост подход, при който repository-то само завършва своята промяна
- това държи кода по-лесен за следване в началото
- ако по-късно transaction flow-ът изисква по-централизирано управление на записите, ще можем да въведем отделен save/unit-of-work подход

---

## 2026-06-02 - Completed repository layer for wallet and transaction access

### SecureWallet.Infrastructure

Добавено:
- `Repositories/WalletRepository.cs`
- `Repositories/TransactionRepository.cs`
- naming update в `RoleRepository` и `UserRepository` от `_dbContext` към `_appDbContext`

Защо:
- вече имаме нужда от реални имплементации и за финансовата част на системата
- naming-ът `_appDbContext` е по-ясен и по-близък до стила, използван в по-големи production проекти

Как работи:
- `WalletRepository` извлича wallet по `Id` и по `UserId` и поддържа add/update операции
- `TransactionRepository` извлича транзакция по `Id` и списък с транзакции за даден wallet
- при transaction read операциите се зареждат `SenderWallet` и `ReceiverWallet`
- transaction списъкът се подрежда по `CreatedAtUtc` в низходящ ред, за да се виждат първо най-новите записи

Архитектурно решение:
- repository слоят запазва същия прагматичен стил като при `RoleRepository` и `UserRepository`
- read операциите използват `AsNoTracking`
- write операциите записват директно чрез `SaveChangesAsync`, без допълнителен unit-of-work слой на този етап

---

## 2026-06-02 - Added infrastructure dependency injection wiring

### SecureWallet.Infrastructure

Добавено:
- `DependencyInjection.cs`

Защо:
- след като вече имаме `AppDbContext` и repository имплементации, трябва да ги регистрираме централизирано в DI контейнера
- това държи `Program.cs` по-чист и събира инфраструктурната регистрация на едно място

Как работи:
- `AddInfrastructure(this IServiceCollection, IConfiguration)` чете connection string `DefaultConnection`
- регистрира `AppDbContext` с `UseNpgsql(...)`
- регистрира `IRoleRepository`, `IUserRepository`, `IWalletRepository` и `ITransactionRepository`

Архитектурно решение:
- provider-ът и connection string-ът се конфигурират отвън, а не вътре в `AppDbContext`
- това пази `DbContext` чист и оставя инфраструктурната wiring логика централизирана

### SecureWallet.API

Добавено:
- извикване на `builder.Services.AddInfrastructure(builder.Configuration);` в `Program.cs`

Защо:
- API проектът е composition root и отговаря за стартиране на приложението и свързване на слоевете

Как работи:
- `Program.cs` вече активира инфраструктурната регистрация чрез един централен extension method
- реалната connection string стойност ще бъде добавена в следващ етап чрез конфигурационен файл

---

## 2026-06-02 - Started auth security abstractions

### SecureWallet.Application

Добавено:
- `Interfaces/Security/IPasswordHasher.cs`
- `Interfaces/Security/IJwtTokenService.cs`

Защо:
- auth модулът има нужда от ясни договори за hash-ване на парола и генериране на access token
- искаме `Application` да знае какво му трябва, без да зависи от конкретна JWT библиотека или конкретен hashing алгоритъм

Как работи:
- `IPasswordHasher` описва две основни операции:
  - създаване на hash от входна парола
  - проверка дали подадената парола съвпада с вече записан hash
- `IJwtTokenService` описва генериране на access token за конкретен потребител

Архитектурно решение:
- абстракциите стоят в `Application`
- реалните имплементации ще бъдат в `Infrastructure`
- това пази auth логиката по-чиста и улеснява бъдещото тестване и подмяна на техническата реализация

---

## 2026-06-02 - Added password hashing implementation

### SecureWallet.Infrastructure

Добавено:
- `Security/PasswordHasher.cs`
- DI регистрация за `IPasswordHasher`

Защо:
- `Application` вече имаше нужда от `IPasswordHasher`, но липсваше реална имплементация
- избран е официалният ASP.NET Core `PasswordHasher<User>`, защото е добре познат, стабилен и лесен за обяснение в .NET среда

Как работи:
- `PasswordHasher` е wrapper над `Microsoft.AspNetCore.Identity.PasswordHasher<User>`
- `Hash(...)` генерира password hash за подадената парола
- `Verify(...)` проверява дали подадената парола съвпада със записания hash
- `SuccessRehashNeeded` също се приема като валиден резултат, защото означава, че паролата е вярна, но hash параметрите може да се нуждаят от обновяване
- underlying подходът е официалният ASP.NET Core Identity password hashing механизъм, който в IdentityV3 стъпва върху PBKDF2

Архитектурно решение:
- `Application` продължава да знае само интерфейса `IPasswordHasher`
- конкретният алгоритъм и техническа реализация стоят в `Infrastructure`
- това следва принципа:
  - `Application = какво искаме`
  - `Infrastructure = как точно го правим`

---

## 2026-06-02 - Added JWT token generation infrastructure

### SecureWallet.Infrastructure

Добавено:
- `Security/JwtTokenService.cs`
- DI регистрация за `IJwtTokenService`

Защо:
- auth слоя вече има нужда от реална имплементация за генериране на access token
- искаме token логиката да стои в `Infrastructure`, а `Application` да зависи само от интерфейса

Как работи:
- `JwtTokenService` чете настройки от конфигурацията:
  - `Jwt:Key`
  - `Jwt:Issuer`
  - `Jwt:Audience`
  - `Jwt:AccessTokenExpirationMinutes`
- JWT token-ът е подписан низ, който съдържа identity данни за потребителя и позволява stateless authentication
- логически JWT се състои от 3 части:
  - `header` - описва типа token и алгоритъма за подписване
  - `payload` - съдържа claims за потребителя
  - `signature` - доказва, че token-ът е издаден от системата и не е променян
- създава claims за:
  - `sub`
  - `email`
  - `unique_name`
  - `jti`
  - `role`, ако потребителят има заредена роля
- използва `SymmetricSecurityKey` и `SecurityAlgorithms.HmacSha256`
- генерира и сериализира JWT access token
- `sub` пази уникалния идентификатор на потребителя
- `email` и `unique_name` дават основни identity данни за приложението
- `role` ще се използва по-късно за authorization по роли
- `jti` е уникален идентификатор на самия token, полезен за trace и future revocation механизми
- `Issuer` и `Audience` описват кой е издал token-а и за кой клиент е предназначен
- `AccessTokenExpirationMinutes` определя колко дълго token-ът ще е валиден
- при последваща JWT authentication конфигурация API-то ще валидира:
  - подписа
  - issuer-а
  - audience-а
  - срока на валидност

Защо използваме JWT:
- token-ът позволява API-то да разпознава потребителя без да държи session в паметта
- това е подходящо за отделен React frontend и отделен Web API backend
- frontend-ът ще получава token след login и ще го изпраща в `Authorization: Bearer <token>` header при защитени заявки
- API-то ще чете claims от token-а и ще знае кой е потребителят и каква роля има

Архитектурно решение:
- `Application` казва само, че иска access token чрез `IJwtTokenService`
- `Infrastructure` решава как точно да бъде създаден token-ът
- това следва същия принцип както при `PasswordHasher`

### SecureWallet.API

Добавено:
- `ConnectionStrings:DefaultConnection` в `appsettings.json`
- `Jwt` секция в `appsettings.json`
- същите development стойности в `appsettings.Development.json`

Защо:
- `AddInfrastructure(...)` вече очаква runtime конфигурация за database и JWT
- без тази конфигурация приложението би build-вало, но не би стартирало коректно

Как работи:
- `DefaultConnection` дава базова PostgreSQL връзка за локална разработка
- `Jwt` секцията подава стойности към `JwtTokenService`
- текущата `Key` е development placeholder и трябва да бъде сменена при production или реално разгръщане

---

## 2026-06-03 - Added first register request, command, and result DTO

### SecureWallet.API

Добавено:
- `Requests/Auth/RegisterRequest.cs`

Защо:
- API слоят има нужда от ясен входен модел за регистрация
- това е формата, която frontend-ът ще изпраща към backend-а

Как работи:
- `RegisterRequest` описва входните данни за регистрация:
  - `Username`
  - `Email`
  - `Password`
  - `PhoneNumber`
  - `FirstName`
  - `LastName`

### SecureWallet.Application

Добавено:
- `Features/Auth/Commands/Register/RegisterUserCommand.cs`
- `Features/Auth/DTOs/RegisterResultDto.cs`

Защо:
- auth use case-ът вече има нужда от отделна application форма за действието "регистрирай потребител"
- искаме да разграничим API входния модел от use-case модела и от резултата

Как работи:
- `RegisterUserCommand` описва самото действие по регистрация в `Application`
- `RegisterResultDto` описва какво ще върнем след успешна регистрация
- по-късно API request-ът ще се map-ва към command, а handler-ът ще връща result DTO
- `PhoneNumber` е включен още на този етап, за да имаме основа за бъдеща SMS верификация, security notifications и recovery flow

Архитектурно решение:
- `Request` е вход към API
- `Command` е действие в `Application`
- `DTO` е резултат/преносим модел
- това държи слоевете по-ясно разделени и улеснява бъдещите handler-и и controller-и

---

## 2026-06-03 - Added first login request, command, and result DTO

### SecureWallet.API

Добавено:
- `Requests/Auth/LoginRequest.cs`

Защо:
- API слоят има нужда от ясен входен модел за login
- това е формата, която frontend-ът ще изпраща при опит за вход в системата

Как работи:
- `LoginRequest` съдържа:
  - `Email`
  - `Password`

### SecureWallet.Application

Добавено:
- `Features/Auth/Commands/Login/LoginUserCommand.cs`
- `Features/Auth/DTOs/LoginResultDto.cs`

Защо:
- login use case-ът има нужда от отделно application действие и отделен резултат
- искаме да разделим входа от API, действието в business слоя и резултата от успешен login

Как работи:
- `LoginUserCommand` описва заявката за вход в `Application`
- `LoginResultDto` описва какво ще върнем след успешен login:
  - access token
  - expiration time
  - основни user данни
  - роля

Архитектурно решение:
- login потокът следва същата форма като register:
  - API request
  - application command
  - result DTO
- това поддържа auth модула консистентен и подготвен за бъдещи handler-и и controller-и

---

## 2026-06-03 - Added first register handler

### SecureWallet.Application

Добавено:
- `Features/Auth/Commands/Register/RegisterUserHandler.cs`

Защо:
- `Register` use case-ът вече има нужда от реална application логика, а не само от request/command/DTO shape
- това е първият handler, който събира repository слоя и security услугите в работеща бизнес операция

Как работи:
- handler-ът валидира паролата чрез `PasswordValidator`
- нормализира `Email`, `Username` и `PhoneNumber`
- проверява дали вече има потребител със същия `Email` или `Username`
- търси default роля `"User"`
- hash-ва паролата чрез `IPasswordHasher`
- създава нов `User`
- създава и начален `Wallet` за новия потребител
- връща `RegisterResultDto`

Архитектурно решение:
- register логиката стои в `Application`, защото това е use-case слой
- handler-ът не знае нищо за EF Core директно, а работи само през интерфейси
- в текущия етап записът на `User` и `Wallet` става в две отделни repository операции
- това е достатъчно за началния етап, но по-късно може да бъде подобрено с по-централизиран transaction подход

---

## 2026-06-03 - Added first login handler and temporary explanatory comments

### SecureWallet.Application

Добавено:
- `Features/Auth/Commands/Login/LoginUserHandler.cs`
- кратки обяснителни коментари в `RegisterUserHandler.cs`

Защо:
- login use case-ът вече има нужда от реална application логика
- искаме кодът в auth handler-ите да се чете по-лесно, докато още оформяме потока и поведението

Как работи:
- `LoginUserHandler` нормализира email-а
- търси потребителя по email
- блокира login за неактивен потребител
- проверява паролата чрез `IPasswordHasher`
- генерира access token чрез `IJwtTokenService`
- връща `LoginResultDto` с token, expiry и основни user данни

Архитектурно решение:
- login логиката стои в `Application`, а не в API слоя
- handler-ът работи само през интерфейси и не знае нищо за EF Core или конкретното JWT implementation
- `ExpiresAtUtc` в момента отразява текущата JWT конфигурация от 60 минути и по-късно може да бъде централизирано изведен от token service-а
- временните inline коментари са добавени с цел по-лесно четене и могат по-късно да бъдат премахнати, след като потокът се стабилизира

---

## 2026-06-03 - Reduced `var` usage in source code

### Общо

Добавено/променено:
- на местата, където типът е ясен и важен за четимостта, `var` е заменено с explicit type

Защо:
- така кодът се чете по-лесно при преглед, обучение и документация
- в текущия проект това помага по-бързо да се вижда какъв точно обект се създава или връща всяка стъпка

Как работи:
- промяната е само stylistic refactor
- не променя логиката, а само прави типовете видими директно в кода

Архитектурно решение:
- оставяме explicit types там, където подобряват четимостта
- целта е кодът да е по-ясен за поддръжка и по-лесен за разбиране при защита на проекта

---

## 2026-06-03 - Added application DI and first auth API endpoints

### SecureWallet.Application

Добавено:
- `DependencyInjection.cs`

Защо:
- `Application` вече има реални handler-и и те трябва да бъдат регистрирани в DI контейнера
- така API слоят може да ги получава през constructor, без да знае как се създават

Как работи:
- `AddApplication()` регистрира:
  - `RegisterUserHandler`
  - `LoginUserHandler`

### SecureWallet.API

Добавено:
- `Controllers/AuthController.cs`
- извикване на `AddApplication()` в `Program.cs`

Защо:
- auth логиката вече е готова на use-case ниво и трябва да се expose-не през реални HTTP endpoints

Как работи:
- `AuthController` приема HTTP заявки за:
  - `POST /api/auth/register`
  - `POST /api/auth/login`
- controller-ът map-ва API request моделите към application command моделите
- после вика съответния handler
- при `InvalidOperationException` връща `400 Bad Request` с message

Архитектурно решение:
- controller-ът остава тънък и не съдържа бизнес логика
- `API` знае за `Request` моделите и за handler-ите
- `Application` продължава да държи същинската auth логика

---

## 2026-06-03 - Added login protection with captcha trigger and temporary lockout

### SecureWallet.Domain

Добавено:
- `User.FailedLoginAttempts`
- `User.LockoutEndUtc`

Защо:
- системата вече има нужда да следи поредица от грешни login опити
- това е основата за captcha след няколко неуспешни опита и за временно заключване на акаунта

Как работи:
- `FailedLoginAttempts` пази броя на поредните неуспешни опити
- `LockoutEndUtc` пази до кога акаунтът е временно заключен

### SecureWallet.Application

Добавено:
- `Interfaces/Security/ICaptchaVerificationService.cs`
- `LoginUserCommand.CaptchaToken`

Променено:
- `Features/Auth/Commands/Login/LoginUserHandler.cs`

Защо:
- login use case-ът вече трябва да може да реагира на поредица от грешни опити
- captcha проверката не трябва да бъде в controller-а, а в application логиката

Как работи:
- след 3-ти неуспешен опит captcha става задължителна
- след 5-ти неуспешен опит акаунтът се заключва за 30 секунди
- при успешен login броячът се занулява и временната блокировка се премахва

Архитектурно решение:
- проверката дали captcha token-ът е валиден става през отделен service интерфейс
- това държи handler-а независим от конкретна captcha реализация

### SecureWallet.Infrastructure

Добавено:
- `Security/TestCaptchaVerificationService.cs`

Променено:
- `DependencyInjection.cs`
- `Data/Configurations/UserConfiguration.cs`

Защо:
- за текущия етап е нужен прост тестов captcha механизъм, без външен доставчик

Как работи:
- test captcha service приема token `1234` като валиден
- услугата е регистрирана в DI като `ICaptchaVerificationService`
- `FailedLoginAttempts` е маркирано като задължително поле в EF configuration

Архитектурно решение:
- избрана е проста тестова реализация, за да може flow-ът да се демонстрира локално без външна интеграция
- по-късно този service може да бъде заменен с реална captcha услуга

### SecureWallet.API

Променено:
- `Requests/Auth/LoginRequest.cs`
- `Controllers/AuthController.cs`

Защо:
- frontend-ът вече трябва да може да подава captcha token при login, когато това стане необходимо

Как работи:
- `LoginRequest` вече има `CaptchaToken`
- controller-ът го прехвърля към `LoginUserCommand`

---

## 2026-06-03 - Moved login protection state from User to LoginAttempt

### SecureWallet.Domain

Добавено:
- `Entities/LoginAttempt.cs`

Променено:
- `Entities/User.cs`

Защо:
- логиката за защита на login-а трябва да брои неуспешни опити и когато:
  - email-ът е грешен
  - captcha-та е грешна
  - паролата е грешна
- това не може да се прави надеждно само през `User`, защото понякога изобщо няма намерен потребител

Как работи:
- `LoginAttempt` пази:
  - вътрешен email ключ за търсене
  - броя на неуспешните опити
  - текущата captcha
  - временната блокировка
- полетата за login защита са премахнати от `User`, защото вече не му принадлежат като отговорност

Архитектурно решение:
- login защитата е преместена в отделен модел, защото това прави поведението по-правилно и по-лесно за развитие

### SecureWallet.Application

Добавено:
- `Interfaces/Repositories/ILoginAttemptRepository.cs`
- `Features/Auth/Exceptions/LoginProtectionException.cs`

Променено:
- `Features/Auth/Commands/Login/LoginUserHandler.cs`
- `Interfaces/Security/ICaptchaVerificationService.cs`

Защо:
- login flow-ът вече има нужда от отделно състояние за опитите за вход
- API-то трябва да може да върне structured информация за:
  - captcha
  - lockout
  - error message

Как работи:
- `LoginUserHandler` вече работи през `LoginAttempt`
- след 3-ти неуспешен опит captcha е задължителна
- при всяка следваща грешка се генерира нова 4-цифрена captcha
- на 5-ти неуспешен опит има lockout за 30 секунди
- на 6-ти и повече неуспешни опити има lockout за 35 секунди
- само при успешен login броячът и captcha-та се зануляват
- ако `email` не съществува, връщаме общо съобщение за грешен вход, но не увеличаваме брояча
- ако `email` съществува, но паролата е грешна, това вече се брои като неуспешен опит
- ако captcha-та е грешна или липсва, когато вече е задължителна, това също се брои като неуспешен опит и се генерира нова captcha

### SecureWallet.Infrastructure

Добавено:
- `Data/Configurations/LoginAttemptConfiguration.cs`
- `Repositories/LoginAttemptRepository.cs`

Променено:
- `Data/AppDbContext.cs`
- `Security/TestCaptchaVerificationService.cs`
- `DependencyInjection.cs`

Защо:
- инфраструктурният слой вече трябва да пази и зарежда `LoginAttempt`
- captcha service-ът вече не връща фиксиран код, а генерира нов 4-цифрен код при нужда

Как работи:
- `AppDbContext` вече има `DbSet<LoginAttempt>`
- `LoginAttemptRepository` чете и записва опитите за вход
- `TestCaptchaVerificationService`:
  - генерира случаен 4-цифрен код
  - проверява дали подаденият код съвпада с очаквания

### SecureWallet.API

Променено:
- `Controllers/AuthController.cs`

Защо:
- frontend-ът вече трябва да може да получи не само обща грешка, а и данни за captcha и временно заключване

Как работи:
- `AuthController` прихваща `LoginProtectionException`
- връща `400 Bad Request` с:
  - `message`
  - `requiresCaptcha`
  - `captchaCode`
  - `lockoutSeconds`

---

## 2026-06-03 - Simplified login protection back to User-based state

### Общо

Променено:
- login protection логиката беше върната от `LoginAttempt` обратно в `User`

Защо:
- след като беше уточнено, че грешен email не се брои като неуспешен опит, вече не е нужен отделен модел за следене на опити при несъществуващ потребител
- така решението става по-просто и по-лесно за обяснение

### SecureWallet.Domain

Променено:
- `Entities/User.cs`

Как работи:
- `User` отново пази:
  - `FailedLoginAttempts`
  - `CurrentCaptchaCode`
  - `LockoutEndUtc`

### SecureWallet.Application

Променено:
- `Features/Auth/Commands/Login/LoginUserHandler.cs`

Как работи:
- ако email-ът не съществува:
  - връща се общо съобщение за грешен login
  - броячът не се увеличава
- ако email-ът съществува, но паролата е грешна:
  - това се брои като неуспешен опит
- ако captcha липсва или е грешна, когато вече е задължителна:
  - това се брои като неуспешен опит
  - генерира се нова captcha

### SecureWallet.Infrastructure

Променено:
- `Data/Configurations/UserConfiguration.cs`
- `Data/AppDbContext.cs`
- `DependencyInjection.cs`

Как работи:
- полетата за login защита отново се описват към `User`
- отделният `LoginAttempt` слой се премахва от активната структура

---

## 2026-06-03 - Switched auth input handling from silent normalization to strict validation

### SecureWallet.Application

Променено:
- `Features/Auth/Commands/Register/RegisterUserHandler.cs`
- `Features/Auth/Commands/Login/LoginUserHandler.cs`
- `Features/Auth/Validators/AuthInputValidator.cs`

Защо:
- решението беше да не поправяме входните данни мълчаливо
- потребителят трябва да подава коректни стойности, а системата да връща ясна грешка, ако форматът е грешен
- това избягва скрити промени по данните и прави поведението по-прозрачно

Как работи:
- премахнато е автоматичното:
  - `Trim()`
  - `ToLowerInvariant()`
- при `Register` и `Login` вече се правят строги проверки:
  - задължителни полета
  - забрана за интервали в началото и края
  - email формат
  - username формат
  - phone number формат
- ако входът не е в правилната форма, handler-ът хвърля ясна `InvalidOperationException`
- това значи, че системата не "оправя" данните вместо потребителя
- общите auth входни проверки са извадени в `AuthInputValidator`, за да не стоят директно в handler-ите
- `PasswordValidator` остава отделен, защото ще се използва и при бъдещи use case-и като `ResetPassword` и `ChangePassword`
- проверките за `Username` и `PhoneNumber` са пренаписани без `Regex`, за да са по-лесни за четене и обяснение

Архитектурно решение:
- избрана е `strict validation` политика вместо `silent normalization`
- това е по-подходящо за проект с акцент върху сигурност и коректно потребителско поведение

### SecureWallet.Infrastructure

Променено:
- `Repositories/UserRepository.cs`

Защо:
- въпреки че вече не променяме входния email или username, не искаме системата да допуска два акаунта, които се различават само по главни и малки букви

Как работи:
- търсенето по `Email` и `Username` вече е `case-insensitive`
- така системата третира:
  - `Nikola@example.com`
  - `nikola@example.com`
  като една и съща стойност при проверка за уникалност и при login

Архитектурно решение:
- входът се пази така, както е подаден
- но сравнението за идентичност на `Email` и `Username` е без значение от главни/малки букви
- това намалява объркването при login и не допуска дублирани акаунти само по case
