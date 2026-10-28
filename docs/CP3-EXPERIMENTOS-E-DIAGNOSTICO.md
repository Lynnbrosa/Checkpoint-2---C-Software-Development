# Dossiê Operacional e Diagnóstico — Checkpoint 3 (CP3)

Este documento reúne a especificação operacional da mensageria, os procedimentos e evidências dos cinco experimentos obrigatórios (`E01` a `E05`) e o diagnóstico técnico de confiabilidade exigido em `CP3-I03`, `CP3-I05` e `CP3-I06`.

---

## 1. Arquitetura de Mensageria, Fila e Imagens (`CP3-I03` e `CP3-I05`)

### Configuração da Fila RabbitMQ

| Parâmetro | Valor configurado | Onde é declarado |
|---|---|---|
| Nome da fila | `expensehub.reports` | `RabbitMq:QueueName` em `appsettings.json` e `docker-compose.yml` |
| Durabilidade da fila | `durable: true`, `exclusive: false`, `autoDelete: false` | `RabbitMqReportMessagePublisher` e `ReportConsumerWorker` |
| Persistência da mensagem | `BasicProperties.Persistent = true` (`delivery_mode = 2`) | `RabbitMqReportMessagePublisher` |
| Confirmação de publicação | Publisher Confirms habilitado (`publisherConfirmationsEnabled: true`) | `RabbitMqReportMessagePublisher` |
| Confirmação de consumo | Manual (`autoAck: false`, `BasicQos prefetchCount = 1`) | `ReportConsumerWorker` |
| Armazenamento RabbitMQ | Volume persistente `rabbitmq-data:/var/lib/rabbitmq` | `docker-compose.yml` |
| Armazenamento SQLite e Chaves | Volume compartilhado `expensehub-data:/data` (`/data/expensehub.db` e `/data/keys`) | `docker-compose.yml` |

### Contrato da Mensagem

A mensagem publicada na fila `expensehub.reports` contém exclusivamente o identificador persistido da solicitação:

```json
{
  "reportId": "0199a000-0000-7000-8000-000000000001"
}
```

O `ExpenseHub.Worker` nunca confia em escopo, período ou proprietário vindos da mensagem: ele carrega o registro `ReportJob` persistido pela aplicação web no banco SQLite compartilhado.

### Imagens Publicadas e Execução Reproduzível

- **Commit base (CP2):** `71393bb`
- **Imagens do CP3 (substituir `<commit-final>` pelo SHA curto ou completo do commit de entrega):**
  - Web API: `ghcr.io/lynnbrosa/expensehub-web:<commit-final>`
  - Worker: `ghcr.io/lynnbrosa/expensehub-worker:<commit-final>`

Como publicar as imagens após aprovação dos testes unitários:

```shell
dotnet test ./sources/ExpenseHub.slnx
docker build -f ./sources/ExpenseHub.Api/Dockerfile -t ghcr.io/lynnbrosa/expensehub-web:<commit-final> .
docker build -f ./sources/ExpenseHub.Worker/Dockerfile -t ghcr.io/lynnbrosa/expensehub-worker:<commit-final> .
docker push ghcr.io/lynnbrosa/expensehub-web:<commit-final>
docker push ghcr.io/lynnbrosa/expensehub-worker:<commit-final>
```

Como iniciar o ambiente a partir das imagens publicadas:

```shell
cp .env.example .env
# Preencha SEED_ADMIN_PASSWORD, RABBITMQ_PASS e as tags de imagem em .env
docker compose pull
docker compose up -d
docker compose ps
```

---

## 2. Os Cinco Experimentos Obrigatórios (`CP3-I06`)

### E01 — Fluxo normal com download Excel

**Objetivo:** Autenticar, solicitar relatório, guardar o `id`, acompanhar até `Completed`, baixar a planilha `.xlsx` e conferir os números contra dados conhecidos.

**Procedimento:**
1. Com `web`, `worker` e `rabbitmq` em execução (`docker compose ps`), autenticar com conta `Employee` e criar 3 reembolsos dentro do período `2026-09-01` a `2026-09-30`:
   - 2 reembolsos `Approved` (`R$ 100,00` e `R$ 50,00`)
   - 1 reembolso `Paid` (`R$ 25,00`)
   - 1 reembolso do mesmo usuário fora do período (`2026-08-31`, `R$ 500,00`, `Paid`)
   - 1 reembolso de outro funcionário no período (`2026-09-10`, `R$ 200,00`, `Approved`)
2. Enviar `POST /api/reports`:
   ```json
   {
     "fromDate": "2026-09-01",
     "toDate": "2026-09-30",
     "scope": "Mine"
   }
   ```
3. Conferir resposta `202 Accepted`, cabeçalho `Location: /api/reports/{id}` e estado inicial `Pending`.
4. Consultar `GET /api/reports/{id}` até `status = "Completed"`.
5. Baixar `GET /api/reports/{id}/excel` e abrir no Excel / LibreOffice.

**Resultado e registros correlacionados esperados:**
- `GET /api/reports/{id}` retorna `200 OK`:
  ```json
  {
    "id": "<reportId-E01>",
    "scope": "Mine",
    "fromDate": "2026-09-01",
    "toDate": "2026-09-30",
    "status": "Completed",
    "createdAt": "2026-10-28T18:00:00Z",
    "startedAt": "2026-10-28T18:00:01Z",
    "completedAt": "2026-10-28T18:00:01Z",
    "result": {
      "generatedAt": "2026-10-28T18:00:01Z",
      "totalCount": 3,
      "totalAmount": 175.00,
      "byStatus": [
        { "status": "Draft", "count": 0, "amount": 0.00 },
        { "status": "Submitted", "count": 0, "amount": 0.00 },
        { "status": "Approved", "count": 2, "amount": 150.00 },
        { "status": "Rejected", "count": 0, "amount": 0.00 },
        { "status": "Paid", "count": 1, "amount": 25.00 }
      ]
    },
    "error": null
  }
  ```
- Logs sanitizados correlacionados pelo mesmo `reportId`:
  - Web: `ReportPublished reportId=<reportId-E01> scope=Mine fromDate=2026-09-01 toDate=2026-09-30`
  - Worker: `ReportStarted reportId=<reportId-E01> scope=Mine fromDate=2026-09-01 toDate=2026-09-30`
  - Worker: `ReportCompleted reportId=<reportId-E01> totalCount=3 totalAmount=175.00`
  - Worker: `WorkerMessageAcked reportId=<reportId-E01> outcome=Completed deliveryTag=1`
- Planilha `report-<reportId-E01>.xlsx`: `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, células numéricas com `Approved = 2 / 150.00`, `Paid = 1 / 25.00`, `Total = 3 / 175.00`.

---

### E02 — Worker parado e retomada da mesma solicitação

**Objetivo:** Parar apenas o `worker`, solicitar um relatório, observar a mensagem `Ready` na fila e o relatório em `Pending`, retomar o `worker` e acompanhar o **mesmo `id`** até `Completed`.

**Procedimento:**
```shell
docker compose stop worker
```
1. Fazer `POST /api/reports` autenticado como `Employee` e guardar o `<reportId-E02>`.
2. Consultar `GET /api/reports/<reportId-E02>`:
   - Estado permanece `Pending` (`startedAt: null`, `completedAt: null`, `result: null`).
   - Tentar `GET /api/reports/<reportId-E02>/excel` retorna `409 Conflict`.
3. Verificar a fila `expensehub.reports` no RabbitMQ (`http://localhost:15672` ou `rabbitmqctl list_queues name messages_ready messages_unacknowledged`):
   - `Ready = 1`, `Unacked = 0`.
4. Iniciar novamente o `worker`:
   ```shell
   docker compose up -d worker
   docker compose logs --tail 50 worker
   ```
5. Consultar o **mesmo identificador** `GET /api/reports/<reportId-E02>` e observar a transição para `Completed`.

**Logs correlacionados esperados:**
- Web (com worker parado): `ReportPublished reportId=<reportId-E02> scope=Mine ...` (retornou `202 Accepted` porque o broker RabbitMQ estava ativo e confirmou a persistência da mensagem na fila durável).
- Worker (após `docker compose up -d worker`):
  - `WorkerListening queue=expensehub.reports`
  - `ReportStarted reportId=<reportId-E02> ...`
  - `ReportCompleted reportId=<reportId-E02> ...`
  - `WorkerMessageAcked reportId=<reportId-E02> outcome=Completed ...`

---

### E03 — Duplicata após reinício do worker (Idempotência persistente)

**Objetivo:** Após concluir `<reportId-E01>`, reiniciar o `worker` (limpando qualquer memória local) e republicar manualmente na fila uma mensagem com o mesmo `reportId`, comprovando que nenhum novo cálculo ou alteração de `completedAt` / `generatedAt` ocorre.

**Procedimento:**
1. Anotar `completedAt` e `result.generatedAt` do relatório `<reportId-E01>` já `Completed`.
2. Reiniciar o worker para eliminar qualquer estado em memória:
   ```shell
   docker compose restart worker
   ```
3. Republicar a mensagem na fila `expensehub.reports` pelo painel de gerenciamento do RabbitMQ (`http://localhost:15672` -> *Queues* -> `expensehub.reports` -> *Publish message*) com payload:
   ```json
   {"reportId": "<reportId-E01>"}
   ```
4. Inspecionar os logs do worker e consultar `GET /api/reports/<reportId-E01>`.

**Evidência e registros esperados:**
- Log do worker:
  - `ReportDuplicateIgnored reportId=<reportId-E01> status=Completed: solicitação já concluída, nenhum novo efeito gerado.`
  - `WorkerMessageAcked reportId=<reportId-E01> outcome=DuplicateIgnored deliveryTag=1`
- Consulta `GET /api/reports/<reportId-E01>` mantém exatamente o mesmo `completedAt`, `result.generatedAt`, quantidades e valores anteriores.

---

### E04 — Isolamento e Autorização

**Objetivo:** Comprovar a matriz de acesso na criação, consulta e exportação Excel.

**Procedimento e resultados verificados:**
1. **Sem token (`401 Unauthorized`):**
   - `POST /api/reports`, `GET /api/reports`, `GET /api/reports/{id}`, `GET /api/reports/{id}/excel` sem cabeçalho `Authorization` retornam `401`.
2. **Employee solicita `Global` (`403 Forbidden`):**
   - `POST /api/reports` com `{"fromDate":"2026-09-01","toDate":"2026-09-30","scope":"Global"}` autenticado como `Employee` retorna `403 Forbidden` e não publica mensagem na fila.
3. **Employee consulta ou baixa Excel de outro Employee (`404 Not Found`):**
   - `GET /api/reports/<reportId-Employee1>` e `GET /api/reports/<reportId-Employee1>/excel` autenticados como `Employee2` retornam `404 Not Found`.
4. **Auditor solicita `Global` (`202 Accepted`), mas não consulta nem baixa relatório criado por outro usuário (`404 Not Found`):**
   - `POST /api/reports` com `scope = "Global"` autenticado como `Auditor` retorna `202 Accepted` e soma reembolsos de todos os funcionários no período (`R$ 375,00` no exemplo verificável).
   - `GET /api/reports/<reportId-Employee1>` e `GET /api/reports/<reportId-Employee1>/excel` autenticados como `Auditor` retornam `404 Not Found`.
   - `GET /api/reports` autenticado como `Auditor` lista somente os relatórios solicitados pelo próprio `Auditor`.

---

### E05 — Persistência e versão após recriar Web e Worker

**Objetivo:** Recriar apenas os contêineres `web` e `worker` a partir das imagens publicadas, sem remover volumes, e comprovar que a versão das imagens e os relatórios concluídos anteriormente permanecem disponíveis.

**Procedimento:**
```shell
docker compose images
docker compose up -d --force-recreate web worker
docker compose ps
```
1. Consultar `GET /api/reports/<reportId-E01>` e `GET /api/reports/<reportId-E01>/excel`.
2. Verificar que o resultado persistido em `/data/expensehub.db` (volume `expensehub-data`) e as chaves de proteção de token em `/data/keys` permanecem intactos, retornando `200 OK` com os mesmos dados e planilha `.xlsx` sem recalcular.

---

## 3. Diagnóstico de Confiabilidade e Falhas (`CP3-I03` e `CP3-I06`)

### 3.1. Aceitação (`202 Accepted`) vs. Conclusão (`Completed`)
- **Aceitação (`202 Accepted`):** Ocorre no `POST /api/reports` imediatamente após (1) gravar o registro `ReportJob` com estado `Pending` no SQLite e (2) receber a confirmação (`ack`) do RabbitMQ via *Publisher Confirms*. A requisição HTTP encerra sem aguardar o cálculo dos reembolsos.
- **Conclusão (`Completed`):** Ocorre de forma assíncrona no `ExpenseHub.Worker`, que consome a mensagem `{"reportId": "..."}`, marca `Processing` (`startedAt`), filtra os reembolsos por `ExpenseDate` e `Scope`, calcula os cinco estados, grava `ResultJson` + `CompletedAt` no mesmo `SaveChangesAsync` e só então envia `BasicAckAsync` ao broker.

### 3.2. Diferença entre Worker Parado e Broker Indisponível
- **Worker parado (`E02`):** O RabbitMQ está no ar. Quando a aplicação web executa `POST /api/reports`, o registro `Pending` é salvo no banco e o RabbitMQ recebe, persiste em disco e confirma a mensagem (`202 Accepted`). A mensagem aguarda na fila (`Ready`) até que o worker volte a executar.
- **Broker RabbitMQ indisponível:** Quando o RabbitMQ está fora do ar ou não confirma a publicação dentro do timeout (`PublishTimeoutSeconds`), o `RabbitMqReportMessagePublisher.TryPublishAsync` retorna `false`. A API **não retorna `202` falso**: ela responde **`503 Service Unavailable`** informando o `id` da solicitação que ficou registrada como `Pending` e um erro sanitizado (`PublicationFailed`).

### 3.3. Limite sem Transactional Outbox (Janela entre Banco e Broker)
- Como o banco SQLite (`SaveChangesAsync`) e a publicação no RabbitMQ (`BasicPublishAsync`) são duas operações em recursos distintos sem *Transactional Outbox*, existe uma janela entre persistir o `ReportJob` em `Pending` e publicar a mensagem:
  1. **Falha no banco antes de registrar:** O `SaveChangesAsync` falha antes da publicação; a API retorna erro explícito sem `202` e sem inventar um identificador.
  2. **Queda da API ou falha do broker após salvar no banco:** O registro `ReportJob` já foi persistido com estado `Pending`, mas a mensagem não chegou à fila (ou sua confirmação foi perdida na rede). Nesse caso, a API retorna `503 Service Unavailable` com o `id` persistido.
  3. **Por que reiniciar o worker não recupera uma mensagem não publicada:** O worker consome mensagens entregues pela fila `expensehub.reports` do RabbitMQ; ele não faz *polling* da tabela `ReportJobs`. Se a mensagem nunca foi confirmada pelo broker, reiniciar o worker não criará a mensagem na fila — o cliente deve submeter um novo `POST /api/reports` (que criará outra solicitação com novo `id`).

### 3.4. Reentrega, Retomada de `Processing` e Idempotência vs. Novo `POST`
- **Novo `POST /api/reports`:** Sempre cria um novo registro `ReportJob` com um novo `Guid` e publica uma nova mensagem.
- **Reentrega da mesma mensagem (Duplicata):** Carrega o mesmo `reportId`.
  - Se o worker caiu **durante** o cálculo (estado ficou em `Pending` ou `Processing` porque o `BasicAckAsync` só é chamado após salvar o resultado final), o RabbitMQ percebe a perda da conexão/canal, move a mensagem de `Unacked` de volta para `Ready` e a reentrega. O `ReportProcessor` aceita solicitações em `Pending` ou `Processing` (`!job.IsTerminal`), preserva o `StartedAt` original (`StartedAt ??= nowUtc`), recalcula e conclui o relatório — garantindo que uma solicitação em `Processing` nunca seja descartada para sempre.
  - Se a solicitação já atingiu estado terminal (`Completed` ou `Failed`), o `ReportProcessor` verifica `job.IsTerminal` no banco de dados (proteção persistente que sobrevive ao reinício do worker), registra `ReportDuplicateIgnored`, não altera datas nem resultados salvos e confirma (`BasicAckAsync`) a mensagem para retirá-la da fila.

