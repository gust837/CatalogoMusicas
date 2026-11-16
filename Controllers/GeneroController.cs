using CatalogoMusicas.DTOs;
using CatalogoMusicas.Interfaces;
using CatalogoMusicas.Models;
using CatalogoMusicas.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CatalogoMusicas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GeneroController : ControllerBase
    {
        private readonly IGeneroRepository _generoRepository;

        public GeneroController(IGeneroRepository generoRepository)
        {
            _generoRepository = generoRepository;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Listar()
        {
            var generos = await _generoRepository.Listar();
            var resposta = generos.Select(g => new GeneroItemDTO(g.Id, g.Nome)).ToList();
            return Ok(resposta);
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> BuscarPorId(int id)
        {
            var genero = await _generoRepository.BuscarPorId(id);
            if (genero == null)
                return NotFound(new { mensagem = "Gênero não encontrado." });

            return Ok(new GeneroItemDTO(genero.Id, genero.Nome));
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Cadastrar([FromBody] GeneroDTO dto)
        {
            var existentes = await _generoRepository.Listar();
            if (existentes.Any(g => g.Nome.Equals(dto.Nome.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict(new { mensagem = "Já existe um gênero cadastrado com esse nome." });
            }

            var novoGenero = new Genero { Nome = dto.Nome.Trim() };
            await _generoRepository.Cadastrar(novoGenero);

            return CreatedAtAction(nameof(BuscarPorId), new { id = novoGenero.Id }, new GeneroItemDTO(novoGenero.Id, novoGenero.Nome));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] GeneroDTO dto)
        {
            var generoExistente = await _generoRepository.BuscarPorId(id);
            if (generoExistente == null)
                return NotFound(new { mensagem = "Gênero não encontrado." });

            var todos = await _generoRepository.Listar();
            if (todos.Any(g => g.Id != id && g.Nome.Equals(dto.Nome.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict(new { mensagem = "Já existe outro gênero com este nome" });
            }

            await _generoRepository.Atualizar(id, new Genero { Nome = dto.Nome.Trim() });
            return NoContent();
        }
    }
}
