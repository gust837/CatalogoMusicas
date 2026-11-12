using CatalogoMusicas.BdContextCatalogo;
using CatalogoMusicas.Interfaces;
using CatalogoMusicas.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogoMusicas.Repositories
{
    public class GeneroRepository : IGeneroRepository
    {
        private readonly CatalogoContext _context;

        public GeneroRepository(CatalogoContext context)
        {
            _context = context;
        }

        public async Task Atualizar(int id, Genero genero)
        {
            var generoExistente = await _context.Generos.FindAsync(id);
            if (generoExistente != null)
            {
                generoExistente.Nome = genero.Nome;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Genero?> BuscarPorId(int id)
        {
            return await _context.Generos.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
        }

        public Task<List<Genero>> BuscarPorIds(IReadOnlyCollection<int> ids)
        {
            var idsArray = ids.ToArray();
            return _context.Generos.Where(g => idsArray.Contains(g.Id)).ToListAsync();
        }

        public async Task Cadastrar(Genero genero)
        {
            await _context.Generos.AddAsync(genero);
            await _context.SaveChangesAsync();
        }

        public async Task Deletar(int id)
        {
            var generoBuscado = await _context.Generos.FindAsync(id);
            if (generoBuscado != null)
            {
                _context.Generos.Remove(generoBuscado);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Genero>> Listar()
        {
            return await _context.Generos.AsNoTracking().OrderBy(g => g.Nome).ToListAsync();
        }
    }
}
