using Microsoft.AspNetCore.Mvc;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;
using Precificador.Server.Controllers.Base;

namespace Precificador.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProdutoController(IProdutoService service, ILogger<ProdutoController> logger) : BaseCrudController<Application.Model.Produto, Domain.Entities.Produto, NomeFilter, IProdutoService, IProdutoRepository>(service, logger)
    {

    }
}