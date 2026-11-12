using CatalogoMusicas.BdContextCatalogo;
using CatalogoMusicas.Interfaces;
using CatalogoMusicas.Models;
using CatalogoMusicas.Utils;
using Microsoft.EntityFrameworkCore;

namespace CatalogoMusicas.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly CatalogoContext _context;

        public UsuarioRepository(CatalogoContext context)
        {
            _context = context;
        }

        public async Task Atualizar(int id, Usuario usuario)
        {
            var usuarioExistente = await _context.Usuarios.FindAsync(id);
            if (usuarioExistente != null)
            {
                usuarioExistente.Nome = usuario.Nome;
                usuarioExistente.Email = usuario.Email;
                usuarioExistente.Perfil = usuario.Perfil;
                usuarioExistente.Ativo = usuario.Ativo;

                if (!string.IsNullOrEmpty(usuario.SenhaHash))
                {
                    usuarioExistente.SenhaHash = Criptografia.GerarHash(usuario.SenhaHash);
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Usuario?> BuscarPorEmail(string email)
        {
            return await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<Usuario?> BuscarPorEmailESenha(string email, string senha)
        {
            var usuarioBuscado = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
            if (usuarioBuscado == null || !usuarioBuscado.Ativo)
            {
                return null;
            }

            bool senhaValida = Criptografia.CompararHash(senha, usuarioBuscado.SenhaHash);

            if (!senhaValida)
            {
                return null;
            }
            return usuarioBuscado;
        }

        public async Task<Usuario?> BuscarPorId(int id)
        {
            return await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task Cadastrar(Usuario usuario)
        {
            usuario.SenhaHash = Criptografia.GerarHash(usuario.SenhaHash);
            await _context.Usuarios.AddAsync(usuario);
            await _context.SaveChangesAsync();
        }

        public async Task Deletar(int id)
        {
            var usuarioBuscado = await _context.Usuarios.FindAsync(id);

            if (usuarioBuscado != null)
            {
                usuarioBuscado.Ativo = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Usuario>> Listar()
        {
            return await _context.Usuarios.AsNoTracking().OrderBy(u => u.Nome).ToListAsync();
        }
    }
}
