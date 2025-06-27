using Moq;
using Precificador.Application.Model;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;

namespace Precificador.Tests.Application.Services
{
    public class ProdutoMateriaPrimaServiceTests
    {
        private readonly Mock<IProdutoMateriaPrimaRepository> _repositoryMock;
        private readonly ProdutoMateriaPrimaService _service;

        public ProdutoMateriaPrimaServiceTests()
        {
            _repositoryMock = new Mock<IProdutoMateriaPrimaRepository>();
            _service = new ProdutoMateriaPrimaService(_repositoryMock.Object);
        }
    }

    public static class ProdutoMateriaPrimaServiceTestExtensions
    {
        public static Domain.Entities.ProdutoMateriaPrima InvokeConvertToEntity(this ProdutoMateriaPrimaService service, ProdutoMateriaPrima model)
        {
            ArgumentNullException.ThrowIfNull(service, nameof(service));
            ArgumentNullException.ThrowIfNull(model, nameof(model));

            var method = typeof(ProdutoMateriaPrimaService).GetMethod("ConvertToEntity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return method == null
                ? throw new InvalidOperationException("Method 'ConvertToEntity' not found.")
                : (Domain.Entities.ProdutoMateriaPrima)method.Invoke(service, [model])!;
        }

        public static ProdutoMateriaPrima InvokeConvertToModel(this ProdutoMateriaPrimaService service, Domain.Entities.ProdutoMateriaPrima entity)
        {
            ArgumentNullException.ThrowIfNull(service, nameof(service));
            ArgumentNullException.ThrowIfNull(entity, nameof(entity));

            var method = typeof(ProdutoMateriaPrimaService).GetMethod("ConvertToModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return method == null
                ? throw new InvalidOperationException("Method 'ConvertToModel' not found.")
                : (ProdutoMateriaPrima)method.Invoke(service, [entity])!;
        }

        public static void InvokeUpdateEntityFromModel(this ProdutoMateriaPrimaService service, Domain.Entities.ProdutoMateriaPrima entity, ProdutoMateriaPrima model)
        {
            ArgumentNullException.ThrowIfNull(service, nameof(service));
            ArgumentNullException.ThrowIfNull(entity, nameof(entity));
            ArgumentNullException.ThrowIfNull(model, nameof(model));

            var method = typeof(ProdutoMateriaPrimaService).GetMethod("UpdateEntityFromModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method 'UpdateEntityFromModel' not found.");
            method.Invoke(service, [entity, model]);
        }

        public static Task<IEnumerable<Domain.Entities.ProdutoMateriaPrima>> InvokeGetEntitiesByFilterAsync(this ProdutoMateriaPrimaService service, ProdutoMateriaPrimaFilter filter)
        {
            ArgumentNullException.ThrowIfNull(service, nameof(service));
            ArgumentNullException.ThrowIfNull(filter, nameof(filter));

            var method = typeof(ProdutoMateriaPrimaService).GetMethod("GetEntitiesByFilterAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method 'GetEntitiesByFilterAsync' not found.");
            var result = method.Invoke(service, [filter]);
            return result == null
                ? throw new InvalidOperationException("Method 'GetEntitiesByFilterAsync' returned null.")
                : (Task<IEnumerable<Domain.Entities.ProdutoMateriaPrima>>)result!;
        }
    }
}