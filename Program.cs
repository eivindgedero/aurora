using aurora.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<DiscordService>();
builder.Services.AddHostedService<AuroraService>();

builder.Services.AddHttpClient("AuroraForecast", client =>
{
    client.BaseAddress = new Uri(
        "https://services.swpc.noaa.gov/"
    );

    client.DefaultRequestHeaders.Add(
        "Accept",
        "application/json"
    );

    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("WeatherForecast", client =>
{
    client.BaseAddress = new Uri(
        "https://api.met.no/weatherapi/locationforecast/2.0/"
    );

    client.DefaultRequestHeaders.Add(
        "Accept",
        "application/json"
    );

    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "AuroraNotifier/1.0"
    );

    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "(github.com/eivindgedero/aurora)"
    );

    client.Timeout = TimeSpan.FromSeconds(30);
});


var host = builder.Build();

var discordService = host.Services.GetRequiredService<DiscordService>();
await discordService.InitializeAsync();

await host.RunAsync();