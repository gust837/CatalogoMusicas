using CatalogoMusicas.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CatalogoMusicas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "ADMIN")]
    public class SpotifyController : ControllerBase
    {
        private readonly SpotifyService _spotifyService;

        public SpotifyController(SpotifyService spotifyService)
        {
            _spotifyService = spotifyService;
        }

        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar([FromQuery] string q, [FromQuery] int offset = 0)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { mensagem = "Informe um termo para buscar no Spotify." });

            var faixas = await _spotifyService.BuscarFaixasAsync(q, offset);
            return Ok(faixas);
        }
    }
}
