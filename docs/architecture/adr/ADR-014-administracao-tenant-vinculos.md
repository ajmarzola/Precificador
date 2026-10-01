# ADR-014 — Administração tenant de vínculos e proteção do último Administrador

- **Status:** Aceito

## Contexto

O Precificador possui identidade global em UsuarioAplicacao e autoridade contextual em UsuarioEmpresa.

A UC031 precisa permitir que Administradores de Empresa gerenciem usuários sem:

- transformar perfil tenant em role global;
- apagar a conta Identity;
- afetar outras Empresas;
- deixar uma Empresa Ativa sem Administrador;
- criar uma máquina de estados paralela de convite.

Também existem operações concorrentes capazes de violar a invariável do último Administrador se forem implementadas apenas com leitura e escrita sem serialização.

## Decisão

### Identidade global, vínculo local

UsuarioAplicacao continua global.

UsuarioEmpresa continua sendo a única fonte de:

- Empresa;
- Ativo/Inativo;
- Perfil Operacional/Administrador.

AdministradorEmpresa só administra vínculos da Empresa Ativa.

### Convite

Não criar entidade Convite.

Adicionar usuário cria ou reativa o vínculo imediatamente.

Usuário sem senha depende da ativação UC040 para autenticar.

Usuário já ativado recebe aviso de acesso liberado.

### Desvínculo

Desvínculo é lógico:

~~~text
UsuarioEmpresa.Ativo = false
~~~

Não há DELETE cotidiano do vínculo.

Isso preserva contexto histórico e permite reativação sem duplicação.

### Último Administrador

Empresa Ativa deve manter ao menos um vínculo Ativo/Administrador.

Demissão e desvínculo de Admin são serializados por Empresa.

Usar transação Serializable e application lock SQL com recurso derivado do EmpresaId.

O lock é transacional e nunca envolve SMTP.

### Concorrência da identidade

O lock por Empresa não tenta resolver unicidade global de usuário.

NormalizedEmail único no Identity permanece autoridade global.

Corridas de criação do mesmo e-mail em Empresas diferentes devem convergir por retry/reconsulta controlada.

### Credenciais

Administrador tenant não gerencia senha, e-mail, tokens ou roles globais.

Ativação e recuperação continuam na UC040.

## Consequências positivas

- preserva o desenho N:N original;
- evita duplicar contas;
- evita hard delete;
- impede Empresa sem Admin mesmo sob concorrência;
- não cria tabela/estado extra de convite;
- mantém UC031 isolada ao tenant;
- reutiliza serviços de e-mail/token existentes.

## Custos

- transações de escrita de vínculo precisam de lock por Empresa;
- corrida Identity cross-tenant exige tratamento de unicidade/retry;
- auto-desvínculo exige limpeza explícita do EmpresaContext;
- vínculo inativo continua armazenado.

## Alternativas rejeitadas

### AdministradorEmpresa como role Identity

Rejeitado porque autoridade empresarial depende da Empresa e pode variar por tenant.

### Excluir UsuarioEmpresa ao desvincular

Rejeitado porque perde o vínculo lógico e dificulta reativação segura.

### Tabela Convite

Rejeitada no MVP porque UC040 já cobre ativação e o vínculo pode existir antes da credencial.

### Verificar último Admin sem lock

Rejeitado porque duas requisições concorrentes poderiam remover os dois últimos Administradores.

### Administrador tenant resetar senha

Rejeitado porque a credencial é global e pode ser usada em múltiplas Empresas.

## Relação com ADRs anteriores

ADR-011 define os dois planos de autorização.

ADR-012 define tokens/e-mail.

ADR-013 define ciclo de vida global da Empresa.

ADR-014 define a administração cotidiana tenant-aware de UsuarioEmpresa.
