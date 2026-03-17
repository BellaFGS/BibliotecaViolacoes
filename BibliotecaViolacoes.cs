// ============================================================
//   SISTEMA DE BIBLIOTECA - VERSÃO COM VIOLAÇÕES SOLID
//   Arquivo entregue intencionalmente com problemas de design.
//   Sua tarefa: identificar, documentar e refatorar.
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace Biblioteca
{
    // =============================================================
    // VIOLAÇÃO 1 - SRP (Single Responsibility Principle)
    // A classe Livro acumula responsabilidades demais:
    // representa o dado, calcula multas, envia notificações,
    // gera relatórios e persiste no banco — tudo ao mesmo tempo.
    // =============================================================
    public class Livro
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Autor { get; set; }
        public string Genero { get; set; }
        public bool Disponivel { get; set; }
        public DateTime? DataEmprestimo { get; set; } // é nullable (anulável)
        public string EmailUsuario { get; set; }
        public string NomeUsuario { get; set; }

        // Responsabilidade de negócio: calcular multa
        public decimal CalcularMulta()
        {
            if (DataEmprestimo == null) return 0;
            var diasAtraso = (DateTime.Now - DataEmprestimo.Value).Days - 14;
            if (diasAtraso <= 0) return 0;
            return diasAtraso * 2.50m;
        }

        // Responsabilidade de comunicação: enviar e-mail
        public void EnviarEmailAtraso()
        {
            var multa = CalcularMulta();
            if (multa > 0)
            {
                // Simula envio de e-mail via HTTP
                var client = new HttpClient();
                var conteudo = $"Olá {NomeUsuario}, você tem uma multa de R${multa} pelo livro '{Titulo}'.";
                Console.WriteLine($"[EMAIL] Para: {EmailUsuario} | Mensagem: {conteudo}");
                // Em produção aqui haveria uma chamada real ao servidor SMTP
            }
        }

        // Responsabilidade de persistência: salvar no banco
        public void SalvarNoBanco()
        {
            // Simulação de acesso direto ao banco
            Console.WriteLine($"[DB] INSERT INTO livros VALUES ({Id}, '{Titulo}', '{Autor}', {Disponivel})");
        }

        // Responsabilidade de relatório: gerar texto formatado
        public string GerarRelatorio()
        {
            return $"Livro: {Titulo} | Autor: {Autor} | Disponível: {Disponivel} | Multa: R${CalcularMulta()}";
        }
    }


    // =============================================================
    // VIOLAÇÃO 2 - OCP (Open/Closed Principle)
    // Para adicionar um novo tipo de desconto, é preciso modificar
    // diretamente o método CalcularDesconto — violando o princípio
    // de que classes devem estar abertas para extensão mas fechadas
    // para modificação.
    // =============================================================
    public class ServicoEmprestimo
    {
        public decimal CalcularDesconto(string tipoUsuario, decimal valorMulta)
        {
            // Toda vez que surgir um novo tipo de usuário,
            // este método precisa ser alterado.
            if (tipoUsuario == "Estudante")
            {
                return valorMulta * 0.50m;
            }
            else if (tipoUsuario == "Professor")
            {
                return valorMulta * 0.80m;
            }
            else if (tipoUsuario == "Funcionario")
            {
                return valorMulta * 0.30m;
            }
            // E se adicionarmos "Idoso", "Bolsista", "VIP"?
            // Teríamos que abrir esta classe e modificar aqui.
            else
            {
                return 0;
            }
        }

        public void RealizarEmprestimo(Livro livro, string nomeUsuario, string emailUsuario)
        {
            if (!livro.Disponivel)
            {
                Console.WriteLine("Livro indisponível.");
                return;
            }

            livro.Disponivel = false;
            livro.DataEmprestimo = DateTime.Now;
            livro.NomeUsuario = nomeUsuario;
            livro.EmailUsuario = emailUsuario;
            livro.SalvarNoBanco();

            Console.WriteLine($"Empréstimo realizado: {livro.Titulo} para {nomeUsuario}");
        }

        public void DevolverLivro(Livro livro, string tipoUsuario)
        {
            var multa = livro.CalcularMulta();
            var desconto = CalcularDesconto(tipoUsuario, multa);
            var multaFinal = multa - desconto;

            livro.Disponivel = true;
            livro.DataEmprestimo = null;
            livro.SalvarNoBanco();

            if (multaFinal > 0)
            {
                Console.WriteLine($"Devolução com multa de R${multaFinal}");
                livro.EnviarEmailAtraso();
            }
            else
            {
                Console.WriteLine("Devolução sem multa. Obrigado!");
            }
        }
    }


    // =============================================================
    // VIOLAÇÃO 3 - LSP (Liskov Substitution Principle)
    // ItemAcervo é a classe base. LivroFisico e EbookEmprestavel
    // herdam dela, mas EbookEmprestavel não suporta o conceito de
    // "reserva física" — lança exceção ao chamar ReservarItem().
    // Qualquer código que use ItemAcervo não pode confiar que
    // subclasses se comportam de forma equivalente.
    // =============================================================
    public abstract class ItemAcervo
    {
        public string Titulo { get; set; }
        public bool Disponivel { get; set; }

        public abstract void Emprestar(string usuario);
        public abstract void Devolver();

        // Reserva física — faz sentido apenas para itens físicos
        public virtual void ReservarItem(string usuario)
        {
            Console.WriteLine($"Item '{Titulo}' reservado para {usuario}.");
        }
    }

    public class LivroFisico : ItemAcervo
    {
        public override void Emprestar(string usuario)
        {
            Disponivel = false;
            Console.WriteLine($"[FÍSICO] '{Titulo}' emprestado para {usuario}.");
        }

        public override void Devolver()
        {
            Disponivel = true;
            Console.WriteLine($"[FÍSICO] '{Titulo}' devolvido.");
        }

        public override void ReservarItem(string usuario)
        {
            Console.WriteLine($"[FÍSICO] '{Titulo}' reservado para {usuario} por 3 dias.");
        }
    }

    public class EbookEmprestavel : ItemAcervo
    {
        public override void Emprestar(string usuario)
        {
            Disponivel = false;
            Console.WriteLine($"[EBOOK] Link de download enviado para {usuario}.");
        }

        public override void Devolver()
        {
            Disponivel = true;
            Console.WriteLine($"[EBOOK] Acesso revogado.");
        }

        // VIOLAÇÃO: Ebooks não têm reserva física — mas a herança força implementar.
        // A solução adotada (lançar exceção) quebra o contrato da classe base.
        public override void ReservarItem(string usuario)
        {
            throw new NotSupportedException("Ebooks não suportam reserva física!");
        }
    }


    // =============================================================
    // VIOLAÇÃO 4 - ISP (Interface Segregation Principle)
    // A interface IRelatorio é "gorda": obriga toda classe que a
    // implemente a suportar exportação em PDF, Excel e HTML,
    // mesmo que aquele tipo de relatório precise apenas de um formato.
    // =============================================================
    public interface IRelatorio
    {
        void GerarRelatorioPDF();
        void GerarRelatorioExcel();
        void GerarRelatorioHTML();
        void EnviarPorEmail(string destinatario);
        void SalvarEmDisco(string caminho);
    }

    // RelatorioEmprestimos quer apenas PDF e e-mail,
    // mas é forçado a implementar Excel e HTML.
    public class RelatorioEmprestimos : IRelatorio
    {
        public void GerarRelatorioPDF()
        {
            Console.WriteLine("Gerando PDF de empréstimos...");
        }

        public void GerarRelatorioExcel()
        {
            // Não faz sentido para este relatório, mas a interface exige.
            throw new NotImplementedException("Relatório de empréstimos não suporta Excel.");
        }

        public void GerarRelatorioHTML()
        {
            // Idem — forçado pela interface.
            throw new NotImplementedException("Relatório de empréstimos não suporta HTML.");
        }

        public void EnviarPorEmail(string destinatario)
        {
            Console.WriteLine($"Enviando relatório de empréstimos para {destinatario}");
        }

        public void SalvarEmDisco(string caminho)
        {
            Console.WriteLine($"Salvando relatório em {caminho}");
        }
    }

    // RelatorioInventario quer apenas Excel,
    // mas também é forçado a implementar os outros formatos.
    public class RelatorioInventario : IRelatorio
    {
        public void GerarRelatorioPDF()
        {
            throw new NotImplementedException("Inventário não precisa de PDF.");
        }

        public void GerarRelatorioExcel()
        {
            Console.WriteLine("Gerando Excel de inventário...");
        }

        public void GerarRelatorioHTML()
        {
            throw new NotImplementedException("Inventário não precisa de HTML.");
        }

        public void EnviarPorEmail(string destinatario)
        {
            throw new NotImplementedException("Inventário não é enviado por e-mail.");
        }

        public void SalvarEmDisco(string caminho)
        {
            Console.WriteLine($"Salvando inventário em {caminho}");
        }
    }


    // =============================================================
    // VIOLAÇÃO 5 - DIP (Dependency Inversion Principle)
    // GerenciadorAcervo depende diretamente de classes concretas
    // (BancoDadosMySQL, ServicoEmailSMTP) em vez de depender de
    // abstrações. Isso torna impossível trocar a implementação
    // (ex: trocar MySQL por PostgreSQL, ou SMTP por SendGrid)
    // sem modificar GerenciadorAcervo.
    // =============================================================

    // Implementações concretas — deveriam estar atrás de interfaces
    public class BancoDadosMySQL
    {
        public void Salvar(string tabela, string dados)
        {
            Console.WriteLine($"[MySQL] INSERT INTO {tabela}: {dados}");
        }

        public List<string> Buscar(string tabela, string filtro)
        {
            Console.WriteLine($"[MySQL] SELECT * FROM {tabela} WHERE {filtro}");
            return new List<string> { "resultado simulado" };
        }
    }

    public class ServicoEmailSMTP
    {
        public void Enviar(string para, string assunto, string corpo)
        {
            Console.WriteLine($"[SMTP] Para: {para} | Assunto: {assunto} | Corpo: {corpo}");
        }
    }

    // GerenciadorAcervo instancia diretamente as dependências concretas.
    // Para testar, é impossível usar mocks. Para trocar de banco ou e-mail,
    // é preciso alterar esta classe.
    public class GerenciadorAcervo
    {
        // Dependências concretas instanciadas internamente — violação do DIP
        private BancoDadosMySQL _banco = new BancoDadosMySQL();
        private ServicoEmailSMTP _email = new ServicoEmailSMTP();

        public void CadastrarLivro(Livro livro)
        {
            _banco.Salvar("livros", $"'{livro.Titulo}', '{livro.Autor}'");
            Console.WriteLine($"Livro '{livro.Titulo}' cadastrado.");
        }

        public void NotificarAtraso(string emailUsuario, string tituloLivro, decimal multa)
        {
            _email.Enviar(emailUsuario, "Atraso na devolução",
                $"Você tem uma multa de R${multa} pelo livro '{tituloLivro}'.");
        }

        public List<string> BuscarLivrosDisponiveis()
        {
            return _banco.Buscar("livros", "disponivel = true");
        }
    }


    // =============================================================
    //   PROGRAMA PRINCIPAL — apenas demonstra o uso das classes
    // =============================================================
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Sistema de Biblioteca ===\n");

            // Demonstração das classes com violações
            var livro = new Livro
            {
                Id = 1,
                Titulo = "Clean Code",
                Autor = "Robert C. Martin",
                Genero = "Tecnologia",
                Disponivel = true,
                EmailUsuario = "aluno@faculdade.edu",
                NomeUsuario = "João Silva"
            };

            var servico = new ServicoEmprestimo();
            servico.RealizarEmprestimo(livro, "João Silva", "aluno@faculdade.edu");

            // Simulando atraso
            livro.DataEmprestimo = DateTime.Now.AddDays(-20);
            servico.DevolverLivro(livro, "Estudante");

            Console.WriteLine("\n--- Polimorfismo (com violação de LSP) ---");
            var itens = new List<ItemAcervo>
            {
                new LivroFisico { Titulo = "Design Patterns", Disponivel = true },
                new EbookEmprestavel { Titulo = "Refactoring", Disponivel = true }
            };

            foreach (var item in itens)
            {
                item.Emprestar("Maria Souza");
                try
                {
                    item.ReservarItem("Carlos"); // Vai lançar exceção no Ebook
                }
                catch (NotSupportedException ex)
                {
                    Console.WriteLine($"[ERRO] {ex.Message}");
                }
            }

            Console.WriteLine("\n--- Relatórios (com violação de ISP) ---");
            IRelatorio relEmp = new RelatorioEmprestimos();
            relEmp.GerarRelatorioPDF();
            try { relEmp.GerarRelatorioExcel(); }
            catch (NotImplementedException ex) { Console.WriteLine($"[ERRO] {ex.Message}"); }

            Console.WriteLine("\n--- Gerenciador (com violação de DIP) ---");
            var gerenciador = new GerenciadorAcervo();
            gerenciador.CadastrarLivro(livro);
            gerenciador.NotificarAtraso(livro.EmailUsuario, livro.Titulo, 15.00m);
        }
    }
}
