# SecureWallet Repository Layer Notes

## Каква е ролята на папката `Repositories`

Тази папка съдържа реалните имплементации на repository интерфейсите, които са дефинирани в:

- `SecureWallet.Application/Interfaces/Repositories`

Тоест:
- в `Application` описваме какво трябва да може системата
- в `Infrastructure` описваме как точно го правим чрез EF Core и `AppDbContext`

С други думи:
- `IUserRepository` казва "трябва да можем да намираме и записваме потребители"
- `UserRepository` казва "това ще стане чрез EF Core заявки към базата"

## Защо repository-тата са точно в `Infrastructure`

Причината е, че те вече зависят от технически детайли:

- `AppDbContext`
- `Entity Framework Core`
- `DbSet`
- `Include`
- `SaveChangesAsync`

Тези неща не трябва да са в `Application`, защото `Application` трябва да мисли в use-case логика, а не в конкретен database framework.

Затова структурата е:

- `Application` държи интерфейса
- `Infrastructure` държи реалната имплементация

## Общата логика на repository кода

Всеки repository в момента работи по прост и ясен начин:

1. получава `AppDbContext` през constructor
2. използва съответния `DbSet`
3. изпълнява read или write операция
4. при write извиква `SaveChangesAsync`

Пример:

```csharp
private readonly AppDbContext _appDbContext;

public UserRepository(AppDbContext appDbContext)
{
    _appDbContext = appDbContext;
}
```

Това означава:
- класът получава достъп до базата чрез dependency injection
- не си създава сам `AppDbContext`
- използва подадената вече конфигурирана инстанция

## Какво означава `AsNoTracking()`

Пример:

```csharp
return await _appDbContext.Users
    .AsNoTracking()
    .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
```

`AsNoTracking()` казва на EF Core:

- "прочети тези данни"
- "но не ги пази във вътрешния tracking механизъм за промени"

Защо е полезно:

- по-бързо е за read операции
- по-леко е като memory usage
- подходящо е, когато само четем и няма да променяме върнатия обект веднага

Тоест:
- за `GetBy...` операции е добър избор
- за write flow може да не го ползваме, ако искаме EF да следи обекта

## Какво означава `Include(...)`

Пример:

```csharp
.Include(user => user.Role)
```

Това казва:
- освен `User`, зареди и свързаната `Role`

Без `Include(...)`:
- EF Core може да върне само user данните
- а `Role` да остане незаредена

С `Include(...)`:
- получаваме и свързания обект
- това е удобно, ако use case-ът иска да знае ролята на потребителя веднага

Използваме `Include(...)` там, където има смисъл:
- `UserRepository` зарежда `Role`
- `WalletRepository` зарежда `User`
- `TransactionRepository` зарежда `SenderWallet` и `ReceiverWallet`

## Какво означава `SaveChangesAsync()`

Пример:

```csharp
await _appDbContext.Users.AddAsync(user, cancellationToken);
await _appDbContext.SaveChangesAsync(cancellationToken);
```

Това значи:
- казваме на EF Core да добави нов обект
- после реално записваме промяната в базата

Без `SaveChangesAsync()`:
- промяната стои само в memory / EF change tracker
- но не стига до базата

В момента избраният подход е:
- repository-то само завършва своята промяна

Тоест:
- `AddAsync` записва
- `UpdateAsync` записва

Това е по-прост подход и е добър за началния етап на проекта.

## Защо засега няма `DeleteAsync`

В момента умишлено не сме добавили `DeleteAsync` навсякъде, защото:

- още не сме изчистили всички бизнес правила около изтриване
- някои данни не трябва да се трият лесно, особено financial и security данни
- искаме първо да изградим стабилна логика за create/read/update

По-късно можем да решим:
- дали да има физическо изтриване
- дали да има soft delete
- кои таблици никога не трябва да се трият

## Логика на всеки repository

### `RoleRepository`

Отговаря за:
- намиране на роля по `Id`
- намиране на роля по име
- връщане на списък от роли

Той е най-простият repository, защото:
- в момента ролите са сравнително статични данни
- там няма write операции засега

### `UserRepository`

Отговаря за:
- намиране на user по `Id`
- намиране на user по `Email`
- намиране на user по `Username`
- добавяне на user
- обновяване на user

Той е ключов за:
- login
- register
- role checks
- future 2FA логика

### `WalletRepository`

Отговаря за:
- намиране на wallet по `Id`
- намиране на wallet по `UserId`
- добавяне на wallet
- обновяване на wallet

Това е важният слой за:
- баланс
- dashboard
- ownership логика

### `TransactionRepository`

Отговаря за:
- намиране на транзакция по `Id`
- връщане на транзакции за конкретен wallet
- добавяне на транзакция
- обновяване на транзакция

Тук има една важна логика:

```csharp
.Where(transaction =>
    transaction.SenderWalletId == walletId ||
    transaction.ReceiverWalletId == walletId)
```

Това означава:
- върни всички транзакции, в които wallet-ът участва
- независимо дали е подател или получател

След това:

```csharp
.OrderByDescending(transaction => transaction.CreatedAtUtc)
```

означава:
- най-новите транзакции да са първи

Това е най-практично за UI и history екрани.

## Защо naming-ът е `_appDbContext`

Избрано е:

```csharp
private readonly AppDbContext _appDbContext;
```

вместо:

```csharp
private readonly AppDbContext _dbContext;
```

Причината е:
- по-ясно се вижда какъв точно context използваме
- naming-ът е по-близък до реален production style
- ако някой ден има повече от един context, няма да стане объркване

## Какво може да се промени по-късно

В следващ етап е възможно да надградим repository слоя с:

- общ `BaseRepository`
- `DeleteAsync`
- pagination
- filtering
- transaction orchestration
- unit of work
- separation между command repository и query repository

Но засега това би било излишно усложняване.

Текущият вариант е:
- чист
- четим
- лесен за обяснение
- достатъчен за дипломния проект на този етап
