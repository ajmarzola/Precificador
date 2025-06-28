using Microsoft.AspNetCore.Mvc;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;
using Precificador.Server.Controllers.Base;

namespace Precificador.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MateriaPrimaController(IMateriaPrimaService service, ILogger<MateriaPrimaController> logger) : BaseCrudController<Application.Model.MateriaPrima, Domain.Entities.MateriaPrima, NomeFilter, IMateriaPrimaService, IMateriaPrimaRepository>(service, logger)
    {

    }
}