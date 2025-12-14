var builder = WebApplication.CreateBuilder(args);

// ✅ Добавляем все зависимости ДО Build()
builder.Services.AddRazorPages();
builder.Services.AddHttpClient("RestaurantApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7188/api/");
});
builder.Services.AddScoped<MenuService>();
builder.Services.AddScoped<DishService>();
builder.Services.AddScoped<OrderService>();

builder.Services.AddSession();



// Затем строим приложение
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();

app.MapRazorPages();
app.Run();
