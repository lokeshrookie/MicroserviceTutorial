# Fill the Gaps — MicroserviceTutorial

Complete the tutorial project by fixing bugs, adding missing CRUD operations, wiring up inter-service communication, and hardening auth/validation.

---

## Open Questions

> [!IMPORTANT]
> **Q1 — Should OrderService store orders in a real database (EF Core InMemory) or keep the static list?**
> Currently both ProductService and OrderService use static in-memory lists (lost on restart). I recommend adding EF Core InMemory to both, same as AuthService, so creates/updates survive within a run. Approve this approach or say "keep static" and I'll only do CRUD on the list.
>
> **Q2 — Should OrderService call ProductService to validate that the ProductId exists when creating an order?**
> This is a classic microservices pattern (inter-service HTTP call via `HttpClient`). Adds real educational value. I recommend yes.
>
> **Default assumption:** Yes to both — I'll proceed with EF Core InMemory + inter-service communication unless you say otherwise.

---

## Proposed Changes

### Category 1 — Critical Bug Fixes

#### [MODIFY] [AuthService.cs](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/AuthService/Services/AuthService.cs)

**Bug:** `GenerateJwtToken` never sets `Issuer` on the `SecurityTokenDescriptor`. The Gateway validates `ValidIssuer = "AuthService"` — so **every token is rejected** at the Gateway right now.

**Fix:** Add `Issuer = _configuration["Jwt:Issuer"]` to the token descriptor, and add `"Issuer": "AuthService"` to `AuthService/appsettings.json`.

---

#### [MODIFY] [AuthService/appsettings.json](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/AuthService/appsettings.json)

Add the missing `Jwt:Issuer` key that `AuthService.cs` will now read.

---

#### [MODIFY] [ProductsController.cs](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/ProductService/Controllers/ProductsController.cs)

**Bug:** All three seed products have `Id = 1`. Fix to `Id = 1, 2, 3`.

---

#### [MODIFY] [OrdersController.cs](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/OrderService/Controllers/OrdersController.cs)

**Gap:** `OrdersController` has no `[Authorize]` attribute — only the Gateway enforces auth. Add `[Authorize]` for defense-in-depth (so the service is also protected if called directly, bypassing the Gateway).

Also expose the Ocelot routes for POST `/orders` and DELETE `/orders/{id}` (see Category 2 + ocelot.json changes).

---

### Category 2 — Full CRUD for ProductService

Currently only `GET /products` and `GET /products/{id}` exist.

#### [MODIFY] [ProductsController.cs](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/ProductService/Controllers/ProductsController.cs)

Add endpoints:

| Method | Path | Role | Description |
|---|---|---|---|
| `POST` | `/products` | Admin | Create a product |
| `PUT` | `/products/{id}` | Admin | Update a product |
| `DELETE` | `/products/{id}` | Admin | Delete a product |

Add input validation DTOs with data annotations (`[Required]`, `[Range]`, `[StringLength]`).

Move seed data from a static readonly field to the EF Core InMemory store (see below).

#### [NEW] [ProductService/Data/ProductDbContext.cs](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/ProductService/Data/ProductDbContext.cs)

Add EF Core InMemory `DbContext` with seed data (Laptop, Mouse, Keyboard with correct distinct IDs).

#### [MODIFY] [ProductService/Program.cs](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/ProductService/Program.cs)

Register `ProductDbContext` with InMemory provider. Seed the DB on startup.

#### [MODIFY] [ProductService/ProductService.csproj](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/ProductService/ProductService.csproj)

Add `Microsoft.EntityFrameworkCore.InMemory` NuGet package.

#### [MODIFY] [Gateway/ocelot.json](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/Gateway/ocelot.json)

Add routes for `POST /products`, `PUT /products/{id}`, `DELETE /products/{id}` — all requiring `Bearer` JWT + `Admin` role.

---

### Category 3 — Full CRUD for OrderService

Currently only `GET /orders` and `GET /orders/{id}` exist. Orders are an immutable static list.

#### [MODIFY] [OrdersController.cs](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/OrderService/Controllers/OrdersController.cs)

Add endpoints:

| Method | Path | Role | Description |
|---|---|---|---|
| `POST` | `/orders` | Admin, User | Place an order (validates ProductId via HTTP call to ProductService) |
| `DELETE` | `/orders/{id}` | Admin | Cancel an order |

#### [NEW] [OrderService/Data/OrderDbContext.cs](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/OrderService/Data/OrderDbContext.cs)

EF Core InMemory `DbContext` for orders with seed data.

#### [NEW] [OrderService/Models/Order.cs](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/OrderService/Models/Order.cs)

Convert from `record` to `class` so EF Core can track it properly.

#### [NEW] [OrderService/Services/IProductServiceClient.cs + ProductServiceClient.cs](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/OrderService/Services/)

**Inter-service communication:** `ProductServiceClient` uses `HttpClient` (registered as a typed client) to call `http://productservice/products/{id}` and check if a product exists before creating an order. Returns `true/false`. If product not found → `400 Bad Request`.

#### [MODIFY] [OrderService/Program.cs](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/OrderService/Program.cs)

Register `OrderDbContext`, `IProductServiceClient`, and `HttpClient` (base address: `http://productservice`).

#### [MODIFY] [OrderService/OrderService.csproj](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/OrderService/OrderService.csproj)

Add `Microsoft.EntityFrameworkCore.InMemory`.

#### [MODIFY] [Gateway/ocelot.json](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/Gateway/ocelot.json)

Add routes for `POST /orders`, `DELETE /orders/{id}`.

---

### Category 4 — Input Validation

#### [NEW] [AuthService/Models/Requests.cs](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/AuthService/Models/Requests.cs)

Move the inline `record` request types out of `AuthController.cs` into a dedicated file and add data annotations:
- `RegisterRequest`: `[Required]`, `[MinLength(3)]` on Username; `[MinLength(6)]` on Password
- `LoginRequest`: `[Required]` on both fields

#### [MODIFY] [AuthController.cs](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/AuthService/Controllers/AuthController.cs)

Add `[ApiController]` already handles `ModelState` automatically — just ensure the request records have the annotations. Add `[StringLength]` comments.

---

### Category 5 — Defense-in-Depth & Cleanup

#### [MODIFY] [Gateway/Program.cs](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/Gateway/Program.cs)

- Remove unused `AddSwaggerGen` / `UseSwagger` / `UseSwaggerUI` (the Gateway is a proxy, not an API with its own docs)
- Remove `UseHttpsRedirection` (inside Docker, everything is HTTP)
- Read JWT key/issuer from `appsettings.json` (not hardcoded string)

#### [MODIFY] [Gateway/appsettings.json](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/Gateway/appsettings.json)

Add `Jwt:Key` and `Jwt:Issuer` config keys so the Gateway reads from config (not hardcoded).

#### [MODIFY] [docker-compose.yml](file:///c:/Users/Lokesh/source/repos/MicroserviceTutorial/docker-compose.yml)

Inject JWT secret via environment variables to all services, so the secret lives in one place (the compose file) rather than repeated across four `appsettings.json` files.

---

## Summary of All Changes

| # | File | Change Type | Category |
|---|---|---|---|
| 1 | `AuthService/Services/AuthService.cs` | Modify | Bug Fix |
| 2 | `AuthService/appsettings.json` | Modify | Bug Fix |
| 3 | `ProductService/Controllers/ProductsController.cs` | Modify | Bug Fix + CRUD |
| 4 | `OrderService/Controllers/OrdersController.cs` | Modify | Bug Fix + CRUD |
| 5 | `ProductService/Data/ProductDbContext.cs` | **New** | CRUD |
| 6 | `ProductService/Program.cs` | Modify | CRUD |
| 7 | `ProductService/ProductService.csproj` | Modify | CRUD |
| 8 | `OrderService/Data/OrderDbContext.cs` | **New** | CRUD |
| 9 | `OrderService/Models/Order.cs` | Modify | CRUD |
| 10 | `OrderService/Services/IProductServiceClient.cs` | **New** | Inter-service |
| 11 | `OrderService/Services/ProductServiceClient.cs` | **New** | Inter-service |
| 12 | `OrderService/Program.cs` | Modify | CRUD + Inter-service |
| 13 | `OrderService/OrderService.csproj` | Modify | CRUD |
| 14 | `Gateway/ocelot.json` | Modify | CRUD routes |
| 15 | `Gateway/Program.cs` | Modify | Cleanup |
| 16 | `Gateway/appsettings.json` | Modify | Config |
| 17 | `docker-compose.yml` | Modify | Config |

---

## Verification Plan

### Build Check
```powershell
dotnet build MicroservicesApp.sln
```

### Docker Compose Test
```powershell
docker-compose up --build
```

Then manually test these flows:
1. `POST /auth/login` → get token → **was broken before (JWT issuer bug)**
2. `GET /products` with token → list products
3. `POST /products` with Admin token → create product
4. `PUT /products/1` with Admin token → update
5. `DELETE /products/1` with Admin token → delete
6. `POST /orders` with valid `productId` → creates order
7. `POST /orders` with invalid `productId` → 400 Bad Request (inter-service validation)
8. `GET /orders` → list orders
9. `DELETE /orders/1` with Admin token → cancel order
