using Learnix.model;
using Learnix.Services;
using System.Collections.Generic;

namespace Learnix.Controllers
{
    /// <summary>
    /// Controller de Curso. Camada de orquestração entre a View e o Service.
    /// SOLID — DIP: depende da abstração ICursoService, nunca acessa Repository ou DbContext diretamente.
    /// </summary>
    public class CursoController
    {
        private readonly ICursoService _cursoService;

        public CursoController(ICursoService cursoService)
        {
            _cursoService = cursoService;
        }

        public List<Curso> BuscarCursos(string termoPesquisa)
        {
            return _cursoService.BuscarPorTermo(termoPesquisa);
        }
    }
}

// test
