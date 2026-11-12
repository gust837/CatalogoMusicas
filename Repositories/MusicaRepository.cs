using CatalogoMusicas.BdContextCatalogo;
using CatalogoMusicas.Interfaces;
using CatalogoMusicas.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogoMusicas.Repositories
{
    public class MusicaRepository : IMusicaRepository
    {
        private readonly CatalogoContext _context;

        public MusicaRepository(CatalogoContext context)
        {
            _context = context;
        }

        public async Task Atualizar(int id, Musica musica, List<string> nomesArtistas, List<Genero> generos)
        {
            var musicaExistente = await _context.Musicas.Include(m => m.Album)
                .Include(m => m.Generos)
                .Include(m => m.MusicaArtista)
                .FirstOrDefaultAsync(m => m.Id == id && m.Ativo);

            if (musicaExistente == null)
                return;

            musicaExistente.Titulo = musica.Titulo;
            musicaExistente.Descricao = musica.Descricao;
            musicaExistente.DuracaoMs = musica.DuracaoMs;
            musicaExistente.SpotifyId = musica.SpotifyId;
            musicaExistente.AlbumId = musica.AlbumId;
            musicaExistente.AtualizadoEm = DateTime.Now;

            musicaExistente.Generos.Clear();
            foreach (var genero in generos)
            {
                musicaExistente.Generos.Add(genero);
            }

            _context.MusicaArtistas.RemoveRange(musicaExistente.MusicaArtista);
            byte ordem = 1;
            foreach (var nomeArtista in nomesArtistas.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var artista = await ObterOuCriarArtistaAsync(nomeArtista);
                musicaExistente.MusicaArtista.Add(new MusicaArtista
                {
                    MusicaId = musicaExistente.Id,
                    ArtistaId = artista.Id,
                    Ordem = ordem++
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<Musica?> BuscarPorId(int id)
        {
            return await _context.Musicas.AsNoTracking()
                .Include(m => m.Album)
                .Include(m => m.Generos)
                .Include(m => m.MusicaArtista)
                .ThenInclude(ma => ma.Artista).FirstOrDefaultAsync(m => m.Id == id && m.Ativo);
        }

        public async Task<Musica?> BuscarPorSpotifyId(string spotifyId)
        {
            return await _context.Musicas.AsNoTracking().FirstOrDefaultAsync(m => m.SpotifyId == spotifyId);
        }

        public async Task Cadastrar(Musica musica)
        {
            await _context.Musicas.AddAsync(musica);
            await _context.SaveChangesAsync();
        }

        public async Task DeletarLogico(int id)
        {
            var musicaBuscada = await _context.Musicas.FindAsync(id);
            if (musicaBuscada != null)
            {
                musicaBuscada.Ativo = false;
                musicaBuscada.AtualizadoEm = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(List<Musica> musicas, int total)> ListarPaginado(int pagina = 1, int tamanhoPagina = 10, string? busca = null, int? generoId = null)
        {
            var query = _context.Musicas.AsNoTracking()
            .Where(m => m.Ativo)
            .Include(m => m.Album)
            .Include(m => m.Generos)
            .Include(m => m.MusicaArtista)
                .ThenInclude(ma => ma.Artista)
            .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                query = query.Where(m => m.Titulo.Contains(busca));
            }

            if (generoId.HasValue && generoId.Value > 0)
            {
                query = query.Where(m => m.Generos.Any(g => g.Id == generoId.Value));
            }

            int total = await query.CountAsync();

            var itens = await query.OrderBy(m => m.Titulo)
                .ThenBy(m => m.Id)
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .ToListAsync();

            return (itens, total);
        }

        public async Task<Albun> ObterOuCriarAlbumAsync(string titulo, string? spotifyId, DateOnly? dataLancamento, string? precisao, string? capaUrl)
        {
            Albun? album = null;

            if (!string.IsNullOrWhiteSpace(spotifyId))
            {
                album = await _context.Albuns.FirstOrDefaultAsync(a => a.SpotifyId == spotifyId);
            }

            if (album == null)
            {
                album = await _context.Albuns.FirstOrDefaultAsync(a => a.Titulo == titulo);
            }

            if (album != null)
            {
                return album;
            }

            album = new Albun
            {
                Titulo = titulo,
                SpotifyId = spotifyId,
                DataLancamento = dataLancamento,
                DataLancamentoPrecisao = precisao,
                CapaUrl = capaUrl,
                CriadoEm = DateTime.Now
            };

            await _context.Albuns.AddAsync(album);
            await _context.SaveChangesAsync();
            return album;
        }

        public async Task<Artista> ObterOuCriarArtistaAsync(string nome, string? spotifyId = null, string? imageUrl = null)
        {
            Artista? artista = null;

            if (!string.IsNullOrWhiteSpace(spotifyId))
            {
                artista = await _context.Artistas.FirstOrDefaultAsync(a => a.SpotifyId == spotifyId);
            }

            if (artista == null)
            {
                artista = await _context.Artistas.FirstOrDefaultAsync(a => a.Nome == nome);
            }

            if (artista != null)
            {
                if (!string.IsNullOrWhiteSpace(spotifyId) && string.IsNullOrWhiteSpace(artista.SpotifyId))
                {
                    artista.SpotifyId = spotifyId;
                    artista.AtualizadoEm = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                return artista;
            }

            artista = new Artista
            {
                Nome = nome,
                SpotifyId = spotifyId,
                ImagemUrl = imageUrl,
                CriadoEm = DateTime.Now
            };

            await _context.Artistas.AddAsync(artista);
            await _context.SaveChangesAsync();
            return artista;
        }
    }
}
