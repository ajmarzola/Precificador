using Microsoft.AspNetCore.Mvc;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;
using Precificador.WebApi.Controllers.Base;

namespace Precificador.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ColecaoProdutoController(IColecaoProdutoService service, ILogger<ColecaoProdutoController> logger) : BaseCrudController<Application.Model.ColecaoProduto, Domain.Entities.ColecaoProduto, ColecaoProdutoFilter, IColecaoProdutoService, IColecaoProdutoRepository>(service, logger)
    {
    }
}