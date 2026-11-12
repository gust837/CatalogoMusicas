using CatalogoMusicas.Models;

namespace CatalogoMusicas.Interfaces
{
    public interface IUsuarioRepository
    {
        Task Cadastrar(Usuario usuario);

        Task Atualizar(int id, Usuario usuario);

        Task Deletar(int id);

        Task<Usuario?> BuscarPorId(int id);

        Task<Usuario?> BuscarPorEmail(string email);

        Task<List<Usuario>> Listar();

        Task<Usuario?> BuscarPorEmailESenha(string email, string senha);
    }
}
