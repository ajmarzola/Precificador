﻿CREATE TABLE [dbo].[UnidadeMedida] (
    [Id]   INT           IDENTITY (1, 1) NOT NULL,
    [Nome] VARCHAR (200) NOT NULL,
    CONSTRAINT [PK_UnidadeMedida] PRIMARY KEY CLUSTERED ([Id] ASC)
);

