using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Precificador.Application.Model;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.WebApi.Controllers;

namespace Precificador.Tests.WebApi.Controllers
{
    public class MateriaPrimaControllerTests
    {
        private readonly Mock<IMateriaPrimaService> _serviceMock;
        private readonly Mock<ILogger<MateriaPrimaController>> _loggerMock;
        private readonly MateriaPrimaController _controller;

        public MateriaPrimaControllerTests()
        {
            _serviceMock = new Mock<IMateriaPrimaService>();
            _loggerMock = new Mock<ILogger<MateriaPrimaController>>();
            _controller = new MateriaPrimaController(_serviceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WhenDataExists()
        {
            var data = new List<MateriaPrima>
            {
                new() {
                    Id = Guid.NewGuid(),
                    Nome = "Açúcar",
                    QtdPacote = 10,
                    VlrPacote = 20,
                    DataPreco = DateTime.UtcNow,
                    VlrUnitario = 2,
                    UnidadeMedidaId = Guid.NewGuid(),
                    GrupoId = Guid.NewGuid()
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
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync((IEnumerable<MateriaPrima>?)null);

            var result = await _controller.GetAll();

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var id = Guid.NewGuid();
            var model = new MateriaPrima
            {
                Id = id,
                Nome = "Farinha",
                QtdPacote = 5,
                VlrPacote = 15,
                DataPreco = DateTime.UtcNow,
                VlrUnitario = 3,
                UnidadeMedidaId = Guid.NewGuid(),
                GrupoId = Guid.NewGuid()
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
            _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((MateriaPrima?)null);

            var result = await _controller.GetById(id);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetByFilterAsync_ReturnsOk_WhenDataExists()
        {
            var filter = new NomeFilter { Nome = "Açúcar" };
            var data = new List<MateriaPrima>
            {
                new() {
                    Id = Guid.NewGuid(),
                    Nome = "Açúcar",
                    QtdPacote = 10,
                    VlrPacote = 20,
                    DataPreco = DateTime.UtcNow,
                    VlrUnitario = 2,
                    UnidadeMedidaId = Guid.NewGuid(),
                    GrupoId = Guid.NewGuid()
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
            var filter = new NomeFilter { Nome = "Açúcar" };
            _serviceMock.Setup(s => s.GetByFilterAsync(filter)).ReturnsAsync((IEnumerable<MateriaPrima>?)null);

            var result = await _controller.GetByFilterAsync(filter);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Post_ReturnsOk_WhenSuccess()
        {
            var model = new MateriaPrima
            {
                Id = Guid.NewGuid(),
                Nome = "Ovo",
                QtdPacote = 12,
                VlrPacote = 24,
                DataPreco = DateTime.UtcNow,
                VlrUnitario = 2,
                UnidadeMedidaId = Guid.NewGuid(),
                GrupoId = Guid.NewGuid()
            };
            _serviceMock.Setup(s => s.AddAsync(model)).ReturnsAsync(true);

            var result = await _controller.Post(model);

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Post_ReturnsBadRequest_WhenFailure()
        {
            var model = new MateriaPrima
            {
                Id = Guid.NewGuid(),
                Nome = "Ovo",
                QtdPacote = 12,
                VlrPacote = 24,
                DataPreco = DateTime.UtcNow,
                VlrUnitario = 2,
                UnidadeMedidaId = Guid.NewGuid(),
                GrupoId = Guid.NewGuid()
            };
            _serviceMock.Setup(s => s.AddAsync(model)).ReturnsAsync(false);

            var result = await _controller.Post(model);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsOk_WhenSuccess()
        {
            var model = new MateriaPrima
            {
                Id = Guid.NewGuid(),
                Nome = "Leite",
                QtdPacote = 1,
                VlrPacote = 5,
                DataPreco = DateTime.UtcNow,
                VlrUnitario = 5,
                UnidadeMedidaId = Guid.NewGuid(),
                GrupoId = Guid.NewGuid()
            };
            _serviceMock.Setup(s => s.UpdateAsync(model)).ReturnsAsync(true);

            var result = await _controller.Put(model);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsNoContent_WhenNotFound()
        {
            var model = new MateriaPrima
            {
                Id = Guid.NewGuid(),
                Nome = "Leite",
                QtdPacote = 1,
                VlrPacote = 5,
                DataPreco = DateTime.UtcNow,
                VlrUnitario = 5,
                UnidadeMedidaId = Guid.NewGuid(),
                GrupoId = Guid.NewGuid()
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