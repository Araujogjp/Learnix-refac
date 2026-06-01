using System.Collections.Generic;
using Learnix.model;
using Learnix.Repositorio;

namespace Learnix.Services
{
    /// <summary>
    /// Serviço de Curso. Orquestra regras de negócio sobre cursos.
    /// SOLID — DIP (Dependency Inversion Principle):
    /// depende da abstração ICursoRepository, não de implementação concreta nem de DbContext.
    /// </summary>
    public class CursoService : ICursoService
    {
        private readonly ICursoRepository _cursoRepository;

        public CursoService(ICursoRepository cursoRepository)
        {
            _cursoRepository = cursoRepository;
        }

        public List<Curso> ListarTodos()
            => _cursoRepository.BuscarTodos();

        public List<Curso> ListarPorCategoria(string nomeCategoria)
            => _cursoRepository.BuscarPorCategoria(nomeCategoria);

        public List<Curso> BuscarPorTermo(string termo)
            => _cursoRepository.BuscarCursosPorNome(termo);

        public Curso? BuscarPorId(int id)
            => _cursoRepository.BuscarPorId(id);
    }
}
