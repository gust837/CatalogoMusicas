using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CatalogoMusicas.DTOs;

namespace CatalogoMusicas.Services
{
    public class LetraService
    {
        private static readonly ConcurrentDictionary<int, LetraRespostaDTO> _cache = new();

        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public LetraService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task<LetraRespostaDTO> ObterLetraAsync(
            int musicaId,
            string titulo,
            string artista,
            string? albumTitulo = null,
            int duracaoMs = 0)
        {
            if (_cache.TryGetValue(musicaId, out var letraEmCache))
                return letraEmCache;

            string tituloLimpo = LimparTituloSpotify(titulo);
            int duracaoSegundos = duracaoMs > 0 ? (int)Math.Round(duracaoMs / 1000.0) : 0;

            var tarefaLrclib = BuscarNaLrclibAsync(tituloLimpo, artista, albumTitulo, duracaoSegundos);
            var tarefaYoutube = BuscarYoutubeVideoIdAsync(tituloLimpo, artista, duracaoSegundos);

            await Task.WhenAll(tarefaLrclib, tarefaYoutube);

            var (letraOriginal, lrcSincronizado) = await tarefaLrclib;
            string? youtubeId = await tarefaYoutube;

            var linhas = ConverterLrcParaLinhas(lrcSincronizado);

            string? textoBase = letraOriginal;
            if (string.IsNullOrWhiteSpace(textoBase) && linhas.Count > 0)
            {
                textoBase = string.Join("\n", linhas.Select(l => l.Texto));
            }

            var resultado = new LetraRespostaDTO
            {
                MusicaId = musicaId,
                Titulo = titulo,
                Artista = artista,
                YoutubeVideoId = youtubeId,
                TemSincronia = linhas.Count > 0,
                LetraOriginal = textoBase,
                TraducaoPortugues = null,
                LinhasSincronizadas = linhas
            };

            if (resultado.LetraOriginal != null || resultado.LinhasSincronizadas.Count > 0)
            {
                _cache.TryAdd(musicaId, resultado);
            }

            return resultado;
        }

        public async Task<string?> ObterTraducaoAsync(
            int musicaId,
            string titulo,
            string artista,
            string? albumTitulo = null,
            int duracaoMs = 0)
        {
            var dadosLetra = await ObterLetraAsync(musicaId, titulo, artista, albumTitulo, duracaoMs);

            if (dadosLetra.TraducaoPortugues != null)
                return dadosLetra.TraducaoPortugues;

            string tituloLimpo = LimparTituloSpotify(titulo);
            string? traducao = await TraduzirComGeminiAsync(dadosLetra.LetraOriginal, tituloLimpo, artista);

            dadosLetra.TraducaoPortugues = traducao;
            return traducao;
        }

        private async Task<(string? LetraPura, string? LrcSincronizado)> BuscarNaLrclibAsync(
            string titulo,
            string artista,
            string? album,
            int duracaoSegundos)
        {
            try
            {
                var urlGet = $"https://lrclib.net/api/get?track_name={Uri.EscapeDataString(titulo)}&artist_name={Uri.EscapeDataString(artista)}";
                if (!string.IsNullOrWhiteSpace(album) && duracaoSegundos > 0)
                {
                    urlGet += $"&album_name={Uri.EscapeDataString(album)}&duration={duracaoSegundos}";
                }

                var resultadoGet = await TentarGetLrclibAsync(urlGet);
                if (resultadoGet.LrcSincronizado != null)
                    return resultadoGet;

                var urlGetSimples = $"https://lrclib.net/api/get?track_name={Uri.EscapeDataString(titulo)}&artist_name={Uri.EscapeDataString(artista)}";
                var resultadoGetSimples = await TentarGetLrclibAsync(urlGetSimples);
                if (resultadoGetSimples.LrcSincronizado != null)
                    return resultadoGetSimples;

                var urlSearch = $"https://lrclib.net/api/search?track_name={Uri.EscapeDataString(titulo)}&artist_name={Uri.EscapeDataString(artista)}";
                var req = new HttpRequestMessage(HttpMethod.Get, urlSearch);
                req.Headers.UserAgent.ParseAdd("CatalogoMusicasSenai/1.0");

                var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode)
                    return (null, null);

                using var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                var array = json.RootElement;

                if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() == 0)
                    return (null, null);

                string? melhorPlain = null;
                string? melhorSynced = null;
                double menorDiferenca = double.MaxValue;

                foreach (var item in array.EnumerateArray())
                {
                    string? plain = item.TryGetProperty("plainLyrics", out var p) ? p.GetString() : null;
                    string? synced = item.TryGetProperty("syncedLyrics", out var s) ? s.GetString() : null;

                    if (string.IsNullOrWhiteSpace(synced))
                        continue;

                    double duracaoItem = item.TryGetProperty("duration", out var d) && d.ValueKind == JsonValueKind.Number
                        ? d.GetDouble()
                        : 0;

                    double diferenca = duracaoSegundos > 0 && duracaoItem > 0
                        ? Math.Abs(duracaoItem - duracaoSegundos)
                        : 0;

                    if (diferenca < menorDiferenca)
                    {
                        menorDiferenca = diferenca;
                        melhorPlain = plain;
                        melhorSynced = synced;
                    }
                }

                if (melhorSynced != null)
                    return (melhorPlain, melhorSynced);

                var primeiro = array[0];
                string? primeiraLetra = primeiro.TryGetProperty("plainLyrics", out var pl) ? pl.GetString() : null;
                return (primeiraLetra, null);
            }
            catch
            {
                return (null, null);
            }
        }

        private async Task<(string? LetraPura, string? LrcSincronizado)> TentarGetLrclibAsync(string url, int duracaoSegundos = 0)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd("CatalogoMusicasSenai/1.0");

            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
                return (null, null);

            using var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var root = json.RootElement;

            if (duracaoSegundos > 0 && root.TryGetProperty("duration", out var d) && d.ValueKind == JsonValueKind.Number)
            {
                if (Math.Abs(d.GetDouble() - duracaoSegundos) > 3)
                    return (null, null);
            }

            string? plain = root.TryGetProperty("plainLyrics", out var p) ? p.GetString() : null;
            string? synced = root.TryGetProperty("syncedLyrics", out var s) ? s.GetString() : null;

            return (plain, synced);
        }

        private async Task<string?> BuscarYoutubeVideoIdAsync(string titulo, string artista, int duracaoSegundos)
        {
            try
            {
                string[] buscas =
                [
                    $"\"Provided to YouTube\" {artista} {titulo}",
                    $"{artista} {titulo} official audio"
                ];

                string? melhorVideoId = null;
                int menorDiferenca = int.MaxValue;

                foreach (var busca in buscas)
                {
                    var url = $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(busca)}";
                    var html = await _http.GetStringAsync(url);

                    var matches = Regex.Matches(
                        html,
                        @"""videoRenderer"":\{""videoId"":""(?<id>[a-zA-Z0-9_-]{11})"".*?""lengthText"":\{.*?""simpleText"":""(?<len>\d+:\d{2})""\}");

                    foreach (Match m in matches.Take(5))
                    {
                        string id = m.Groups["id"].Value;
                        int segundosVideo = ConverterMinutosSegundos(m.Groups["len"].Value);

                        if (duracaoSegundos <= 0)
                            return id;

                        int diferenca = Math.Abs(segundosVideo - duracaoSegundos);
                        if (diferenca <= 2)
                            return id;

                        if (diferenca < menorDiferenca)
                        {
                            menorDiferenca = diferenca;
                            melhorVideoId = id;
                        }
                    }

                    if (melhorVideoId != null && menorDiferenca <= 5)
                        return melhorVideoId;
                }

                return melhorVideoId;
            }
            catch
            {
                return null;
            }
        }

        private static int ConverterMinutosSegundos(string textoTempo)
        {
            var partes = textoTempo.Split(':');
            if (partes.Length == 2 && int.TryParse(partes[0], out int min) && int.TryParse(partes[1], out int seg))
            {
                return (min * 60) + seg;
            }
            return 0;
        }

        private async Task<string?> TraduzirComGeminiAsync(string? letraOriginal, string titulo, string artista)
        {
            if (string.IsNullOrWhiteSpace(letraOriginal))
                return null;

            string? apiKey = _config["Gemini:ApiKey"] ?? _config["Spotify:Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return null;

            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={apiKey}";

                var prompt = $"Analise a letra da música '{titulo}' do artista '{artista}' abaixo.\n" +
                             "Regra 1: Se 100% da letra já estiver em português (sem nenhum verso em inglês ou outro idioma), responda única e exclusivamente a palavra: JA_EM_PORTUGUES\n" +
                             "Regra 2: Se a letra estiver em outro idioma, ou se for mista (misturar português com inglês ou outro idioma), devolva a letra completa em português do Brasil: mantenha iguais os versos que já estão em português e traduza apenas os versos estrangeiros mantendo o sentido poético/musical, as gírias e exatamente as mesmas quebras de linha.\n" +
                             "Regra 3: Retorne apenas a letra traduzida pura, sem introduções, sem títulos e sem explicações.\n\n" +
                             letraOriginal;

                var corpo = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                };

                var jsonEnvio = JsonSerializer.Serialize(corpo);
                var conteudo = new StringContent(jsonEnvio, Encoding.UTF8, "application/json");

                var resp = await _http.PostAsync(url, conteudo);
                if (!resp.IsSuccessStatusCode)
                    return null;

                using var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                string? respostaTexto = json.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString()?.Trim();

                if (string.IsNullOrWhiteSpace(respostaTexto) || respostaTexto.Contains("JA_EM_PORTUGUES", StringComparison.OrdinalIgnoreCase))
                    return null;

                return respostaTexto;
            }
            catch
            {
                return null;
            }
        }

        private static List<LinhaSincronizadaDTO> ConverterLrcParaLinhas(string? lrc)
        {
            var resultado = new List<LinhaSincronizadaDTO>();
            if (string.IsNullOrWhiteSpace(lrc))
                return resultado;

            var regex = new Regex(@"^\[(?<min>\d{2}):(?<seg>\d{2}\.\d{2,3})\](?<texto>.*)$");

            foreach (var linha in lrc.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var match = regex.Match(linha.Trim());
                if (!match.Success)
                    continue;

                int minutos = int.Parse(match.Groups["min"].Value);
                double segundos = double.Parse(match.Groups["seg"].Value, CultureInfo.InvariantCulture);
                string texto = match.Groups["texto"].Value.Trim();

                double totalSegundos = Math.Round((minutos * 60) + segundos, 2);
                string formatado = $"{minutos:D2}:{(int)segundos:D2}";

                resultado.Add(new LinhaSincronizadaDTO(totalSegundos, formatado, texto));
            }

            return resultado;
        }

        private static string LimparTituloSpotify(string titulo)
        {
            int indiceTraco = titulo.IndexOf(" - ", StringComparison.Ordinal);
            if (indiceTraco > 0)
                titulo = titulo[..indiceTraco];

            return titulo.Trim();
        }
    }
}