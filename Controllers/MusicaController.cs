using CatalogoMusicas.DTOs;
using CatalogoMusicas.Interfaces;
using CatalogoMusicas.Models;
using CatalogoMusicas.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogoMusicas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MusicaController : ControllerBase
    {
        private readonly IMusicaRepository _musicaRepository;
        private readonly IGeneroRepository _generoRepository;
        private readonly SpotifyService _spotifyService;
        private readonly LetraService _letraService;

        public MusicaController(
            IMusicaRepository musicaRepository,
            IGeneroRepository generoRepository,
            SpotifyService spotifyService,
            LetraService letraService)
        {
            _musicaRepository = musicaRepository;
            _generoRepository = generoRepository;
            _spotifyService = spotifyService;
            _letraService = letraService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Listar(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanhoPagina = 10,
            [FromQuery] string? busca = null,
            [FromQuery] int? generoId = null)
        {
            var (musicas, total) = await _musicaRepository.ListarPaginado(pagina, tamanhoPagina, busca, generoId);

            return Ok(new
            {
                total,
                pagina,
                tamanhoPagina,
                itens = musicas.Select(MusicaRespostaDTO.Converter)
            });
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> BuscarPorId(int id)
        {
            var musica = await _musicaRepository.BuscarPorId(id);
            if (musica == null)
                return NotFound(new { mensagem = "Música não encontrada." });

            return Ok(MusicaRespostaDTO.Converter(musica));
        }

        [HttpPost("importar-spotify")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ImportarDoSpotify([FromBody] MusicaImportacaoSpotify dto)
        {
            var jaExiste = await _musicaRepository.BuscarPorSpotifyId(dto.SpotifyTrackId);
            if (jaExiste != null)
                return Conflict(new { mensagem = "Esta música do Spotify já está cadastrada no catálogo." });

            var generos = await _generoRepository.BuscarPorIds(dto.IdGeneros);
            if (generos.Count == 0)
                return BadRequest(new { mensagem = "Selecione pelo menos um gênero válido." });

            var trackSpotify = await _spotifyService.ObterFaixaPorIdAsync(dto.SpotifyTrackId);
            if (trackSpotify == null)
                return NotFound(new { mensagem = "Música não encontrada na API do Spotify." });

            var album = await _musicaRepository.ObterOuCriarAlbumAsync(
                trackSpotify.AlbumTitulo,
                trackSpotify.AlbumSpotifyId,
                trackSpotify.AlbumDataLancamento,
                trackSpotify.AlbumDataPrecisao,
                trackSpotify.AlbumCapaUrl);

            var novaMusica = new Musica
            {
                SpotifyId = trackSpotify.SpotifyId,
                Titulo = trackSpotify.Titulo,
                Descricao = dto.Descricao,
                AlbumId = album.Id,
                DuracaoMs = trackSpotify.DuracaoMs,
                Ativo = true,
                CriadoEm = DateTime.Now,
                Generos = generos
            };

            byte ordem = 1;
            foreach (var art in trackSpotify.Artistas)
            {
                var artista = await _musicaRepository.ObterOuCriarArtistaAsync(art.Nome, art.SpotifyId);
                novaMusica.MusicaArtista.Add(new MusicaArtista
                {
                    ArtistaId = artista.Id,
                    Ordem = ordem++
                });
            }

            await _musicaRepository.Cadastrar(novaMusica);

            var musicaCompleta = await _musicaRepository.BuscarPorId(novaMusica.Id);
            return CreatedAtAction(nameof(BuscarPorId), new { id = novaMusica.Id }, MusicaRespostaDTO.Converter(musicaCompleta!));
        }

        [HttpGet("{id:int}/letra")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterLetra(int id)
        {
            var musica = await _musicaRepository.BuscarPorId(id);
            if (musica == null)
                return NotFound(new { mensagem = "Música não encontrada." });
            var artistaPrincipal = musica.MusicaArtista
                .OrderBy(ma => ma.Ordem)
                .Select(ma => ma.Artista.Nome)
                .FirstOrDefault() ?? string.Empty;
            var resultado = await _letraService.ObterLetraAsync(
                musica.Id,
                musica.Titulo,
                artistaPrincipal,
                musica.Album?.Titulo,
                musica.DuracaoMs);
            if (resultado.LetraOriginal == null && resultado.LinhasSincronizadas.Count == 0)
                return NotFound(new { mensagem = "Letra não encontrada para esta música." });
            return Ok(resultado);
        }

        [HttpGet("{id:int}/traducao")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterTraducao(int id)
        {
            var musica = await _musicaRepository.BuscarPorId(id);
            if (musica == null)
                return NotFound(new { mensagem = "Música não encontrada." });
            var artistaPrincipal = musica.MusicaArtista
                .OrderBy(ma => ma.Ordem)
                .Select(ma => ma.Artista.Nome)
                .FirstOrDefault() ?? string.Empty;
            var traducao = await _letraService.ObterTraducaoAsync(
                musica.Id,
                musica.Titulo,
                artistaPrincipal,
                musica.Album?.Titulo,
                musica.DuracaoMs);
            return Ok(new { traducaoPortugues = traducao });
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> CadastrarManual([FromBody] MusicaDTO dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.SpotifyId))
            {
                var jaExiste = await _musicaRepository.BuscarPorSpotifyId(dto.SpotifyId);
                if (jaExiste != null)
                    return Conflict(new { mensagem = "Já existe uma música com este SpotifyId." });
            }

            var generos = await _generoRepository.BuscarPorIds(dto.IdGeneros);
            if (generos.Count == 0)
                return BadRequest(new { mensagem = "Selecione pelo menos um gênero válido." });

            int? albumId = null;
            if (!string.IsNullOrWhiteSpace(dto.AlbumTitulo))
            {
                var album = await _musicaRepository.ObterOuCriarAlbumAsync(
                    dto.AlbumTitulo,
                    null,
                    dto.AlbumDataLancamento,
                    dto.AlbumDataPrecisao,
                    dto.AlbumCapaUrl);
                albumId = album.Id;
            }

            var novaMusica = new Musica
            {
                SpotifyId = string.IsNullOrWhiteSpace(dto.SpotifyId) ? null : dto.SpotifyId,
                Titulo = dto.Titulo,
                Descricao = dto.Descricao,
                AlbumId = albumId,
                DuracaoMs = dto.Duracao,
                Ativo = true,
                CriadoEm = DateTime.Now,
                Generos = generos
            };

            byte ordem = 1;
            foreach (var nomeArtista in dto.NomeArtistas.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var artista = await _musicaRepository.ObterOuCriarArtistaAsync(nomeArtista.Trim());
                novaMusica.MusicaArtista.Add(new MusicaArtista
                {
                    ArtistaId = artista.Id,
                    Ordem = ordem++
                });
            }

            await _musicaRepository.Cadastrar(novaMusica);

            var musicaCompleta = await _musicaRepository.BuscarPorId(novaMusica.Id);
            return CreatedAtAction(nameof(BuscarPorId), new { id = novaMusica.Id }, MusicaRespostaDTO.Converter(musicaCompleta!));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] MusicaDTO dto)
        {
            var musicaExistente = await _musicaRepository.BuscarPorId(id);
            if (musicaExistente == null)
                return NotFound(new { mensagem = "Música não encontrada." });

            var generos = await _generoRepository.BuscarPorIds(dto.IdGeneros);
            if (generos.Count == 0)
                return BadRequest(new { mensagem = "Selecione pelo menos um gênero válido." });

            int? albumId = musicaExistente.AlbumId;
            if (!string.IsNullOrWhiteSpace(dto.AlbumTitulo))
            {
                var album = await _musicaRepository.ObterOuCriarAlbumAsync(
                    dto.AlbumTitulo,
                    null,
                    dto.AlbumDataLancamento,
                    dto.AlbumDataPrecisao,
                    dto.AlbumCapaUrl);
                albumId = album.Id;
            }

            var dadosAtualizados = new Musica
            {
                Titulo = dto.Titulo,
                Descricao = dto.Descricao,
                DuracaoMs = dto.Duracao,
                SpotifyId = string.IsNullOrWhiteSpace(dto.SpotifyId) ? null : dto.SpotifyId,
                AlbumId = albumId
            };

            await _musicaRepository.Atualizar(id, dadosAtualizados, dto.NomeArtistas, generos);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Excluir(int id)
        {
            var musicaExistente = await _musicaRepository.BuscarPorId(id);
            if (musicaExistente == null)
                return NotFound(new { mensagem = "Música não encontrada." });

            await _musicaRepository.DeletarLogico(id);
            return NoContent();
        }
    }
}
