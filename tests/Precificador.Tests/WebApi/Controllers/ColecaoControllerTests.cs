using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Precificador.Application.Model;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.WebApi.Controllers;

namespace Precificador.Tests.WebApi.Controllers
{
    public class ColecaoControllerTests
    {
        private readonly Mock<IColecaoService> _serviceMock;
        private readonly Mock<ILogger<ColecaoController>> _loggerMock;
        private readonly ColecaoController _controller;

        public ColecaoControllerTests()
        {
            _serviceMock = new Mock<IColecaoService>();
            _loggerMock = new Mock<ILogger<ColecaoController>>();
            _controller = new ColecaoController(_serviceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WhenDataExists()
        {
            var data = new List<Colecao>
            {
                new() {
                    Id = Guid.NewGuid(),
                    Nome = "Coleção Verão",
                    Ano = 2025,
                    DataLancamento = DateTime.UtcNow
                }
            };
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(data);

            var result = await _controller.GetAll();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(data, okResult.Value);
        }

        [Fact]
        public async Task GetAll_ReturnsNoContent_WhenNoData()
        {
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync((IEnumerable<Colecao>?)null);

            var result = await _controller.GetAll();

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var id = Guid.NewGuid();
            var model = new Colecao
            {
                Id = id,
                Nome = "Coleção Outono",
                Ano = 2024,
                DataLancamento = DateTime.UtcNow
            };
            _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(model);

            var result = await _controller.GetById(id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(model, okResult.Value);
        }

        [Fact]
        public async Task GetById_ReturnsNoContent_WhenNotFound()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((Colecao?)null);

            var result = await _controller.GetById(id);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetByFilterAsync_ReturnsOk_WhenDataExists()
        {
            var filter = new ColecaoFilter { Nome = "Verão", Ano = 2025 };
            var data = new List<Colecao>
            {
                new() {
                    Id = Guid.NewGuid(),
                    Nome = "Coleção Verão",
                    Ano = 2025,
                    DataLancamento = DateTime.UtcNow
                }
            };
            _serviceMock.Setup(s => s.GetByFilterAsync(filter)).ReturnsAsync(data);

            var result = await _controller.GetByFilterAsync(filter);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(data, okResult.Value);
        }

        [Fact]
        public async Task GetByFilterAsync_ReturnsNoContent_WhenNoData()
        {
            var filter = new ColecaoFilter { Nome = "Inexistente", Ano = 2020 };
            _serviceMock.Setup(s => s.GetByFilterAsync(filter)).ReturnsAsync((IEnumerable<Colecao>?)null);

            var result = await _controller.GetByFilterAsync(filter);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Post_ReturnsOk_WhenSuccess()
        {
            var model = new Colecao
            {
                Id = Guid.NewGuid(),
                Nome = "Coleção Primavera",
                Ano = 2026,
                DataLancamento = DateTime.UtcNow
            };
            _serviceMock.Setup(s => s.AddAsync(model)).ReturnsAsync(true);

            var result = await _controller.Post(model);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Post_ReturnsBadRequest_WhenFailure()
        {
            var model = new Colecao
            {
                Id = Guid.NewGuid(),
                Nome = "Coleção Primavera",
                Ano = 2026,
                DataLancamento = DateTime.UtcNow
            };
            _serviceMock.Setup(s => s.AddAsync(model)).ReturnsAsync(false);

            var result = await _controller.Post(model);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsOk_WhenSuccess()
        {
            var model = new Colecao
            {
                Id = Guid.NewGuid(),
                Nome = "Coleção Atualizada",
                Ano = 2027,
                DataLancamento = DateTime.UtcNow
            };
            _serviceMock.Setup(s => s.UpdateAsync(model)).ReturnsAsync(true);

            var result = await _controller.Put(model);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsNoContent_WhenNotFound()
        {
            var model = new Colecao
            {
                Id = Guid.NewGuid(),
                Nome = "Coleção Atualizada",
                Ano = 2027,
                DataLancamento = DateTime.UtcNow
            };
            _serviceMock.Setup(s => s.UpdateAsync(model)).ReturnsAsync(false);

            var result = await _controller.Put(model);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsOk_WhenSuccess()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeleteAsync(id)).ReturnsAsync(true);

            var result = await _controller.Delete(id);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent_WhenNotFound()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeleteAsync(id)).ReturnsAsync(false);

            var result = await _controller.Delete(id);

            Assert.IsType<NoContentResult>(result);
        }
    }
}