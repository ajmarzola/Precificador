using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Precificador.Application.Model;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.WebApi.Controllers;

namespace Precificador.Tests.WebApi.Controllers
{
    public class ProdutoControllerTests
    {
        private readonly Mock<IProdutoService> _serviceMock;
        private readonly Mock<ILogger<ProdutoController>> _loggerMock;
        private readonly ProdutoController _controller;

        public ProdutoControllerTests()
        {
            _serviceMock = new Mock<IProdutoService>();
            _loggerMock = new Mock<ILogger<ProdutoController>>();
            _controller = new ProdutoController(_serviceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WhenDataExists()
        {
            var data = new List<Produto>
            {
                new() {
                    Id = Guid.NewGuid(),
                    Nome = "Produto 1",
                    ColecaoId = Guid.NewGuid(),
                    Margem = 10.0m,
                    DataCalculoPreco = DateTime.UtcNow,
                    PrecoCusto = 100,
                    PrecoFinal = 120,
                    PrecoCustoX3 = 300,
                    PrecoCustoX35 = 350,
                    PrecoCustoX4 = 400
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
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync((IEnumerable<Produto>?)null);

            var result = await _controller.GetAll();

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var id = Guid.NewGuid();
            var model = new Produto
            {
                Id = id,
                Nome = "Produto Teste",
                ColecaoId = Guid.NewGuid(),
                Margem = 15.0m,
                DataCalculoPreco = DateTime.UtcNow,
                PrecoCusto = 200,
                PrecoFinal = 250,
                PrecoCustoX3 = 600,
                PrecoCustoX35 = 700,
                PrecoCustoX4 = 800
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
            _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((Produto?)null);

            var result = await _controller.GetById(id);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetByFilterAsync_ReturnsOk_WhenDataExists()
        {
            var filter = new NomeFilter { Nome = "Produto" };
            var data = new List<Produto>
            {
                new() {
                    Id = Guid.NewGuid(),
                    Nome = "Produto",
                    ColecaoId = Guid.NewGuid(),
                    Margem = 20.0m,
                    DataCalculoPreco = DateTime.UtcNow,
                    PrecoCusto = 300,
                    PrecoFinal = 350,
                    PrecoCustoX3 = 900,
                    PrecoCustoX35 = 1050,
                    PrecoCustoX4 = 1200
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
            var filter = new NomeFilter { Nome = "Produto" };
            _serviceMock.Setup(s => s.GetByFilterAsync(filter)).ReturnsAsync((IEnumerable<Produto>?)null);

            var result = await _controller.GetByFilterAsync(filter);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Post_ReturnsOk_WhenSuccess()
        {
            var model = new Produto
            {
                Id = Guid.NewGuid(),
                Nome = "Produto Novo",
                ColecaoId = Guid.NewGuid(),
                Margem = 25.0m,
                DataCalculoPreco = DateTime.UtcNow,
                PrecoCusto = 400,
                PrecoFinal = 500,
                PrecoCustoX3 = 1200,
                PrecoCustoX35 = 1400,
                PrecoCustoX4 = 1600
            };
            _serviceMock.Setup(s => s.AddAsync(model)).ReturnsAsync(true);

            var result = await _controller.Post(model);

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Post_ReturnsBadRequest_WhenFailure()
        {
            var model = new Produto
            {
                Id = Guid.NewGuid(),
                Nome = "Produto Novo",
                ColecaoId = Guid.NewGuid(),
                Margem = 25.0m,
                DataCalculoPreco = DateTime.UtcNow,
                PrecoCusto = 400,
                PrecoFinal = 500,
                PrecoCustoX3 = 1200,
                PrecoCustoX35 = 1400,
                PrecoCustoX4 = 1600
            };
            _serviceMock.Setup(s => s.AddAsync(model)).ReturnsAsync(false);

            var result = await _controller.Post(model);

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsOk_WhenSuccess()
        {
            var model = new Produto
            {
                Id = Guid.NewGuid(),
                Nome = "Produto Atualizado",
                ColecaoId = Guid.NewGuid(),
                Margem = 30.0m,
                DataCalculoPreco = DateTime.UtcNow,
                PrecoCusto = 500,
                PrecoFinal = 600,
                PrecoCustoX3 = 1500,
                PrecoCustoX35 = 1750,
                PrecoCustoX4 = 2000
            };
            _serviceMock.Setup(s => s.UpdateAsync(model)).ReturnsAsync(true);

            var result = await _controller.Put(model);

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsNoContent_WhenNotFound()
        {
            var model = new Produto
            {
                Id = Guid.NewGuid(),
                Nome = "Produto Atualizado",
                ColecaoId = Guid.NewGuid(),
                Margem = 30.0m,
                DataCalculoPreco = DateTime.UtcNow,
                PrecoCusto = 500,
                PrecoFinal = 600,
                PrecoCustoX3 = 1500,
                PrecoCustoX35 = 1750,
                PrecoCustoX4 = 2000
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

            Assert.IsType<OkResult>(result);
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