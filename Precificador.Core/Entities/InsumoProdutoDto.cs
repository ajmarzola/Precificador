﻿using Dapper;

namespace Precificador.Core.Entities
{
    internal class InsumoProdutoDto : BaseEntity
    {
        [Required]
        public int IdProduto { get; set; }

        [Required]
        public int IdInsumo { get; set; }

        [Required]
        public decimal Quantidade { get; set; }

        [Required]
        public decimal PrecoInsumo { get; set; }

        [Required]
        public decimal TotalInsumo { get; set; }
    }
}