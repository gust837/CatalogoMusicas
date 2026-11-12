using CatalogoMusicas.Models;

namespace CatalogoMusicas.DTOs
{
    public record UsuarioRespostaDTO(int Id, string Nome, string Email, string Perfil, bool Ativo, DateTime CriadoEm)
    {
        public static UsuarioRespostaDTO Converter(Usuario usuario) =>
            new(usuario.Id, usuario.Nome, usuario.Email, usuario.Perfil, usuario.Ativo, usuario.CriadoEm);
    }
}