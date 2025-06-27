using Precificador.Application.Services.Base;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;

namespace Precificador.Application.Services
{
    public class MateriaPrimaService(IMateriaPrimaRepository repository) : CrudServiceBase<Model.MateriaPrima, Domain.Entities.MateriaPrima, NomeFilter, IMateriaPrimaRepository>(repository), IMateriaPrimaService
    {
        protected override Domain.Entities.MateriaPrima ConvertToEntity(Model.MateriaPrima model)
        {
            var retorno = new Domain.Entities.MateriaPrima(model.Nome, model.QtdPacote, model.VlrPacote, model.DataPreco, model.GrupoId, model.UnidadeMedidaId);
            retorno.SetId(model.Id);
            return retorno;
        }

        protected override Model.MateriaPrima ConvertToModel(Domain.Entities.MateriaPrima entity)
        {
            return new Model.MateriaPrima
            {
                Id = entity.Id,
                Nome = entity.Nome,
                QtdPacote = entity.QtdPacote,
                VlrPacote = entity.VlrPacote,
                DataPreco = entity.DataPreco,
                VlrUnitario = entity.VlrUnitario,
                GrupoId = entity.GrupoId,
                UnidadeMedidaId = entity.UnidadeMedidaId
            };
        }

        protected override void UpdateEntityFromModel(Domain.Entities.MateriaPrima entity, Model.MateriaPrima model)
        {
            entity.SetNome(model.Nome);
            entity.SetQtdPacote(model.QtdPacote);
            entity.SetVlrPacote(model.VlrPacote);
            entity.SetDataPreco(model.DataPreco);
            entity.SetGrupoId(model.GrupoId);
            entity.SetUnidadeMedidaId(model.UnidadeMedidaId);
        }
    }
}