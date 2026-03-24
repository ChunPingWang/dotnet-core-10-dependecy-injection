// ============================================================
// 範例 02：服務生命週期 (Service Lifetimes)
// ============================================================
// 學習目標：
//   1. 理解 Transient、Scoped、Singleton 的差異
//   2. 觀察不同生命週期的實體共用情況
//   3. 了解 Scope 的概念
//   4. 避免 Captive Dependency 問題
// ============================================================

using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("=== 範例 02：服務生命週期 ===\n");

// ─────────────────────────────────────────────────────────────
// 建立容器，同時登記三種生命週期的服務
// ─────────────────────────────────────────────────────────────
var services = new ServiceCollection();

services.AddTransient<ITransientCounter, Counter>();   // 每次都新建
services.AddScoped<IScopedCounter, Counter>();          // 同 Scope 共用
services.AddSingleton<ISingletonCounter, Counter>();    // 全域唯一

var rootProvider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateScopes = true,    // 開發時啟用，防止 Captive Dependency
    ValidateOnBuild = true    // 啟動時驗證所有依賴是否正確
});

// ─────────────────────────────────────────────────────────────
// PART 1：直接觀察生命週期差異
// ─────────────────────────────────────────────────────────────
Console.WriteLine("── PART 1：觀察實體 ID ──\n");

// 第一個 Scope（模擬第一個 HTTP 請求）
Console.WriteLine("【第一個 Scope（請求 1）】");
using (var scope1 = rootProvider.CreateScope())
{
    var sp = scope1.ServiceProvider;

    // 同一個 Scope 內，取得三次每種服務
    var t1a = sp.GetRequiredService<ITransientCounter>();
    var t1b = sp.GetRequiredService<ITransientCounter>();
    var s1a = sp.GetRequiredService<IScopedCounter>();
    var s1b = sp.GetRequiredService<IScopedCounter>();
    var g1a = sp.GetRequiredService<ISingletonCounter>();
    var g1b = sp.GetRequiredService<ISingletonCounter>();

    Console.WriteLine($"  Transient #1: ID={t1a.Id}");
    Console.WriteLine($"  Transient #2: ID={t1b.Id}  ← {(t1a.Id == t1b.Id ? "相同 ✓" : "不同（每次新建）")}");
    Console.WriteLine($"  Scoped    #1: ID={s1a.Id}");
    Console.WriteLine($"  Scoped    #2: ID={s1b.Id}  ← {(s1a.Id == s1b.Id ? "相同（同 Scope 共用）✓" : "不同")}");
    Console.WriteLine($"  Singleton #1: ID={g1a.Id}");
    Console.WriteLine($"  Singleton #2: ID={g1b.Id}  ← {(g1a.Id == g1b.Id ? "相同（全域唯一）✓" : "不同")}");
}

Console.WriteLine();

// 第二個 Scope（模擬第二個 HTTP 請求）
Console.WriteLine("【第二個 Scope（請求 2）】");
using (var scope2 = rootProvider.CreateScope())
{
    var sp = scope2.ServiceProvider;

    var t2 = sp.GetRequiredService<ITransientCounter>();
    var s2 = sp.GetRequiredService<IScopedCounter>();
    var g2 = sp.GetRequiredService<ISingletonCounter>();

    Console.WriteLine($"  Transient: ID={t2.Id}  ← 和請求 1 不同（每次新建）");
    Console.WriteLine($"  Scoped:    ID={s2.Id}  ← 和請求 1 不同（新 Scope）");
    Console.WriteLine($"  Singleton: ID={g2.Id}  ← 和請求 1 相同（全域唯一）✓");
}

Console.WriteLine();

// ─────────────────────────────────────────────────────────────
// PART 2：計數器行為差異
// ─────────────────────────────────────────────────────────────
Console.WriteLine("── PART 2：計數器行為（狀態差異）──\n");

var services2 = new ServiceCollection();
services2.AddTransient<ICounterService, CounterService>();
services2.AddScoped<IScopedCounterService, CounterService>();
services2.AddSingleton<IGlobalCounterService, CounterService>();

var sp2 = services2.BuildServiceProvider();

Console.WriteLine("模擬三個使用者各操作 2 次計數器：");
for (int user = 1; user <= 3; user++)
{
    Console.WriteLine($"\n  使用者 {user}：");
    using var scope = sp2.CreateScope();
    var scopeSp = scope.ServiceProvider;

    for (int i = 0; i < 2; i++)
    {
        var transient = scopeSp.GetRequiredService<ICounterService>();
        var scoped = scopeSp.GetRequiredService<IScopedCounterService>();
        var global = scopeSp.GetRequiredService<IGlobalCounterService>();

        transient.Increment();
        scoped.Increment();
        global.Increment();

        Console.WriteLine($"    操作 {i + 1}：Transient={transient.Count}, Scoped={scoped.Count}, Singleton={global.Count}");
    }
}

Console.WriteLine();
Console.WriteLine("結論：");
Console.WriteLine("  Transient → 每次取得都是新的，計數永遠是 1");
Console.WriteLine("  Scoped    → 同一請求共用，計數在同請求內累加");
Console.WriteLine("  Singleton → 全域共用，計數一直累加到應用程式結束");

Console.WriteLine();

// ─────────────────────────────────────────────────────────────
// PART 3：Captive Dependency 問題示範
// ─────────────────────────────────────────────────────────────
Console.WriteLine("── PART 3：Captive Dependency（囚禁依賴）問題 ──\n");

// ❌ 問題情境：Singleton 直接持有 Scoped 服務
// 這樣做的話，Scoped 服務的生命週期會被拉長到 Singleton 的範圍
// 建議啟用 ValidateScopes = true 讓容器在開發時警告

var badServices = new ServiceCollection();
badServices.AddScoped<IScopedDependency, ScopedDependency>();
badServices.AddSingleton<ProblematicSingleton>();

// ValidateScopes = false 才能建立（真實環境中這是個 Bug）
var badSp = badServices.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateScopes = false
});

Console.WriteLine("❌ 問題：Singleton 持有 Scoped 服務");
var problematic = badSp.GetRequiredService<ProblematicSingleton>();
Console.WriteLine($"  第 1 次取得 Scoped: ID={problematic.GetScopedId()}");
Console.WriteLine($"  第 2 次取得 Scoped: ID={problematic.GetScopedId()}  ← ID 相同！Scoped 被困住了");

Console.WriteLine();

// ✅ 正確做法：透過 IServiceProvider 和 CreateScope() 正確使用 Scoped 服務
var goodServices = new ServiceCollection();
goodServices.AddScoped<IScopedDependency, ScopedDependency>();
goodServices.AddSingleton<CorrectSingleton>();

var goodSp = goodServices.BuildServiceProvider();

Console.WriteLine("✅ 正確：透過 IServiceProvider 建立 Scope");
var correct = goodSp.GetRequiredService<CorrectSingleton>();
Console.WriteLine($"  第 1 次操作：");
correct.DoWork();
Console.WriteLine($"  第 2 次操作（新 Scope）：");
correct.DoWork();

Console.WriteLine("\n=== 範例完成 ===");

// ============================================================
// 服務定義
// ============================================================

// ─── 用於觀察實體 ID 的計數服務 ───

interface ITransientCounter { Guid Id { get; } }
interface IScopedCounter { Guid Id { get; } }
interface ISingletonCounter { Guid Id { get; } }

/// <summary>
/// 透過多個介面讓同一個類別可以用不同生命週期登記
/// 實際上 Transient、Scoped、Singleton 都是這個類別的不同實體
/// </summary>
class Counter : ITransientCounter, IScopedCounter, ISingletonCounter
{
    // 每個實體都有唯一的 ID
    public Guid Id { get; } = Guid.NewGuid();
}

// ─── 用於觀察計數器行為的服務 ───

interface ICounterService { int Count { get; } void Increment(); }
interface IScopedCounterService { int Count { get; } void Increment(); }
interface IGlobalCounterService { int Count { get; } void Increment(); }

class CounterService : ICounterService, IScopedCounterService, IGlobalCounterService
{
    private int _count = 0;
    public int Count => _count;
    public void Increment() => _count++;
}

// ─── 用於示範 Captive Dependency 的服務 ───

interface IScopedDependency { Guid Id { get; } }

class ScopedDependency : IScopedDependency
{
    public Guid Id { get; } = Guid.NewGuid();
}

/// <summary>
/// ❌ 有問題的設計：Singleton 直接持有 Scoped 服務
/// Scoped 服務的實體被困在 Singleton 中，生命週期被拉長
/// </summary>
class ProblematicSingleton
{
    private readonly IScopedDependency _scoped;

    public ProblematicSingleton(IScopedDependency scoped)
    {
        // 這個 scoped 實體永遠不會被釋放！
        _scoped = scoped;
    }

    public Guid GetScopedId() => _scoped.Id;
}

/// <summary>
/// ✅ 正確的設計：Singleton 透過 IServiceProvider 動態取得 Scoped 服務
/// </summary>
class CorrectSingleton
{
    private readonly IServiceProvider _sp;

    public CorrectSingleton(IServiceProvider sp)
    {
        _sp = sp;
    }

    public void DoWork()
    {
        // 建立新的 Scope，在 Scope 結束時 Scoped 服務會被釋放
        using var scope = _sp.CreateScope();
        var scoped = scope.ServiceProvider.GetRequiredService<IScopedDependency>();
        Console.WriteLine($"    使用 Scoped 服務: ID={scoped.Id}");
    } // scope 在這裡被 Dispose，scoped 服務也跟著釋放
}
