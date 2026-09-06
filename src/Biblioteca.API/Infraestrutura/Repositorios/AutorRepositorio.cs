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
public class AutorRepositorio : IAutorRepositorio
{
    private readonly BibliotecaContexto _contexto;

    public AutorRepositorio(BibliotecaContexto contexto) => _contexto = contexto;

    // Retorna todos os autores com os livros associados (Include do relacionamento N:N)
    public IEnumerable<Autor> ObterTodos() =>
        _contexto.Autores
                 .AsNoTracking()
                 .Include(a => a.Livros)
                 .ThenInclude(la => la.Livro)
                 .OrderBy(a => a.Nome)
                 .ToList();

    // Busca um autor por ID mantendo o rastreamento para permitir alterações
    public Autor? ObterPorId(int id) =>
        _contexto.Autores
                 .Include(a => a.Livros)
                 .ThenInclude(la => la.Livro)
                 .FirstOrDefault(a => a.Id == id);

    // Busca vários autores de uma vez para montar o vínculo N:N
    public IEnumerable<Autor> ObterPorIds(IEnumerable<int> ids)
    {
        var lista = ids.Distinct().ToList();

        return _contexto.Autores.Where(a => lista.Contains(a.Id)).ToList();
    }

    // Insere o autor e confirma a transação
    public void Adicionar(Autor autor)
    {
        _contexto.Autores.Add(autor);
        _contexto.SaveChanges();
    }

    // Atualiza o autor e confirma a transação
    public void Atualizar(Autor autor)
    {
        _contexto.Autores.Update(autor);
        _contexto.SaveChanges();
    }

    // Remove o autor e confirma a transação
    public void Remover(Autor autor)
    {
        _contexto.Autores.Remove(autor);
        _contexto.SaveChanges();
    }
}
