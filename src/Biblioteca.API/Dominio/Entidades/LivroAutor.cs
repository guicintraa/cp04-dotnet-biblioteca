// Declara o namespace correspondente à estrutura de pastas do domínio
namespace Biblioteca.API.Dominio.Entidades;

// Entidade de junção que materializa o relacionamento muitos-para-muitos (N:N)
// entre Livro e Autor: um livro pode ter vários autores e um autor vários livros.
public class LivroAutor
{
    // Construtor sem parâmetros exigido pelo Entity Framework Core
    protected LivroAutor() { }

    // Construtor que amarra as duas pontas do relacionamento
    public LivroAutor(Livro livro, Autor autor)
    {
        Livro = livro;
        Autor = autor;
        LivroId = livro.Id;
        AutorId = autor.Id;
    }

    // Chave estrangeira e navegação para o livro
    public int LivroId { get; private set; }
    public Livro? Livro { get; private set; }

    // Chave estrangeira e navegação para o autor
    public int AutorId { get; private set; }
    public Autor? Autor { get; private set; }
}
