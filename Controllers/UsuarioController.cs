using CatalogoMusicas.DTOs;
using CatalogoMusicas.Interfaces;
using CatalogoMusicas.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CatalogoMusicas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioRepository _usuarioRepository;

        public UsuarioController(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Listar()
        {
            var usuarios = await _usuarioRepository.Listar();
            return Ok(usuarios.Select(UsuarioRespostaDTO.Converter));
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> BuscarPorId(int id)
        {
            var usuario = await _usuarioRepository.BuscarPorId(id);
            if (usuario == null)
                return NotFound(new { mensagem = "Usuário não encontrado" });

            return Ok(UsuarioRespostaDTO.Converter(usuario));
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Cadastrar([FromBody] UsuarioDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Senha))
                return BadRequest(new { mensagem = "A senha é obrigatória para cadastrar um novo usuário." });

            var emailExistente = await _usuarioRepository.BuscarPorEmail(dto.Email);
            if (emailExistente != null)
                return Conflict(new { mensagem = "Este emailjá está cadastrado." });

            var novoUsuario = new Usuario
            {
                Nome = dto.Nome,
                Email = dto.Email,
                SenhaHash = dto.Senha,
                Perfil = dto.Perfil,
                Ativo = dto.Ativo,
                CriadoEm = DateTime.Now
            };

            await _usuarioRepository.Cadastrar(novoUsuario);
            return CreatedAtAction(nameof(BuscarPorId), new { id = novoUsuario.Id }, UsuarioRespostaDTO.Converter(novoUsuario));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] UsuarioDTO dto)
        {
            var usuarioExistente = await _usuarioRepository.BuscarPorId(id);
            if (usuarioExistente == null)
                return NotFound(new { mensagem = "Usuário não encontrado" });

            var usuarioMesmoEmail = await _usuarioRepository.BuscarPorEmail(dto.Email);
            if (usuarioMesmoEmail != null && usuarioMesmoEmail.Id != id)
                return Conflict(new { mensagem = "Este email já está em uso." });

            var dadosAtualizado = new Usuario
            {
                Nome = dto.Nome,
                Email = dto.Email,
                SenhaHash = dto.Senha ?? string.Empty,
                Perfil = dto.Perfil,
                Ativo = dto.Ativo
            };

            await _usuarioRepository.Atualizar(id, dadosAtualizado);
            return NoContent();
        }

        [HttpPut("meu-perfil")]
        public async Task<IActionResult> AtualizarMeuPerfil([FromBody] UsuarioAutenticacaoDTO dto)
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claimId, out int usuarioLogadoId))
                return Unauthorized();

            var usuarioExistente = await _usuarioRepository.BuscarPorId(usuarioLogadoId);
            if (usuarioExistente == null)
                return NotFound(new {mensagem = "Usuário não encontrado."});

            var usuarioMesmoEmail = await _usuarioRepository.BuscarPorEmail(dto.Email);
            if (usuarioMesmoEmail != null && usuarioMesmoEmail.Id != usuarioLogadoId)
                return Conflict(new { mensagem = "Esta email já está em uso." });

            var dadosAtualizados = new Usuario
            {
                Nome = dto.Nome,
                Email = dto.Email,
                SenhaHash = dto.Senha ?? string.Empty,
                Perfil = usuarioExistente.Perfil,
                Ativo = usuarioExistente.Ativo
            };

            await _usuarioRepository.Atualizar(usuarioLogadoId, dadosAtualizados);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Desativar(int id)
        {
            var usuarioExistente = await _usuarioRepository.BuscarPorId(id);
            if (usuarioExistente == null)
                return NotFound(new {mensagem = "Usuário não encontrado."});

            await _usuarioRepository.Deletar(id);
            return NoContent();
        }
    }
}
