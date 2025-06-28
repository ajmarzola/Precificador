using Microsoft.AspNetCore.Mvc;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;
using Precificador.Server.Controllers.Base;

namespace Precificador.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnidadeMedidaController(IUnidadeMedidaService service, ILogger<UnidadeMedidaController> logger) : BaseCrudController<Application.Model.UnidadeMedida, Domain.Entities.UnidadeMedida, NomeFilter, IUnidadeMedidaService, IUnidadeMedidaRepository>(service, logger)
    {

    }
}