using System.ComponentModel.DataAnnotations;

namespace CatalogoMusicas.DTOs
{
    public class GeneroDTO
    {
        [Required(ErrorMessage = "O nome do gênero é obrigatório.")]
        [StringLength(60, ErrorMessage = "O nome do gênero deve ter no máximo 60 caracteres.")]
        public string Nome { get; set; } = string.Empty;
    }
}
