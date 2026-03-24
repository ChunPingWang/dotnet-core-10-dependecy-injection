// ============================================================
// 範例 04：進階 DI 模式 (Advanced DI Patterns)
// ============================================================
// 學習目標：
//   1. Options 模式 - 型別安全的設定值注入
//   2. 裝飾者模式 - 不修改原始程式碼增加功能
//   3. 工廠模式 - 執行時動態決定實作
//   4. 泛型服務 - 一次登記服務所有型別
// ============================================================

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

Console.WriteLine("=== 範例 04：進階 DI 模式 ===\n");

// 載入設定檔
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// ─────────────────────────────────────────────────────────────
// PART 1：Options 模式
// ─────────────────────────────────────────────────────────────
Console.WriteLine("── PART 1：Options 模式（型別安全設定）──\n");

var optionServices = new ServiceCollection();
optionServices.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));

// 將 appsettings.json 的設定綁定到強型別物件
optionServices.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
optionServices.Configure<CacheSettings>(configuration.GetSection("CacheSettings"));
optionServices.Configure<FeatureFlags>(configuration.GetSection("FeatureFlags"));

optionServices.AddTransient<EmailSender>();
optionServices.AddTransient<FeatureFlagService>();

var optionSp = optionServices.BuildServiceProvider();

var emailSender = optionSp.GetRequiredService<EmailSender>();
emailSender.ShowConfig();

Console.WriteLine();

var featureService = optionSp.GetRequiredService<FeatureFlagService>();
featureService.ShowFlags();

Console.WriteLine();

// ─────────────────────────────────────────────────────────────
// PART 2：裝飾者模式
// ─────────────────────────────────────────────────────────────
Console.WriteLine("── PART 2：裝飾者模式（洋蔥式功能疊加）──\n");

var decoratorServices = new ServiceCollection();
decoratorServices.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));
decoratorServices.AddMemoryCache();

// 登記原始實作
decoratorServices.AddScoped<ProductRepositoryBase>();

// 建立裝飾者鏈：ProductRepositoryBase → Cached → Logging
// 從外到內的順序：請求先進 Logging → 再進 Cache → 最後才到真正的 DB
decoratorServices.AddScoped<IProductRepository>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<LoggingProductRepositoryDecorator>>();
    var cache = sp.GetRequiredService<IMemoryCache>();
    var baseRepo = sp.GetRequiredService<ProductRepositoryBase>();

    IProductRepository repo = baseRepo;
    repo = new CachedProductRepositoryDecorator(repo, cache);   // 第二層：快取
    repo = new LoggingProductRepositoryDecorator(repo, logger); // 第一層（最外層）：日誌

    return repo;
});

var decoratorSp = decoratorServices.BuildServiceProvider();

using var scope = decoratorSp.CreateScope();
var productRepo = scope.ServiceProvider.GetRequiredService<IProductRepository>();

Console.WriteLine("第一次查詢（從資料庫載入）：");
var p1 = await productRepo.GetByIdAsync(1);
Console.WriteLine($"  結果: {p1?.Name ?? "找不到"}");

Console.WriteLine("\n第二次查詢（應從快取取得）：");
var p2 = await productRepo.GetByIdAsync(1);
Console.WriteLine($"  結果: {p2?.Name ?? "找不到"}");

Console.WriteLine("\n查詢所有產品：");
var all = await productRepo.GetAllAsync();
Console.WriteLine($"  共 {all.Count()} 個產品");

Console.WriteLine();

// ─────────────────────────────────────────────────────────────
// PART 3：工廠模式
// ─────────────────────────────────────────────────────────────
Console.WriteLine("── PART 3：工廠模式（動態建立服務）──\n");

var factoryServices = new ServiceCollection();
factoryServices.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));

// 登記各種匯出服務
factoryServices.AddTransient<CsvExporter>();
factoryServices.AddTransient<ExcelExporter>();
factoryServices.AddTransient<PdfExporter>();
factoryServices.AddTransient<IExporterFactory, ExporterFactory>();
factoryServices.AddTransient<ReportGenerator>();

var factorySp = factoryServices.BuildServiceProvider();
var reportGen = factorySp.GetRequiredService<ReportGenerator>();

var data = new[] { "銷售報告第一季", "總收入: NT$ 1,200,000", "訂單數: 1,523" };

Console.WriteLine("根據用戶選擇動態產生不同格式的報告：");
await reportGen.GenerateAsync(data, "csv");
await reportGen.GenerateAsync(data, "excel");
await reportGen.GenerateAsync(data, "pdf");

Console.WriteLine();

// ─────────────────────────────────────────────────────────────
// PART 4：泛型服務
// ─────────────────────────────────────────────────────────────
Console.WriteLine("── PART 4：泛型服務（一次登記，多型別適用）──\n");

var genericServices = new ServiceCollection();
genericServices.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));

// 一次登記泛型 Repository，所有實體類型都可以使用
genericServices.AddTransient(typeof(IRepository<>), typeof(InMemoryRepository<>));

// 不需要額外登記就能使用
// genericServices.AddTransient<IRepository<Product>, InMemoryRepository<Product>>(); ← 不需要
// genericServices.AddTransient<IRepository<Order>, InMemoryRepository<Order>>();     ← 不需要

var genericSp = genericServices.BuildServiceProvider();

// 使用 Product Repository
var productRepository = genericSp.GetRequiredService<IRepository<ProductEntity>>();
await productRepository.AddAsync(new ProductEntity(1, "MacBook Pro", 75000m));
await productRepository.AddAsync(new ProductEntity(2, "iPhone 16", 32000m));
await productRepository.AddAsync(new ProductEntity(3, "AirPods Pro", 8500m));

Console.WriteLine("Product Repository：");
var products = await productRepository.GetAllAsync();
foreach (var p in products)
    Console.WriteLine($"  [{p.Id}] {p.Name} - NT$ {p.Price:N0}");

// 使用 Order Repository（完全相同的 Repository 邏輯，不同型別）
var orderRepository = genericSp.GetRequiredService<IRepository<OrderEntity>>();
await orderRepository.AddAsync(new OrderEntity(1001, "小明", 75000m));
await orderRepository.AddAsync(new OrderEntity(1002, "小美", 40500m));

Console.WriteLine("\nOrder Repository：");
var orders = await orderRepository.GetAllAsync();
foreach (var o in orders)
    Console.WriteLine($"  [#{o.Id}] {o.CustomerName} - NT$ {o.Total:N0}");

Console.WriteLine("\n=== 範例完成 ===");

// ============================================================
// 服務定義
// ============================================================

// ─── Options 相關 ───

/// <summary>SMTP 設定（對應 appsettings.json 中的 SmtpSettings）</summary>
class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>快取設定</summary>
class CacheSettings
{
    public int DefaultExpirationMinutes { get; set; } = 10;
    public int MaxSizeInMb { get; set; } = 100;
}

/// <summary>功能開關設定</summary>
class FeatureFlags
{
    public bool EnableNewCheckout { get; set; }
    public bool EnableAIRecommendation { get; set; }
    public int MaxItemsPerPage { get; set; } = 10;
}

class EmailSender
{
    private readonly SmtpSettings _smtp;

    // IOptions<T>.Value 在建構時讀取設定值
    public EmailSender(IOptions<SmtpSettings> smtpOptions)
    {
        _smtp = smtpOptions.Value;
    }

    public void ShowConfig()
    {
        Console.WriteLine("  SMTP 設定（從 appsettings.json 載入）：");
        Console.WriteLine($"    Host: {_smtp.Host}:{_smtp.Port}");
        Console.WriteLine($"    Username: {_smtp.Username}");
        Console.WriteLine($"    SSL: {(_smtp.EnableSsl ? "啟用" : "停用")}");
        Console.WriteLine($"    顯示名稱: {_smtp.DisplayName}");
    }
}

class FeatureFlagService
{
    private readonly FeatureFlags _flags;
    private readonly CacheSettings _cache;

    public FeatureFlagService(IOptions<FeatureFlags> flags, IOptions<CacheSettings> cache)
    {
        _flags = flags.Value;
        _cache = cache.Value;
    }

    public void ShowFlags()
    {
        Console.WriteLine("  功能開關狀態：");
        Console.WriteLine($"    新版結帳流程: {(_flags.EnableNewCheckout ? "✓ 啟用" : "✗ 停用")}");
        Console.WriteLine($"    AI 推薦: {(_flags.EnableAIRecommendation ? "✓ 啟用" : "✗ 停用")}");
        Console.WriteLine($"    每頁最大筆數: {_flags.MaxItemsPerPage}");
        Console.WriteLine($"    快取時間: {_cache.DefaultExpirationMinutes} 分鐘");
    }
}

// ─── 裝飾者模式 ───

record Product(int Id, string Name, decimal Price);

interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetAllAsync();
}

/// <summary>基礎 Repository（模擬資料庫存取）</summary>
class ProductRepositoryBase : IProductRepository
{
    private static readonly Product[] _data =
    [
        new(1, "筆記型電腦", 35000m),
        new(2, "滑鼠", 800m),
        new(3, "鍵盤", 1500m),
        new(4, "螢幕", 12000m),
    ];

    public async Task<Product?> GetByIdAsync(int id)
    {
        Console.WriteLine($"  [資料庫] 查詢產品 #{id}...");
        await Task.Delay(10); // 模擬 I/O 延遲
        return _data.FirstOrDefault(p => p.Id == id);
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        Console.WriteLine("  [資料庫] 查詢所有產品...");
        await Task.Delay(20); // 模擬 I/O 延遲
        return _data;
    }
}

/// <summary>快取裝飾者：在原始 Repository 外加一層快取</summary>
class CachedProductRepositoryDecorator : IProductRepository
{
    private readonly IProductRepository _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public CachedProductRepositoryDecorator(IProductRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        var key = $"product:{id}";
        if (_cache.TryGetValue(key, out Product? cached))
        {
            Console.WriteLine($"  [快取] 命中！產品 #{id}");
            return cached;
        }

        Console.WriteLine($"  [快取] 未命中，向內層查詢...");
        var product = await _inner.GetByIdAsync(id);
        if (product != null)
            _cache.Set(key, product, Ttl);
        return product;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        const string key = "products:all";
        if (_cache.TryGetValue(key, out IEnumerable<Product>? cached))
        {
            Console.WriteLine("  [快取] 命中！所有產品");
            return cached!;
        }

        var products = await _inner.GetAllAsync();
        _cache.Set(key, products, Ttl);
        return products;
    }
}

/// <summary>日誌裝飾者：在原始 Repository 外加一層效能日誌</summary>
class LoggingProductRepositoryDecorator : IProductRepository
{
    private readonly IProductRepository _inner;
    private readonly ILogger<LoggingProductRepositoryDecorator> _logger;

    public LoggingProductRepositoryDecorator(
        IProductRepository inner,
        ILogger<LoggingProductRepositoryDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        Console.WriteLine($"  [日誌] 開始查詢產品 #{id}");
        var sw = Stopwatch.StartNew();
        var result = await _inner.GetByIdAsync(id);
        Console.WriteLine($"  [日誌] 查詢完成，耗時 {sw.ElapsedMilliseconds}ms");
        return result;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        Console.WriteLine("  [日誌] 開始查詢所有產品");
        var sw = Stopwatch.StartNew();
        var result = await _inner.GetAllAsync();
        Console.WriteLine($"  [日誌] 查詢完成，耗時 {sw.ElapsedMilliseconds}ms");
        return result;
    }
}

// ─── 工廠模式 ───

interface IDataExporter
{
    string Format { get; }
    Task ExportAsync(IEnumerable<string> data, string outputPath);
}

class CsvExporter : IDataExporter
{
    public string Format => "CSV";
    public Task ExportAsync(IEnumerable<string> data, string outputPath)
    {
        Console.WriteLine($"  [CSV 匯出] 寫入 {outputPath}");
        foreach (var line in data)
            Console.WriteLine($"    \"{line}\"");
        return Task.CompletedTask;
    }
}

class ExcelExporter : IDataExporter
{
    public string Format => "Excel";
    public Task ExportAsync(IEnumerable<string> data, string outputPath)
    {
        Console.WriteLine($"  [Excel 匯出] 寫入 {outputPath}");
        Console.WriteLine($"    建立工作表，共 {data.Count()} 行資料");
        return Task.CompletedTask;
    }
}

class PdfExporter : IDataExporter
{
    public string Format => "PDF";
    public Task ExportAsync(IEnumerable<string> data, string outputPath)
    {
        Console.WriteLine($"  [PDF 匯出] 寫入 {outputPath}");
        Console.WriteLine($"    產生 PDF，共 {data.Count()} 行資料");
        return Task.CompletedTask;
    }
}

interface IExporterFactory
{
    IDataExporter Create(string format);
}

/// <summary>
/// 匯出工廠：根據格式名稱動態建立對應的匯出器
/// 新增格式只需要新增 class 和更新工廠，不需要修改使用端
/// </summary>
class ExporterFactory : IExporterFactory
{
    private readonly IServiceProvider _sp;

    public ExporterFactory(IServiceProvider sp)
        => _sp = sp;

    public IDataExporter Create(string format) => format.ToLower() switch
    {
        "csv"   => _sp.GetRequiredService<CsvExporter>(),
        "excel" => _sp.GetRequiredService<ExcelExporter>(),
        "pdf"   => _sp.GetRequiredService<PdfExporter>(),
        _ => throw new NotSupportedException($"不支援的格式: {format}")
    };
}

class ReportGenerator
{
    private readonly IExporterFactory _factory;

    public ReportGenerator(IExporterFactory factory)
        => _factory = factory;

    public async Task GenerateAsync(IEnumerable<string> data, string format)
    {
        Console.WriteLine($"產生 {format.ToUpper()} 報告：");
        var exporter = _factory.Create(format);
        await exporter.ExportAsync(data, $"report.{format.ToLower()}");
    }
}

// ─── 泛型服務 ───

interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
}

class InMemoryRepository<T> : IRepository<T> where T : class
{
    private readonly List<T> _store = new();
    private readonly Func<T, int> _idSelector;

    public InMemoryRepository()
    {
        // 透過反射取得 Id 屬性（簡化示範，實際可用基底類別或介面）
        var idProp = typeof(T).GetProperty("Id")
            ?? throw new InvalidOperationException($"{typeof(T).Name} 必須有 Id 屬性");
        _idSelector = entity => (int)idProp.GetValue(entity)!;
    }

    public Task<T?> GetByIdAsync(int id)
        => Task.FromResult(_store.FirstOrDefault(e => _idSelector(e) == id));

    public Task<IEnumerable<T>> GetAllAsync()
        => Task.FromResult(_store.AsEnumerable());

    public Task<T> AddAsync(T entity)
    {
        _store.Add(entity);
        return Task.FromResult(entity);
    }
}

record ProductEntity(int Id, string Name, decimal Price);
record OrderEntity(int Id, string CustomerName, decimal Total);
