using System.ComponentModel.DataAnnotations;

namespace CatalogoMusicas.DTOs
{
    public class MusicaDTO : IValidatableObject
    {
        [Required(ErrorMessage = "O título da música é obrigatório.")]
        [StringLength(250)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Descricao { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A duração em milissegundos deve ser maior do que zero.")]
        public int Duracao { get; set; }

        [StringLength(22)]
        public string? SpotifyId { get; set; }

        [StringLength(250)]
        public string? AlbumTitulo { get; set; }

        public DateOnly? AlbumDataLancamento { get; set; }

        [RegularExpression("^(year|month|day)$", ErrorMessage = "A precisão deve ser 'year', 'month' ou 'day'")]
        public string? AlbumDataPrecisao { get; set; }

        public string? AlbumCapaUrl {  get; set; }

        [Required(ErrorMessage = "Informe pelo menos um artista.")]
        [MinLength(1, ErrorMessage = "A música deve ter pelo menos um artista.")]
        public List<string> NomeArtistas { get; set; } = new();

        [Required(ErrorMessage = "Informe pelo menos um gênero.")]
        [MinLength(1, ErrorMessage = "A música deve ter pelo menos um gênero.")]
        public List<int> IdGeneros { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (AlbumDataLancamento.HasValue && AlbumDataLancamento.Value < new DateOnly(1900, 1, 1))
            {
                yield return new ValidationResult("A data do álbum não pode ser anterior a 01/01/1900.", new[] { nameof(AlbumDataLancamento) });
            }

            if (AlbumDataLancamento.HasValue && string.IsNullOrWhiteSpace(AlbumDataPrecisao))
            {
                yield return new ValidationResult("Quando a data de lançamento é informada, a precisão('year', 'month', 'day') é obrigatória.", new[] { nameof(AlbumDataPrecisao) });
            }
            
        }
    }
}
