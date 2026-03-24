// ============================================================
// 範例 03：介面抽象化 (Interface Abstraction)
// ============================================================
// 學習目標：
//   1. 理解「依賴抽象，不依賴具體」的原則
//   2. 多個實作的切換與選擇
//   3. 使用 Keyed Services 管理多個同介面實作
//   4. 透過介面讓程式碼易於測試
// ============================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Console.WriteLine("=== 範例 03：介面抽象化 ===\n");

// ─────────────────────────────────────────────────────────────
// PART 1：基本介面替換
// ─────────────────────────────────────────────────────────────
Console.WriteLine("── PART 1：儲存服務的多種實作 ──\n");

// 模擬「開發環境」設定：用記憶體儲存
var devServices = new ServiceCollection();
devServices.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));
devServices.AddSingleton<IStorageService, InMemoryStorageService>();  // 開發用
devServices.AddTransient<DocumentManager>();

var devSp = devServices.BuildServiceProvider();
var devManager = devSp.GetRequiredService<DocumentManager>();

Console.WriteLine("【開發環境 - 使用記憶體儲存】");
devManager.SaveDocument("report.pdf", "PDF 內容...");
devManager.SaveDocument("notes.txt", "筆記內容...");
var docs = devManager.ListDocuments();
Console.WriteLine($"  已儲存 {docs.Count()} 個文件: {string.Join(", ", docs)}");

Console.WriteLine();

// 模擬「生產環境」設定：用 "雲端" 儲存（這裡用 Console 模擬）
var prodServices = new ServiceCollection();
prodServices.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));
prodServices.AddSingleton<IStorageService, CloudStorageService>();    // 生產用
prodServices.AddTransient<DocumentManager>();

var prodSp = prodServices.BuildServiceProvider();
var prodManager = prodSp.GetRequiredService<DocumentManager>();

Console.WriteLine("【生產環境 - 使用雲端儲存】");
prodManager.SaveDocument("report.pdf", "PDF 內容...");
Console.WriteLine($"  （DocumentManager 的程式碼完全沒有改變！）");

Console.WriteLine();

// ─────────────────────────────────────────────────────────────
// PART 2：同時使用多個通知服務（IEnumerable 注入）
// ─────────────────────────────────────────────────────────────
Console.WriteLine("── PART 2：多通知管道（IEnumerable 注入）──\n");

var notifyServices = new ServiceCollection();
notifyServices.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));

// 登記多個相同介面的實作，DI 容器會全部提供
notifyServices.AddTransient<INotificationChannel, EmailNotificationChannel>();
notifyServices.AddTransient<INotificationChannel, SmsNotificationChannel>();
notifyServices.AddTransient<INotificationChannel, PushNotificationChannel>();
notifyServices.AddTransient<NotificationBroadcaster>();

var notifySp = notifyServices.BuildServiceProvider();
var broadcaster = notifySp.GetRequiredService<NotificationBroadcaster>();

Console.WriteLine("廣播通知給所有頻道：");
await broadcaster.BroadcastAsync("用戶123", "您的訂單已出貨！");

Console.WriteLine();

// ─────────────────────────────────────────────────────────────
// PART 3：Keyed Services（.NET 8+）
// ─────────────────────────────────────────────────────────────
Console.WriteLine("── PART 3：Keyed Services - 依名稱選擇服務 ──\n");

var keyedServices = new ServiceCollection();
keyedServices.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));

// 用 Key 區分多個同介面的實作
keyedServices.AddKeyedTransient<IPaymentProcessor, StripePaymentProcessor>("stripe");
keyedServices.AddKeyedTransient<IPaymentProcessor, PayPalPaymentProcessor>("paypal");
keyedServices.AddKeyedTransient<IPaymentProcessor, LinePayProcessor>("linepay");
keyedServices.AddTransient<PaymentGateway>();

var keyedSp = keyedServices.BuildServiceProvider();
var gateway = keyedSp.GetRequiredService<PaymentGateway>();

Console.WriteLine("使用不同支付方式：");
await gateway.ProcessPaymentAsync(1500m, "stripe");
await gateway.ProcessPaymentAsync(2000m, "paypal");
await gateway.ProcessPaymentAsync(500m, "linepay");

Console.WriteLine();

// ─────────────────────────────────────────────────────────────
// PART 4：用假實作進行測試（Mock）
// ─────────────────────────────────────────────────────────────
Console.WriteLine("── PART 4：測試友善設計（使用假實作）──\n");

// 在測試環境中，用假實作替換真實服務
var testServices = new ServiceCollection();
testServices.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));
testServices.AddSingleton<IStorageService, FakeStorageService>();      // 假的儲存
testServices.AddSingleton<IEmailService, FakeEmailService>();          // 假的 Email
testServices.AddTransient<OrderProcessingService>();

var testSp = testServices.BuildServiceProvider();
var orderService = testSp.GetRequiredService<OrderProcessingService>();

Console.WriteLine("【測試環境：使用假服務，不會真的儲存或發 Email】");
await orderService.ProcessOrderAsync(new Order(42, "小明", 3500m));

Console.WriteLine("\n=== 範例完成 ===");

// ============================================================
// 服務定義
// ============================================================

// ─── 儲存服務 ───

interface IStorageService
{
    Task SaveAsync(string key, string content);
    Task<string?> LoadAsync(string key);
    IEnumerable<string> ListKeys();
}

class InMemoryStorageService : IStorageService
{
    private readonly Dictionary<string, string> _store = new();

    public Task SaveAsync(string key, string content)
    {
        _store[key] = content;
        Console.WriteLine($"  [記憶體] 儲存: {key}");
        return Task.CompletedTask;
    }

    public Task<string?> LoadAsync(string key)
        => Task.FromResult(_store.TryGetValue(key, out var v) ? v : null);

    public IEnumerable<string> ListKeys() => _store.Keys;
}

class CloudStorageService : IStorageService
{
    public Task SaveAsync(string key, string content)
    {
        Console.WriteLine($"  [雲端儲存] 上傳至 gs://my-bucket/{key} ({content.Length} bytes)");
        return Task.CompletedTask;
    }

    public Task<string?> LoadAsync(string key)
    {
        Console.WriteLine($"  [雲端儲存] 下載: gs://my-bucket/{key}");
        return Task.FromResult<string?>("雲端內容");
    }

    public IEnumerable<string> ListKeys()
    {
        Console.WriteLine("  [雲端儲存] 列出所有物件");
        return [];
    }
}

class FakeStorageService : IStorageService
{
    public readonly List<(string Key, string Content)> SavedItems = new();

    public Task SaveAsync(string key, string content)
    {
        SavedItems.Add((key, content));
        Console.WriteLine($"  [假儲存] 記錄呼叫: SaveAsync({key})");
        return Task.CompletedTask;
    }

    public Task<string?> LoadAsync(string key) => Task.FromResult<string?>(null);
    public IEnumerable<string> ListKeys() => SavedItems.Select(i => i.Key);
}

/// <summary>文件管理器：只依賴 IStorageService 介面，不知道實際如何儲存</summary>
class DocumentManager
{
    private readonly IStorageService _storage;

    public DocumentManager(IStorageService storage)
        => _storage = storage;

    public void SaveDocument(string name, string content)
        => _storage.SaveAsync(name, content).Wait();

    public IEnumerable<string> ListDocuments()
        => _storage.ListKeys();
}

// ─── 通知服務 ───

interface INotificationChannel
{
    string ChannelName { get; }
    Task SendAsync(string userId, string message);
}

class EmailNotificationChannel : INotificationChannel
{
    public string ChannelName => "Email";
    public Task SendAsync(string userId, string message)
    {
        Console.WriteLine($"  [Email] → 用戶 {userId}: {message}");
        return Task.CompletedTask;
    }
}

class SmsNotificationChannel : INotificationChannel
{
    public string ChannelName => "SMS";
    public Task SendAsync(string userId, string message)
    {
        Console.WriteLine($"  [SMS] → 用戶 {userId}: {message}");
        return Task.CompletedTask;
    }
}

class PushNotificationChannel : INotificationChannel
{
    public string ChannelName => "推播";
    public Task SendAsync(string userId, string message)
    {
        Console.WriteLine($"  [推播] → 用戶 {userId}: {message}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// 廣播器：注入所有通知頻道（IEnumerable<T>）
/// DI 容器會自動把所有 INotificationChannel 的實作都注入進來
/// </summary>
class NotificationBroadcaster
{
    private readonly IEnumerable<INotificationChannel> _channels;

    public NotificationBroadcaster(IEnumerable<INotificationChannel> channels)
        => _channels = channels;

    public async Task BroadcastAsync(string userId, string message)
    {
        var tasks = _channels.Select(c => c.SendAsync(userId, message));
        await Task.WhenAll(tasks);
        Console.WriteLine($"  已透過 {_channels.Count()} 個頻道廣播完成");
    }
}

// ─── 支付服務（Keyed Services）───

interface IPaymentProcessor
{
    Task<bool> ProcessAsync(decimal amount, string currency = "TWD");
}

class StripePaymentProcessor : IPaymentProcessor
{
    public Task<bool> ProcessAsync(decimal amount, string currency = "TWD")
    {
        Console.WriteLine($"  [Stripe] 處理 {amount:C0} {currency} 的付款");
        return Task.FromResult(true);
    }
}

class PayPalPaymentProcessor : IPaymentProcessor
{
    public Task<bool> ProcessAsync(decimal amount, string currency = "TWD")
    {
        Console.WriteLine($"  [PayPal] 處理 {amount:C0} {currency} 的付款");
        return Task.FromResult(true);
    }
}

class LinePayProcessor : IPaymentProcessor
{
    public Task<bool> ProcessAsync(decimal amount, string currency = "TWD")
    {
        Console.WriteLine($"  [LINE Pay] 處理 {amount:C0} {currency} 的付款");
        return Task.FromResult(true);
    }
}

/// <summary>
/// 支付閘道：根據使用者選擇的支付方式，使用對應的處理器
/// </summary>
class PaymentGateway
{
    private readonly IServiceProvider _sp;

    public PaymentGateway(IServiceProvider sp)
        => _sp = sp;

    public async Task ProcessPaymentAsync(decimal amount, string provider)
    {
        var processor = _sp.GetRequiredKeyedService<IPaymentProcessor>(provider);
        var success = await processor.ProcessAsync(amount);
        Console.WriteLine($"  結果：{(success ? "成功 ✓" : "失敗 ✗")}");
    }
}

// ─── 訂單服務（用於測試示範）───

record Order(int Id, string CustomerName, decimal Amount);

interface IEmailService
{
    Task SendOrderConfirmationAsync(Order order);
}

class FakeEmailService : IEmailService
{
    public Task SendOrderConfirmationAsync(Order order)
    {
        Console.WriteLine($"  [假 Email] 記錄呼叫：SendOrderConfirmation(訂單 #{order.Id})");
        return Task.CompletedTask;
    }
}

class OrderProcessingService
{
    private readonly IStorageService _storage;
    private readonly IEmailService _email;

    public OrderProcessingService(IStorageService storage, IEmailService email)
    {
        _storage = storage;
        _email = email;
    }

    public async Task ProcessOrderAsync(Order order)
    {
        Console.WriteLine($"  處理訂單 #{order.Id}（{order.CustomerName}，金額: {order.Amount:C0}）");
        await _storage.SaveAsync($"order:{order.Id}", $"Order data for {order.Id}");
        await _email.SendOrderConfirmationAsync(order);
        Console.WriteLine("  訂單處理完成！");
    }
}
