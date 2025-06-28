using Microsoft.AspNetCore.Mvc;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;
using Precificador.Server.Controllers.Base;

namespace Precificador.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GrupoController(IGrupoService service, ILogger<GrupoController> logger) : BaseCrudController<Application.Model.Grupo, Domain.Entities.Grupo, NomeFilter, IGrupoService, IGrupoRepository>(service, logger)
    {

    }
}