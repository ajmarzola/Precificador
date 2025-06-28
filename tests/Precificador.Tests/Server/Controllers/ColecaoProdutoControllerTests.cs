using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Precificador.Server.Controllers;
using Precificador.Application.Services;
using Precificador.Application.Model;
using Precificador.Domain.Filters;

namespace Precificador.Tests.Server.Controllers
{
    public class ColecaoProdutoControllerTests
    {
        private readonly Mock<IColecaoProdutoService> _serviceMock;
        private readonly Mock<ILogger<ColecaoProdutoController>> _loggerMock;
        private readonly ColecaoProdutoController _controller;

        public ColecaoProdutoControllerTests()
        {
            _serviceMock = new Mock<IColecaoProdutoService>();
            _loggerMock = new Mock<ILogger<ColecaoProdutoController>>();
            _controller = new ColecaoProdutoController(_serviceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WhenDataExists()
        {
            var data = new List<ColecaoProduto> { new() { Id = Guid.NewGuid(), ColecaoId = Guid.NewGuid(), ProdutoId = Guid.NewGuid() } };
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(data);

            var result = await _controller.GetAll();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(data, okResult.Value);
        }

        [Fact]
        public async Task GetAll_ReturnsNoContent_WhenNoData()
        {
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync((IEnumerable<ColecaoProduto>?)null);

            var result = await _controller.GetAll();

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var id = Guid.NewGuid();
            var model = new ColecaoProduto { Id = id, ColecaoId = Guid.NewGuid(), ProdutoId = Guid.NewGuid() };
            _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(model);

            var result = await _controller.GetById(id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(model, okResult.Value);
        }

        [Fact]
        public async Task GetById_ReturnsNoContent_WhenNotFound()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((ColecaoProduto?)null);

            var result = await _controller.GetById(id);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetByFilterAsync_ReturnsOk_WhenDataExists()
        {
            var filter = new ColecaoProdutoFilter();
            var data = new List<ColecaoProduto> { new() { Id = Guid.NewGuid(), ColecaoId = Guid.NewGuid(), ProdutoId = Guid.NewGuid() } };
            _serviceMock.Setup(s => s.GetByFilterAsync(filter)).ReturnsAsync(data);

            var result = await _controller.GetByFilterAsync(filter);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(data, okResult.Value);
        }

        [Fact]
        public async Task GetByFilterAsync_ReturnsNoContent_WhenNoData()
        {
            var filter = new ColecaoProdutoFilter();
            _serviceMock.Setup(s => s.GetByFilterAsync(filter)).ReturnsAsync((IEnumerable<ColecaoProduto>?)null);

            var result = await _controller.GetByFilterAsync(filter);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Post_ReturnsOk_WhenSuccessful()
        {
            var model = new ColecaoProduto { Id = Guid.NewGuid(), ColecaoId = Guid.NewGuid(), ProdutoId = Guid.NewGuid() };
            _serviceMock.Setup(s => s.AddAsync(model)).ReturnsAsync(true);

            var result = await _controller.Post(model);

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Post_ReturnsBadRequest_WhenFailed()
        {
            var model = new ColecaoProduto { Id = Guid.NewGuid(), ColecaoId = Guid.NewGuid(), ProdutoId = Guid.NewGuid() };
            _serviceMock.Setup(s => s.AddAsync(model)).ReturnsAsync(false);

            var result = await _controller.Post(model);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsOk_WhenSuccessful()
        {
            var model = new ColecaoProduto { Id = Guid.NewGuid(), ColecaoId = Guid.NewGuid(), ProdutoId = Guid.NewGuid() };
            _serviceMock.Setup(s => s.UpdateAsync(model.Id, model)).ReturnsAsync(true);

            var result = await _controller.Put(model.Id, model);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True((bool)okResult.Value!);
        }

        [Fact]
        public async Task Put_ReturnsNoContent_WhenNotFound()
        {
            var model = new ColecaoProduto { Id = Guid.NewGuid(), ColecaoId = Guid.NewGuid(), ProdutoId = Guid.NewGuid() };
            _serviceMock.Setup(s => s.UpdateAsync(model.Id, model)).ReturnsAsync(false);

            var result = await _controller.Put(model.Id, model);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsOk_WhenSuccessful()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeleteAsync(id)).ReturnsAsync(true);

            var result = await _controller.Delete(id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True((bool)okResult.Value!);
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