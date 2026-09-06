// Importa as entidades de domínio
using Biblioteca.API.Dominio.Entidades;
// Importa a interface do repositório
using Biblioteca.API.Dominio.Interfaces;
// Importa o contexto do Entity Framework Core
using Biblioteca.API.Infraestrutura.Contexto;
using Microsoft.EntityFrameworkCore;

// Declara o namespace correspondente à camada de infraestrutura
namespace Biblioteca.API.Infraestrutura.Repositorios;

// Implementa a interface do repositório utilizando o Entity Framework Core + Oracle
public class LivroRepositorio : ILivroRepositorio
{
    private readonly BibliotecaContexto _contexto;

    public LivroRepositorio(BibliotecaContexto contexto) => _contexto = contexto;

    // Retorna todos os livros com os autores associados (Include do relacionamento N:N)
    public IEnumerable<Livro> ObterTodos() =>
        _contexto.Livros
                 .AsNoTracking()
                 .Include(l => l.Autores)
                 .ThenInclude(la => la.Autor)
                 .OrderBy(l => l.Titulo)
                 .ToList();

    // Busca um livro por ID mantendo o rastreamento para permitir alterações
    public Livro? ObterPorId(int id) =>
        _contexto.Livros
                 .Include(l => l.Autores)
                 .ThenInclude(la => la.Autor)
                 .FirstOrDefault(l => l.Id == id);

    // Verifica a unicidade do ISBN no acervo
    public bool ExisteIsbn(string isbn) => _contexto.Livros.Any(l => l.Isbn == isbn);

    // Insere o livro (e as linhas da tabela de junção) e confirma a transação
    public void Adicionar(Livro livro)
    {
        _contexto.Livros.Add(livro);
        _contexto.SaveChanges();
    }

    // Atualiza o livro e confirma a transação
    public void Atualizar(Livro livro)
    {
        _contexto.Livros.Update(livro);
        _contexto.SaveChanges();
    }

    // Remove o livro e confirma a transação
    public void Remover(Livro livro)
    {
        _contexto.Livros.Remove(livro);
        _contexto.SaveChanges();
    }
}
