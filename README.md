# Partners — API de Clientes e Fornecedores

API REST para gerir o cadastro de clientes e fornecedores de uma única empresa. Cada entidade
(`Partner`) pode ser cliente, fornecedor ou ambos, e existe **um único registo por NIF**.

## Funcionalidades

- Criação, consulta por Id, listagem paginada e actualização de entidades (4 endpoints).
- Normalização de nome, NIF e email antes da validação e da gravação.
- Unicidade do NIF garantida por um índice único no SQL Server (inclui registos inactivos).
- Concorrência optimista com `rowversion` do SQL Server, exposta como `Version` (Base64).
- Pesquisa por nome (parcial, sem distinguir maiúsculas/minúsculas) ou por NIF (com ou sem separadores).
- Erros uniformes em `ProblemDetails` (RFC 9457), sem stack traces nem detalhes de SQL.
- Dados de demonstração opcionais, apenas em Development.
- Testes unitários e testes de integração contra SQL Server real (Testcontainers).

## Arquitectura

Clean Architecture em 4 projectos, mais 2 de testes:

```
Partners.sln               → solução
Partners.Domain            → entidade Partner e enum PartnerType (sem dependências)
Partners.Persistence       → EF Core: AppDbContext, configuração, migrações, repositório genérico, seed
Partners.Application       → casos de uso (CQRS com MediatR), validadores (FluentValidation), DTOs
Partners.Api               → controller, Program.cs, tratamento global de erros, Swagger/Scalar
Partners.UnitTests         → normalização, validadores, token de versão, serialização dos contratos
Partners.IntegrationTests  → endpoints + persistência contra SQL Server real
http/partners.http         → pedidos de exemplo
```

Padrões usados:

- **CQRS com MediatR** — cada operação é um `Command`/`Query` + `Handler` em
  `Partners.Application/UseCases/Partners/{Commands,Queries}/<Operação>/`.
- **`ValidationBehavior`** — valida cada pedido antes do handler; falhas → HTTP 400.
- **`TransactionBehavior`** — cada `Command` corre numa transação, com rollback em caso de erro.
- **Repositório genérico** — `IGenericRepositoryAsync<T>`, sem repositórios por entidade.
- **`GlobalExceptionHandler`** — converte `ValidationException` → 400, `NotFoundException` → 404,
  `ConflictException` → 409 e qualquer outra excepção → 500 genérico.

Fluxo de um pedido: `PartnersController` → `ISender` (MediatR) → `ValidationBehavior` →
`TransactionBehavior` → handler → `IGenericRepositoryAsync<Partner>` → `AppDbContext` → SQL Server.

## Tecnologias e versões

| Componente | Versão |
|---|---|
| .NET SDK / runtime | 9.0.318 / 9.0.20 (`net9.0`) |
| ASP.NET Core | 9.0.20 |
| Entity Framework Core + SQL Server provider | 9.0.20 |
| dotnet-ef (ferramenta local) | 9.0.20 |
| MediatR | 12.3.0 |
| FluentValidation | 12.1.0 |
| Swashbuckle.AspNetCore | 9.0.6 |
| Microsoft.AspNetCore.OpenApi / Scalar.AspNetCore | 9.0.20 / 2.14.10 |
| xUnit v3 / xunit.runner.visualstudio / Microsoft.NET.Test.Sdk | 3.2.2 / 3.1.5 / 18.10.1 |
| Microsoft.AspNetCore.Mvc.Testing | 9.0.20 |
| Testcontainers.MsSql | 4.15.0 |
| SQL Server (testes) | imagem `mcr.microsoft.com/mssql/server:2022-latest` |

O `global.json` exige um SDK 9.0.x (`rollForward: latestFeature`).

## Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server 2019+ (LocalDB, Developer/Express, ou container Docker)
- Docker, para os testes de integração (ou uma instância SQL Server — ver abaixo)

## Instalação e configuração

### Connection string

A API lê `ConnectionStrings:Connection`. Não há credenciais no repositório.

- **Development** (`Partners.Api/appsettings.Development.json`) usa LocalDB com autenticação Windows:
  `Server=(localdb)\MSSQLLocalDB;Database=Partners;Trusted_Connection=True;TrustServerCertificate=True`
- **Outros ambientes**: definir por variável de ambiente ou user-secrets. Se não estiver definida,
  a API não arranca e indica a configuração em falta.

Exemplos de variáveis de ambiente:

```bash
# bash
export ConnectionStrings__Connection="Server=localhost,1433;Database=Partners;User Id=sa;Password=<a-sua-password>;TrustServerCertificate=True"
export ASPNETCORE_ENVIRONMENT=Development
export DemoData__SeedOnStartup=false
```

```powershell
# PowerShell
$env:ConnectionStrings__Connection = "Server=localhost,1433;Database=Partners;User Id=sa;Password=<a-sua-password>;TrustServerCertificate=True"
```

Ou, só na máquina local, com user-secrets:

```bash
dotnet user-secrets set "ConnectionStrings:Connection" "<connection string>" --project Partners.Api
```

SQL Server em Docker para desenvolvimento (opcional):

```bash
docker run -d --name sql-partners -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=<password-forte>" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
```

### Restaurar, compilar e migrar

```bash
dotnet tool restore                 # instala o dotnet-ef 9.0.20 (dotnet-tools.json)
dotnet restore Partners.sln
dotnet build Partners.sln

# Aplicar as migrações à base de dados configurada (cria a BD se não existir)
dotnet ef database update -p Partners.Persistence -s Partners.Api

# Criar uma nova migração depois de alterar o modelo
dotnet ef migrations add <Nome> -p Partners.Persistence -s Partners.Api -o DBContexts/Migrations

# Gerar o script SQL (para rever ou aplicar manualmente)
dotnet ef migrations script -p Partners.Persistence -s Partners.Api -o partners.sql
```

A migração inicial é `InitialPartners` (`Partners.Persistence/DBContexts/Migrations`). Cria a tabela
`partner` com:

| Coluna | Tipo | Regras |
|---|---|---|
| `id` | `uniqueidentifier` | chave primária, gerada pela API |
| `name` | `nvarchar(100)` collation `Latin1_General_100_CI_AS` | obrigatório |
| `tax_number` | `varchar(9)` | índice único `idx_partner_tax_number`; check: 9 dígitos |
| `type` | `varchar(10)` | check: `CUSTOMER`, `SUPPLIER`, `BOTH` |
| `email` | `nvarchar(254)` | opcional |
| `credit_limit` | `decimal(18,2)` | check: 0 a 1 000 000 |
| `is_active` | `bit` | |
| `version` | `rowversion` | token de concorrência |
| `created_at`, `updated_at` | `datetime2` | UTC, definidos pela API |

Índice adicional `idx_partner_name_id (name, id)` para a ordenação da listagem.

### Executar a API

```bash
dotnet run --project Partners.Api --launch-profile http
```

- API: `http://localhost:5268`
- Swagger UI: `http://localhost:5268/swagger`
- OpenAPI: `http://localhost:5268/openapi/v1.json` · Scalar: `http://localhost:5268/scalar/v1`

### Dados de demonstração

Com `ASPNETCORE_ENVIRONMENT=Development` e `DemoData:SeedOnStartup=true` (valor por defeito em
`appsettings.Development.json`), a API insere 25 entidades fictícias no arranque **se a tabela
estiver vazia**. Noutros ambientes nunca corre. Não aplica migrações: corra primeiro
`dotnet ef database update`.

Os dados cobrem clientes, fornecedores, `BOTH`, activos e inactivos, nomes repetidos
(“Ana Silva”, “Distribuidora Atlântico”), nomes para pesquisa parcial (“Ana”, “Mariana”, “Luana”),
NIF com zeros à esquerda (`001234567`, `012345678`) e volume suficiente para paginar.
Para desactivar: `DemoData__SeedOnStartup=false`.

## Testes

```bash
# Todos
dotnet test Partners.sln

# Unitários
dotnet test Partners.UnitTests/Partners.UnitTests.csproj

# Integração (SQL Server real)
dotnet test Partners.IntegrationTests/Partners.IntegrationTests.csproj
```

### SQL Server para os testes de integração

- **Por defeito**: o Testcontainers arranca um container `mcr.microsoft.com/mssql/server:2022-latest`
  (Docker tem de estar a correr). O container é partilhado pela execução e removido no fim.
- **Sem Docker**: definir `INTEGRATION_TESTS_SQLSERVER` com a connection string de um servidor
  (a base de dados indicada é ignorada):

  ```powershell
  $env:INTEGRATION_TESTS_SQLSERVER = "Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True"
  dotnet test Partners.IntegrationTests/Partners.IntegrationTests.csproj
  ```

Cada teste cria uma base de dados nova (`partners_test_<guid>`) com a migração real
(`MigrateAsync`) e apaga-a no fim. Os testes de concorrência usam pedidos HTTP simultâneos
(cada um com o seu scope, `DbContext` e ligação) e também dois `DbContext` independentes com
transacções sobrepostas, para provar que o bloqueio e a falha acontecem no SQL Server.

### O que é testado

| Projecto | O que testa | Base de dados |
|---|---|---|
| `Partners.UnitTests` | Regras isoladas (normalização, validadores, token de versão, contratos JSON) | Não |
| `Partners.IntegrationTests` | Os 4 endpoints via HTTP, com a API em memória (`WebApplicationFactory`) | SQL Server real |

#### Partners.UnitTests

Testes rápidos, sem API nem base de dados. Chamam directamente as classes da camada Application.

- **`PartnerNormalizerTests`** — normalização e formatos:
  - NIF: `001.234.567`, `001 234 567`, `001-234-567` e combinações → `001234567`;
    rejeita 8 ou 10 dígitos, letras, outros separadores (`/`) e dígitos Unicode não ASCII.
  - Nome: aplica `Trim`.
  - Email: vazio ou só espaços → `null`; caso contrário `Trim`; formatos válidos e inválidos
    (`ana@`, `ana@example`, `ana@@example.com`, espaços, `..`).
  - Type: aceita só `CUSTOMER`, `SUPPLIER`, `BOTH` exactos; rejeita minúsculas, espaços,
    números, listas e `null`.
- **`PartnerValidatorTests`** — validadores FluentValidation:
  - Criação: nome obrigatório, 2 a 100 caracteres depois de `Trim`, sem caracteres de controlo;
    NIF inválido; Type inválido; email inválido; email ausente ou em branco aceite;
    CreditLimit `10.999`, `-0.01` e `1000000.01` rejeitados; `0`, `10.99`, `10.990` e `1000000` aceites.
  - Actualização: comando válido aceite; `Version` em falta, vazia, não Base64 ou com tamanho errado → erro.
  - Listagem: `Page` < 1 e `PageSize` fora de 1–100 rejeitados; Type inválido rejeitado;
    valores por defeito (`Page=1`, `PageSize=20`, `IncludeInactive=false`) válidos.
- **`VersionTokenTests`** — `Version` (rowversion ↔ Base64):
  - Codificar e descodificar devolve os mesmos 8 bytes (`AAAAAAAAB9E=`).
  - Rejeita `null`, vazio, só espaços, texto não Base64, e Base64 com 3 ou 10 bytes.
- **`ContractSerializationTests`** — contrato JSON:
  - PUT aceita `email: null` explícito, mas rejeita o pedido se faltar `email`, `version`,
    `isActive`, `creditLimit` ou `name`.
  - POST: `email` é opcional, `creditLimit` é obrigatório.
  - A resposta (`PartnerDto`) serializa em camelCase, com o NIF como texto e as datas em UTC (`Z`).

#### Partners.IntegrationTests

Cada teste arranca a API em memória, faz pedidos HTTP reais e confirma o resultado também
directamente na base de dados (com um `DbContext` ou `SqlConnection` independente da API).

- **`TaxNumberAndUpdateTests`** — NIF, actualização e 404:
  - Criar com `" Ana Silva "` e `"001.234.567"` grava `"Ana Silva"` e `"001234567"` (coluna
    `varchar`, zero à esquerda preservado); `"001 234 567"` → 409; o NIF continua reservado
    depois de desactivar.
  - Desactivar sai da listagem por defeito, aparece com `IncludeInactive=true`, continua
    acessível por Id; reactivar devolve-a à listagem.
  - Manter o próprio NIF é permitido; mudar para o NIF de outra entidade → 409 sem alterar dados.
  - `CreatedAt` não muda, `UpdatedAt` e `Version` mudam; Id e datas enviados pelo cliente são ignorados.
  - `Version` desactualizada → 409, mantendo os dados mais recentes.
  - POST devolve 201 com `Location` que aponta para o GET; ignora `id`, `isActive`, datas e `version` do cliente.
  - GET e PUT com Id inexistente → 404.
  - PUT exige `email` presente (pode ser `null`) e `version` presente → 400 caso contrário.
- **`ListingTests`** — listagem:
  - `Type=CUSTOMER` e `Type=SUPPLIER` incluem `BOTH`; `Type=BOTH` só `BOTH`.
  - Pesquisa por nome parcial, sem distinguir maiúsculas; `Search=Ana` não faz corresponder
    todos os NIF.
  - Pesquisa por NIF com ou sem separadores, e por parte do NIF; `...` ou `- -` não devolvem tudo;
    `%` e `_` são tratados literalmente.
  - Filtros combinados com AND (pesquisa + tipo + inactivos).
  - `TotalCount` é contado antes da paginação; página além do fim devolve `Items` vazio com
    `TotalCount` correcto; defeitos `Page=1`, `PageSize=20`.
  - Nomes iguais são desempatados por Id, também entre páginas.
  - `IncludeInactive` é `false` por defeito.
- **`ConcurrencyTests`** — concorrência no SQL Server:
  - Dois POST simultâneos com o mesmo NIF (escrito de formas diferentes) → um 201 e um 409, e um
    único registo (repetido 5 vezes).
  - Dois PUT simultâneos com a mesma `Version` → um 200 e um 409; o estado final é exactamente o
    da escrita confirmada (repetido 5 vezes).
  - Dois `DbContext` independentes com transacções sobrepostas: o segundo INSERT fica bloqueado
    pelo SQL Server e depois falha pelo índice único; o segundo UPDATE falha pelo `rowversion`.
- **`ValidationTests`** — erros 400 sem alterações:
  - POST inválido (CreditLimit `10.999`, negativo ou acima do limite, NIF com letras ou 10 dígitos,
    Type inválido, nome vazio ou curto, email inválido, JSON com tipos errados) → 400 e nada gravado.
  - PUT inválido (incluindo `Version` malformada) → 400 e o registo fica igual.
  - Id malformado no URL (`GET`/`PUT /api/partners/not-a-guid`) → 400 e nada alterado.
  - Listagem com `Page=0`, `PageSize` 0 ou 101, Type inválido, valores não numéricos → 400;
    valores-limite aceites.
  - Mensagens de erro em português (validação, JSON malformado, corpo vazio, 404 e 409), em
    `application/problem+json`.
  - `CreditLimit` com 2 casas decimais é gravado sem arredondamento.
- **`Infrastructure/SqlServerFixture`** — arranca um SQL Server por execução de testes (container
  Docker via Testcontainers, ou o servidor indicado em `INTEGRATION_TESTS_SQLSERVER`).
- **`Infrastructure/PartnersApiTestBase`** — classe base: cria uma base de dados nova por teste
  (com a migração real), arranca a API ligada a ela, apaga-a no fim e disponibiliza os métodos
  auxiliares (`CreateAsync`, `GetAsync`, `UpdateAsync`, `ListAsync`, `CreateDbContext`, ...).

#### Cenários do enunciado

| Cenário | Testes |
|---|---|
| 1. Normalização do nome e NIF; NIF duplicado → 409; reservado após desactivação | `TaxNumberAndUpdateTests.Create_NormalizesNameAndTaxNumber_AndKeepsTaxNumberReservedWhenInactive` |
| 2. `BOTH` aparece em CUSTOMER, SUPPLIER e BOTH; CUSTOMER não aparece em SUPPLIER | `ListingTests.TypeFilter_CustomerAndSupplierIncludeBoth_BothIsExclusive` |
| 3. Desactivar e reactivar por PUT | `TaxNumberAndUpdateTests.DeactivateAndReactivate_ControlsDefaultListing` |
| 4. Manter o próprio NIF; NIF de outra entidade → 409 sem alterar dados | `TaxNumberAndUpdateTests.Update_KeepsOwnTaxNumber_ButRejectsTaxNumberOfAnotherPartner` |
| 5. Criações e actualizações simultâneas; contextos independentes | `ConcurrencyTests` (todos) |
| 6. CreditLimit `10,999`, NIF com letras, `Page=0`, Type inválido, Version malformada → 400 sem alterações | `ValidationTests.Post_InvalidData_Returns400_AndWritesNothing`, `Put_InvalidData_Returns400_AndLeavesRowUnchanged`, `List_InvalidParameters_Returns400` |
| 7. Filtros combinados, `TotalCount` antes da paginação, desempate por Id, `Search=Ana` | `ListingTests.Filters_AreCombinedWithAnd`, `Paging_TotalCountIsBeforePaging_AndPageBeyondEndIsEmpty`, `Ordering_SameNames_AreTieBrokenById`, `Search_ByName_IsPartialAndCaseInsensitive_AndDoesNotMatchEveryTaxNumber` |

## Endpoints

| Método | Rota | Sucesso | Erros |
|---|---|---|---|
| `POST` | `/api/partners` | 201 + `Location` | 400, 409 |
| `GET` | `/api/partners/{id}` | 200 | 400 (Id malformado), 404 |
| `GET` | `/api/partners` | 200 | 400 |
| `PUT` | `/api/partners/{id}` | 200 | 400, 404, 409 |

Ver `http/partners.http` para pedidos prontos a executar (Visual Studio, Rider ou REST Client).

### POST /api/partners

```http
POST /api/partners
Content-Type: application/json

{ "name": "  Ana Silva ", "taxNumber": "001.234.567", "type": "CUSTOMER", "email": "ana@example.com", "creditLimit": 1500.50 }
```

`name`, `taxNumber`, `type` e `creditLimit` são obrigatórios; `email` é opcional. `id`, `isActive`,
`version` e datas enviados pelo cliente são ignorados.

```http
HTTP/1.1 201 Created
Location: http://localhost:5268/api/partners/2b1c5e0a-...

{
  "id": "2b1c5e0a-...",
  "name": "Ana Silva",
  "taxNumber": "001234567",
  "type": "CUSTOMER",
  "email": "ana@example.com",
  "creditLimit": 1500.50,
  "isActive": true,
  "version": "AAAAAAAAB9E=",
  "createdAt": "2026-10-10T11:20:00.1234567Z",
  "updatedAt": "2026-10-10T11:20:00.1234567Z"
}
```

### GET /api/partners/{id}

Devolve a entidade (também se estiver inactiva) ou 404.

### GET /api/partners

| Parâmetro | Defeito | Regras |
|---|---|---|
| `Search` | — | parte do nome (sem distinguir maiúsculas) ou do NIF (separadores ignorados); máx. 100 caracteres |
| `Type` | — | `CUSTOMER` (inclui `BOTH`), `SUPPLIER` (inclui `BOTH`), `BOTH` (só `BOTH`) |
| `IncludeInactive` | `false` | `true` inclui inactivos |
| `Page` | `1` | ≥ 1 |
| `PageSize` | `20` | 1 a 100 |

Filtros combinados com AND. Ordenação por `name` e depois `id`. Filtros, contagem, ordenação e
paginação são executados no SQL Server.

```json
{ "items": [ { "id": "...", "name": "Ana Silva", "...": "..." } ], "totalCount": 3, "page": 1, "pageSize": 20 }
```

`totalCount` é o total depois dos filtros e antes da paginação. Uma página sem resultados devolve
`items: []` com o `totalCount` correcto.

### PUT /api/partners/{id}

Todas as propriedades são obrigatórias no JSON; `email` pode ser `null`, mas tem de estar presente.

```json
{ "name": "Ana Silva", "taxNumber": "001234567", "type": "BOTH", "email": null, "creditLimit": 2000, "isActive": false, "version": "AAAAAAAAB9E=" }
```

Devolve 200 com os dados e a nova `version`. `createdAt` mantém-se; `updatedAt` é definido pelo
servidor. Para desactivar/reactivar, enviar `isActive` a `false`/`true`.

## Regras de validação e normalização

- **Name**: `Trim`; 2 a 100 caracteres; sem caracteres de controlo.
- **TaxNumber (NIF)**: removem-se espaços, pontos e hífenes; depois tem de ter exactamente 9
  dígitos ASCII. `001.234.567`, `001 234 567` e `001-234-567` são todos `001234567`.
  Guardado como texto (zeros à esquerda preservados). A mesma normalização é aplicada na
  criação, actualização e pesquisa. O NIF continua reservado se a entidade for desactivada.
- **Type**: exactamente `CUSTOMER`, `SUPPLIER` ou `BOTH` (sensível a maiúsculas; números e
  outras variações são rejeitados).
- **Email**: vazio ou só espaços → `null`; caso contrário `Trim` e validação de formato; máx. 254.
- **CreditLimit**: `decimal`, 0 a 1 000 000, no máximo 2 casas decimais (`10.999` → 400, sem arredondar).
- **Pesquisa**: o ramo do NIF só é usado se o termo normalizado for uma sequência não vazia de
  dígitos — `Search=Ana` ou `Search=...` nunca fazem todos os NIF corresponder. Os wildcards de
  `LIKE` (`%`, `_`) são tratados literalmente.

## NIF único e concorrência optimista

- A unicidade do NIF é garantida pelo índice único `idx_partner_tax_number`. A API não faz uma
  consulta prévia: tenta gravar e converte a violação do índice (erros SQL 2601/2627) em 409.
  Dois pedidos simultâneos com o mesmo NIF resultam num 201 e num 409.
- `Version` é o `rowversion` da linha em Base64 (8 bytes). Cada resposta inclui a versão actual.
  No PUT, a versão enviada é usada como valor original do token de concorrência e o EF Core gera
  `UPDATE ... WHERE id = @id AND version = @version`. Se outra escrita já mudou a linha, o UPDATE
  afecta 0 linhas → `DbUpdateConcurrencyException` → 409. Duas actualizações com a mesma versão
  resultam num 200 e num 409; a escrita desactualizada nunca sobrescreve a mais recente.
- `Version` em falta ou malformada (não Base64 ou tamanho diferente de 8 bytes) → 400.
- Cada `Command` corre numa transação: um erro de validação ou conflito não deixa alterações parciais.

## Tratamento de erros

Todas as respostas de erro são `application/problem+json`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Ocorreram um ou mais erros de validação.",
  "status": 400,
  "instance": "/api/partners",
  "errors": { "CreditLimit": ["O limite de crédito deve ter no máximo 2 casas decimais."] },
  "traceId": "00-..."
}
```

Todas as mensagens são em português: as regras de negócio (FluentValidation, em
`PartnerValidationRules` e nos validadores de cada caso de uso) e também os erros detectados
pelo ASP.NET Core antes dos validadores — JSON malformado, propriedades obrigatórias em falta,
tipos errados ou parâmetros de query não numéricos (`Partners.Api/Configuration/ValidationMessagesConfig.cs`).
As chaves de `errors` mantêm o nome do campo (`CreditLimit`, `Page`) ou o caminho JSON (`$.creditLimit`).

| Código | Quando |
|---|---|
| 400 | campos, JSON, parâmetros de query, filtros ou `Version` inválidos |
| 404 | Id inexistente |
| 409 | NIF já usado por outra entidade, ou `Version` desactualizada |
| 500 | erro inesperado — mensagem genérica, detalhes só no log do servidor |

## Resolução de problemas

- **`A compatible .NET SDK was not found` / `requires SDK 9.0.100`** — instale o .NET 9 SDK; o
  `global.json` não aceita SDKs anteriores.
- **`Connection string 'ConnectionStrings:Connection' is not configured`** — defina a variável
  `ConnectionStrings__Connection` (ou user-secrets) no ambiente em que está a correr.
- **`Invalid object name 'partner'`** ou aviso “Demo data was not seeded” — as migrações não foram
  aplicadas: `dotnet ef database update -p Partners.Persistence -s Partners.Api`.
- **`dotnet ef` não encontrado** — `dotnet tool restore` na raiz do repositório.
- **Testes de integração falham ao arrancar o container** — confirme que o Docker está a correr
  (`docker info`) ou use `INTEGRATION_TESTS_SQLSERVER`.
- **Erro de certificado ao ligar ao SQL Server** — acrescente `TrustServerCertificate=True` à
  connection string em ambientes de desenvolvimento.
- **409 inesperado no PUT** — leia a entidade outra vez (GET) e reenvie com a `version` actual.
