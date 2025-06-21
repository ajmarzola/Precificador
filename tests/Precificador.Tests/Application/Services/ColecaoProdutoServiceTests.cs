using Moq;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;
using Precificador.Application.Model;

namespace Precificador.Tests.Application.Services
{
    public class ColecaoProdutoServiceTests
    {
        private readonly Mock<IColecaoProdutoRepository> _repositoryMock;
        private readonly IColecaoProdutoService _service;

        public ColecaoProdutoServiceTests()
        {
            _repositoryMock = new Mock<IColecaoProdutoRepository>();
            _service = new ColecaoProdutoService(_repositoryMock.Object); // Replace with your actual implementation
        }
    }

    // Métodos auxiliares para acessar membros protegidos via reflexão
    public static class ColecaoProdutoServiceTestsExtensions
    {
        public static Domain.Entities.ColecaoProduto InvokeConvertToEntity(this ColecaoProdutoService service, ColecaoProduto model)
        {
            ArgumentNullException.ThrowIfNull(service, nameof(service));
            ArgumentNullException.ThrowIfNull(model, nameof(model));

            var method = typeof(ColecaoProdutoService).GetMethod("ConvertToEntity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return method == null
                ? throw new InvalidOperationException("Method 'ConvertToEntity' not found.")
                : (Domain.Entities.ColecaoProduto)method.Invoke(service, [model])!;
        }

        public static ColecaoProduto InvokeConvertToModel(this ColecaoProdutoService service, Domain.Entities.ColecaoProduto entity)
        {
            ArgumentNullException.ThrowIfNull(service, nameof(service));
            ArgumentNullException.ThrowIfNull(entity, nameof(entity));

            var method = typeof(ColecaoProdutoService).GetMethod("ConvertToModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return method == null
                ? throw new InvalidOperationException("Method 'ConvertToModel' not found.")
                : (ColecaoProduto)method.Invoke(service, [entity])!;
        }

        public static void InvokeUpdateEntityFromModel(this ColecaoProdutoService service, Domain.Entities.ColecaoProduto entity, ColecaoProduto model)
        {
            ArgumentNullException.ThrowIfNull(service, nameof(service));
            ArgumentNullException.ThrowIfNull(entity, nameof(entity));
            ArgumentNullException.ThrowIfNull(model, nameof(model));

            var method = typeof(ColecaoProdutoService).GetMethod("UpdateEntityFromModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method 'UpdateEntityFromModel' not found.");
            method.Invoke(service, [entity, model]);
        }

        public static Task<IEnumerable<Domain.Entities.ColecaoProduto>> InvokeGetEntitiesByFilterAsync(this ColecaoProdutoService service, ColecaoProdutoFilter filter)
        {
            ArgumentNullException.ThrowIfNull(service, nameof(service));
            ArgumentNullException.ThrowIfNull(filter, nameof(filter));

            var method = typeof(ColecaoProdutoService).GetMethod("GetEntitiesByFilterAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method 'GetEntitiesByFilterAsync' not found.");
            var result = method.Invoke(service, [filter]);
            return result == null
                ? throw new InvalidOperationException("Method 'GetEntitiesByFilterAsync' returned null.")
                : (Task<IEnumerable<Domain.Entities.ColecaoProduto>>)result!;
        }
    }
}