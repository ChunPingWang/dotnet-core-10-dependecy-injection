# .NET Core 10 依賴注入 (Dependency Injection) 完整教學指南

> 適合初學者的完整 DI 教學，包含大量實際案例

---

## 目錄

1. [什麼是依賴注入？](#1-什麼是依賴注入)
2. [為什麼需要依賴注入？](#2-為什麼需要依賴注入)
3. [.NET Core DI 核心概念](#3-net-core-di-核心概念)
4. [服務生命週期](#4-服務生命週期)
5. [基礎範例：從零開始](#5-基礎範例從零開始)
6. [介面抽象化](#6-介面抽象化)
7. [進階模式](#7-進階模式)
8. [真實世界案例：電商 API](#8-真實世界案例電商-api)
9. [常見錯誤與最佳實踐](#9-常見錯誤與最佳實踐)

---

## 1. 什麼是依賴注入？

**依賴注入 (Dependency Injection, DI)** 是一種設計模式，它讓你的物件不需要自己建立所需的依賴物件，而是由外部容器提供。

### 生活化比喻

想像你去咖啡廳點咖啡：
- **沒有 DI**：你自己種咖啡豆、烘焙、研磨、沖泡
- **有 DI**：你只說「我要一杯拿鐵」，咖啡廳負責處理所有細節

```
┌─────────────────────────────────────────────────────────┐
│                    沒有 DI 的世界                         │
│                                                         │
│  OrderService                                           │
│  ┌────────────────────────────────────┐                 │
│  │  var db = new DatabaseConnection() │  ← 自己建立依賴  │
│  │  var logger = new FileLogger()     │  ← 自己建立依賴  │
│  │  var email = new EmailSender()     │  ← 自己建立依賴  │
│  └────────────────────────────────────┘                 │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                     有 DI 的世界                          │
│                                                         │
│  DI 容器                                                 │
│  ┌──────────────┐                                        │
│  │ DatabaseConn │──────┐                                │
│  │ FileLogger   │──────┼──→ OrderService(db, log, email)│
│  │ EmailSender  │──────┘                                │
│  └──────────────┘                                       │
└─────────────────────────────────────────────────────────┘
```

---

## 2. 為什麼需要依賴注入？

### 問題：緊耦合 (Tight Coupling)

```csharp
// ❌ 不好的設計：緊耦合
public class OrderService
{
    private readonly SqlDatabase _database;
    private readonly FileLogger _logger;

    public OrderService()
    {
        // 直接建立具體實作 → 無法替換、無法測試
        _database = new SqlDatabase("connection_string");
        _logger = new FileLogger("app.log");
    }

    public void PlaceOrder(Order order)
    {
        _logger.Log($"下訂單: {order.Id}");
        _database.Save(order);
    }
}

// 問題：
// 1. 無法在測試時換成假資料庫
// 2. 換資料庫需要修改 OrderService
// 3. 每次建立 OrderService 都要知道所有依賴的細節
```

### 解決方案：依賴注入

```csharp
// ✅ 好的設計：鬆耦合
public class OrderService
{
    private readonly IDatabase _database;
    private readonly ILogger _logger;

    // 依賴由外部注入，不自己建立
    public OrderService(IDatabase database, ILogger logger)
    {
        _database = database;
        _logger = logger;
    }

    public void PlaceOrder(Order order)
    {
        _logger.Log($"下訂單: {order.Id}");
        _database.Save(order);
    }
}

// 好處：
// 1. 測試時可注入假資料庫 (MockDatabase)
// 2. 換資料庫不需修改 OrderService
// 3. 依賴關係清晰可見
```

---

## 3. .NET Core DI 核心概念

### 三個重要角色

| 角色 | 說明 | .NET 類別 |
|------|------|-----------|
| **服務 (Service)** | 要被注入的物件 | 任何 class 或 interface |
| **容器 (Container)** | 管理所有服務的建立與生命週期 | `IServiceCollection` / `IServiceProvider` |
| **消費者 (Consumer)** | 需要使用服務的物件 | Controller / Service / etc. |

### 基本註冊流程

```csharp
// Program.cs (.NET 10)
var builder = WebApplication.CreateBuilder(args);

// 1. 向容器註冊服務
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddSingleton<IAppConfig, AppConfig>();

var app = builder.Build();

// 2. 使用服務
app.MapGet("/order/{id}", (IOrderRepository repo, int id) =>
    repo.GetById(id));

app.Run();
```

### 注入方式

#### 建構子注入（最常用，推薦）

```csharp
public class OrderService
{
    private readonly IOrderRepository _repo;
    private readonly IEmailService _email;

    // 建構子注入：依賴在建立物件時自動提供
    public OrderService(IOrderRepository repo, IEmailService email)
    {
        _repo = repo;
        _email = email;
    }
}
```

#### 方法注入（適合 Minimal API）

```csharp
// .NET 10 Minimal API 中直接在方法參數注入
app.MapPost("/orders", async (
    Order order,
    IOrderService orderService,    // 自動注入
    ILogger<Program> logger) =>    // 自動注入
{
    logger.LogInformation("收到新訂單");
    return await orderService.CreateAsync(order);
});
```

#### 屬性注入（較少用，需要 [FromServices]）

```csharp
// 在 Minimal API 中使用
app.MapGet("/products", ([FromServices] IProductService service) =>
    service.GetAll());
```

---

## 4. 服務生命週期

這是 DI 中最重要也最容易搞混的概念！

### 三種生命週期

```
Transient  ─── 每次要求就建立新的實體
               Request 1 ─→ Service A (instance #1)
               Request 1 ─→ Service A (instance #2)  ← 不同實體！
               Request 2 ─→ Service A (instance #3)

Scoped     ─── 同一個請求共用同一個實體
               Request 1 ─→ Service A (instance #1)
               Request 1 ─→ Service A (instance #1)  ← 相同實體！
               Request 2 ─→ Service A (instance #2)

Singleton  ─── 整個應用程式只有一個實體
               Request 1 ─→ Service A (instance #1)
               Request 2 ─→ Service A (instance #1)  ← 相同實體！
               Request 3 ─→ Service A (instance #1)
```

### 選擇指南

| 生命週期 | 使用時機 | 範例 |
|----------|----------|------|
| **Transient** | 輕量、無狀態的服務 | EmailFormatter, DataMapper |
| **Scoped** | 需要在同一請求中保持狀態 | DbContext, UnitOfWork |
| **Singleton** | 全域共用、建立成本高 | Configuration, Cache, HttpClient |

### 程式碼範例

```csharp
// 註冊不同生命週期
builder.Services.AddTransient<IEmailFormatter, EmailFormatter>();
builder.Services.AddScoped<ApplicationDbContext>();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

// 驗證生命週期差異
app.MapGet("/lifecycle-demo", (
    IServiceProvider sp) =>
{
    // 取得兩次 Transient 服務
    var t1 = sp.GetRequiredService<ITransientService>();
    var t2 = sp.GetRequiredService<ITransientService>();
    Console.WriteLine($"Transient 相同? {ReferenceEquals(t1, t2)}"); // False

    // 取得兩次 Singleton 服務
    var s1 = sp.GetRequiredService<ISingletonService>();
    var s2 = sp.GetRequiredService<ISingletonService>();
    Console.WriteLine($"Singleton 相同? {ReferenceEquals(s1, s2)}"); // True
});
```

---

## 5. 基礎範例：從零開始

> 專案位置：`src/01-BasicDI/`

這個範例展示最基本的 DI 使用方式，以「問候系統」為例。

### 範例說明

我們建立一個問候系統，支援多語言問候，展示如何用 DI 輕鬆切換實作。

### 完整程式碼

參考 [`src/01-BasicDI/Program.cs`](src/01-BasicDI/Program.cs)

**重點學習：**
- 如何定義服務介面
- 如何實作多個版本
- 如何向容器註冊
- 如何使用 `GetRequiredService<T>()` 手動取得服務

---

## 6. 介面抽象化

> 專案位置：`src/03-InterfaceAbstraction/`

**介面抽象化**是 DI 的靈魂，它讓你的程式碼依賴「能力」而非「實作」。

### 設計原則

```
依賴抽象，不依賴具體
(Depend on abstractions, not concretions)
                         ── SOLID 原則之 D
```

### 範例：通知系統

```csharp
// 定義「能力」（介面）
public interface INotificationService
{
    Task SendAsync(string to, string message);
}

// 實作 1：Email 通知
public class EmailNotificationService : INotificationService
{
    public async Task SendAsync(string to, string message)
    {
        Console.WriteLine($"[Email] 傳送給 {to}: {message}");
        await Task.CompletedTask;
    }
}

// 實作 2：SMS 通知
public class SmsNotificationService : INotificationService
{
    public async Task SendAsync(string to, string message)
    {
        Console.WriteLine($"[SMS] 傳送給 {to}: {message}");
        await Task.CompletedTask;
    }
}

// 消費者不需要知道實際使用哪種通知方式
public class UserRegistrationService
{
    private readonly INotificationService _notification;

    public UserRegistrationService(INotificationService notification)
    {
        _notification = notification;
    }

    public async Task RegisterAsync(User user)
    {
        // 儲存使用者...
        // 不管是 Email 還是 SMS，呼叫方式完全相同
        await _notification.SendAsync(user.Contact, "歡迎加入！");
    }
}
```

### 多個實作的選擇

```csharp
// 使用 Key 區分多個實作 (.NET 8+)
builder.Services.AddKeyedTransient<INotificationService, EmailNotificationService>("email");
builder.Services.AddKeyedTransient<INotificationService, SmsNotificationService>("sms");

// 使用時指定 Key
app.MapPost("/register", async (
    User user,
    [FromKeyedServices("email")] INotificationService emailNotifier,
    [FromKeyedServices("sms")] INotificationService smsNotifier) =>
{
    await emailNotifier.SendAsync(user.Email, "歡迎！");
    await smsNotifier.SendAsync(user.Phone, "歡迎！");
});
```

---

## 7. 進階模式

> 專案位置：`src/04-AdvancedPatterns/`

### 7.1 Options 模式（設定值注入）

Options 模式是 .NET 中注入設定值的標準方式。

```csharp
// 定義設定類別
public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}

// appsettings.json
{
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your@email.com",
    "Password": "your-password",
    "EnableSsl": true
  }
}

// Program.cs 註冊
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("SmtpSettings"));

// 在服務中使用
public class EmailService
{
    private readonly SmtpSettings _settings;

    public EmailService(IOptions<SmtpSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        using var client = new SmtpClient(_settings.Host, _settings.Port);
        client.EnableSsl = _settings.EnableSsl;
        // ...
    }
}
```

**Options 的三種型別比較：**

| 類型 | 說明 | 適用場景 |
|------|------|----------|
| `IOptions<T>` | 固定值，啟動時載入 | 一般設定 |
| `IOptionsSnapshot<T>` | 每次請求重新讀取 | 需要熱更新的設定 |
| `IOptionsMonitor<T>` | 即時監控變更 | 需要通知變更的場景 |

### 7.2 裝飾者模式 (Decorator Pattern)

在不修改原始類別的情況下，為服務添加功能。

```csharp
// 原始服務
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetAllAsync();
}

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    public ProductRepository(AppDbContext db) => _db = db;

    public async Task<Product?> GetByIdAsync(int id)
        => await _db.Products.FindAsync(id);

    public async Task<IEnumerable<Product>> GetAllAsync()
        => await _db.Products.ToListAsync();
}

// 裝飾者：加入快取功能
public class CachedProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;  // 包裝原始服務
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public CachedProductRepository(
        IProductRepository inner,
        IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        var key = $"product:{id}";
        if (_cache.TryGetValue(key, out Product? cached))
            return cached;

        var product = await _inner.GetByIdAsync(id);
        if (product != null)
            _cache.Set(key, product, CacheDuration);

        return product;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        const string key = "products:all";
        if (_cache.TryGetValue(key, out IEnumerable<Product>? cached))
            return cached!;

        var products = await _inner.GetAllAsync();
        _cache.Set(key, products, CacheDuration);
        return products;
    }
}

// 裝飾者：加入日誌功能
public class LoggingProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;
    private readonly ILogger<LoggingProductRepository> _logger;

    public LoggingProductRepository(
        IProductRepository inner,
        ILogger<LoggingProductRepository> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        _logger.LogInformation("查詢產品 {ProductId}", id);
        var stopwatch = Stopwatch.StartNew();
        var result = await _inner.GetByIdAsync(id);
        _logger.LogInformation("查詢完成，耗時 {Elapsed}ms", stopwatch.ElapsedMilliseconds);
        return result;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        _logger.LogInformation("查詢所有產品");
        return await _inner.GetAllAsync();
    }
}

// 在 Program.cs 中組合裝飾者（洋蔥式包裝）
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddMemoryCache();

builder.Services.AddScoped<IProductRepository>(sp =>
{
    var db = sp.GetRequiredService<ProductRepository>();
    var cache = sp.GetRequiredService<IMemoryCache>();
    var logger = sp.GetRequiredService<ILogger<LoggingProductRepository>>();

    // 從內到外：ProductRepository → CachedProductRepository → LoggingProductRepository
    IProductRepository repo = db;
    repo = new CachedProductRepository(repo, cache);
    repo = new LoggingProductRepository(repo, logger);
    return repo;
});
```

### 7.3 工廠模式 (Factory Pattern)

當服務的建立需要執行時資訊時，使用工廠模式。

```csharp
// 支付服務工廠
public interface IPaymentService
{
    Task<PaymentResult> ProcessAsync(decimal amount);
}

public interface IPaymentServiceFactory
{
    IPaymentService Create(string provider);
}

public class PaymentServiceFactory : IPaymentServiceFactory
{
    private readonly IServiceProvider _sp;

    public PaymentServiceFactory(IServiceProvider sp)
        => _sp = sp;

    public IPaymentService Create(string provider) => provider switch
    {
        "stripe"    => _sp.GetRequiredService<StripePaymentService>(),
        "paypal"    => _sp.GetRequiredService<PayPalPaymentService>(),
        "linepay"   => _sp.GetRequiredService<LinePayService>(),
        _ => throw new ArgumentException($"不支援的支付方式: {provider}")
    };
}

// 使用
public class CheckoutService
{
    private readonly IPaymentServiceFactory _factory;

    public CheckoutService(IPaymentServiceFactory factory)
        => _factory = factory;

    public async Task<OrderResult> CheckoutAsync(Cart cart, string paymentProvider)
    {
        var payment = _factory.Create(paymentProvider);
        var result = await payment.ProcessAsync(cart.Total);
        // ...
    }
}
```

### 7.4 泛型服務

```csharp
// 泛型 Repository
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

public class Repository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _db;
    private readonly DbSet<T> _set;

    public Repository(AppDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id)
        => await _set.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync()
        => await _set.ToListAsync();

    public async Task<T> AddAsync(T entity)
    {
        _set.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        _set.Update(entity);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _set.Remove(entity);
            await _db.SaveChangesAsync();
        }
    }
}

// 一次性為所有實體註冊
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// 使用時直接指定類型，不需要額外註冊
public class ProductService
{
    private readonly IRepository<Product> _products;
    private readonly IRepository<Category> _categories;

    public ProductService(
        IRepository<Product> products,
        IRepository<Category> categories)
    {
        _products = products;
        _categories = categories;
    }
}
```

---

## 8. 真實世界案例：電商 API

> 專案位置：`src/05-RealWorldWebAPI/`

這是一個完整的電商 API，展示 DI 在真實專案中的應用。

### 專案架構

```
05-RealWorldWebAPI/
├── Program.cs                    # 應用程式入口，DI 設定
├── Domain/
│   ├── Entities/
│   │   ├── Product.cs
│   │   ├── Order.cs
│   │   └── User.cs
│   └── Interfaces/               # 抽象層（介面定義）
│       ├── IProductRepository.cs
│       ├── IOrderService.cs
│       └── IEmailService.cs
├── Infrastructure/
│   ├── Repositories/             # 資料存取實作
│   │   └── ProductRepository.cs
│   └── Services/                 # 外部服務實作
│       └── EmailService.cs
├── Application/
│   └── Services/                 # 業務邏輯
│       └── OrderService.cs
└── Api/
    └── Endpoints/                # API 端點
        ├── ProductEndpoints.cs
        └── OrderEndpoints.cs
```

### 分層架構中的 DI

```
┌──────────────────────────────────────────────────────┐
│                    API 層 (Endpoints)                 │
│    只依賴 Application 層的介面                         │
└──────────────┬───────────────────────────────────────┘
               │ 注入 IOrderService, IProductRepository
┌──────────────▼───────────────────────────────────────┐
│                 Application 層 (Services)             │
│    包含業務邏輯，依賴 Domain 介面                       │
└──────────────┬───────────────────────────────────────┘
               │ 注入 IProductRepository, IEmailService
┌──────────────▼───────────────────────────────────────┐
│              Infrastructure 層 (Repositories)         │
│    實作 Domain 介面，處理資料存取                        │
└──────────────────────────────────────────────────────┘
```

完整程式碼請參考 [`src/05-RealWorldWebAPI/`](src/05-RealWorldWebAPI/) 目錄。

---

## 9. 常見錯誤與最佳實踐

### ⚠️ 常見錯誤 1：Captive Dependency（囚禁依賴）

```csharp
// ❌ 錯誤：Singleton 服務注入了 Scoped 服務
// Scoped 服務的生命週期被 Singleton 拉長了！
public class MySingleton
{
    private readonly IScopedService _scoped;  // 危險！

    public MySingleton(IScopedService scoped)
        => _scoped = scoped;
}

builder.Services.AddSingleton<MySingleton>();  // Singleton
builder.Services.AddScoped<IScopedService, ScopedService>();  // Scoped

// ✅ 正確做法：注入 IServiceProvider 並在方法中取得 Scoped 服務
public class MySingleton
{
    private readonly IServiceProvider _sp;

    public MySingleton(IServiceProvider sp)
        => _sp = sp;

    public void DoWork()
    {
        // 建立新的 Scope 來使用 Scoped 服務
        using var scope = _sp.CreateScope();
        var scoped = scope.ServiceProvider.GetRequiredService<IScopedService>();
        scoped.DoSomething();
    }
}
```

### ⚠️ 常見錯誤 2：服務定位器模式（反模式）

```csharp
// ❌ 不好：Service Locator 模式（隱藏了依賴關係）
public class OrderService
{
    private readonly IServiceProvider _sp;

    public OrderService(IServiceProvider sp)
        => _sp = sp;

    public void Process(Order order)
    {
        // 在方法內部才取得依賴 → 依賴關係不透明
        var email = _sp.GetRequiredService<IEmailService>();
        var repo = _sp.GetRequiredService<IOrderRepository>();
        // ...
    }
}

// ✅ 好：透過建構子注入（依賴關係明確）
public class OrderService
{
    private readonly IEmailService _email;
    private readonly IOrderRepository _repo;

    public OrderService(IEmailService email, IOrderRepository repo)
    {
        _email = email;
        _repo = repo;
    }
}
```

### ⚠️ 常見錯誤 3：DbContext 的生命週期

```csharp
// ❌ 危險：DbContext 不應該是 Singleton
builder.Services.AddSingleton<AppDbContext>();  // 絕對不要這樣做！

// ✅ 正確：DbContext 使用 Scoped（預設行為）
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
// AddDbContext 預設使用 Scoped
```

### ✅ 最佳實踐總結

```csharp
// 1. 驗證服務容器（開發環境）
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;    // 驗證 Scope 錯誤
    options.ValidateOnBuild = true;   // 啟動時驗證所有依賴
});

// 2. 使用 GetRequiredService 而非 GetService（明確拋出例外）
var service = sp.GetRequiredService<IMyService>();  // ✅ 找不到會拋出例外
var service = sp.GetService<IMyService>();          // ❌ 找不到傳回 null

// 3. 避免在建構子中做複雜邏輯
public class MyService
{
    private readonly IRepository _repo;

    public MyService(IRepository repo)
    {
        // ✅ 只做賦值
        _repo = repo;
        // ❌ 不要在這裡做 I/O、網路請求、複雜計算
    }
}

// 4. 善用 Extension Methods 組織服務註冊
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("Default")));
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        return services;
    }
}

// Program.cs 變得簡潔清楚
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);
```

---

## 學習路徑建議

```
初學者
  │
  ▼
[01-BasicDI] ──────→ 理解 DI 基本概念、容器使用
  │
  ▼
[02-ServiceLifetimes] ──→ 掌握三種生命週期
  │
  ▼
[03-InterfaceAbstraction] ──→ 學習介面設計與抽象化
  │
  ▼
[04-AdvancedPatterns] ──→ Options、Decorator、Factory 模式
  │
  ▼
[05-RealWorldWebAPI] ──→ 整合所有概念的完整專案

進階開發者
```

## 執行範例

```bash
# 執行基礎範例
cd src/01-BasicDI
dotnet run

# 執行服務生命週期範例
cd src/02-ServiceLifetimes
dotnet run

# 執行 Web API 範例
cd src/05-RealWorldWebAPI
dotnet run
# 開啟 https://localhost:5001/swagger 查看 API 文件
```

---

*本教學基於 .NET 10，所有範例皆可在 .NET 8+ 環境執行*
