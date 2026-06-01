using Learnix.data;
using Learnix.model;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Learnix.Repositorio
{
    /// <summary>
    /// Repositório de Curso. Responsabilidade única: encapsular o acesso a dados
    /// da entidade Curso. Não valida regras de negócio, não formata para a View,
    /// não orquestra fluxos — apenas persistência.
    /// 
    /// SOLID — SRP (Single Responsibility Principle): a classe tem uma única razão
    /// para mudar (alterações na forma de consultar/persistir cursos).
    /// 
    /// Clean Code: o método BuscarCursosPorNome usa parâmetro {0} no SQL puro
    /// (FromSqlRaw) para prevenir SQL Injection — boa prática de segurança
    /// explícita no código.
    /// </summary>
    public class CursoRepository : ICursoRepository
    {
        private readonly LearnixDbContext _context;

        public CursoRepository(LearnixDbContext context)
        {
            _context = context;
        }

        public List<Curso> BuscarTodos()
        {
            return _context.Cursos
                .Include(c => c.Categoria)
                .Include(c => c.Instrutor)
                .Include(c => c.Modulos)
                    .ThenInclude(m => m.Aulas)
                .ToList();
        }

        public Curso? BuscarPorId(int id)
        {
            return _context.Cursos
                .Include(c => c.Categoria)
                .Include(c => c.Instrutor)
                .Include(c => c.Modulos)
                    .ThenInclude(m => m.Aulas)
                .FirstOrDefault(c => c.Id == id);
        }

        public List<Curso> BuscarCursosPorNome(string termoPesquisa)
        {
            using var context = new LearnixDbContext();

            // Query com SQL puro e fazendo uso do {0} para previnir SQL Injection
            string query = "SELECT * FROM Cursos WHERE Titulo LIKE {0}";

            return context.Cursos
                .FromSqlRaw(query, $"%{termoPesquisa}%")
                .ToList();
        }

        public List<Curso> BuscarPorCategoria(string nomeCategoria)
        {
            return _context.Cursos
                .Include(c => c.Categoria)
                .Include(c => c.Instrutor)
                .Where(c => c.Categoria != null && c.Categoria.Nome == nomeCategoria)
                .ToList();
        }
    }
}
