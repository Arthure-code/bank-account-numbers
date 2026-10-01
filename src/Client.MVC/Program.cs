using Client.MVC.Interfaces;
using Client.MVC.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

string adresseDeLApi = builder.Configuration.GetValue<string>("urlAPI")
    ?? throw new InvalidOperationException("The API address is missing from the configuration.");

builder.Services.AddHttpClient<INumerosProxy, NumerosProxy>(client =>
    client.BaseAddress = new Uri(adresseDeLApi));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
