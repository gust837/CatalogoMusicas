using System.ComponentModel.DataAnnotations;

namespace CatalogoMusicas.DTOs
{
    public class MusicaImportacaoSpotify
    {
        [Required(ErrorMessage = "O Id da música no Spotify é obrigatório.")]
        [StringLength(22)]
        public string SpotifyTrackId { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Descricao { get; set; }

        [Required(ErrorMessage = "Selecione pelo menos um gênero.")]
        [MinLength(1, ErrorMessage = "A música deve estar associada a pelo menos um gênero.")]
        public List<int> IdGeneros { get; set; } = new();
    }
}
