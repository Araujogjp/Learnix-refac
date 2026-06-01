# Histórico de Refatorações — Learnix

Este arquivo registra alterações estruturais e de código aplicadas ao projeto, com versões ANTES/DEPOIS de cada arquivo afetado. Serve como evidência da documentação de uso de IA exigida pela atividade (nível IA-2) e como rastreio das decisões arquiteturais.

---

## [Sub-passo 1.A] Refatoração: aplicar DIP em CursoService e CursoController

Data: 01/06/2026

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

---

## [Sub-passo 1.B] Plugar a TelaMenu no CursoController (FromSqlRaw em produção)

**Data:** 01/06/2026
**Motivação:** Após endireitar o fluxo Controller → Service → Repository no sub-passo 1.A, a próxima etapa é fazer com que pelo menos uma View consuma efetivamente a cadeia. A `TelaMenu` foi escolhida porque já possui uma caixa de busca (`TxtBusca`) cujo filtro era feito por LINQ em memória. Migrar essa busca para o controller atende a três requisitos da atividade simultaneamente:

1. **MVC em camadas:** a View deixa de acessar dados diretamente (para essa operação) e passa a falar com o Controller.
2. **SOLID — DIP:** a View depende da abstração `CursoController`, que depende de `ICursoService`, que depende de `ICursoRepository`.
3. **SQL manual:** a busca por termo executa o `FromSqlRaw` definido em `CursoRepository.BuscarCursosPorNome` — atende ao requisito *"pelo menos uma funcionalidade específica utilizará manipulação via SQL"*.

### Estratégia técnica

A `TelaMenu` carrega todos os cursos do banco uma única vez (`CarregarCursos`) com joins de Categoria, Instrutor e MatriculasAtivas, montando uma lista de `CursoMenuVM` enriquecida em memória (`_todosCursos`).

Quando o usuário digita na busca:
- A View chama `_cursoController.BuscarCursos(termo)`.
- O Controller delega para o Service, que delega para o Repository, que executa o `FromSqlRaw`.
- O resultado SQL retorna apenas os IDs dos cursos que casam com o termo.
- A View cruza esses IDs com `_todosCursos` para preservar a montagem visual completa (categoria, instrutor, número de alunos).

Quando a busca está vazia, a tela exibe `_todosCursos` integralmente sem chamar o Controller — evitando ida desnecessária ao banco.

### Arquivo: `view/TelaMenu.xaml.cs`

#### ANTES (trecho relevante — método AplicarFiltro)
```csharp
private void AplicarFiltro()
{
    var filtrados = _todosCursos
        .Where(c => TxtBusca.Text == "Buscar curso..." ||
                    string.IsNullOrWhiteSpace(TxtBusca.Text) ||
                    c.Titulo.Contains(TxtBusca.Text, StringComparison.OrdinalIgnoreCase))
        .ToList();

    if (filtrados.Count == 0)
    {
        PainelVazio.Visibility = Visibility.Visible;
        ListaCursos.Visibility = Visibility.Collapsed;
    }
    else
    {
        PainelVazio.Visibility = Visibility.Collapsed;
        ListaCursos.Visibility = Visibility.Visible;
        ListaCursos.ItemsSource = filtrados;
    }
}
```

#### DEPOIS (trecho relevante)
```csharp
private readonly CursoController _cursoController;

public TelaMenu()
{
    InitializeComponent();

    // Composition Root — montagem manual da cadeia DIP para a busca de cursos.
    // View → Controller → Service → Repository → DbContext
    var db = new LearnixDbContext();
    _cursoController = new CursoController(new CursoService(new CursoRepository(db)));
}

private void AplicarFiltro()
{
    string termo = TxtBusca.Text;
    bool semBusca = string.IsNullOrWhiteSpace(termo) || termo == "Buscar curso...";

    List<CursoMenuVM> filtrados;

    if (semBusca)
    {
        filtrados = _todosCursos;
    }
    else
    {
        // COM termo de busca → delega para a cadeia Controller → Service → Repository.
        // O Repository executa a busca via SQL puro (FromSqlRaw).
        List<Curso> resultadoSql = _cursoController.BuscarCursos(termo);
        var idsEncontrados = resultadoSql.Select(c => c.Id).ToHashSet();
        filtrados = _todosCursos.Where(c => idsEncontrados.Contains(c.CursoId)).ToList();
    }

    if (filtrados.Count == 0)
    {
        PainelVazio.Visibility = Visibility.Visible;
        ListaCursos.Visibility = Visibility.Collapsed;
    }
    else
    {
        PainelVazio.Visibility = Visibility.Collapsed;
        ListaCursos.Visibility = Visibility.Visible;
        ListaCursos.ItemsSource = filtrados;
    }
}
```

### Impacto funcional
Nenhum do ponto de vista do usuário. A caixa de busca continua filtrando cursos pelo título conforme o usuário digita, com o mesmo comportamento visual. A mudança interna é que, agora, quando há termo digitado, a consulta passa por `Controller → Service → Repository.FromSqlRaw`, validando o requisito de SQL manual em produção e expondo evidência arquitetural de MVC + SOLID-DIP.

### Localização das evidências no código
- **MVC em camadas:** `view/TelaMenu.xaml.cs` — construtor (montagem da cadeia) e método `AplicarFiltro` (consumo do controller).
- **SOLID-DIP:** comentários XML no construtor de `TelaMenu`, e nas classes `CursoController`, `CursoService` e `CursoRepository`.
- **SQL manual (`FromSqlRaw`):** `repository/CursoRepository.cs`, método `BuscarCursosPorNome`.
`
---

## [Sub-passo 1.C] Documentação inline dos princípios SOLID e Clean Code

**Data:** 01/06/2026
**Motivação:** O documento da atividade exige explicitamente *"a aplicação e informação do local onde foi aplicado SOLID"* e *"a aplicação e informação do local onde foi aplicado Clean Code"*. Os sub-passos 1.A e 1.B aplicaram o DIP — este passo cobre os princípios SRP e OCP além de Clean Code, marcando inline (via XML doc) as classes onde cada princípio aparece de forma evidente.

### Princípios documentados e onde encontrá-los

| Princípio | Arquivo | O que evidencia |
|---|---|---|
| **SRP** — Single Responsibility | `repository/CursoRepository.cs` | Uma única razão para mudar: persistência de Curso. Não valida, não formata, não orquestra. |
| **OCP** — Open/Closed | `model/Usuario.cs` | Aberta para extensão (novos tipos de usuário via herança), fechada para modificação (contrato base estável; método abstrato força comportamento próprio em cada subclasse). |
| **DIP** — Dependency Inversion | `control/CursoController.cs`, `service/CursoService.cs`, `view/TelaMenu.xaml.cs` | Cobertos nos sub-passos 1.A e 1.B. Cada camada depende da abstração da vizinha. |
| **Clean Code** | `repository/CursoRepository.cs` (método `BuscarCursosPorNome`) | Uso de parâmetro `{0}` no `FromSqlRaw` previne SQL Injection — boa prática de segurança explícita no código. Nomes de métodos auto-descritivos em português, sem abreviações ambíguas. |

### Sobre LSP e ISP

LSP (Liskov Substitution) e ISP (Interface Segregation) também estão presentes no projeto de forma implícita:
- **LSP:** `Aluno` e `Instrutor` substituem `Usuario` em qualquer contexto onde a classe base é esperada (ex.: `AuthService.Autenticar`).
- **ISP:** As interfaces `ICursoService`, `ICursoRepository` e `IPlanejamento` são segregadas por responsabilidade — cada cliente conhece apenas o contrato que efetivamente usa.

Esses dois princípios não receberam comentário inline próprio para evitar poluição de código, mas podem ser defendidos oralmente apontando para a herança em `model/Aluno.cs`/`model/Instrutor.cs` (LSP) e para a estrutura das interfaces em `service/` e `repository/` (ISP).

### Impacto funcional
Nenhum. Toda a alteração é em comentários XML — zero impacto em runtime ou comportamento.
