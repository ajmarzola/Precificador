namespace Precificador.WebApi.Infra
{
    public class BaseLogger<T>(ILogger<T> logger, ICorrelationIdGenerator correlationId)
    {
        protected readonly ILogger<T> _logger = logger;
        protected readonly ICorrelationIdGenerator _correlationId = correlationId;

        public virtual void LogInformation(string message)
        {
            _logger.LogInformation($"[CorrelationId: {_correlationId.Get()}] {message}");
        }

        public virtual void LogError(string message)
        {
            _logger.LogError($"[CorrelationId: {_correlationId.Get()}] {message}");
        }

        public virtual void LogWarning(string message)
        {
            _logger.LogWarning($"[CorrelationId: {_correlationId.Get()}] {message}");
        }
    }
}