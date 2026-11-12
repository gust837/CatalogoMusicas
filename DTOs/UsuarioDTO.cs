using System.ComponentModel.DataAnnotations;

namespace CatalogoMusicas.DTOs
{
    public class UsuarioDTO
    {
        [Required(ErrorMessage = "O nome do usuário é obrigatório.")]
        [StringLength(120)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email do usuário é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um email válido.")]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [MinLength(8, ErrorMessage = "A senha deve ter no minímo 8 caracteres.")]
        public string? Senha { get; set; }

        [Required(ErrorMessage = "O perfil é obrigatório.")]
        [RegularExpression("^(ADMIN|USER)$", ErrorMessage = "O perfil deve ser ADMIN ou USER.")]
        public string Perfil { get; set; } = "USER";

        public bool Ativo { get; set; } = true;
    }
}
