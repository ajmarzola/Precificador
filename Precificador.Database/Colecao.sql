﻿CREATE TABLE [dbo].[Colecao] (
    [Id]             INT           NOT NULL IDENTITY(1, 1),
    [Nome]           VARCHAR (200) NOT NULL,
    [DataLancamento] DATE          NOT NULL,
    CONSTRAINT [PK_Colecao] PRIMARY KEY CLUSTERED ([Id] ASC)
);

