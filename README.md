# ExpenseHub

API REST de reembolsos corporativos do Checkpoint 2 de C#. Funcionários registram
despesas, aprovadores decidem, o financeiro paga e auditores consultam o histórico.

Especificação e backlog: [Racass/checkpoint-csharpracass-expensehub](https://github.com/Racass/checkpoint-csharpracass-expensehub).
Cópia do enunciado em [docs/ENUNCIADO.md](docs/ENUNCIADO.md) e dos contratos em [docs/REQUISITOS.md](docs/REQUISITOS.md).

## Integrantes

| Nome | RM | GitHub |
|---|---|---|
| _preencher_ | _preencher_ | _preencher_ |
| _preencher_ | _preencher_ | _preencher_ |
| _preencher_ | _preencher_ | _preencher_ |

## Stack

- .NET 10 e ASP.NET Core
- Entity Framework Core 10 com SQLite
- MSTest para os testes unitários

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

## Como executar

```shell
dotnet restore ./sources/ExpenseHub.slnx
dotnet build ./sources/ExpenseHub.slnx
dotnet test ./sources/ExpenseHub.slnx
dotnet run --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj
```

A API sobe em `http://localhost:5245` e `GET /health` responde `{"status":"ok"}`.
