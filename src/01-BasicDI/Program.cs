// ============================================================
// 範例 01：基礎依賴注入 (Basic Dependency Injection)
// ============================================================
// 學習目標：
//   1. 理解為什麼需要 DI
//   2. 建立第一個 DI 容器
//   3. 手動解析服務
//   4. 建構子注入
// ============================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Console.WriteLine("=== 範例 01：基礎依賴注入 ===\n");

// ─────────────────────────────────────────────────────────────
// PART 1：沒有 DI 的世界（緊耦合）
// ─────────────────────────────────────────────────────────────
Console.WriteLine("── PART 1：沒有 DI 的傳統做法 ──");

var badGreeter = new BadGreeterService();
badGreeter.Greet("小明");

Console.WriteLine();

// ─────────────────────────────────────────────────────────────
// PART 2：建立 DI 容器並手動解析服務
// ─────────────────────────────────────────────────────────────
Console.WriteLine("── PART 2：建立 DI 容器 ──");

// Step 1: 建立 ServiceCollection（服務登記冊）
var services = new ServiceCollection();

// Step 2: 向容器登記服務
//   AddLogging → 加入日誌支援
//   AddTransient → 每次取得都是新實體
services.AddLogging(config => config.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddTransient<IGreetingFormatter, FormalGreetingFormatter>(); // 正式問候格式
services.AddTransient<GreeterService>();                               // 問候服務

// Step 3: 從 ServiceCollection 建立 ServiceProvider（實際提供服務的容器）
var serviceProvider = services.BuildServiceProvider();

// Step 4: 從容器取得服務（手動解析）
var greeter = serviceProvider.GetRequiredService<GreeterService>();
greeter.Greet("小華");
greeter.Greet("小美");

Console.WriteLine();

// ─────────────────────────────────────────────────────────────
// PART 3：切換實作（展示 DI 的彈性）
// ─────────────────────────────────────────────────────────────
Console.WriteLine("── PART 3：切換問候格式（只需更改一行）──");

var services2 = new ServiceCollection();
services2.AddLogging(config => config.AddConsole().SetMinimumLevel(LogLevel.Warning));

// 只需要改這一行，就能切換整個問候方式
services2.AddTransient<IGreetingFormatter, CasualGreetingFormatter>(); // 改成輕鬆格式

services2.AddTransient<GreeterService>();
var serviceProvider2 = services2.BuildServiceProvider();

var greeter2 = serviceProvider2.GetRequiredService<GreeterService>();
greeter2.Greet("大衛");
greeter2.Greet("小李");

Console.WriteLine();

// ─────────────────────────────────────────────────────────────
// PART 4：服務鏈（服務依賴其他服務）
// ─────────────────────────────────────────────────────────────
Console.WriteLine("── PART 4：服務依賴鏈 ──");

var services3 = new ServiceCollection();
services3.AddLogging(config => config.AddConsole().SetMinimumLevel(LogLevel.Warning));
services3.AddTransient<IGreetingFormatter, BilingualGreetingFormatter>(); // 雙語格式
services3.AddTransient<IMessageStore, InMemoryMessageStore>();            // 訊息儲存
services3.AddTransient<AdvancedGreeterService>();                         // 進階服務

var sp3 = services3.BuildServiceProvider();
var advancedGreeter = sp3.GetRequiredService<AdvancedGreeterService>();

advancedGreeter.Greet("Alice");
advancedGreeter.Greet("Bob");
advancedGreeter.Greet("Charlie");

Console.WriteLine("\n已儲存的訊息：");
var store = sp3.GetRequiredService<IMessageStore>();
foreach (var msg in store.GetAll())
    Console.WriteLine($"  • {msg}");

Console.WriteLine("\n=== 範例完成 ===");

// ============================================================
// 服務定義（通常放在獨立的 .cs 檔案）
// ============================================================

// ─── 反面教材：緊耦合的類別 ───
class BadGreeterService
{
    // 直接建立依賴，無法替換
    private readonly string _format = "您好，{0}！歡迎光臨。";

    public void Greet(string name)
    {
        // 直接 Console.WriteLine，無法替換成其他輸出方式
        Console.WriteLine($"[BadGreeter] " + string.Format(_format, name));
    }
}

// ─── 介面定義（抽象層）───

/// <summary>問候格式化介面</summary>
interface IGreetingFormatter
{
    string Format(string name);
}

/// <summary>訊息儲存介面</summary>
interface IMessageStore
{
    void Save(string message);
    IEnumerable<string> GetAll();
}

// ─── 具體實作 ───

/// <summary>正式問候格式</summary>
class FormalGreetingFormatter : IGreetingFormatter
{
    public string Format(string name) => $"您好，尊敬的 {name} 先生/女士，歡迎光臨！";
}

/// <summary>輕鬆問候格式</summary>
class CasualGreetingFormatter : IGreetingFormatter
{
    public string Format(string name) => $"嗨 {name}！好久不見～";
}

/// <summary>雙語問候格式</summary>
class BilingualGreetingFormatter : IGreetingFormatter
{
    public string Format(string name) => $"哈囉 {name}！Hello {name}!";
}

/// <summary>記憶體訊息儲存</summary>
class InMemoryMessageStore : IMessageStore
{
    private readonly List<string> _messages = new();

    public void Save(string message) => _messages.Add(message);
    public IEnumerable<string> GetAll() => _messages.AsReadOnly();
}

// ─── 服務類別（消費者）───

/// <summary>
/// 基礎問候服務
/// 透過建構子注入 IGreetingFormatter，不需要知道實際使用哪種格式
/// </summary>
class GreeterService
{
    private readonly IGreetingFormatter _formatter;
    private readonly ILogger<GreeterService> _logger;

    // 建構子注入：DI 容器會自動提供這兩個依賴
    public GreeterService(IGreetingFormatter formatter, ILogger<GreeterService> logger)
    {
        _formatter = formatter;
        _logger = logger;
    }

    public void Greet(string name)
    {
        var message = _formatter.Format(name);
        _logger.LogDebug("準備問候 {Name}", name);
        Console.WriteLine($"[GreeterService] {message}");
    }
}

/// <summary>
/// 進階問候服務
/// 展示服務依賴多個其他服務
/// </summary>
class AdvancedGreeterService
{
    private readonly IGreetingFormatter _formatter;
    private readonly IMessageStore _store;
    private readonly ILogger<AdvancedGreeterService> _logger;

    public AdvancedGreeterService(
        IGreetingFormatter formatter,
        IMessageStore store,
        ILogger<AdvancedGreeterService> logger)
    {
        _formatter = formatter;
        _store = store;
        _logger = logger;
    }

    public void Greet(string name)
    {
        var message = _formatter.Format(name);
        Console.WriteLine($"[AdvancedGreeter] {message}");

        // 儲存訊息記錄
        _store.Save($"{DateTime.Now:HH:mm:ss} - {message}");
        _logger.LogDebug("已問候 {Name} 並儲存記錄", name);
    }
}
