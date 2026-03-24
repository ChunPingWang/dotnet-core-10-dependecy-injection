# .NET Core 10 依賴注入（Dependency Injection）教學專案

> 用「跑範例 + 看測試 + 驗證結果」學會 DI。這份教材特別適合剛接觸 .NET 與依賴注入的初學者。

---

## 你會在這個專案學到什麼

- 什麼是 DI，為什麼它能降低耦合。
- `ServiceCollection` / `ServiceProvider` 的基本用法。
- `Transient`、`Scoped`、`Singleton` 的差異。
- 為什麼「依賴抽象」會讓程式更容易擴充與測試。
- 如何把 DI 用在真實分層 Web API。
- 如何用自動化測試驗證 DI 設計是否合理。

---

## 專案地圖

| 專案 | 路徑 | 重點 |
| --- | --- | --- |
| 01-BasicDI | `src/01-BasicDI` | 第一個 DI 容器、建構子注入、緊耦合 vs 鬆耦合 |
| 02-ServiceLifetimes | `src/02-ServiceLifetimes` | `Transient` / `Scoped` / `Singleton` 的生命週期差異 |
| 03-InterfaceAbstraction | `src/03-InterfaceAbstraction` | 介面抽象化、多實作切換、依賴抽象 |
| 04-AdvancedPatterns | `src/04-AdvancedPatterns` | Options、Decorator、Factory、泛型服務 |
| 05-RealWorldWebAPI | `src/05-RealWorldWebAPI` | 真實分層架構、Extension Methods、Minimal API、服務協作 |
| RealWorldWebAPI.Tests | `tests/RealWorldWebAPI.Tests` | `xUnit` 測試，驗證服務邏輯、Repository 與 DI 註冊 |

---

## 建議學習順序

1. 先跑 `01-BasicDI`，理解「為什麼不要在 class 裡自己 `new` 依賴」。
2. 再看 `02-ServiceLifetimes`，理解不同生命週期會如何影響執行結果。
3. 接著看 `03-InterfaceAbstraction`，理解為什麼介面能讓切換實作更輕鬆。
4. 然後看 `04-AdvancedPatterns`，把 DI 和常見設計模式連起來。
5. 最後進入 `05-RealWorldWebAPI`，看 DI 如何用在比較接近正式產品的架構。
6. 每看完一段，再回頭跑測試，從驗證角度重新理解設計。

---

## 執行環境

- .NET 10 SDK
- 建議使用最新的 Visual Studio、VS Code 或 JetBrains Rider

確認 SDK：

```bash
dotnet --version
```

---

## 快速開始

先還原與建置整個 solution：

```bash
dotnet restore
dotnet build DependencyInjection.sln
```

執行全部測試：

```bash
dotnet test DependencyInjection.sln --nologo
```

如果你只想先體驗教學範例，可以逐一執行：

```bash
dotnet run --project src/01-BasicDI
dotnet run --project src/02-ServiceLifetimes
dotnet run --project src/03-InterfaceAbstraction
dotnet run --project src/04-AdvancedPatterns
dotnet run --project src/05-RealWorldWebAPI
```

啟動 Web API 後，開啟：

```text
http://localhost:5000
http://localhost:5000/swagger
```

實際埠號依本機啟動結果為準。

---

## 先用一個問題理解 DI

### 沒有 DI 時

當一個 service 在 class 內部直接建立依賴：

```csharp
public class OrderService
{
    private readonly SqlDatabase _database = new("connection_string");
    private readonly FileLogger _logger = new("app.log");
}
```

問題會很快出現：

- 類別直接綁死具體實作，難以替換。
- 測試時不容易放入假物件。
- 每次修改依賴都可能要改動業務邏輯。

### 有 DI 時

改成由外部注入依賴：

```csharp
public class OrderService
{
    private readonly IDatabase _database;
    private readonly ILogger _logger;

    public OrderService(IDatabase database, ILogger logger)
    {
        _database = database;
        _logger = logger;
    }
}
```

好處會很明顯：

- 依賴關係一眼可見。
- 可以替換不同實作。
- 測試時能注入 fake / stub / mock。
- 類別只專注自己的責任。

---

## 每個範例要觀察什麼

## 1. `01-BasicDI`

請重點比較：

- `BadGreeterService`：把格式和輸出寫死，幾乎不能測。
- `GreeterService`：透過建構子接收 `IGreetingFormatter` 與 `ILogger`。

你應該問自己：

- 如果要切換問候語格式，我需要改多少地方？
- 如果要測試 `GreeterService`，我能不能提供假的 formatter？

## 2. `02-ServiceLifetimes`

請觀察同一個服務在不同解析時機下，是否得到同一個實例。

要記住：

- `Transient`：每次取用都是新物件。
- `Scoped`：同一個 scope 內共用。
- `Singleton`：整個應用程式只有一份。

## 3. `03-InterfaceAbstraction`

這個範例適合用來理解「依賴抽象，不依賴細節」。

你應該觀察：

- 切換實作時，消費者類別幾乎不用改。
- 多個實作可以用集合方式注入。
- DI 容器負責組裝，而不是業務類別自己決定依賴。

## 4. `04-AdvancedPatterns`

這裡把 DI 和常見模式結合：

- Options：把設定集中管理。
- Decorator：不改原服務介面就能包裝額外行為。
- Factory：在執行時決定要用哪一個實作。
- Generic Services：讓共通邏輯更容易重用。

## 5. `05-RealWorldWebAPI`

這是整份教材最值得從測試角度閱讀的專案。

請優先看這幾個檔案：

- `src/05-RealWorldWebAPI/Program.cs`
- `src/05-RealWorldWebAPI/Application/Services/OrderService.cs`
- `src/05-RealWorldWebAPI/Application/Extensions/ServiceCollectionExtensions.cs`
- `src/05-RealWorldWebAPI/Infrastructure/Extensions/ServiceCollectionExtensions.cs`
- `src/05-RealWorldWebAPI/Api/Endpoints/OrderEndpoints.cs`

---

## 為什麼這個 repo 要從「測試」角度學 DI

DI 最大的價值之一，不只是好維護，而是**好驗證**。

如果一個類別：

- 依賴明確寫在建構子上。
- 只依賴介面。
- 不自己建立資料庫、寄信元件或 logger。

那它通常就更容易寫單元測試。

在這個 repo 裡，`OrderService` 是最佳示範：

- 它不直接碰 HTTP。
- 它不直接碰資料庫技術細節。
- 它只協調 `IOrderRepository`、`IProductRepository`、`IEmailService`、`ILogger<OrderService>`。

因此我們可以在測試中注入假的 repository / email service，專注驗證業務邏輯本身。

---

## 目前的自動化測試覆蓋了什麼

測試專案位置：

```text
tests/RealWorldWebAPI.Tests
```

目前包含三類測試：

### 1. `Application/OrderServiceTests.cs`

驗證核心業務邏輯：

- 建立訂單成功時，是否建立訂單、扣庫存、發送確認通知。
- 商品不存在時，是否正確丟出錯誤。
- 庫存不足時，是否阻止下單。
- 取消訂單時，是否恢復庫存並更新狀態。
- 已出貨訂單是否禁止取消。

### 2. `Infrastructure/InMemoryRepositoriesTests.cs`

驗證 in-memory repository 的基本行為：

- 分類查詢是否正確。
- 軟刪除後是否不再出現在有效資料中。
- 庫存是否不會被扣成負數。
- 客戶信箱查詢是否區分大小寫不敏感。
- 建立訂單時是否會分配流水號。

### 3. `DI/ServiceCollectionExtensionsTests.cs`

驗證 DI 註冊本身：

- `AddInfrastructureServices()` 是否註冊了正確的型別。
- `Scoped` 與 `Transient` 的行為是否符合預期。
- `AddApplicationServices()` + `AddInfrastructureServices()` 是否能真正解析並執行 `IOrderService`。

---

## 測試是怎麼寫的

這份教材刻意使用**簡單 fake 物件**，而不是一開始就引入 Moq。

這樣對初學者有兩個好處：

- 先理解「替換依賴」這件事本身。
- 先學會測試設計，再學 mocking framework。

例如 `OrderService` 的測試策略是：

1. 用假的 `IProductRepository` 控制商品與庫存。
2. 用假的 `IOrderRepository` 觀察是否真的建立或更新訂單。
3. 用假的 `IEmailService` 記錄有沒有發送通知。
4. 用 `NullLogger<T>` 避免把焦點放在 logging 細節上。

這正是 DI 在測試上的實際價值。

---

## 初學者建議的實作練習

你可以照下面順序自己動手：

1. 先讀 `OrderService.CreateOrderAsync()`。
2. 猜一個「庫存不足」的測試案例。
3. 打開 `tests/RealWorldWebAPI.Tests/Application/OrderServiceTests.cs`。
4. 比對你腦中的流程和測試是否一致。
5. 嘗試新增一個測試，例如：
   - 空訂單。
   - 重複商品。
   - 取消不存在的訂單。
   - 更新訂單狀態失敗。

---

## `05-RealWorldWebAPI` 的 DI 架構怎麼看

### Domain

- 放資料模型與介面。
- 不依賴 Infrastructure 的具體細節。

### Application

- 放業務邏輯。
- 依賴 Domain 介面，不直接依賴具體 repository。

### Infrastructure

- 實作 repository、email service 之類的技術細節。
- 這一層提供 Application 所需的具體能力。

### API

- 接 HTTP 請求。
- 透過 DI 取得 service 或 repository。
- 專注處理輸入、輸出與錯誤轉換。

這種分層的重點是：

- 業務邏輯不綁定框架細節。
- 技術細節可以替換。
- 單元測試可以集中在真正重要的邏輯。

---

## 驗證這個 solution 的建議流程

每次修改程式後，建議照這個順序檢查：

```bash
dotnet build DependencyInjection.sln
dotnet test DependencyInjection.sln --nologo
```

如果你正在研究 Web API，再額外啟動：

```bash
dotnet run --project src/05-RealWorldWebAPI
```

然後手動確認：

- `/` 首頁是否能正常回應。
- `/swagger` 是否能看到 API 文件。
- 建立訂單後，資料與庫存是否符合預期。

---

## 常見錯誤與你現在可以怎麼檢查

### 錯誤 1：在類別裡直接 `new` 依賴

檢查方式：

- 這個類別能不能直接被替換依賴？
- 這個類別能不能不用碰真實資料來源就做測試？

### 錯誤 2：生命週期選錯

檢查方式：

- 這個服務是不是不該跨 request 共用？
- 它是否依賴了比較短生命週期的物件？

### 錯誤 3：API 直接塞滿業務邏輯

檢查方式：

- controller / endpoint 是否只做輸入輸出轉換？
- 商業規則是否集中在 service？

### 錯誤 4：只有功能，沒有驗證

檢查方式：

- 新增 service 時，有沒有至少一個成功案例與一個失敗案例測試？
- 新增註冊方法時，有沒有驗證容器真的能解析？

---

## 如果你要繼續擴充這個 repo

推薦下一步：

- 為 `OrderEndpoints` 補 integration tests。
- 幫 `ProductEndpoints` 新增 API 測試。
- 把 repository 從 in-memory 換成 EF Core，並保留既有測試思維。
- 新增更多錯誤案例測試，而不是只測 happy path。

---

## 最後總結

這個專案不是只要你「會註冊服務」而已，而是要你理解：

- 為什麼要這樣設計。
- 這樣設計如何讓程式更容易改。
- 這樣設計如何讓測試更容易寫。

當你能同時看懂 `Program.cs`、`OrderService.cs` 和對應測試時，你就已經不只是會用 DI，而是真的開始會設計可維護的 DI 程式了。
