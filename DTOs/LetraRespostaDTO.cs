namespace CatalogoMusicas.DTOs
{
    public class LetraRespostaDTO
    {
        public int MusicaId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Artista {  get; set; } = string.Empty;
        public string? YoutubeVideoId { get; set; }
        public bool TemSincronia { get; set; }
        public string? LetraOriginal { get; set; }
        public string? TraducaoPortugues {  get; set; }
        public List<LinhaSincronizadaDTO> LinhasSincronizadas { get; set; } = new();

    }

    public record LinhaSincronizadaDTO(
        double TempoSegundos,
        string TempoFormatado,
        string Texto
    );
}
