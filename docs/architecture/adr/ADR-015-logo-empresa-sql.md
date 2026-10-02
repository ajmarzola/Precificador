# ADR-015 — Persistir logo da Empresa no SQL Server/Azure SQL

- **Status:** Aceito

## Contexto

A UC037 precisa armazenar um logo opcional por Empresa.

O Precificador está publicado em App Service Linux F1 e Azure SQL. O filesystem local do App Service não deve ser tratado como armazenamento durável de dados de negócio, e introduzir Blob Storage apenas para logos pequenos aumentaria infraestrutura, configuração e custo operacional.

O piloto é de baixo volume e cada logo será limitado a 512 KiB.

## Decisão

Persistir o logo em entidade tenant-owned separada da Empresa:

~~~text
IdentidadeVisualEmpresa
- EmpresaId
- CorPrimaria
- LogoConteudo
- LogoContentType
~~~

O logo será armazenado em varbinary(max), limitado logicamente e por constraint a 512 KiB.

Somente PNG/JPEG detectado por assinatura binária será aceito.

A entidade permanece opcional: ausência de linha significa identidade padrão.

O endpoint de leitura usa sempre a Empresa Ativa, não recebe EmpresaId e responde com Cache-Control: no-store.

## Alternativas rejeitadas

### Filesystem do App Service

Rejeitado porque acopla dado persistente ao filesystem da instância/deploy e complica portabilidade e recuperação.

### Azure Blob Storage

Arquiteturalmente válido, mas rejeitado nesta fase por adicionar serviço externo, configuração, credencial/Managed Identity específica, provisionamento e custo sem necessidade para o volume previsto.

### Base64 diretamente em HTML

Rejeitado porque aumenta o HTML de todas as páginas, impede cache independente e repete bytes do logo a cada resposta.

### Colunas em Empresa

Rejeitado para evitar carregar binário em consultas frequentes de Empresa usadas por autenticação/administração.

## Consequências positivas

- persistência durável junto ao restante do tenant;
- backup/restauração acompanha o banco;
- nenhum serviço externo adicional;
- isolamento tenant reutiliza GQF/guard existente;
- Empresa permanece leve.

## Custos

- cada leitura do logo consulta o banco;
- varbinary aumenta o tamanho do banco;
- Cache-Control no-store prioriza correção multiempresa sobre otimização.

Esses custos são aceitáveis para o piloto e para o limite de 512 KiB.

## Evolução futura

Se volume, tamanho ou tráfego de imagens crescerem, mover conteúdo para Blob Storage mantendo metadados/ownership no banco poderá ser tratado em ADR futura.