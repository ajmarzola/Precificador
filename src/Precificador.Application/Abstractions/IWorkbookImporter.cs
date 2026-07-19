namespace Precificador.Application.Abstractions;

public interface IWorkbookImporter
{
    Task ImportAsync(string workbookPath, CancellationToken cancellationToken = default);
}
