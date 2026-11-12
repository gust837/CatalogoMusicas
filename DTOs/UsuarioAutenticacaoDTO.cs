using System.ComponentModel.DataAnnotations;

namespace CatalogoMusicas.DTOs
{
    public class UsuarioAutenticacaoDTO
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(120)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
        public string? Senha { get; set; }
    }
}