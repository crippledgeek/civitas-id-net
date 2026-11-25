using System.Runtime.CompilerServices;

// Access for this repository's tests. The assemblies are unsigned, so this is no security boundary, and it
// makes nothing public.
[assembly: InternalsVisibleTo("Civitas.Id.Sweden.Tests")]
[assembly: InternalsVisibleTo("Civitas.Id.Sweden.AspNetCore.Tests")]
[assembly: InternalsVisibleTo("Civitas.Id.Sweden.Dapper.Tests")]
[assembly: InternalsVisibleTo("Civitas.Id.Sweden.DataAnnotations.Tests")]
[assembly: InternalsVisibleTo("Civitas.Id.Sweden.EntityFrameworkCore.Tests")]
[assembly: InternalsVisibleTo("Civitas.Id.Sweden.Json.Tests")]
