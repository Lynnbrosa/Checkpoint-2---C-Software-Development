# ExpenseHub

API REST de reembolsos corporativos do Checkpoint 2 de C#. Funcionários registram
despesas, aprovadores decidem, o financeiro paga e auditores consultam o histórico.

Especificação e backlog: [Racass/checkpoint-csharpracass-expensehub](https://github.com/Racass/checkpoint-csharpracass-expensehub).
Cópia do enunciado em [docs/ENUNCIADO.md](docs/ENUNCIADO.md) e dos contratos em [docs/REQUISITOS.md](docs/REQUISITOS.md).

## Integrantes

| Nome | RM | GitHub |
|---|---|---|
| Lynn Bueno Rosa | 551102 | [@Lynnbrosa](https://github.com/Lynnbrosa) |
| Gustavo Moura | 555827 | [@gumoura82](https://github.com/gumoura82) |
| Giovanne Zaniboni | 556223 | [@GiovanneZaniboni](https://github.com/GiovanneZaniboni) |

## Stack

- .NET 10 e ASP.NET Core
- Entity Framework Core 10 com SQLite
- MSTest para os testes unitários

## Como executar

Pré-requisito: SDK do .NET 10.

```shell
dotnet restore ./sources/ExpenseHub.slnx
dotnet build ./sources/ExpenseHub.slnx
dotnet test ./sources/ExpenseHub.slnx
```

Antes do primeiro start, defina a senha do Admin inicial (fica fora do repositório):

```shell
dotnet user-secrets set "Seed:Admin:Password" "<senha-forte>" --project ./sources/ExpenseHub.Api
dotnet run --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj
```

A API sobe em `http://localhost:5245`, cria o banco SQLite, aplica as migrations e
cria as roles e o Admin. `GET /health` responde `{"status":"ok"}`. Em Development o
documento OpenAPI fica em `/openapi/v1.json`.

O arquivo `sources/ExpenseHub.Api/ExpenseHub.Api.http` tem o roteiro completo de
validação (cadastro, roles, rascunho, envio, aprovação, pagamento, histórico e os casos
negativos). Ele roda no Visual Studio, no Rider e no VS Code com a extensão REST Client.
Preencha as variáveis de senha no topo antes de usar e não commite senhas reais.

Roteiro rápido:

1. `POST /login` com o Admin.
2. `POST /register` para cada pessoa (funcionário, aprovador, financeiro, auditor).
3. `PUT /api/admin/users/{id}/roles` para dar as roles.
4. Cada pessoa faz `POST /login` (de novo, se já tinha token) e segue o fluxo.

## Banco de dados

| Item | Valor |
|---|---|
| Provider | SQLite |
| Pacote | `Microsoft.EntityFrameworkCore.Sqlite` 10.0.12 |
| Connection string | `ConnectionStrings:ExpenseHub` em `sources/ExpenseHub.Api/appsettings.json` |
| Arquivo | `sources/ExpenseHub.Api/expensehub.db` (ignorado pelo Git) |

O caminho relativo do `Data Source` é resolvido a partir da pasta do projeto da API,
então o arquivo fica no mesmo lugar rodando pelo terminal ou pela IDE.
O SQLite não precisa de servidor, e nem o build nem os testes dependem do banco.

### Criar ou atualizar o banco

A API aplica as migrations pendentes quando inicia. Para aplicar sem subir a API:

```shell
dotnet tool restore
dotnet ef database update --project ./sources/ExpenseHub.Api
```

Nova migration:

```shell
dotnet ef migrations add NomeDaMigration --project ./sources/ExpenseHub.Api --output-dir Data/Migrations
```

Para começar do zero, pare a API e apague `expensehub.db`, `expensehub.db-shm` e `expensehub.db-wal`.

## Autenticação

A API usa ASP.NET Core Identity com o token bearer do próprio Identity. O token é
opaco, protegido pelo Data Protection do ASP.NET Core, então não existe chave de
assinatura para guardar em configuração. A senha é armazenada só como hash pelo Identity.

### Admin inicial

Ao iniciar, a API cria as roles `Admin`, `Employee`, `Approver`, `Finance` e `Auditor`
e uma única conta Admin. Nenhum outro usuário é criado pelo seed. Rodar de novo não
duplica nada: role existente é mantida e, se a conta do Admin já existe, o seed não mexe nela.

O e-mail fica em `Seed:Admin:Email` no `appsettings.json`. A senha não é versionada;
configure antes do primeiro start com User Secrets:

```shell
dotnet user-secrets set "Seed:Admin:Password" "<senha-forte>" --project ./sources/ExpenseHub.Api
```

ou pela variável de ambiente `Seed__Admin__Password`. A senha precisa seguir a política
padrão do Identity: 6 caracteres ou mais, com maiúscula, minúscula, número e símbolo.
Sem ela a API não sobe e avisa qual chave falta.

### Login

`POST /login` com `{"email": "...", "password": "..."}` devolve:

```json
{ "tokenType": "Bearer", "accessToken": "...", "expiresIn": 3600, "refreshToken": "..." }
```

Envie `Authorization: Bearer <accessToken>` nas rotas protegidas. `GET /me` mostra
o id e as roles que estão no token atual.

| Situação | Resposta |
|---|---|
| E-mail inexistente ou senha errada | `401`, mesma mensagem nos dois casos |
| 5 senhas erradas seguidas | `401` com conta bloqueada por 5 minutos |
| Rota protegida sem token ou com token inválido | `401` |
| Token válido sem a role exigida | `403` |

Todas as respostas de erro seguem `ProblemDetails`.

## Usuários e roles

`POST /register` é público e cria o usuário **sem nenhuma role**:

```json
{ "email": "ana@empresa.com", "password": "<senha>", "fullName": "Ana Souza" }
```

O contrato não tem campo de role; mandar `roles` (ou qualquer campo extra) devolve `400`.
E-mail já cadastrado devolve `409` e senha fora da política do Identity devolve `400`.
Um usuário sem role consegue fazer login, mas recebe `403` em todas as rotas funcionais.

Quem dá role é o Admin:

| Método e rota | O que faz |
|---|---|
| `GET /api/admin/users` | Lista usuários com as roles de cada um (uma consulta só) |
| `PUT /api/admin/users/{id}/roles` | Substitui as roles do usuário |

```json
{ "roles": ["Employee", "Approver"] }
```

A lista é o conjunto final: o que não estiver nela é removido, e lista vazia tira todas.

| Situação | Resposta |
|---|---|
| Role fora de `Admin`, `Employee`, `Approver`, `Finance`, `Auditor` (inclusive com outra caixa) | `400`, nada muda e nenhuma role é criada |
| Usuário inexistente | `404` |
| Admin tentando tirar a própria role `Admin` | `403` |
| Quem não é Admin | `403` |

**Depois de alterar roles o usuário precisa fazer login de novo.** O token bearer guarda
as roles do momento do login. A troca de roles atualiza o security stamp do usuário, e o
`SecurityStampValidationMiddleware` recusa com `401` qualquer token emitido antes disso.
O novo `POST /login` já traz as roles atualizadas. O custo é uma consulta por requisição autenticada.

## Reembolsos

| Método e rota | Quem pode | O que faz |
|---|---|---|
| `POST /api/expenses` | Employee | Cria um rascunho (`Draft`) com o usuário do token como dono |
| `PUT /api/expenses/{id}` | Employee dono | Edita o rascunho |
| `GET /api/expenses` | Employee, Approver, Finance, Auditor | Lista o que o perfil enxerga |
| `GET /api/expenses/{id}` | Employee, Approver, Finance, Auditor | Detalhe, se estiver visível |
| `POST /api/expenses/{id}/submit` | Employee dono | `Draft` → `Submitted` |

Corpo de criação e edição:

```json
{ "description": "Táxi do aeroporto até o cliente", "amount": 87.50, "expenseDate": "2026-09-20", "categoryId": 1 }
```

| Campo | Regra |
|---|---|
| `description` | obrigatório, 10 a 500 caracteres |
| `amount` | de 0,01 a 2.147.483.647,00 (`decimal`) |
| `expenseDate` | `yyyy-MM-dd`, não pode ser futura (dia do servidor) |
| `categoryId` | 1 Transporte, 2 Alimentação, 3 Hospedagem, 4 Material de escritório, 5 Outros |

Id, dono, estado, atores e horários são sempre do servidor. Mandar qualquer campo fora
do contrato (`ownerId`, `status`, `createdAt`...) devolve `400`, em vez de ser ignorado.
Editar um rascunho sem mudar nada devolve `200` e não gera histórico.

## Aprovação e reprovação

| Método e rota | Quem pode | O que faz |
|---|---|---|
| `POST /api/expenses/{id}/approve` | Approver que não é o dono | `Submitted` → `Approved` |
| `POST /api/expenses/{id}/reject` | Approver que não é o dono | `Submitted` → `Rejected` |

A reprovação exige justificativa de 10 a 500 caracteres (só espaços não vale):

```json
{ "justification": "Nota fiscal ilegível, reenviar com comprovante." }
```

A justificativa fica no reembolso (`rejectionReason`) e na entrada de histórico da
reprovação. Quem decidiu e quando vêm do token e do relógio do servidor; o corpo não
aceita ator nem horário.

| Situação | Resposta |
|---|---|
| Employee, Finance, Auditor ou Admin sem a role Approver | `403` |
| Approver decidindo sobre o próprio reembolso (mesmo sendo Employee também) | `403` |
| Rascunho de outra pessoa ou id inexistente | `404` |
| Reembolso já `Approved`, `Rejected` ou `Paid` | `409`, sem histórico novo |
| Duas decisões ao mesmo tempo no mesmo reembolso | a segunda recebe `409` |
| Justificativa vazia, curta ou longa | `400` |

A proteção contra decisão simultânea é concorrência otimista: o reembolso tem um
`ConcurrencyStamp` que muda a cada transição, e o `UPDATE` só passa se o valor lido
ainda for o do banco.

## Pagamento e histórico

| Método e rota | Quem pode | O que faz |
|---|---|---|
| `POST /api/expenses/{id}/pay` | Finance que não é o dono | `Approved` → `Paid` e cria o `PaymentRecord` |
| `GET /api/expenses/{id}/history` | Employee, Approver, Finance, Auditor | Histórico, com a mesma visibilidade do reembolso |

O pagamento é simulado: não existe gateway. O `PaymentRecord` guarda o valor aprovado,
quem pagou e quando, tudo vindo do token e do relógio do servidor. O corpo da requisição
é ignorado.

| Situação | Resposta |
|---|---|
| `Submitted`, `Rejected` ou `Paid` | `409`, sem pagamento nem histórico novos |
| Finance pagando o próprio reembolso (mesmo sendo Employee também) | `403` |
| Quem não tem a role Finance, incluindo o Auditor | `403` |
| Rascunho de outra pessoa ou id inexistente | `404` |

### O que entra no histórico

| Ação | Estado anterior → novo | Extra |
|---|---|---|
| `Created` | — → `Draft` | |
| `Updated` | `Draft` → `Draft` | `changes`: campos alterados com valor antigo e novo |
| `Submitted` | `Draft` → `Submitted` | |
| `Approved` | `Submitted` → `Approved` | |
| `Rejected` | `Submitted` → `Rejected` | `justification` |
| `Paid` | `Approved` → `Paid` | |

Toda entrada tem ator e instante em UTC. A mudança de estado, o `PaymentRecord` e a
entrada de histórico são adicionados ao mesmo agregado e gravados num único
`SaveChanges`, que o EF Core executa numa transação: ou entra tudo, ou nada.

O histórico só aparece para quem enxerga o reembolso. Um Approver, por exemplo, não vê
o histórico de um reembolso já pago (`404`), igual ao detalhe.

## Matriz de acesso

A matriz de [docs/MATRIZ-AUTORIZACAO.md](docs/MATRIZ-AUTORIZACAO.md) está em
`sources/ExpenseHub.Api/Services/ExpenseAccessPolicy.cs`. O `[Authorize(Roles = ...)]`
dos controllers é só a primeira barreira; o serviço confere a role de novo e aplica
escopo, dono e estado. Toda escrita segue a mesma ordem:

1. o usuário tem a role da operação? senão `403`
2. o reembolso está no escopo dele? senão `404`
3. a regra de dono permite? (dono edita e envia; dono nunca aprova, reprova ou paga) senão `403`
4. o estado atual aceita a transição? senão `409`, sem gravar histórico

O que cada role enxerga na leitura:

| Role | Enxerga |
|---|---|
| Employee | os próprios reembolsos, em qualquer estado |
| Approver | `Submitted` |
| Finance | `Approved` e `Paid` |
| Auditor | todos |
| Admin | nada; o Admin só lê reembolso se tiver também uma das roles acima |

Roles somam: Employee + Approver vê os próprios e os `Submitted` dos outros, mas não os
rascunhos dos outros. O filtro é uma `Expression` aplicada no `IQueryable`, então vira
`WHERE` no SQL; nada é carregado para ser filtrado em memória.

Decisão sobre `404` x `409`: para aprovar, reprovar ou pagar, o Approver e o Finance
encontram qualquer reembolso que já saiu de rascunho. Assim, repetir uma aprovação ou
tentar pagar algo ainda `Submitted` responde `409`, como pede a tabela de transições.
Rascunho de outra pessoa nunca aparece para ninguém além do dono e do Auditor: `404`.

## Testes

```shell
dotnet test ./sources/ExpenseHub.slnx
```

São 193 testes unitários com MSTest, sem banco, rede ou serviço externo:

- `Fakes/InMemoryExpenseRepository` troca o repositório do EF Core por listas em memória
  e segue o mesmo contrato: o `Add` só vale depois do save e o filtro de escopo é
  aplicado antes de devolver qualquer reembolso;
- `Fakes/FakeUserDirectory` fica no lugar do `UserManager` do Identity;
- `Fakes/FixedTimeProvider` fixa o relógio, então "hoje" e os horários do histórico
  são sempre os mesmos;
- nenhuma senha literal no código de teste: `TestSecrets` gera uma por execução.

| Classe | O que cobre |
|---|---|
| `Domain/ExpenseWorkflowTests`, `Domain/ExpenseWorkflowTableTests` | tabela de transições inteira e estados finais |
| `Domain/ExpenseTests` | invariantes da entidade |
| `Services/ExpenseServiceDraftTests` | criação, validações, edição e dono vindo do token |
| `Services/ExpenseServiceSubmitTests` | envio, repetição e edição depois de enviar |
| `Services/ExpenseQueryTests` | listagem e detalhe por perfil |
| `Services/ExpenseAccessPolicyTests`, `Services/ExpenseOwnershipTests` | matriz, roles acumuladas, `403` x `404` |
| `Services/ExpenseDecisionServiceTests` | aprovação, reprovação, justificativa e autoaprovação |
| `Services/ExpensePaymentServiceTests` | pagamento, registro e autopagamento |
| `Services/ExpenseHistoryServiceTests` | conteúdo e visibilidade do histórico |
| `Services/ExpenseLifecycleTests` | fluxo completo e repetição sem histórico duplicado |
| `Services/AccountServiceTests`, `Services/UserAdministrationServiceTests` | cadastro sem role e administração de roles |
| `Contracts/RequestValidationTests` | DataAnnotations dos DTOs e campos aceitos |

Para conferir se a suíte pega regressão de verdade, introduzimos um defeito por vez
no código da API e rodamos os testes. Todos foram detectados:

| Defeito introduzido | Testes que falharam |
|---|---:|
| dono consegue aprovar ou pagar o próprio reembolso | 4 |
| Finance passa a enxergar `Submitted` | 2 |
| Employee passa a enxergar reembolsos de todos | 9 |
| pagamento sem checar o estado | 5 |
| R$ 0,01 deixa de ser aceito | 1 |
| justificativa de 9 caracteres aceita | 1 |
| transição sem gravar histórico | 10 |
| `Rejected` passa a ser pagável | 5 |
| data de amanhã aceita | 1 |
| Admin consegue tirar a própria role Admin | 1 |
| role desconhecida aceita | 3 |
| cadastro já entrega a role Employee | 1 |
| dono do reembolso não vem do token | 7 |

## Qualidade de código

O workflow `code-quality` (`.github/workflows/build.yml`, do template) roda a cada push
e publica o relatório no artefato `code-quality-report`. Para rodar o mesmo script
localmente é preciso PowerShell 7; o Gitleaks é opcional fora do CI:

```shell
pwsh ./scripts/Invoke-CodeQuality.ps1
```

O relatório sai em `artifacts/code-quality/report.md` (pasta ignorada pelo Git).

- build sem warnings de compilador, analisadores ou estilo;
- nenhum analisador, severidade, workflow ou script do template foi alterado;
- nenhum `#pragma` ou `NoWarn` no código escrito à mão (as migrations são geradas pelo EF);
- DTOs com validação declarativa e nenhuma entidade recebida direto pela API;
- nenhum segredo versionado: senha do Admin por User Secrets ou variável de ambiente.

## Estrutura

```text
sources/
├── ExpenseHub.Api/
│   ├── Controllers/   rotas HTTP e [Authorize] por role
│   ├── Contracts/     DTOs de entrada (Requests) e de saída (Responses)
│   ├── Services/      regras de negócio, matriz de acesso e erros tipados
│   ├── Domain/        entidades, tabela de transições e regras de campo
│   ├── Data/          DbContext, mapeamentos, migrations e repositório do EF Core
│   ├── Identity/      usuário, roles, seed do Admin e diretório de usuários
│   └── Security/      contexto do usuário e validação do security stamp
└── ExpenseHub.UnitTests/
    ├── Fakes/         repositório, diretório de usuários e relógio em memória
    ├── Domain/
    ├── Services/
    └── Contracts/
```

Os controllers não têm regra: montam o `UserContext` a partir do token, chamam o serviço
e traduzem o `ServiceError` em `ProblemDetails` num lugar só (`ServiceResultExtensions`).

## Processo

Cada issue do backlog central virou uma branch (`i01-foundation-ef`, `i02-identity-auth`...)
e uma pull request neste repositório, com título no formato `I06 — Ownership e matriz de acesso`
e referência completa, por exemplo `Racass/checkpoint-csharpracass-expensehub#6`, sem
palavras de fechamento automático. As issues originais continuam abertas.
