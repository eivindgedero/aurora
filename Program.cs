using aurora.Services;
using aurora.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "AuroraNotifier";
});

builder.Services.AddSingleton<AuroraApi>();
builder.Services.AddSingleton<WeatherApi>();
builder.Services.AddSingleton<MoonApi>();
builder.Services.AddSingleton<DiscordService>();
builder.Services.AddHostedService<AuroraService>();

builder.Services.AddHttpClient("AuroraForecast", client =>
{
    client.BaseAddress = new Uri("https://services.swpc.noaa.gov/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("WeatherForecast", client =>
{
    client.BaseAddress = new Uri("https://api.met.no/weatherapi/locationforecast/2.0/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AuroraNotifier/1.0");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("(github.com/eivindgedero/aurora)");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("MoonPhase", client =>
{
    client.BaseAddress = new Uri("https://api.met.no/weatherapi/sunrise/3.0/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AuroraNotifier/1.0");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("(github.com/eivindgedero/aurora)");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var host = builder.Build();
await host.RunAsync();