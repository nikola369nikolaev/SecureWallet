# SecureWallet — Setup

Как да пуснеш проекта на нова машина.

## Изисквания
- .NET SDK 10
- Docker
- Node.js 20+
- EF Core CLI: `dotnet tool install --global dotnet-ef`

## 1. Конфигурация
Файлът `appsettings.json` не е в Git, затова го създай ръчно:
`SecureWallet.API/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=SecureWalletDb;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Key": "сложи-дълъг-случаен-ключ-минимум-32-символа",
    "Issuer": "SecureWallet.Api",
    "Audience": "SecureWallet.Client",
    "AccessTokenExpirationMinutes": 10
  },
  "Email": {
    "SmtpClient": "smtp-relay.brevo.com",
    "SmtpPort": 587,
    "Username": "<smtp-username>",
    "Password": "<smtp-key>",
    "FromEmail": "<имейл>",
    "FromName": "SecureWallet"
  },
  "SMS": {
    "SmtpClient": "smtp.abv.bg",
    "SmtpPort": 465,
    "Username": "<имейл>",
    "Password": "<парола>"
  }
}
```

## 2. База данни (PostgreSQL в контейнер)

```powershell
docker run --name securewallet-db -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=SecureWalletDb -p 5432:5432 -d postgres:16
```

## 3. Миграции
Контейнерът трябва да работи. После приложи миграциите:

```powershell
dotnet ef database update `
  --project .\SecureWallet.Infrastructure\SecureWallet.Infrastructure.csproj `
  --startup-project .\SecureWallet.API\SecureWallet.API.csproj
```

> Ако `dotnet ef migrations list` (със същите `--project`/`--startup-project`)
> не покаже нито една миграция, първо създай: `dotnet ef migrations add InitialCreate ...`

## 4. Backend

```powershell
dotnet run --project .\SecureWallet.API\SecureWallet.API.csproj --launch-profile http
```

API: http://localhost:5231

## 5. Frontend

```powershell
cd securewallet.frontend
npm install
npm run dev
```

Frontend: http://localhost:5173

## Вход (admin)
- Потребител: `admin`
- Парола: `Admin123`
