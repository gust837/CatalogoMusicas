using CatalogoMusicas.Models;

namespace CatalogoMusicas.DTOs
{
    public class MusicaRespostaDTO
    {
        public int Id { get; set; }
        public string? SpotifyId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public int DuracaoMs { get; set; }
        public string? AlbumTitulo { get; set; }
        public string? AlbumCapaUrl { get; set; }
        public DateOnly? AlbumDataLancamento { get; set; }
        public string? AlbumDataPrecisao { get; set; }
        public List<string> Artistas { get; set; } = new();
        public List<GeneroItemDTO> Generos { get; set; } = new();

        public static MusicaRespostaDTO Converter(Musica m) => new()
        {
            Id = m.Id,
            SpotifyId = m.SpotifyId,
            Titulo = m.Titulo,
            Descricao = m.Descricao,
            DuracaoMs = m.DuracaoMs,
            AlbumTitulo = m.Album?.Titulo,
            AlbumCapaUrl = m.Album?.CapaUrl,
            AlbumDataLancamento = m.Album?.DataLancamento,
            AlbumDataPrecisao = m.Album?.DataLancamentoPrecisao,
            Artistas = m.MusicaArtista
                .OrderBy(ma => ma.Ordem)
                .Select(ma => ma.Artista.Nome)
                .ToList(),
            Generos = m.Generos
                .Select(g => new GeneroItemDTO(g.Id, g.Nome))
                .ToList()
        };
    }

    public record GeneroItemDTO(int Id, string Nome);
}