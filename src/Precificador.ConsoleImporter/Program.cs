using Precificador.Infrastructure.Importing;
using Precificador.Infrastructure.Persistence;

if (args.Length == 0)
{
    Console.WriteLine("Uso:");
    Console.WriteLine(@"dotnet run --project src/Precificador.ConsoleImporter -- ""C:\Users\ajmar\Downloads\Precificação.xlsx""");
    return;
}

var workbookPath = args[0];

var connectionString =
    Environment.GetEnvironmentVariable("PRECIFICADOR_CONNECTION_STRING")
    ?? "Server=PCZINDOANDERSON;Database=Precificador;Password=priand13;User ID=usr_precificador;Trusted_Connection=True;TrustServerCertificate=True;";

var connectionFactory = new DbConnectionFactory(connectionString);
var importer = new PricingWorkbookImporter(connectionFactory);

try
{
    await importer.ImportAsync(workbookPath);
    Console.WriteLine("Importação concluída com sucesso.");
}
catch (Exception ex)
{
    Console.Error.WriteLine("Falha na importação:");
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine(ex);
}
