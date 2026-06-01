using System;

namespace Learnix.model
{
    /// <summary>
    /// Classe base abstrata para todos os usuários do sistema.
    /// Telas: TelaLogin, TelaCadastro, TelaPerfil.
    /// 
    /// SOLID — OCP (Open/Closed Principle): a hierarquia está aberta para extensão
    /// (novos tipos de usuário podem ser criados herdando de Usuario — ex: Aluno,
    /// Instrutor, e futuros como Coordenador) e fechada para modificação (o contrato
    /// base permanece estável). O método abstrato ObterCaminhoDashboard força cada
    /// subclasse a definir seu próprio comportamento sem alterar a classe base.
    /// </summary>
    public abstract class Usuario
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Senha { get; set; } = null!;
        public DateTime DataCadastro { get; set; }

        protected Usuario()
        {
            DataCadastro = DateTime.Now;
        }

        protected Usuario(int id, string nome, string email)
        {
            Id = id;
            Nome = nome;
            Email = email;
            DataCadastro = DateTime.Now;
        }

        // Polimorfismo: cada tipo de usuário define seu próprio painel
        public abstract string ObterCaminhoDashboard();
    }
}
