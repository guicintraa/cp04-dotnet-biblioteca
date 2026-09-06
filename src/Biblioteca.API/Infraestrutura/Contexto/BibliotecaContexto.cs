// Importa as entidades do domínio
using Biblioteca.API.Dominio.Entidades;
// Importa o Entity Framework Core
using Microsoft.EntityFrameworkCore;

// Declara o namespace do contexto de persistência da infraestrutura
namespace Biblioteca.API.Infraestrutura.Contexto;

// Contexto do EF Core mapeando as entidades para as tabelas do banco Oracle
public class BibliotecaContexto : DbContext
{
    public BibliotecaContexto(DbContextOptions<BibliotecaContexto> options) : base(options) { }

    // Conjunto mapeado para a tabela TB_AUTORES
    public DbSet<Autor> Autores => Set<Autor>();

    // Conjunto mapeado para a tabela TB_LIVROS
    public DbSet<Livro> Livros => Set<Livro>();

    // Conjunto mapeado para a tabela de junção TB_LIVROS_AUTORES
    public DbSet<LivroAutor> LivrosAutores => Set<LivroAutor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigurarAutor(modelBuilder);
        ConfigurarLivro(modelBuilder);
        ConfigurarJuncao(modelBuilder);
        ConfigurarAcessoPorCampo(modelBuilder);
    }

    // Mapeamento da entidade Autor para a tabela TB_AUTORES
    private static void ConfigurarAutor(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Autor>(entidade =>
        {
            entidade.ToTable("TB_AUTORES");
            entidade.HasKey(a => a.Id);

            entidade.Property(a => a.Id)
                    .HasColumnName("ID_AUTOR")
                    .ValueGeneratedOnAdd();

            entidade.Property(a => a.Nome)
                    .HasColumnName("NM_AUTOR")
                    .HasMaxLength(150)
                    .IsRequired();

            entidade.Property(a => a.Nacionalidade)
                    .HasColumnName("DS_NACIONALIDADE")
                    .HasMaxLength(80)
                    .IsRequired();

            entidade.Property(a => a.DataNascimento)
                    .HasColumnName("DT_NASCIMENTO")
                    .IsRequired();

            entidade.HasIndex(a => a.Nome).HasDatabaseName("IX_AUTORES_NOME");
        });

    // Mapeamento da entidade Livro para a tabela TB_LIVROS
    private static void ConfigurarLivro(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Livro>(entidade =>
        {
            entidade.ToTable("TB_LIVROS");
            entidade.HasKey(l => l.Id);

            entidade.Property(l => l.Id)
                    .HasColumnName("ID_LIVRO")
                    .ValueGeneratedOnAdd();

            entidade.Property(l => l.Titulo)
                    .HasColumnName("NM_TITULO")
                    .HasMaxLength(200)
                    .IsRequired();

            entidade.Property(l => l.Isbn)
                    .HasColumnName("CD_ISBN")
                    .HasMaxLength(20)
                    .IsRequired();

            entidade.Property(l => l.Preco)
                    .HasColumnName("VL_PRECO")
                    .HasColumnType("NUMBER(10,2)")
                    .IsRequired();

            entidade.Property(l => l.Estoque)
                    .HasColumnName("QT_ESTOQUE")
                    .IsRequired();

            entidade.Property(l => l.AnoPublicacao)
                    .HasColumnName("NR_ANO_PUBLICACAO")
                    .IsRequired();

            entidade.HasIndex(l => l.Isbn).IsUnique().HasDatabaseName("UK_LIVROS_ISBN");
        });

    // Tabela de junção do relacionamento N:N com chave primária composta
    private static void ConfigurarJuncao(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<LivroAutor>(entidade =>
        {
            entidade.ToTable("TB_LIVROS_AUTORES");
            entidade.HasKey(la => new { la.LivroId, la.AutorId });

            entidade.Property(la => la.LivroId).HasColumnName("ID_LIVRO");
            entidade.Property(la => la.AutorId).HasColumnName("ID_AUTOR");

            entidade.HasOne(la => la.Livro)
                    .WithMany(nameof(Livro.Autores))
                    .HasForeignKey(la => la.LivroId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_LIVROS_AUTORES_LIVRO");

            entidade.HasOne(la => la.Autor)
                    .WithMany(nameof(Autor.Livros))
                    .HasForeignKey(la => la.AutorId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_LIVROS_AUTORES_AUTOR");
        });

    // As coleções são expostas como somente leitura: o EF escreve nos campos privados
    private static void ConfigurarAcessoPorCampo(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Autor>()
                    .Metadata
                    .FindNavigation(nameof(Autor.Livros))?
                    .SetPropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.Entity<Livro>()
                    .Metadata
                    .FindNavigation(nameof(Livro.Autores))?
                    .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
