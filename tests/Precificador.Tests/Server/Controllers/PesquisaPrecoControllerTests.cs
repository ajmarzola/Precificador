using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Precificador.Application.Model;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.Server.Controllers;

namespace Precificador.Tests.Server.Controllers
{
    public class PesquisaPrecoControllerTests
    {
        private readonly Mock<IPesquisaPrecoService> _serviceMock;
        private readonly Mock<ILogger<PesquisaPrecoController>> _loggerMock;
        private readonly PesquisaPrecoController _controller;

        public PesquisaPrecoControllerTests()
        {
            _serviceMock = new Mock<IPesquisaPrecoService>();
            _loggerMock = new Mock<ILogger<PesquisaPrecoController>>();
            _controller = new PesquisaPrecoController(_serviceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WhenDataExists()
        {
            var data = new List<PesquisaPreco>
            {
                new() {
                    Id = Guid.NewGuid(),
                    ProdutoId = Guid.NewGuid(),
                    Local = "Supermercado X",
                    Valor = 10.5m,
                    DataPesquisa = DateTime.UtcNow
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
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync((IEnumerable<PesquisaPreco>?)null);

            var result = await _controller.GetAll();

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var id = Guid.NewGuid();
            var model = new PesquisaPreco
            {
                Id = id,
                ProdutoId = Guid.NewGuid(),
                Local = "Supermercado Y",
                Valor = 12.0m,
                DataPesquisa = DateTime.UtcNow
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
            _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((PesquisaPreco?)null);

            var result = await _controller.GetById(id);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetByFilterAsync_ReturnsOk_WhenDataExists()
        {
            var filter = new PesquisaPrecoFilter { Local = "Supermercado Z" };
            var data = new List<PesquisaPreco>
            {
                new() {
                    Id = Guid.NewGuid(),
                    ProdutoId = Guid.NewGuid(),
                    Local = "Supermercado Z",
                    Valor = 15.0m,
                    DataPesquisa = DateTime.UtcNow
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
            var filter = new PesquisaPrecoFilter { Local = "Supermercado Z" };
            _serviceMock.Setup(s => s.GetByFilterAsync(filter)).ReturnsAsync((IEnumerable<PesquisaPreco>?)null);

            var result = await _controller.GetByFilterAsync(filter);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Post_ReturnsOk_WhenSuccess()
        {
            var model = new PesquisaPreco
            {
                Id = Guid.NewGuid(),
                ProdutoId = Guid.NewGuid(),
                Local = "Supermercado A",
                Valor = 20.0m,
                DataPesquisa = DateTime.UtcNow
            };
            _serviceMock.Setup(s => s.AddAsync(model)).ReturnsAsync(true);

            var result = await _controller.Post(model);

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Post_ReturnsBadRequest_WhenFailure()
        {
            var model = new PesquisaPreco
            {
                Id = Guid.NewGuid(),
                ProdutoId = Guid.NewGuid(),
                Local = "Supermercado A",
                Valor = 20.0m,
                DataPesquisa = DateTime.UtcNow
            };
            _serviceMock.Setup(s => s.AddAsync(model)).ReturnsAsync(false);

            var result = await _controller.Post(model);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsOk_WhenSuccess()
        {
            var model = new PesquisaPreco
            {
                Id = Guid.NewGuid(),
                ProdutoId = Guid.NewGuid(),
                Local = "Supermercado B",
                Valor = 22.0m,
                DataPesquisa = DateTime.UtcNow
            };
            _serviceMock.Setup(s => s.UpdateAsync(model.Id, model)).ReturnsAsync(true);

            var result = await _controller.Put(model.Id, model);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsNoContent_WhenNotFound()
        {
            var model = new PesquisaPreco
            {
                Id = Guid.NewGuid(),
                ProdutoId = Guid.NewGuid(),
                Local = "Supermercado B",
                Valor = 22.0m,
                DataPesquisa = DateTime.UtcNow
            };
            _serviceMock.Setup(s => s.UpdateAsync(model.Id, model)).ReturnsAsync(false);

            var result = await _controller.Put(model.Id, model);

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