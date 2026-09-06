namespace Biblioteca.API.Endpoints;

// Tipos marcadores usados apenas como categoria dos loggers dos endpoints.
//
// As classes de endpoint são estáticas (exigência para conter métodos de
// extensão), e o C# não aceita tipo estático como argumento genérico — daí o
// erro CS0718 ao tentar usar ILogger<AutoresEndpoints>. Estes marcadores
// resolvem isso mantendo uma categoria de log legível na saída do Serilog,
// por exemplo "Biblioteca.API.Endpoints.CategoriaAutores".
//
// O contêiner de injeção de dependência nunca instancia estes tipos: ele só
// usa o nome deles para nomear o logger.

public sealed class CategoriaAutores
{
    private CategoriaAutores() { }
}

public sealed class CategoriaLivros
{
    private CategoriaLivros() { }
}
