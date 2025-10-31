# API de Idempotência - Demonstração Educacional

Esta API demonstra **6 padrões diferentes de idempotência** implementados em ASP.NET Core 8.0 para fins educacionais.

## 📚 O que é Idempotência?

Uma operação é **idempotente** quando pode ser executada múltiplas vezes sem alterar o resultado além da primeira aplicação.

**Exemplo do Mundo Real:** Ligar um interruptor de luz que já está ligado não muda nada - a luz continua acesa.

### Por que Idempotência é Importante?

- ✅ **Retry seguro**: Pode retentar operações sem efeitos colaterais
- ✅ **Resiliência**: Protege contra falhas de rede e timeouts
- ✅ **Mensageria**: Previne processamento duplicado de mensagens
- ✅ **Distributed Systems**: Essencial em sistemas distribuídos

---

## 🚀 Como Executar

```bash
# Restaurar dependências
dotnet restore

# Executar a API
dotnet run

# Ou usar o comando watch para desenvolvimento
dotnet watch run
```

A API estará disponível em:
- **HTTP**: http://localhost:5000
- **HTTPS**: https://localhost:5001
- **Swagger**: https://localhost:5001/swagger

---

## 📋 Padrões Implementados

### 1️⃣ Chave de Idempotência (Idempotency Key)

**Conceito**: Cliente gera um identificador único e o envia com a requisição. Servidor armazena e verifica a chave antes de processar.

**Endpoints:**
- `POST /api/idempotencykey/order` - Criar pedido
- `POST /api/idempotencykey/payment` - Processar pagamento

**Como Testar:**

```bash
# Primeira requisição - será processada
curl -X POST https://localhost:5001/api/idempotencykey/order \
  -H "Content-Type: application/json" \
  -d '{
    "idempotencyKey": "minha-chave-unica-123",
    "data": "Pedido de exemplo"
  }'

# Segunda requisição com MESMA chave - retorna resultado em cache
curl -X POST https://localhost:5001/api/idempotencykey/order \
  -H "Content-Type: application/json" \
  -d '{
    "idempotencyKey": "minha-chave-unica-123",
    "data": "Pedido de exemplo"
  }'
```

**Resultado Esperado:**
- Primeira chamada: `isFromCache: false` (processado)
- Segunda chamada: `isFromCache: true` (retornado do cache)

**Quando Usar:**
- ✅ APIs de pagamento
- ✅ Criação de recursos críticos
- ✅ Operações que não devem ser duplicadas

---

### 2️⃣ Idempotência Natural

**Conceito**: Algumas operações HTTP são naturalmente idempotentes (PUT, DELETE).

**Endpoints:**
- `GET /api/naturalidempotency/profiles` - Listar perfis
- `GET /api/naturalidempotency/profiles/{userId}` - Obter perfil
- `PUT /api/naturalidempotency/profiles/{userId}` - Atualizar perfil (IDEMPOTENTE)
- `DELETE /api/naturalidempotency/profiles/{userId}` - Deletar perfil (IDEMPOTENTE)
- `POST /api/naturalidempotency/profiles` - Criar perfil (NÃO IDEMPOTENTE)

**Como Testar:**

```bash
# PUT é idempotente - sempre resulta no mesmo estado final
curl -X PUT https://localhost:5001/api/naturalidempotency/profiles/user-1 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "João Silva",
    "email": "joao@example.com",
    "bio": "Desenvolvedor"
  }'

# Repetir o PUT - mesmo resultado
curl -X PUT https://localhost:5001/api/naturalidempotency/profiles/user-1 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "João Silva",
    "email": "joao@example.com",
    "bio": "Desenvolvedor"
  }'

# DELETE é idempotente
curl -X DELETE https://localhost:5001/api/naturalidempotency/profiles/user-1

# DELETE novamente - ainda retorna sucesso
curl -X DELETE https://localhost:5001/api/naturalidempotency/profiles/user-1
```

**Quando Usar:**
- ✅ REST APIs seguindo padrões
- ✅ Operações baseadas em estado final
- ✅ Substituição completa de recursos

**Diferença entre métodos HTTP:**
- ✅ `GET` - Idempotente (só lê)
- ✅ `PUT` - Idempotente (substitui completamente)
- ✅ `DELETE` - Idempotente (deletar algo já deletado não muda nada)
- ❌ `POST` - NÃO Idempotente (cria novo recurso cada vez)

---

### 3️⃣ Idempotência Version-Based (Optimistic Locking)

**Conceito**: Cada recurso tem um número de versão. Operações incluem a versão esperada. Se a versão for diferente, há conflito.

**Endpoints:**
- `GET /api/versionbased/resources` - Listar recursos
- `GET /api/versionbased/resources/{id}` - Obter recurso
- `PUT /api/versionbased/resources/{id}` - Atualizar com versão
- `POST /api/versionbased/resources/{id}/simulate-conflict` - Demo de conflito

**Como Testar:**

```bash
# 1. Obter recurso e versão atual
curl https://localhost:5001/api/versionbased/resources/config-1

# Resposta: { "version": 1, ... }

# 2. Atualizar com versão correta - SUCESSO
curl -X PUT https://localhost:5001/api/versionbased/resources/config-1 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "AppTimeout",
    "value": "60",
    "expectedVersion": 1
  }'

# Resposta: { "version": 2, ... } - versão incrementada!

# 3. Tentar atualizar com versão antiga - CONFLITO
curl -X PUT https://localhost:5001/api/versionbased/resources/config-1 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "AppTimeout",
    "value": "90",
    "expectedVersion": 1
  }'

# Resposta: 409 Conflict - versão esperada (1) ≠ versão atual (2)
```

**Quando Usar:**
- ✅ Optimistic Locking
- ✅ Prevenir "lost updates"
- ✅ Sistemas com múltiplos clientes editando dados
- ✅ Bancos de dados distribuídos

**Vantagens:**
- Não requer locks
- Melhor performance
- Detecta conflitos

---

### 4️⃣ Idempotência Token-Based

**Conceito**: Servidor gera um token único que só pode ser usado uma vez. Similar a CSRF tokens.

**Endpoints:**
- `POST /api/tokenbased/token/generate` - Gerar token
- `POST /api/tokenbased/payment` - Processar pagamento com token
- `POST /api/tokenbased/order` - Criar pedido com token
- `GET /api/tokenbased/demo` - Demonstração

**Como Testar:**

```bash
# 1. Gerar token
TOKEN=$(curl -X POST https://localhost:5001/api/tokenbased/token/generate | jq -r '.token')

echo "Token gerado: $TOKEN"

# 2. Usar token - SUCESSO
curl -X POST https://localhost:5001/api/tokenbased/payment \
  -H "Content-Type: application/json" \
  -d "{
    \"token\": \"$TOKEN\",
    \"amount\": 250.00,
    \"description\": \"Pagamento de teste\"
  }"

# 3. Tentar usar o MESMO token novamente - ERRO
curl -X POST https://localhost:5001/api/tokenbased/payment \
  -H "Content-Type: application/json" \
  -d "{
    \"token\": \"$TOKEN\",
    \"amount\": 250.00,
    \"description\": \"Pagamento de teste\"
  }"

# Resposta: 400 Bad Request - "Token já foi utilizado"
```

**Quando Usar:**
- ✅ Formulários de pagamento
- ✅ Prevenção de dupla submissão
- ✅ Operações sensíveis
- ✅ Similar a CSRF protection

**Características:**
- Token expira em 15 minutos
- Só pode ser usado uma vez
- Gerado pelo servidor (não pelo cliente)

---

### 5️⃣ Desduplicação Timestamp-Based

**Conceito**: Armazena timestamp da última operação. Ignora operações com timestamp anterior.

**⚠️ CUIDADO**: Problemas com **Clock Skew** (relógios desincronizados)!

**Endpoints:**
- `GET /api/timestampbased/operations` - Listar operações
- `GET /api/timestampbased/operations/{resourceId}` - Obter operação
- `POST /api/timestampbased/sensor` - Atualizar sensor
- `POST /api/timestampbased/demo/clock-skew` - Demo de problemas

**Como Testar:**

```bash
# 1. Enviar operação com timestamp atual - ACEITO
curl -X POST https://localhost:5001/api/timestampbased/sensor \
  -H "Content-Type: application/json" \
  -d "{
    \"resourceId\": \"sensor-1\",
    \"value\": \"25.5\",
    \"timestamp\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"
  }"

# 2. Enviar operação com timestamp antigo - REJEITADO
curl -X POST https://localhost:5001/api/timestampbased/sensor \
  -H "Content-Type: application/json" \
  -d "{
    \"resourceId\": \"sensor-1\",
    \"value\": \"26.0\",
    \"timestamp\": \"2024-01-01T00:00:00Z\"
  }"

# Resposta: 409 Conflict - timestamp anterior ao último processado
```

**Quando Usar:**
- ✅ Dados de sensores IoT
- ✅ Sistemas de telemetria
- ⚠️ Apenas se relógios estão sincronizados (NTP)

**Problemas:**
- ❌ Clock skew entre clientes
- ❌ Timezones diferentes
- ❌ Relógios desajustados

**Recomendação:** Use outros métodos se possível!

---

### 6️⃣ Desduplicação Content-Based

**Conceito**: Calcula hash (SHA-256) do conteúdo completo. Operações com mesmo hash são duplicatas.

**Endpoints:**
- `GET /api/contentbased/operations` - Listar operações
- `POST /api/contentbased/event` - Processar evento
- `POST /api/contentbased/compute-hash` - Calcular hash
- `GET /api/contentbased/demo/hash-sensitivity` - Demo de sensibilidade

**Como Testar:**

```bash
# 1. Processar evento - SUCESSO
curl -X POST https://localhost:5001/api/contentbased/event \
  -H "Content-Type: application/json" \
  -d '{
    "eventType": "UserCreated",
    "userId": "user-123",
    "data": "João Silva"
  }'

# 2. Enviar EXATAMENTE o mesmo conteúdo - DUPLICATA DETECTADA
curl -X POST https://localhost:5001/api/contentbased/event \
  -H "Content-Type: application/json" \
  -d '{
    "eventType": "UserCreated",
    "userId": "user-123",
    "data": "João Silva"
  }'

# Resposta: 409 Conflict - evento duplicado

# 3. Mudar 1 caractere - PROCESSADO (hash diferente)
curl -X POST https://localhost:5001/api/contentbased/event \
  -H "Content-Type: application/json" \
  -d '{
    "eventType": "UserCreated",
    "userId": "user-124",
    "data": "João Silva"
  }'
```

**Quando Usar:**
- ✅ Message Queues (RabbitMQ, Kafka, SQS)
- ✅ Event Sourcing
- ✅ Webhooks
- ✅ Sistemas de eventos distribuídos

**Vantagens:**
- Não precisa de chave gerada pelo cliente
- Não depende de timestamps
- Detecta duplicatas exatas automaticamente
- Ideal para mensageria!

---

## 🔄 Mock de Mensageria (Consumer)

A API inclui um **mock de sistema de mensageria** com Producer e Consumer.

### Arquitetura

```
Producer (API) → Queue → Consumer (Background Service) → Deduplication
```

### Endpoints

- `POST /api/messagequeue/publish` - Publicar evento (Producer)
- `POST /api/messagequeue/publish-batch` - Publicar lote com duplicatas
- `GET /api/messagequeue/processing-history` - Histórico do Consumer
- `GET /api/messagequeue/consumer/status` - Status do Consumer
- `POST /api/messagequeue/demo/full-flow` - Demo completo

### Como Funciona

1. **Producer**: Você publica eventos via API
2. **Queue**: Eventos ficam em uma fila em memória (Channel)
3. **Consumer**: Background Service processa eventos automaticamente
4. **Deduplication**: Usa Content-Based (hash SHA-256) para detectar duplicatas

### Teste Completo

```bash
# 1. Verificar status do Consumer
curl https://localhost:5001/api/messagequeue/consumer/status

# 2. Publicar evento
curl -X POST https://localhost:5001/api/messagequeue/publish \
  -H "Content-Type: application/json" \
  -d '{
    "eventType": "OrderCreated",
    "payload": "{\"orderId\":\"order-123\",\"amount\":500}"
  }'

# 3. Publicar o MESMO evento (duplicata)
curl -X POST https://localhost:5001/api/messagequeue/publish \
  -H "Content-Type: application/json" \
  -d '{
    "eventType": "OrderCreated",
    "payload": "{\"orderId\":\"order-123\",\"amount\":500}"
  }'

# 4. Aguardar processamento (alguns segundos)

# 5. Ver histórico - Consumer detectou a duplicata!
curl https://localhost:5001/api/messagequeue/processing-history
```

**Resultado Esperado:**
```json
{
  "summary": {
    "totalProcessed": 2,
    "successful": 1,
    "duplicates": 1,
    "failed": 0
  },
  "history": [
    {
      "eventId": "...",
      "success": true,
      "wasDuplicate": false,
      "message": "Evento processado com sucesso"
    },
    {
      "eventId": "...",
      "success": false,
      "wasDuplicate": true,
      "message": "Evento duplicado! Já processado em ..."
    }
  ]
}
```

### Demo Automático

```bash
# Publicar 3 eventos (1 original + 2 duplicatas)
curl -X POST https://localhost:5001/api/messagequeue/demo/full-flow
```

Aguarde alguns segundos e verifique o histórico. O Consumer terá:
- ✅ Processado o primeiro evento
- 🚫 Detectado e ignorado as 2 duplicatas

---

## 📊 Comparação dos Padrões

| Padrão | Geração | Vantagens | Desvantagens | Caso de Uso |
|--------|---------|-----------|--------------|-------------|
| **Idempotency Key** | Cliente | Simples, confiável | Cliente precisa gerar chave | Pagamentos, Pedidos |
| **Natural** | N/A | Sem código extra | Só PUT/DELETE | REST APIs padrão |
| **Version-Based** | Servidor | Detecta conflitos | Precisa versionar recursos | Optimistic Locking |
| **Token-Based** | Servidor | Previne replay | Token expira | Formulários sensíveis |
| **Timestamp-Based** | Cliente | Ordem temporal | Clock skew! ⚠️ | IoT (com NTP) |
| **Content-Based** | Automático | Sem chave, sem timestamp | CPU para hash | Mensageria, Eventos |

---

## 🎓 Exercícios para Alunos

### Exercício 1: Idempotency Key
1. Gere um UUID: `uuidgen` (Linux/Mac) ou online
2. Faça 3 requisições de pagamento com o MESMO UUID
3. Verifique que apenas 1 foi processado

### Exercício 2: Optimistic Locking
1. GET um recurso e note a versão
2. Simule 2 clientes atualizando simultaneamente
3. Um terá conflito - como resolver?

### Exercício 3: Token-Based
1. Gere um token
2. Tente usar em 2 pagamentos
3. O que acontece? Por quê?

### Exercício 4: Clock Skew
1. Use o endpoint `/api/timestampbased/demo/clock-skew`
2. Analise os resultados
3. Por que timestamp no futuro é aceito?

### Exercício 5: Mensageria
1. Publique um evento 5 vezes (mesmo conteúdo)
2. Verifique o histórico
3. Quantos foram processados? Quantos duplicados?

### Exercício 6: Hash Sensitivity
1. Use `/api/contentbased/demo/hash-sensitivity`
2. Note como 1 espaço muda o hash completamente
3. Por que isso é importante?

---

## 🏗️ Arquitetura

```
webapi/
├── Controllers/          # Endpoints por padrão
│   ├── IdempotencyKeyController.cs
│   ├── NaturalIdempotencyController.cs
│   ├── VersionBasedController.cs
│   ├── TokenBasedController.cs
│   ├── TimestampBasedController.cs
│   ├── ContentBasedController.cs
│   └── MessageQueueController.cs
├── Models/               # DTOs e entidades
│   ├── IdempotencyKey.cs
│   ├── NaturalIdempotency.cs
│   ├── ResourceVersion.cs
│   ├── IdempotencyToken.cs
│   ├── TimestampDeduplication.cs
│   ├── ContentBasedDeduplication.cs
│   └── MessageQueueEvent.cs
├── Services/             # Lógica de negócio
│   ├── IdempotencyKeyService.cs
│   ├── NaturalIdempotencyService.cs
│   ├── VersionBasedIdempotencyService.cs
│   ├── TokenBasedIdempotencyService.cs
│   ├── TimestampDeduplicationService.cs
│   ├── ContentBasedDeduplicationService.cs
│   ├── MessageQueueService.cs
│   └── MessageConsumerBackgroundService.cs
└── Program.cs            # Configuração
```

---

## 🔧 Tecnologias

- **ASP.NET Core 8.0** - Framework web
- **Swagger/OpenAPI** - Documentação interativa
- **System.Threading.Channels** - Fila em memória
- **SHA-256** - Hash criptográfico
- **Background Services** - Consumer assíncrono

---

## 📝 Notas Importantes

### Armazenamento em Memória
⚠️ Esta é uma aplicação **educacional**. Em produção:
- Use Redis, Memcached ou banco de dados
- Implemente TTL (Time-To-Live) para chaves
- Considere persistência

### Clock Skew
⚠️ Timestamp-Based tem problemas conhecidos:
- Use NTP para sincronizar relógios
- Prefira outros métodos quando possível
- Adicione margem de tolerância (± segundos)

### Performance
- Hash SHA-256 é rápido mas usa CPU
- Armazenamento cresce com o tempo
- Implemente cleanup/garbage collection