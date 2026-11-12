using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CatalogoMusicas.Services
{
    public class SpotifyService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        private string? _token;
        private DateTime _tokenExpiraEm;

        public SpotifyService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

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
            _tokenExpiraEm = DateTime.UtcNow.AddSeconds(expiraEmSegundos - 60);

            return _token;
        }

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

        public async Task<SpotifyTrackInfo?> ObterFaixaPorIdAsync(string spotifyTrackId)
        {
            var token = await ObterTokenAsync();
            var url = $"https://api.spotify.com/v1/tracks/{Uri.EscapeDataString(spotifyTrackId)}";

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
                return null;

            using var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var root = json.RootElement;

            var albumEl = root.GetProperty("album");
            string? releaseDateStr = albumEl.GetProperty("release_date").GetString();
            string? precision = albumEl.GetProperty("release_date_precision").GetString();
            DateOnly? dataLancamento = ConverterDataSpotify(releaseDateStr, precision);

            string? capaUrl = null;
            if (albumEl.TryGetProperty("images", out var imagesEl) && imagesEl.GetArrayLength() > 0)
            {
                capaUrl = imagesEl[0].GetProperty("url").GetString();
            }

            var artistas = new List<(string SpotifyId, string Nome)>();
            foreach (var artEl in root.GetProperty("artists").EnumerateArray())
            {
                artistas.Add((
                    artEl.GetProperty("id").GetString()!,
                    artEl.GetProperty("name").GetString()!
                ));
            }

            return new SpotifyTrackInfo(
                SpotifyId: root.GetProperty("id").GetString()!,
                Titulo: root.GetProperty("name").GetString()!,
                DuracaoMs: root.GetProperty("duration_ms").GetInt32(),
                AlbumSpotifyId: albumEl.GetProperty("id").GetString()!,
                AlbumTitulo: albumEl.GetProperty("name").GetString()!,
                AlbumDataLancamento: dataLancamento,
                AlbumDataPrecisao: precision,
                AlbumCapaUrl: capaUrl,
                Artistas: artistas
            );
        }

        private static DateOnly? ConverterDataSpotify(string? releaseDate, string? precision)
        {
            if (string.IsNullOrWhiteSpace(releaseDate))
                return null;

            string dataCompleta = precision switch
            {
                "year" => $"{releaseDate}-01-01",
                "month" => $"{releaseDate}-01",
                _ => releaseDate
            };

            if (DateOnly.TryParse(dataCompleta, out var data) && data >= new DateOnly(1900, 1, 1))
                return data;

            return null;
        }
    }

    public record SpotifyTrackInfo(
        string SpotifyId,
        string Titulo,
        int DuracaoMs,
        string AlbumSpotifyId,
        string AlbumTitulo,
        DateOnly? AlbumDataLancamento,
        string? AlbumDataPrecisao,
        string? AlbumCapaUrl,
        List<(string SpotifyId, string Nome)> Artistas
    );
}