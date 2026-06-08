# What to Build Next — A Microservices Learning Roadmap

> *You found this project on GitHub. The code runs. It demonstrates the basic shape of microservices.
> Now what? Here is exactly what an architect-in-training would build on top of it — and why.*

---

## The Mental Model First

Before writing a single line of code, an architect thinks in layers:

```
Layer 7 — Business Value      (what the system does for users)
Layer 6 — Architecture        (how services relate and communicate)
Layer 5 — Reliability         (what happens when things fail)
Layer 4 — Observability       (can you see what's happening?)
Layer 3 — Security            (who can do what, and is data safe?)
Layer 2 — API Design          (how do clients interact?)
Layer 1 — Foundation          (does it build, test, and deploy cleanly?)
```

This project currently has thin coverage of Layer 2 and almost nothing above it.
Work bottom-up. Do not add Kafka before you have logging.

---

## Level 1 — Foundation (Week 1–2)

*"Before scaling anything, make sure the basics are solid."*

### 1.1 Replace In-Memory Databases with Real Persistence

**What to do:** Add PostgreSQL (one database per service — the golden rule).

```yaml
# docker-compose.yml additions
postgres-auth:
  image: postgres:16
  environment:
    POSTGRES_DB: authdb
    POSTGRES_USER: auth
    POSTGRES_PASSWORD: secret

postgres-product:
  image: postgres:16
  environment:
    POSTGRES_DB: productdb
```

**What this teaches:**
- Database-per-service isolation (services can't share a database)
- EF Core migrations in a containerized environment
- Connection resilience (what happens when the DB restarts?)
- Why in-memory databases are only for prototyping

---

### 1.2 Replace SHA-256 Password Hashing with BCrypt

**What to do:** Add `BCrypt.Net-Next` NuGet package. Replace `HashPassword()` in `AuthService.cs`.

```csharp
// Current (WRONG — no salt, vulnerable to rainbow tables)
return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

// Correct
return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
```

**What this teaches:**
- Why salted adaptive hashing (BCrypt/Argon2/scrypt) is mandatory
- Work factor tuning — balancing security vs. login latency
- The difference between hashing, encoding, and encryption

---

### 1.3 Add Refresh Tokens

**What to do:** When a user logs in, return both an `accessToken` (15 min expiry) and a `refreshToken` (7 days, stored in the DB). Add `POST /auth/refresh` endpoint.

**What this teaches:**
- Short-lived access tokens limit blast radius of token theft
- Refresh token rotation (invalidate old refresh token on use)
- Token revocation (logout invalidates the refresh token)
- Secure cookie vs. localStorage tradeoffs for storing tokens

---

### 1.4 Add Unit and Integration Tests

**What to do:** Create `AuthService.Tests`, `ProductService.Tests`, `OrderService.Tests` projects.

```
MicroserviceTutorial/
├── AuthService.Tests/
│   ├── AuthServiceTests.cs      ← unit tests (mock DbContext)
│   └── AuthControllerTests.cs   ← integration tests (WebApplicationFactory)
```

**What this teaches:**
- `WebApplicationFactory<T>` for in-process integration tests
- Mocking EF Core with InMemory provider
- Testing JWT generation and validation
- Why microservices are easier to test in isolation

---

### 1.5 Add Proper Structured Logging

**What to do:** Add `Serilog` with console (JSON format) and file sinks.

```csharp
builder.Host.UseSerilog((ctx, config) =>
    config.ReadFrom.Configuration(ctx.Configuration)
          .Enrich.FromLogContext()
          .Enrich.WithProperty("Service", "ProductService")
          .WriteTo.Console(new JsonFormatter()));
```

**What this teaches:**
- Structured logs vs. string concatenation (why `{ProductId}` beats `"id: " + id`)
- Log levels (Verbose → Debug → Info → Warning → Error → Fatal)
- Correlation IDs across service boundaries
- Why `Console.WriteLine` is not logging

---

## Level 2 — API Design (Week 3–4)

*"An API is a contract. Bad APIs are technical debt that lives forever."*

### 2.1 Add API Versioning

**What to do:** Add `Asp.Versioning.Mvc` package. Route as `/api/v1/products`, `/api/v2/products`.

**What this teaches:**
- How to evolve APIs without breaking existing clients
- URL versioning vs. header versioning vs. query param versioning
- Deprecation strategy — keep v1 alive while clients migrate

---

### 2.2 Standardize Error Responses (RFC 7807 Problem Details)

**What to do:** Replace ad-hoc `BadRequest("message")` with `Problem()`.

```csharp
// Before (inconsistent)
return BadRequest("Product not found");
return NotFound(new { Message = "..." });

// After (RFC 7807 — every error looks the same)
return Problem(
    title: "Product not found",
    detail: $"No product exists with ID {id}",
    statusCode: 404,
    instance: $"/api/products/{id}"
);
```

**What this teaches:**
- Why consistent error contracts matter for API consumers
- RFC 7807 — the industry standard for HTTP error bodies
- Global exception handling middleware (`IExceptionHandler`)

---

### 2.3 Add Pagination, Filtering, and Sorting

**What to do:** Extend `GET /products` to support query parameters.

```
GET /products?page=1&pageSize=10&sortBy=price&order=asc&minPrice=10&maxPrice=500
```

Response includes metadata:
```json
{
  "data": [...],
  "page": 1,
  "pageSize": 10,
  "totalCount": 47,
  "totalPages": 5
}
```

**What this teaches:**
- Cursor-based vs. offset-based pagination (and why cursor is better at scale)
- IQueryable composition — filtering without loading all data
- Consistent query parameter naming conventions

---

### 2.4 Add a BFF — Backend for Frontend

**What to do:** Create a new `BFFService` that aggregates data from multiple services for a specific UI need.

```
GET /bff/dashboard
→ Calls ProductService for products
→ Calls OrderService for recent orders
→ Merges and returns a single response tailored for the dashboard UI
```

**What this teaches:**
- Why mobile apps and web apps often need different response shapes
- The cost of N+1 requests from the client
- Aggregation vs. composition at the service layer
- GraphQL as an alternative BFF pattern

---

## Level 3 — Security (Week 5–6)

*"Security is not a feature you add at the end. It's a property of the design."*

### 3.1 Replace Custom JWT with OAuth 2.0 / OpenID Connect

**What to do:** Run **Keycloak** (open-source identity provider) as a Docker container. Remove `AuthService`'s custom JWT generation and delegate to Keycloak.

```yaml
keycloak:
  image: quay.io/keycloak/keycloak:24.0
  command: start-dev
  ports:
    - "8080:8080"
  environment:
    KEYCLOAK_ADMIN: admin
    KEYCLOAK_ADMIN_PASSWORD: admin
```

**What this teaches:**
- OAuth 2.0 grant types (Authorization Code, Client Credentials, Device Flow)
- OpenID Connect (OIDC) — OAuth for authentication (not just authorization)
- JWT claims, scopes, and audiences
- Why rolling your own auth is almost always the wrong choice
- Single Sign-On (SSO) across services

---

### 3.2 Add Rate Limiting at the Gateway

**What to do:** Configure Ocelot's rate limiting, or add `AspNetCoreRateLimit` middleware.

```json
// ocelot.json
"RateLimitOptions": {
  "ClientWhitelist": [],
  "EnableRateLimiting": true,
  "Period": "1m",
  "PeriodTimespan": 60,
  "Limit": 100
}
```

**What this teaches:**
- Protecting services from DoS and abusive clients
- Sliding window vs. token bucket vs. fixed window algorithms
- Per-user vs. per-IP vs. per-API-key rate limits

---

### 3.3 Add mTLS for Service-to-Service Communication

**What to do:** Configure `ProductServiceClient` in OrderService to present a client certificate when calling ProductService internally.

**What this teaches:**
- Why services should authenticate each other, not just clients
- mTLS (mutual TLS) — both sides present certificates
- The difference between transport security (TLS) and application security (JWT)
- Service mesh (Istio/Linkerd) as a way to enforce mTLS automatically

---

### 3.4 Add an Audit Log

**What to do:** Create a middleware or EF Core interceptor that writes to an append-only `AuditLog` table: who did what, when, on which resource.

```
[2026-06-08 14:32:01] admin (id:1) DELETED product:5 "Monitor" from 192.168.1.10
[2026-06-08 14:32:45] alice (id:7) PLACED  order:23 (product:2, qty:1)
```

**What this teaches:**
- Non-repudiation — the ability to prove who did what
- Append-only data (never UPDATE or DELETE audit rows)
- Why audit logs must be in a separate service/store from operational data
- Compliance requirements (GDPR, PCI-DSS, SOX)

---

## Level 4 — Reliability (Week 7–8)

*"Distributed systems fail in ways that monoliths cannot. Design for failure, not against it."*

### 4.1 Add Circuit Breaker and Retry Policies with Polly

**What to do:** Wrap the `ProductServiceClient` HTTP call with Polly policies.

```csharp
builder.Services.AddHttpClient<IProductServiceClient, ProductServiceClient>()
    .AddTransientHttpErrorPolicy(p =>
        p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
    .AddTransientHttpErrorPolicy(p =>
        p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```

**What this teaches:**
- Retry with exponential backoff + jitter (avoids thundering herd)
- Circuit breaker pattern — stop hammering a failing service
- Timeout policies — how long is "too long" to wait?
- Bulkhead isolation — one failing dependency shouldn't crash everything
- Polly v8 and `Microsoft.Extensions.Resilience`

---

### 4.2 Introduce a Message Bus (Event-Driven Architecture)

**What to do:** Add **RabbitMQ** and **MassTransit**. When an order is placed, publish an `OrderPlacedEvent`. Add a `NotificationService` that subscribes and sends an email confirmation.

```
OrderService  ──publishes──►  [RabbitMQ exchange]  ──routes──►  NotificationService
                                                   ──routes──►  InventoryService
```

**What this teaches:**
- Async vs. sync inter-service communication — when to use each
- Publisher/subscriber decoupling — OrderService doesn't know NotificationService exists
- Message durability (what happens if NotificationService is down?)
- Dead letter queues — handling poison messages
- At-least-once vs. exactly-once delivery semantics
- Idempotency — handling duplicate messages safely

---

### 4.3 Implement the Outbox Pattern

**What to do:** Instead of publishing directly to RabbitMQ in `CreateOrder()`, write the event to an `OutboxMessages` table in the same DB transaction. A background worker polls and publishes.

```
Transaction {
    INSERT Order
    INSERT OutboxMessage { event: "OrderPlaced", payload: {...} }
}
// Background worker publishes OutboxMessage → deletes on success
```

**What this teaches:**
- The dual-write problem — why `SaveChangesAsync()` + `Publish()` can leave you in an inconsistent state
- Eventual consistency — accepting that the notification might arrive seconds later
- The Outbox pattern is the standard solution to this class of problem

---

### 4.4 Implement the Saga Pattern

**What to do:** The order placement flow involves multiple services (validate product, reserve inventory, process payment, confirm order). Model this as a Saga using **MassTransit's** saga state machine.

```
PlaceOrderSaga:
  STEP 1: Reserve inventory (InventoryService)
  STEP 2: Process payment (PaymentService)
  STEP 3: Confirm order (OrderService)
  
  If STEP 2 fails → compensate: Release reserved inventory
  If STEP 3 fails → compensate: Refund payment + Release inventory
```

**What this teaches:**
- Why distributed transactions (2PC) are impractical in microservices
- Choreography vs. orchestration-based sagas
- Compensating transactions — how to "undo" in a distributed system
- This is architect-level thinking

---

## Level 5 — Observability (Week 9–10)

*"You cannot manage what you cannot measure. You cannot debug what you cannot see."*

### 5.1 Distributed Tracing with OpenTelemetry

**What to do:** Add `OpenTelemetry` packages. Run **Jaeger** as a Docker container. Every request gets a `TraceId` that follows it across all services.

```
Client → Gateway → OrderService → ProductService
         [traceId: abc123]
         
In Jaeger UI you can see:
  Gateway:       12ms
  OrderService:  45ms
    └── ProductServiceClient HTTP call: 38ms
        ProductService: 36ms
```

**What this teaches:**
- Trace → Span → Baggage hierarchy
- How to find which service is the bottleneck
- W3C TraceContext standard for header propagation
- The difference between tracing, logging, and metrics (the three pillars)

---

### 5.2 Metrics with Prometheus + Grafana

**What to do:** Add `prometheus-net.AspNetCore`. Run Prometheus and Grafana. Build a dashboard showing:
- Request rate per service
- Error rate (HTTP 4xx/5xx)
- P50/P95/P99 latency
- Active DB connections
- Message queue depth

**What this teaches:**
- RED method: Rate, Errors, Duration
- USE method: Utilization, Saturation, Errors (for infrastructure)
- Alerting rules — "page me if error rate > 1% for 5 minutes"
- The difference between push-based and pull-based metrics

---

### 5.3 Centralized Log Aggregation (ELK Stack)

**What to do:** Add **Elasticsearch + Logstash + Kibana** (or **Grafana Loki** as a lighter alternative). All services ship structured JSON logs to a central store.

**What this teaches:**
- Why `docker logs` doesn't scale across many containers
- Log correlation using `TraceId` + `RequestId`
- Searching across services: "Show me all logs for order #42"

---

## Level 6 — Infrastructure & DevOps (Week 11–12)

*"Code that can't be deployed reliably has no value."*

### 6.1 CI/CD Pipeline with GitHub Actions

**What to do:** Create `.github/workflows/ci.yml`.

```yaml
on: [push, pull_request]
jobs:
  build-and-test:
    steps:
      - dotnet restore
      - dotnet build
      - dotnet test
      - docker build (per service)
      - docker push (to registry)
  deploy:
    needs: build-and-test
    steps:
      - kubectl apply (to staging)
      - run smoke tests
      - kubectl apply (to production) # on main branch only
```

**What this teaches:**
- Fail fast — catch breaks before they reach production
- Build once, deploy many — same image to staging and prod
- Branch strategies (trunk-based development vs. GitFlow)
- Environment promotion gates

---

### 6.2 Deploy to Kubernetes

**What to do:** Replace `docker-compose.yml` with Kubernetes manifests (`Deployment`, `Service`, `Ingress`, `ConfigMap`, `Secret`).

```
k8s/
├── gateway/
│   ├── deployment.yaml
│   └── service.yaml
├── authservice/
│   ├── deployment.yaml
│   └── service.yaml
└── ingress.yaml   ← replaces port mappings
```

**What this teaches:**
- Pod → Deployment → ReplicaSet → Service → Ingress hierarchy
- Horizontal Pod Autoscaling (scale ProductService to 10 replicas under load)
- Rolling updates with zero downtime
- Liveness vs. readiness probes (your health endpoints become critical here)
- ConfigMap + Secret for configuration (replaces docker-compose environment vars)
- Why Docker Compose is for development, Kubernetes is for production

---

### 6.3 Add Redis Caching

**What to do:** Add Redis as a Docker service. Cache `GET /products` for 60 seconds in ProductService.

```csharp
var cached = await _cache.GetStringAsync("products:all");
if (cached != null) return JsonSerializer.Deserialize<List<Product>>(cached);

var products = await _db.Products.ToListAsync();
await _cache.SetStringAsync("products:all", JsonSerializer.Serialize(products),
    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) });
return products;
```

**What this teaches:**
- Cache-aside pattern
- Cache invalidation — the second hardest problem in computer science
- Distributed cache vs. in-memory cache in a multi-replica environment
- TTL (time-to-live) tuning
- Redis as a session store, rate limiter, and pub/sub broker

---

## Level 7 — New Services (Ongoing)

*"Build real features. The patterns only click when solving real problems."*

### Add InventoryService
Tracks stock per product. Subscribes to `OrderPlaced` events and decrements inventory. Publishes `OutOfStock` events.

**Teaches:** Event-driven state management, eventual consistency, reactive systems.

---

### Add PaymentService
Processes payments (mock Stripe). Part of the order Saga. Publishes `PaymentSucceeded` / `PaymentFailed`.

**Teaches:** Saga compensating transactions, idempotency keys, financial data immutability.

---

### Add NotificationService
Subscribes to domain events (`OrderPlaced`, `OutOfStock`, `PaymentFailed`). Sends email/SMS.

**Teaches:** Consumer decoupling, fan-out messaging, template rendering, retry on delivery failure.

---

### Add SearchService
Indexes products in **Elasticsearch**. Subscribes to `ProductCreated/Updated/Deleted` events to keep the index in sync.

**Teaches:** Search-optimized data models, CQRS (write to PostgreSQL, read from Elasticsearch), index design.

---

## The Architect's Summary

| Level | Focus | Key Pattern Learned |
|---|---|---|
| 1 | Foundation | Real DB, proper auth, testing, logging |
| 2 | API Design | Versioning, pagination, problem details, BFF |
| 3 | Security | OAuth 2.0/OIDC, rate limiting, mTLS, audit |
| 4 | Reliability | Circuit breaker, message bus, Outbox, Saga |
| 5 | Observability | Tracing, metrics, centralized logging |
| 6 | Infrastructure | CI/CD, Kubernetes, caching |
| 7 | Domain | New bounded contexts (Inventory, Payment, Notification) |

---

## The One Principle That Ties It All Together

> **Every change you make should answer one of these four questions:**
> 1. Does this make the system *harder to break*?
> 2. Does this make failures *easier to detect*?
> 3. Does this make the system *easier to change*?
> 4. Does this make the system *easier to understand*?

If a change doesn't answer yes to at least one of them, you're adding complexity without value.

That's the difference between someone who knows microservices patterns and someone who thinks like an architect.
