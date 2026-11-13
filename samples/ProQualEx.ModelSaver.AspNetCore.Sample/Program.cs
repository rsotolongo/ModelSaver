WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

WebApplication app = builder.Build();

app.UseRouting();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Order}/{action=Edit}/{id?}")
    .WithStaticAssets();

await app.RunAsync().ConfigureAwait(false);
