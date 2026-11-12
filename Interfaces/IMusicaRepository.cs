using CatalogoMusicas.Models;

namespace CatalogoMusicas.Interfaces
{
    public interface IMusicaRepository
    {
        Task Cadastrar(Musica musica);

        Task Atualizar(int id, Musica musica, List<string> nomesArtistas, List<Genero> generos);

        Task DeletarLogico(int id);

        Task<Musica?> BuscarPorId(int id);

        Task<Musica?> BuscarPorSpotifyId(string spotifyId);

        Task<(List<Musica> musicas, int total)> ListarPaginado(int pagina = 1, int tamanhoPagina = 10, 
            string? busca = null, int? generoId = null);

        Task<Artista> ObterOuCriarArtistaAsync(string nome, string? spotifyId = null, string? imageUrl = null);

        Task<Albun> ObterOuCriarAlbumAsync(string titulo, string? spotifyId, DateOnly? dataLancamento,
            string? precisao, string? capaUrl);
    }
}
