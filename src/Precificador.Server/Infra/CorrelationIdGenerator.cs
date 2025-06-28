namespace Precificador.Server.Infra
{
    public class CorrelationIdGenerator : ICorrelationIdGenerator
    {
        private static string? _correlationId;

        public string Get() => _correlationId ?? string.Empty;

        public void Set(string correlationId) => _correlationId = correlationId;
    }
}