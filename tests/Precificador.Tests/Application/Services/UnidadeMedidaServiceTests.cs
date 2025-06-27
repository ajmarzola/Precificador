using Moq;
using Precificador.Application.Model;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;

namespace Precificador.Tests.Application.Services
{
    public class UnidadeMedidaServiceTests
    {
        private readonly Mock<IUnidadeMedidaRepository> _repositoryMock;
        private readonly UnidadeMedidaService _service;

        public UnidadeMedidaServiceTests()
        {
            _repositoryMock = new Mock<IUnidadeMedidaRepository>();
            _service = new UnidadeMedidaService(_repositoryMock.Object);
        }

        [Fact]
        public void ConvertToEntity_DeveConverterModelParaEntity()
        {
            var model = new UnidadeMedida
            {
                Id = Guid.NewGuid(),
                Nome = "Litro",
                Abreviacao = "L"
            };

            var entity = _service.InvokeConvertToEntity(model);

            Assert.NotNull(entity);
            Assert.Equal(model.Id, entity!.Id);
            Assert.Equal(model.Nome, entity.Nome);
            Assert.Equal(model.Abreviacao, entity.Abrebiacao);
        }

        [Fact]
        public void ConvertToModel_DeveConverterEntityParaModel()
        {
            var entity = new Domain.Entities.UnidadeMedida("Quilograma", "Kg");

            var model = _service.InvokeConvertToModel(entity);

            Assert.NotNull(model);
            Assert.Equal(entity.Id, model!.Id);
            Assert.Equal(entity.Nome, model.Nome);
            Assert.Equal(entity.Abrebiacao, model.Abreviacao);
        }

        [Fact]
        public void UpdateEntityFromModel_DeveAtualizarEntityComDadosDoModel()
        {
            var entity = new Domain.Entities.UnidadeMedida("Antigo", "A");

            var model = new UnidadeMedida
            {
                Nome = "Novo",
                Abreviacao = "N"
            };

            _service.InvokeUpdateEntityFromModel(entity, model);

            Assert.NotNull(entity);
            Assert.Equal(model.Nome, entity.Nome);
            Assert.Equal(model.Abreviacao, entity.Abrebiacao);
        }
    }

    public static class UnidadeMedidaServiceTestExtensions
    {
        public static Domain.Entities.UnidadeMedida? InvokeConvertToEntity(this UnidadeMedidaService service, UnidadeMedida model)
        {
            var methodInfo = typeof(UnidadeMedidaService)
                .GetMethod("ConvertToEntity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method 'ConvertToEntity' not found.");
            var result = methodInfo.Invoke(service, [model]);
            return result as Domain.Entities.UnidadeMedida;
        }

        public static UnidadeMedida? InvokeConvertToModel(this UnidadeMedidaService service, Domain.Entities.UnidadeMedida entity)
        {
            var methodInfo = typeof(UnidadeMedidaService)
                .GetMethod("ConvertToModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method 'ConvertToModel' not found.");
            var result = methodInfo.Invoke(service, [entity]);
            return result as UnidadeMedida;
        }

        public static void InvokeUpdateEntityFromModel(this UnidadeMedidaService service, Domain.Entities.UnidadeMedida entity, UnidadeMedida model)
        {
            var methodInfo = typeof(UnidadeMedidaService)
                .GetMethod("UpdateEntityFromModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method 'UpdateEntityFromModel' not found.");
            methodInfo.Invoke(service, [entity, model]);
        }

        public static Task<IEnumerable<Domain.Entities.UnidadeMedida>> InvokeGetEntitiesByFilterAsync(this UnidadeMedidaService service, NomeFilter filter)
        {
            var methodInfo = typeof(UnidadeMedidaService)
                .GetMethod("GetEntitiesByFilterAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method 'GetEntitiesByFilterAsync' not found.");
            var result = methodInfo.Invoke(service, [filter]);
            return result as Task<IEnumerable<Domain.Entities.UnidadeMedida>> ?? Task.FromResult(Enumerable.Empty<Domain.Entities.UnidadeMedida>());
        }
    }
}