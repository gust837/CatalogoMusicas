using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CatalogoMusicas.Api.Services;

public class SpotifyService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    // O token vale 1 hora; guardamos para não pedir a cada chamada
    private string? _token;
    private DateTime _tokenExpiraEm;

    public SpotifyService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    // Fluxo Client Credentials: troca ClientId+Secret por um token de acesso
    private async Task<string> ObterTokenAsync()
    {
        if (_token != null && DateTime.UtcNow < _tokenExpiraEm)
            return _token;

        var credenciais = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{_config["Spotify:ClientId"]}:{_config["Spotify:ClientSecret"]}"));

        var req = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials"
            })
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", credenciais);

        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        _token = json.RootElement.GetProperty("access_token").GetString()!;
        var expiraEmSegundos = json.RootElement.GetProperty("expires_in").GetInt32();
        _tokenExpiraEm = DateTime.UtcNow.AddSeconds(expiraEmSegundos - 60); // margem de 1 min

        return _token;
    }

    // Busca faixas pelo texto e devolve o JSON cru do Spotify (para estudar a estrutura)
    public async Task<string> BuscarFaixasAsync(string texto, int offset = 0)
    {
        var token = await ObterTokenAsync();
        var url = $"https://api.spotify.com/v1/search?type=track&limit=10&offset={offset}" +
                  $"&q={Uri.EscapeDataString(texto)}";

        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync();
    }
}