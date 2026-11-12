using CatalogoMusicas.Models;

namespace CatalogoMusicas.Interfaces
{
    public interface IGeneroRepository
    {
        Task Cadastrar(Genero genero);

        Task Atualizar(int id, Genero genero);

        Task Deletar(int id);

        Task<Genero?> BuscarPorId(int id);

        Task<List<Genero>> Listar();

        Task<List<Genero>> BuscarPorIds(IReadOnlyCollection<int> ids);
    }
}
