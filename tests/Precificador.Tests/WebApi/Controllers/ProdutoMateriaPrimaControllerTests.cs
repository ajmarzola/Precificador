using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Precificador.Application.Model;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.WebApi.Controllers;

namespace Precificador.Tests.WebApi.Controllers
{
    public class ProdutoMateriaPrimaControllerTests
    {
        private readonly Mock<IProdutoMateriaPrimaService> _serviceMock;
        private readonly Mock<ILogger<ProdutoMateriaPrimaController>> _loggerMock;
        private readonly ProdutoMateriaPrimaController _controller;

        public ProdutoMateriaPrimaControllerTests()
        {
            _serviceMock = new Mock<IProdutoMateriaPrimaService>();
            _loggerMock = new Mock<ILogger<ProdutoMateriaPrimaController>>();
            _controller = new ProdutoMateriaPrimaController(_serviceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WhenDataExists()
        {
            var data = new List<ProdutoMateriaPrima>
            {
                new() { Id = Guid.NewGuid(), ProdutoId = Guid.NewGuid(), MateriaPrimaId = Guid.NewGuid(), Quantidade = 1.5m }
            };
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(data);

            var result = await _controller.GetAll();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(data, okResult.Value);
        }

        [Fact]
        public async Task GetAll_ReturnsNoContent_WhenNoData()
        {
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync((IEnumerable<ProdutoMateriaPrima>?)null);

            var result = await _controller.GetAll();

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var id = Guid.NewGuid();
            var model = new ProdutoMateriaPrima { Id = id, ProdutoId = Guid.NewGuid(), MateriaPrimaId = Guid.NewGuid(), Quantidade = 2.0m };
            _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(model);

            var result = await _controller.GetById(id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(model, okResult.Value);
        }

        [Fact]
        public async Task GetById_ReturnsNoContent_WhenNotFound()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((ProdutoMateriaPrima?)null);

            var result = await _controller.GetById(id);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetByFilterAsync_ReturnsOk_WhenDataExists()
        {
            var filter = new ProdutoMateriaPrimaFilter { ProdutoId = Guid.NewGuid() };
            var data = new List<ProdutoMateriaPrima>
            {
                new() { Id = Guid.NewGuid(), ProdutoId = filter.ProdutoId.Value, MateriaPrimaId = Guid.NewGuid(), Quantidade = 1.0m }
            };
            _serviceMock.Setup(s => s.GetByFilterAsync(filter)).ReturnsAsync(data);

            var result = await _controller.GetByFilterAsync(filter);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(data, okResult.Value);
        }

        [Fact]
        public async Task GetByFilterAsync_ReturnsNoContent_WhenNoData()
        {
            var filter = new ProdutoMateriaPrimaFilter { ProdutoId = Guid.NewGuid() };
            _serviceMock.Setup(s => s.GetByFilterAsync(filter)).ReturnsAsync((IEnumerable<ProdutoMateriaPrima>?)null);

            var result = await _controller.GetByFilterAsync(filter);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Post_ReturnsOk_WhenSuccess()
        {
            var model = new ProdutoMateriaPrima { Id = Guid.NewGuid(), ProdutoId = Guid.NewGuid(), MateriaPrimaId = Guid.NewGuid(), Quantidade = 3.0m };
            _serviceMock.Setup(s => s.AddAsync(model)).ReturnsAsync(true);

            var result = await _controller.Post(model);

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Post_ReturnsBadRequest_WhenFailure()
        {
            var model = new ProdutoMateriaPrima { Id = Guid.NewGuid(), ProdutoId = Guid.NewGuid(), MateriaPrimaId = Guid.NewGuid(), Quantidade = 3.0m };
            _serviceMock.Setup(s => s.AddAsync(model)).ReturnsAsync(false);

            var result = await _controller.Post(model);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsOk_WhenSuccess()
        {
            var model = new ProdutoMateriaPrima { Id = Guid.NewGuid(), ProdutoId = Guid.NewGuid(), MateriaPrimaId = Guid.NewGuid(), Quantidade = 4.0m };
            _serviceMock.Setup(s => s.UpdateAsync(model)).ReturnsAsync(true);

            var result = await _controller.Put(model);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsNoContent_WhenNotFound()
        {
            var model = new ProdutoMateriaPrima { Id = Guid.NewGuid(), ProdutoId = Guid.NewGuid(), MateriaPrimaId = Guid.NewGuid(), Quantidade = 4.0m };
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