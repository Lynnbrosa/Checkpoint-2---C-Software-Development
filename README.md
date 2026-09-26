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

## Como executar

```shell
dotnet restore ./sources/ExpenseHub.slnx
dotnet build ./sources/ExpenseHub.slnx
dotnet test ./sources/ExpenseHub.slnx
dotnet run --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj
```

A API sobe em `http://localhost:5245` e `GET /health` responde `{"status":"ok"}`.
