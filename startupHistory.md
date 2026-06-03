# SecureWallet Startup History

## 1. Създаване на solution и проектите

### Solution
- В Rider беше създадено `Empty Solution` с име `SecureWallet`.
- Пътят на решението е:

```text
D:\repos\SecureWallet
```

### Проекти
- Създадени бяха следните .NET проекти:

```text
SecureWallet.Domain
SecureWallet.Application
SecureWallet.Infrastructure
SecureWallet.API
```

- `SecureWallet.Domain` е създаден като `Class Library`
- `SecureWallet.Application` е създаден като `Class Library`
- `SecureWallet.Infrastructure` е създаден като `Class Library`
- `SecureWallet.API` е създаден като `Web API`

### Target framework
- Всички .NET проекти са създадени на:

```text
net10.0
```

## 2. Архитектурна посока

Избраната начална структура е:

```text
SecureWallet.Domain
SecureWallet.Application
SecureWallet.Infrastructure
SecureWallet.API
securewallet.frontend
```

Идеята е проектът да е по-чист и по-прост от `BillingServer`, като се използва само полезният контекст от него, без тежките и омешани части.

## 3. Project references

Добавени бяха следните зависимости между проектите:

- `SecureWallet.Application` -> `SecureWallet.Domain`
- `SecureWallet.Infrastructure` -> `SecureWallet.Application`
- `SecureWallet.Infrastructure` -> `SecureWallet.Domain`
- `SecureWallet.API` -> `SecureWallet.Application`
- `SecureWallet.API` -> `SecureWallet.Infrastructure`

Важно решение:
- `SecureWallet.API` не реферира директно `SecureWallet.Domain`

## 4. .NET SDK и build среда

### Проблем, който срещнахме
Първоначално Rider и терминалът използваха различни `dotnet` инсталации:

- Rider използваше:

```text
C:\Users\User\.dotnet\dotnet.exe
```

- Терминалът използваше:

```text
C:\Program Files\dotnet\dotnet.exe
```

Това водеше до проблем, при който от терминала се виждаше .NET 8 SDK вместо .NET 10 SDK.

### Как беше решено
Беше добавен `global.json` в root-а на solution-а:

```json
{
  "sdk": {
    "version": "10.0.100"
  }
}
```

Беше коригиран и `PATH` за текущия Windows user, така че `dotnet` по подразбиране да сочи към:

```text
C:\Users\User\.dotnet
C:\Users\User\.dotnet\tools
```

### Проверки
Бяха изпълнени следните проверки:

```powershell
where.exe dotnet
dotnet --version
dotnet --list-sdks
```

Очакваният резултат след корекцията беше:

- `dotnet --version` -> `10.0.100`

## 5. Първи build

Беше валидиран build на solution-а с:

```powershell
dotnet build
```

Успешният резултат беше:

- `SecureWallet.Domain`
- `SecureWallet.Application`
- `SecureWallet.Infrastructure`
- `SecureWallet.API`

всички build-ват успешно на `net10.0`.

## 6. Първоначално почистване на проекта

Премахнати бяха template файловете, които идват по подразбиране:

- `SecureWallet.Domain/Class1.cs`
- `SecureWallet.Application/Class1.cs`
- `SecureWallet.Infrastructure/Class1.cs`
- `SecureWallet.API/WeatherForecast.cs`
- `SecureWallet.API/Controllers/WeatherForecastController.cs`
- `SecureWallet.API/SecureWallet.API.http`

## 7. Начална папкова структура

Създадена беше начална архитектурна структура в проектите.

### Domain
- `Entities`
- `Enums`
- `ValueObjects`
- `Constants`
- `Exceptions`

### Application
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

### Infrastructure
- `Data`
- `Data/Configurations`
- `Data/Migrations`
- `Data/Seed`
- `Repositories`
- `Security`
- `Services`
- `Logging`

### API
- `Controllers`
- `Requests`
- `Responses`
- `Middleware`
- `Extensions`

### Frontend
- `securewallet.frontend`
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

## 8. Git setup

### .gitignore
- Добавен беше `.gitignore` в root-а на проекта.
- След това `.gitignore` беше редактиран допълнително ръчно.

### Инициализация на Git repo
В root-а на проекта беше инициализирано Git repository:

```powershell
git init
```

### Добавяне на файловете

```powershell
git add .
```

### Първи commit

```powershell
git commit -m "Initial solution structure"
```

### Създаване на GitHub repo
В GitHub беше създадено repository:

```text
SecureWallet
```

Настройките при създаването бяха:
- без `README`
- без `.gitignore` от GitHub
- без `license`

### Свързване на локалното repo с GitHub

Използван е `SSH` remote:

```powershell
git remote add origin git@github.com:nikola369nikolaev/SecureWallet.git
```

### Преименуване на branch

```powershell
git branch -M main
```

### Първи push към GitHub

```powershell
git push -u origin main
```

Това качи локалния проект успешно в GitHub и настрои tracking към `origin/main`.

## 9. Важно уточнение

Git не пази празни папки. Това означава, че в GitHub ще се виждат само директориите, в които има реални файлове.

## 10. Състояние след началния setup

След всички тези стъпки проектът е:

- създаден като multi-project solution
- настроен на `.NET 10`
- build-ва успешно
- version-controlled с Git
- качен в GitHub
- с подготвен начален clean architecture skeleton
