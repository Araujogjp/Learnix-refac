# Histórico de Refatorações — Learnix

Este arquivo registra alterações estruturais e de código aplicadas ao projeto, com versões ANTES/DEPOIS de cada arquivo afetado. Serve como evidência da documentação de uso de IA exigida pela atividade (nível IA-2) e como rastreio das decisões arquiteturais.

---

## [Sub-passo 1.A] Refatoração: aplicar DIP em CursoService e CursoController

Data: 01/06/2025

Motivação: As camadas Service e Repository estavam paralelas (cada uma falando com DbContext). Endireitar para Controller → Service → Repository expõe evidência de SOLID-DIP e prepara o terreno para a TelaMenu consumir o FromSqlRaw do Repository via Controller (sub-passo 1.B).

### Arquivo: service/CursoService.cs

#### ANTES

```csharp
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Learnix.data;
using Learnix.model;

namespace Learnix.Services
{
    /// <summary>
    /// Servico de Curso. Encapsula consultas usadas pela TelaMenu.
    /// </summary>
    public class CursoService : ICursoService
    {
        private readonly LearnixDbContext _context;

        public CursoService(LearnixDbContext context)
        {
            _context = context;
        }

        public List<Curso> ListarTodos()
        {
            return _context.Cursos
                .Include(c => c.Categoria)
                .Include(c => c.Instrutor)
                .ToList();
        }

        public List<Curso> ListarPorCategoria(string nomeCategoria)
        {
            return _context.Cursos
                .Include(c => c.Categoria)
                .Include(c => c.Instrutor)
                .Where(c => c.Categoria.Nome == nomeCategoria)
                .ToList();
        }

        public List<Curso> BuscarPorTermo(string termo)
        {
            string termoLower = (termo ?? string.Empty).ToLower();
            return _context.Cursos
                .Include(c => c.Categoria)
                .Include(c => c.Instrutor)
                .Where(c => c.Titulo.ToLower().Contains(termoLower))
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
    }
}
```

#### DEPOIS

```csharp
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
```

### Arquivo: control/CursoController.cs

#### ANTES

```csharp
using Learnix.model;
using Learnix.Repositorio;
using System.Collections.Generic;

namespace Learnix.Controllers
{
    public class CursoController
    {
        private readonly ICursoRepository _cursoRepository;

        // Injeção de Dependência do Repositório
        public CursoController(ICursoRepository cursoRepository)
        {
            _cursoRepository = cursoRepository;
        }

        public List<Curso> BuscarCursos(string termoPesquisa)
        {
            List<Curso> cursos = _cursoRepository.BuscarCursosPorNome(termoPesquisa);
            return cursos;
        }
    }
}
```

#### DEPOIS

```csharp
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
```

### Impacto funcional

Nenhum. O comportamento externo dos métodos públicos é idêntico — apenas o caminho interno de resolução de dependências mudou. As Views ainda não consomem essas camadas (será feito no sub-passo 1.B).
