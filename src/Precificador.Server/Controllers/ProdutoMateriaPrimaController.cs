using Microsoft.AspNetCore.Mvc;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;
using Precificador.Server.Controllers.Base;

namespace Precificador.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProdutoMateriaPrimaController(IProdutoMateriaPrimaService service, ILogger<ProdutoMateriaPrimaController> logger) : BaseCrudController<Application.Model.ProdutoMateriaPrima, Domain.Entities.ProdutoMateriaPrima, ProdutoMateriaPrimaFilter, IProdutoMateriaPrimaService, IProdutoMateriaPrimaRepository>(service, logger)
    {

    }
}