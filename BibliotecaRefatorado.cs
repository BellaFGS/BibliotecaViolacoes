// ============================================================
//   SISTEMA DE BIBLIOTECA - VERSÃO REFATORADA (SOLID)
//   Cada decisão de design está documentada com comentários.
//   Todas as cinco violações foram corrigidas.
// ============================================================

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace BibliotecaRefatorada
{
    // =============================================================
    // CORREÇÃO 1 — SRP (Single Responsibility Principle)
    //
    // PROBLEMA ORIGINAL: A classe Livro acumulava 4 responsabilidades
    // distintas (dado, multa, e-mail, persistência, relatório).
    // Qualquer mudança numa dessas áreas forçava a modificar Livro.
    //
    // SOLUÇÃO: Extrair cada responsabilidade para sua própria classe.
    //   • Livro          → apenas representa o dado (entidade)
    //   • ServicoMulta   → calcula a multa por atraso
    //   • ServicoEmail   → envia notificações por e-mail (ver DIP abaixo)
    //   • RepositorioLivro → persiste o livro (ver DIP abaixo)
    //   • RelatorioLivro → gera o texto de relatório
    //
    // Resultado: cada classe tem agora UM único motivo para mudar.
    // =============================================================

    /// <summary>
    /// Entidade de domínio: representa apenas os dados de um livro.
    /// Não conhece banco de dados, e-mail nem regras de negócio externas.
    /// </summary>
    public class Livro
    {
        public int    Id              { get; set; }
        public string Titulo          { get; set; }
        public string Autor           { get; set; }
        public string Genero          { get; set; }
        public bool   Disponivel      { get; set; }
        public DateTime? DataEmprestimo { get; set; }
        public string EmailUsuario    { get; set; }
        public string NomeUsuario     { get; set; }
    }

    /// <summary>
    /// Responsabilidade única: calcular multa por atraso na devolução.
    /// Separado de Livro para que mudanças na política de multa não
    /// exijam alterar a entidade de domínio.
    /// </summary>
    public class ServicoMulta
    {
        private const int DiasPrazo        = 14;
        private const decimal ValorPorDia  = 2.50m;

        public decimal Calcular(Livro livro)
        {
            if (livro.DataEmprestimo == null) return 0;
            var diasAtraso = (DateTime.Now - livro.DataEmprestimo.Value).Days - DiasPrazo;
            return diasAtraso <= 0 ? 0 : diasAtraso * ValorPorDia;
        }
    }

    /// <summary>
    /// Responsabilidade única: gerar o texto formatado de relatório de um livro.
    /// </summary>
    public class RelatorioLivro
    {
        private readonly ServicoMulta _servicoMulta;

        public RelatorioLivro(ServicoMulta servicoMulta)
        {
            _servicoMulta = servicoMulta;
        }

        public string Gerar(Livro livro)
        {
            var multa = _servicoMulta.Calcular(livro);
            return $"Livro: {livro.Titulo} | Autor: {livro.Autor} " +
                   $"| Disponível: {livro.Disponivel} | Multa: R${multa}";
        }
    }


    // =============================================================
    // CORREÇÃO 2 — OCP (Open/Closed Principle)
    //
    // PROBLEMA ORIGINAL: ServicoEmprestimo.CalcularDesconto usava
    // if/else encadeado por tipo de usuário. Para adicionar "Idoso"
    // ou "Bolsista" era necessário abrir e modificar a classe.
    //
    // SOLUÇÃO: Introduzir a interface IDescontoUsuario.
    //   • Cada tipo de usuário tem sua própria classe de desconto.
    //   • Para adicionar um novo tipo, basta criar uma nova classe
    //     que implemente IDescontoUsuario — sem tocar no código existente.
    //   • ServicoEmprestimo.DevolverLivro recebe IDescontoUsuario
    //     por injeção, tornando-se fechado para modificação.
    // =============================================================

    /// <summary>
    /// Abstração para cálculo de desconto. Aberta para extensão:
    /// novos tipos de usuário = novas implementações desta interface.
    /// </summary>
    public interface IDescontoUsuario
    {
        decimal AplicarDesconto(decimal valorMulta);
    }

    // Cada implementação encapsula a regra de um único tipo de usuário.
    public class DescontoEstudante : IDescontoUsuario
    {
        public decimal AplicarDesconto(decimal valorMulta) => valorMulta * 0.50m;
    }

    public class DescontoProfessor : IDescontoUsuario
    {
        public decimal AplicarDesconto(decimal valorMulta) => valorMulta * 0.80m;
    }

    public class DescontoFuncionario : IDescontoUsuario
    {
        public decimal AplicarDesconto(decimal valorMulta) => valorMulta * 0.30m;
    }

    // Exemplo de extensão SEM modificar código existente:
    public class DescontoIdoso : IDescontoUsuario
    {
        public decimal AplicarDesconto(decimal valorMulta) => valorMulta * 0.60m;
    }

    public class SemDesconto : IDescontoUsuario
    {
        public decimal AplicarDesconto(decimal valorMulta) => 0m;
    }


    // =============================================================
    // CORREÇÃO 3 — LSP (Liskov Substitution Principle)
    //
    // PROBLEMA ORIGINAL: EbookEmprestavel herdava ItemAcervo e era
    // forçado a implementar ReservarItem() — que não faz sentido para
    // e-books. A solução adotada (lançar NotSupportedException) quebra
    // o contrato: qualquer código que usasse ItemAcervo polimorficamente
    // poderia explodir em tempo de execução.
    //
    // SOLUÇÃO: Remover ReservarItem() da classe base.
    //   • ItemAcervo define apenas o contrato que TODOS os itens cumprem.
    //   • A interface IReservavel é implementada somente por LivroFisico.
    //   • Código que precisa de reserva deve depender de IReservavel,
    //     não de ItemAcervo — respeitando LSP e também o ISP.
    // =============================================================

    /// <summary>
    /// Classe base limpa: define apenas o que TODO item do acervo faz.
    /// Não inclui ReservarItem, pois e-books não suportam essa operação.
    /// </summary>
    public abstract class ItemAcervo
    {
        public string Titulo     { get; set; }
        public bool   Disponivel { get; set; }

        public abstract void Emprestar(string usuario);
        public abstract void Devolver();
        // ReservarItem foi removido — pertence apenas a itens físicos.
    }

    /// <summary>
    /// Interface segregada para reserva física.
    /// Somente implementada por quem realmente suporta reservas.
    /// </summary>
    public interface IReservavel
    {
        void ReservarItem(string usuario);
    }

    public class LivroFisico : ItemAcervo, IReservavel
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

        // LivroFisico implementa IReservavel porque realmente suporta reserva.
        public void ReservarItem(string usuario)
        {
            Console.WriteLine($"[FÍSICO] '{Titulo}' reservado para {usuario} por 3 dias.");
        }
    }

    public class EbookEmprestavel : ItemAcervo
    {
        // EbookEmprestavel NÃO implementa IReservavel — e isso é correto.
        // Nenhuma exceção, nenhuma surpresa: o contrato de ItemAcervo é cumprido.
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
    }


    // =============================================================
    // CORREÇÃO 4 — ISP (Interface Segregation Principle)
    //
    // PROBLEMA ORIGINAL: IRelatorio era uma interface "gorda" com
    // 5 métodos. RelatorioEmprestimos precisava de PDF + e-mail e
    // RelatorioInventario precisava apenas de Excel — mas ambos eram
    // forçados a implementar tudo, gerando NotImplementedException.
    //
    // SOLUÇÃO: Decompor IRelatorio em interfaces menores e coesas.
    //   Cada classe implementa somente as interfaces que realmente usa.
    //   Isso elimina todas as NotImplementedException e torna o contrato
    //   honesto e verificável em tempo de compilação.
    // =============================================================

    // Interfaces segregadas — cada uma com responsabilidade única:
    public interface IGeravelPDF    { void GerarRelatorioPDF();              }
    public interface IGeravelExcel  { void GerarRelatorioExcel();            }
    public interface IGeravelHTML   { void GerarRelatorioHTML();             }
    public interface IEnviavel      { void EnviarPorEmail(string dest);      }
    public interface ISalvavelDisco { void SalvarEmDisco(string caminho);    }

    /// <summary>
    /// Implementa apenas PDF + e-mail + disco.
    /// Não há mais métodos inúteis nem NotImplementedException.
    /// </summary>
    public class RelatorioEmprestimos : IGeravelPDF, IEnviavel, ISalvavelDisco
    {
        public void GerarRelatorioPDF()
        {
            Console.WriteLine("Gerando PDF de empréstimos...");
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

    /// <summary>
    /// Implementa apenas Excel + disco.
    /// Sem implementações forçadas de PDF ou e-mail.
    /// </summary>
    public class RelatorioInventario : IGeravelExcel, ISalvavelDisco
    {
        public void GerarRelatorioExcel()
        {
            Console.WriteLine("Gerando Excel de inventário...");
        }

        public void SalvarEmDisco(string caminho)
        {
            Console.WriteLine($"Salvando inventário em {caminho}");
        }
    }


    // =============================================================
    // CORREÇÃO 5 — DIP (Dependency Inversion Principle)
    //
    // PROBLEMA ORIGINAL: GerenciadorAcervo instanciava diretamente
    // BancoDadosMySQL e ServicoEmailSMTP com `new` dentro de si,
    // acoplando o módulo de alto nível às implementações concretas.
    // Era impossível trocar o banco ou o e-mail sem modificar
    // GerenciadorAcervo, e impossível usar mocks nos testes.
    //
    // SOLUÇÃO: Introduzir abstrações (interfaces) e injetar as
    // dependências pelo construtor (Constructor Injection).
    //   • GerenciadorAcervo passa a depender de IRepositorioLivro e
    //     IServicoEmail — não de classes concretas.
    //   • As implementações concretas (MySQL, SMTP) continuam existindo,
    //     mas são fornecidas de fora — invertendo a dependência.
    //   • Para testes, basta injetar fakes/mocks.
    // =============================================================

    /// <summary>
    /// Abstração para persistência de livros.
    /// GerenciadorAcervo não sabe se o banco é MySQL, PostgreSQL ou em memória.
    /// </summary>
    public interface IRepositorioLivro
    {
        void Salvar(Livro livro);
        List<Livro> BuscarDisponiveis();
    }

    /// <summary>
    /// Abstração para envio de e-mails.
    /// Permite trocar SMTP por SendGrid ou qualquer outro provedor
    /// sem alterar GerenciadorAcervo.
    /// </summary>
    public interface IServicoEmail
    {
        void Enviar(string para, string assunto, string corpo);
    }

    // Implementações concretas — vivem por trás das abstrações:

    public class RepositorioLivroMySQL : IRepositorioLivro
    {
        public void Salvar(Livro livro)
        {
            Console.WriteLine($"[MySQL] INSERT INTO livros VALUES " +
                              $"({livro.Id}, '{livro.Titulo}', '{livro.Autor}', {livro.Disponivel})");
        }

        public List<Livro> BuscarDisponiveis()
        {
            Console.WriteLine("[MySQL] SELECT * FROM livros WHERE disponivel = true");
            return new List<Livro>(); // simulação
        }
    }

    public class ServicoEmailSMTP : IServicoEmail
    {
        public void Enviar(string para, string assunto, string corpo)
        {
            Console.WriteLine($"[SMTP] Para: {para} | Assunto: {assunto} | Corpo: {corpo}");
        }
    }

    /// <summary>
    /// Gerenciador de alto nível que depende APENAS de abstrações.
    /// As dependências concretas são injetadas pelo construtor —
    /// nenhum `new` de implementação concreto vive aqui.
    /// </summary>
    public class GerenciadorAcervo
    {
        // Dependências declaradas como interfaces — inversão de dependência aplicada.
        private readonly IRepositorioLivro _repositorio;
        private readonly IServicoEmail     _email;

        /// <summary>
        /// Constructor Injection: quem constrói GerenciadorAcervo decide
        /// qual banco e qual serviço de e-mail serão usados.
        /// </summary>
        public GerenciadorAcervo(IRepositorioLivro repositorio, IServicoEmail email)
        {
            _repositorio = repositorio;
            _email       = email;
        }

        public void CadastrarLivro(Livro livro)
        {
            _repositorio.Salvar(livro);
            Console.WriteLine($"Livro '{livro.Titulo}' cadastrado.");
        }

        public void NotificarAtraso(string emailUsuario, string tituloLivro, decimal multa)
        {
            _email.Enviar(
                emailUsuario,
                "Atraso na devolução",
                $"Você tem uma multa de R${multa} pelo livro '{tituloLivro}'."
            );
        }

        public List<Livro> BuscarLivrosDisponiveis()
        {
            return _repositorio.BuscarDisponiveis();
        }
    }


    // =============================================================
    // SERVIÇO DE EMPRÉSTIMO REFATORADO
    // Agora usa IDescontoUsuario (OCP) e IServicoEmail (DIP/SRP).
    // =============================================================

    public class ServicoEmprestimo
    {
        private readonly ServicoMulta  _servicoMulta;
        private readonly IServicoEmail _servicoEmail;

        public ServicoEmprestimo(ServicoMulta servicoMulta, IServicoEmail servicoEmail)
        {
            _servicoMulta  = servicoMulta;
            _servicoEmail  = servicoEmail;
        }

        public void RealizarEmprestimo(Livro livro, string nomeUsuario, string emailUsuario,
                                        IRepositorioLivro repositorio)
        {
            if (!livro.Disponivel)
            {
                Console.WriteLine("Livro indisponível.");
                return;
            }

            livro.Disponivel      = false;
            livro.DataEmprestimo  = DateTime.Now;
            livro.NomeUsuario     = nomeUsuario;
            livro.EmailUsuario    = emailUsuario;

            repositorio.Salvar(livro); // SRP: persistência delegada ao repositório

            Console.WriteLine($"Empréstimo realizado: {livro.Titulo} para {nomeUsuario}");
        }

        /// <summary>
        /// OCP: o tipo de desconto é injetado via IDescontoUsuario.
        /// Adicionar um novo tipo de usuário = criar nova classe,
        /// sem modificar este método.
        /// </summary>
        public void DevolverLivro(Livro livro, IDescontoUsuario desconto,
                                   IRepositorioLivro repositorio)
        {
            var multa      = _servicoMulta.Calcular(livro);
            var multaFinal = multa - desconto.AplicarDesconto(multa);

            livro.Disponivel     = true;
            livro.DataEmprestimo = null;

            repositorio.Salvar(livro); // SRP: persistência delegada

            if (multaFinal > 0)
            {
                Console.WriteLine($"Devolução com multa de R${multaFinal}");

                // DIP: notificação via abstração IServicoEmail
                _servicoEmail.Enviar(
                    livro.EmailUsuario,
                    "Atraso na devolução",
                    $"Olá {livro.NomeUsuario}, você tem uma multa de R${multaFinal} " +
                    $"pelo livro '{livro.Titulo}'."
                );
            }
            else
            {
                Console.WriteLine("Devolução sem multa. Obrigado!");
            }
        }
    }


    // =============================================================
    //   PROGRAMA PRINCIPAL — demonstra o uso do design refatorado
    // =============================================================
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Sistema de Biblioteca (Refatorado — SOLID) ===\n");

            // --- Montagem das dependências (Composition Root) ---
            // As implementações concretas são definidas aqui, uma única vez.
            // O restante do código depende apenas de abstrações.
            IRepositorioLivro repositorio = new RepositorioLivroMySQL();
            IServicoEmail     email       = new ServicoEmailSMTP();
            var servicoMulta              = new ServicoMulta();
            var servicoEmprestimo         = new ServicoEmprestimo(servicoMulta, email);
            var gerenciador               = new GerenciadorAcervo(repositorio, email);

            // --- SRP: Livro é só dado; demais responsabilidades são de outras classes ---
            var livro = new Livro
            {
                Id           = 1,
                Titulo       = "Clean Code",
                Autor        = "Robert C. Martin",
                Genero       = "Tecnologia",
                Disponivel   = true,
                EmailUsuario = "aluno@faculdade.edu",
                NomeUsuario  = "João Silva"
            };

            // --- OCP: desconto injetado — nenhum if/else no ServicoEmprestimo ---
            servicoEmprestimo.RealizarEmprestimo(livro, "João Silva", "aluno@faculdade.edu", repositorio);

            livro.DataEmprestimo = DateTime.Now.AddDays(-20); // simula atraso
            servicoEmprestimo.DevolverLivro(livro, new DescontoEstudante(), repositorio);

            // Demonstra extensão do OCP sem modificar código existente:
            livro.Disponivel = true;
            livro.DataEmprestimo = DateTime.Now.AddDays(-18);
            servicoEmprestimo.DevolverLivro(livro, new DescontoIdoso(), repositorio);

            // --- LSP: iteração polimórfica sem exceções inesperadas ---
            Console.WriteLine("\n--- Polimorfismo (LSP corrigido) ---");
            var itens = new List<ItemAcervo>
            {
                new LivroFisico     { Titulo = "Design Patterns", Disponivel = true },
                new EbookEmprestavel{ Titulo = "Refactoring",     Disponivel = true }
            };

            foreach (var item in itens)
            {
                item.Emprestar("Maria Souza");

                // Reserva só é tentada em quem realmente implementa IReservavel.
                // Nenhuma exceção, nenhum try/catch necessário.
                if (item is IReservavel reservavel)
                    reservavel.ReservarItem("Carlos");
                else
                    Console.WriteLine($"[INFO] '{item.Titulo}' não suporta reserva.");
            }

            // --- ISP: cada relatório usa apenas suas interfaces ---
            Console.WriteLine("\n--- Relatórios (ISP corrigido) ---");
            var relEmp = new RelatorioEmprestimos();
            relEmp.GerarRelatorioPDF();
            relEmp.EnviarPorEmail("gestor@biblioteca.edu");
            relEmp.SalvarEmDisco("/relatorios/emprestimos.pdf");

            var relInv = new RelatorioInventario();
            relInv.GerarRelatorioExcel();
            relInv.SalvarEmDisco("/relatorios/inventario.xlsx");
            // relInv.GerarRelatorioPDF() — não existe; erro de compilação, não de runtime.

            // --- DIP: GerenciadorAcervo não sabe o que está por trás das abstrações ---
            Console.WriteLine("\n--- Gerenciador (DIP corrigido) ---");
            gerenciador.CadastrarLivro(livro);
            gerenciador.NotificarAtraso(livro.EmailUsuario, livro.Titulo, 15.00m);

            // Relatório via SRP (RelatorioLivro separado de Livro):
            var relLivro = new RelatorioLivro(servicoMulta);
            Console.WriteLine(relLivro.Gerar(livro));
        }
    }
}
