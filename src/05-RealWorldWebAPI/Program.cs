// ============================================================
// 範例 05：真實世界電商 Web API
// ============================================================
// 展示 DI 在完整 Web API 專案中的應用：
//   - 分層架構（Domain / Application / Infrastructure / API）
//   - Extension Methods 組織服務登記
//   - Minimal API 方法參數注入
//   - 服務間的協作（OrderService 協調多個依賴）
// ============================================================

using RealWorldWebAPI.Api.Endpoints;
using RealWorldWebAPI.Application.Extensions;
using RealWorldWebAPI.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────
// 服務登記（Dependency Injection 設定）
// ─────────────────────────────────────────────────────────────

// 1. 使用 Extension Methods 按層登記服務
//    好處：Program.cs 簡潔，每層負責自己的服務管理
builder.Services
    .AddInfrastructureServices()   // Repository、Email 等基礎設施服務
    .AddApplicationServices();     // OrderService 等業務邏輯服務

// 2. API 文件（Swagger）
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = ".NET Core 10 DI 教學 - 電商 API",
        Version = "v1",
        Description = "展示依賴注入在真實 Web API 專案中的應用"
    });
});

// 3. 開發環境：啟用 DI 容器驗證（防止常見錯誤）
if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;    // 防止 Captive Dependency
        options.ValidateOnBuild = true;   // 啟動時驗證所有依賴
    });
}

// ─────────────────────────────────────────────────────────────
// 建立應用程式
// ─────────────────────────────────────────────────────────────
var app = builder.Build();

// ─────────────────────────────────────────────────────────────
// 中介軟體 (Middleware) 設定
// ─────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "電商 API v1");
        c.RoutePrefix = string.Empty; // 設定 Swagger 為首頁
    });
}

app.UseHttpsRedirection();

// ─────────────────────────────────────────────────────────────
// API 端點映射
// ─────────────────────────────────────────────────────────────
app.MapProductEndpoints();
app.MapOrderEndpoints();

// 首頁：顯示 API 簡介
app.MapGet("/", () => new
{
    Title = ".NET Core 10 依賴注入教學 - 電商 API",
    Version = "1.0.0",
    Description = "請開啟 /swagger 查看 API 文件",
    Endpoints = new[]
    {
        "GET    /api/products          - 查詢所有產品",
        "GET    /api/products/{id}     - 查詢指定產品",
        "POST   /api/products          - 新增產品",
        "PUT    /api/products/{id}     - 更新產品",
        "DELETE /api/products/{id}     - 刪除產品",
        "POST   /api/orders            - 建立訂單",
        "GET    /api/orders/{id}       - 查詢訂單",
        "GET    /api/orders/customer/{email} - 查詢客戶訂單",
        "POST   /api/orders/{id}/cancel     - 取消訂單",
        "PATCH  /api/orders/{id}/status     - 更新訂單狀態",
    }
})
.WithTags("Info");

app.Run();
