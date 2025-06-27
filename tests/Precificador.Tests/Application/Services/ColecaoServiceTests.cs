using Moq;
using Precificador.Application.Model;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;

namespace Precificador.Tests.Application.Services
{
    public class ColecaoServiceTests
    {
        private readonly Mock<IColecaoRepository> _repositoryMock;
        private readonly ColecaoService _service;

        public ColecaoServiceTests()
        {
            _repositoryMock = new Mock<IColecaoRepository>();
            _service = new ColecaoService(_repositoryMock.Object);
        }

        [Fact]
        public void ConvertToEntity_DeveConverterModelParaEntity()
        {
            var model = new Colecao
            {
                Id = Guid.NewGuid(),
                Nome = "Coleção Teste",
                Ano = 2024,
                DataLancamento = new DateTime(2024, 1, 1)
            };

            var entity = _service.InvokeConvertToEntity(model);

            Assert.NotNull(entity);
            Assert.Equal(model.Id, entity!.Id);
            Assert.Equal(model.Nome, entity.Nome);
            Assert.Equal(model.Ano, entity.Ano);
            Assert.Equal(model.DataLancamento, entity.DataLancamento);
        }

        [Fact]
        public void ConvertToModel_DeveConverterEntityParaModel()
        {
            var entity = new Domain.Entities.Colecao("Coleção Entity", 2023, new DateTime(2023, 5, 10));

            var model = _service.InvokeConvertToModel(entity);

            Assert.NotNull(model);
            Assert.Equal(entity.Id, model!.Id);
            Assert.Equal(entity.Nome, model.Nome);
            Assert.Equal(entity.Ano, model.Ano);
            Assert.Equal(entity.DataLancamento, model.DataLancamento);
        }

        [Fact]
        public void UpdateEntityFromModel_DeveAtualizarEntityComDadosDoModel()
        {
            var entity = new Domain.Entities.Colecao("Antigo", 2020, new DateTime(2020, 1, 1));

            var model = new Colecao
            {
                Nome = "Novo",
                Ano = 2025,
                DataLancamento = new DateTime(2025, 2, 2)
            };

            _service.InvokeUpdateEntityFromModel(entity, model);

            Assert.Equal(model.Nome, entity.Nome);
            Assert.Equal(model.Ano, entity.Ano);
            Assert.Equal(model.DataLancamento, entity.DataLancamento);
        }
    }

    public static class ColecaoServiceTestExtensions
    {
        public static Domain.Entities.Colecao? InvokeConvertToEntity(this ColecaoService service, Colecao model)
        {
            var methodInfo = typeof(ColecaoService).GetMethod("ConvertToEntity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return methodInfo == null
                ? throw new InvalidOperationException("Method ConvertToEntity not found.")
                : (Domain.Entities.Colecao?)methodInfo.Invoke(service, [model]);
        }

        public static Colecao? InvokeConvertToModel(this ColecaoService service, Domain.Entities.Colecao entity)
        {
            var methodInfo = typeof(ColecaoService).GetMethod("ConvertToModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return methodInfo == null
                ? throw new InvalidOperationException("Method ConvertToModel not found.")
                : (Colecao?)methodInfo.Invoke(service, [entity]);
        }

        public static void InvokeUpdateEntityFromModel(this ColecaoService service, Domain.Entities.Colecao entity, Colecao model)
        {
            var methodInfo = typeof(ColecaoService).GetMethod("UpdateEntityFromModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method UpdateEntityFromModel not found.");
            methodInfo.Invoke(service, [entity, model]);
        }

        public static Task<IEnumerable<Domain.Entities.Colecao>> InvokeGetEntitiesByFilterAsync(this ColecaoService service, ColecaoFilter filter)
        {
            var methodInfo = typeof(ColecaoService).GetMethod("GetEntitiesByFilterAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method GetEntitiesByFilterAsync not found.");
            var result = methodInfo.Invoke(service, [filter]);
            return result as Task<IEnumerable<Domain.Entities.Colecao>> ?? Task.FromResult(Enumerable.Empty<Domain.Entities.Colecao>());
        }
    }
}