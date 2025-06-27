using Moq;
using Precificador.Application.Model;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;

namespace Precificador.Tests.Application.Services
{
    public class GrupoServiceTests
    {
        private readonly Mock<IGrupoRepository> _repositoryMock;
        private readonly GrupoService _service;

        public GrupoServiceTests()
        {
            _repositoryMock = new Mock<IGrupoRepository>();
            _service = new GrupoService(_repositoryMock.Object);
        }

        [Fact]
        public void ConvertToEntity_DeveConverterModelParaEntity()
        {
            var model = new Grupo
            {
                Id = Guid.NewGuid(),
                Nome = "Grupo Teste"
            };

            var entity = _service.InvokeConvertToEntity(model);

            Assert.NotNull(entity);
            Assert.Equal(model.Id, entity!.Id);
            Assert.Equal(model.Nome, entity.Nome);
        }

        [Fact]
        public void ConvertToModel_DeveConverterEntityParaModel()
        {
            var entity = new Domain.Entities.Grupo("Grupo Entity");

            var model = _service.InvokeConvertToModel(entity);

            Assert.NotNull(model);
            Assert.Equal(entity.Id, model!.Id);
            Assert.Equal(entity.Nome, model.Nome);
        }

        [Fact]
        public void UpdateEntityFromModel_DeveAtualizarEntityComDadosDoModel()
        {
            var entity = new Domain.Entities.Grupo("Antigo");

            var model = new Grupo
            {
                Nome = "Novo"
            };

            _service.InvokeUpdateEntityFromModel(entity, model);

            Assert.Equal(model.Nome, entity.Nome);
        }
    }

    public static class GrupoServiceTestExtensions
    {
        public static Domain.Entities.Grupo? InvokeConvertToEntity(this GrupoService service, Grupo model)
        {
            var methodInfo = typeof(GrupoService)
                .GetMethod("ConvertToEntity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method 'ConvertToEntity' not found.");
            var result = methodInfo.Invoke(service, [model]);
            return result as Domain.Entities.Grupo;
        }

        public static Grupo? InvokeConvertToModel(this GrupoService service, Domain.Entities.Grupo entity)
        {
            var methodInfo = typeof(GrupoService)
                .GetMethod("ConvertToModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method 'ConvertToModel' not found.");
            var result = methodInfo.Invoke(service, [entity]);
            return result as Grupo;
        }

        public static void InvokeUpdateEntityFromModel(this GrupoService service, Domain.Entities.Grupo entity, Grupo model)
        {
            var methodInfo = typeof(GrupoService)
                .GetMethod("UpdateEntityFromModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method 'UpdateEntityFromModel' not found.");
            methodInfo.Invoke(service, [entity, model]);
        }

        public static async Task<IEnumerable<Domain.Entities.Grupo>> InvokeGetEntitiesByFilterAsync(this GrupoService service, NomeFilter filter)
        {
            var methodInfo = typeof(GrupoService)
                .GetMethod("GetEntitiesByFilterAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method 'GetEntitiesByFilterAsync' not found.");
            var result = await (methodInfo.Invoke(service, [filter]) as Task<IEnumerable<Domain.Entities.Grupo>> ?? Task.FromResult(Enumerable.Empty<Domain.Entities.Grupo>()));
            return result;
        }
    }
}