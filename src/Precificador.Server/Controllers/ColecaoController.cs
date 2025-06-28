using Microsoft.AspNetCore.Mvc;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;
using Precificador.Server.Controllers.Base;

namespace Precificador.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ColecaoController(IColecaoService service, ILogger<ColecaoController> logger) : BaseCrudController<Application.Model.Colecao, Domain.Entities.Colecao, ColecaoFilter, IColecaoService, IColecaoRepository>(service, logger)
    {

    }
}