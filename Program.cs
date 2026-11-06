using CatalogoMusicas.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton(sp => new SpotifyService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
    sp.GetRequiredService<IConfiguration>()));

var app = builder.Build();

app.MapGet("/api/spotify/buscar", async (string q, int? offset, SpotifyService spotify) =>
    Results.Content(await spotify.BuscarFaixasAsync(q, offset ?? 0), "application/json"));

app.Run();