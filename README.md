# MicroserviceTutorial — Setup & Run Guide

A .NET 8 microservices tutorial with an API Gateway, authentication, and inter-service communication.
Run it in **two minutes** using Docker Compose, or run each service individually for local development.

---

## Table of Contents

1. [Architecture at a Glance](#1-architecture-at-a-glance)
2. [Prerequisites](#2-prerequisites)
3. [Option A — Docker Compose (Recommended)](#3-option-a--docker-compose-recommended)
4. [Option B — Run Locally Without Docker](#4-option-b--run-locally-without-docker)
5. [Testing the APIs](#5-testing-the-apis)
6. [Default Credentials & Seed Data](#6-default-credentials--seed-data)
7. [Troubleshooting](#7-troubleshooting)

---

## 1. Architecture at a Glance

```
Client
  │
  ▼
Gateway         :5000  ← single entry point for all requests
  ├─► AuthService     :5003  ← register, login, get JWT
  ├─► ProductService  :5001  ← CRUD products (JWT required)
  └─► OrderService    :5002  ← CRUD orders  (JWT required)
             │
             └──(internal)──► ProductService  ← validates ProductId on order creation
```

All data is stored **in-memory** and resets when services restart. There is no external database to set up.

---

## 2. Prerequisites

### Option A — Docker Compose (easiest)

| Tool | Version | Download |
|---|---|---|
| Docker Desktop | 4.x or later | https://www.docker.com/products/docker-desktop |
| Git | Any | https://git-scm.com |

> **Windows users:** Make sure Docker Desktop is running and set to **Linux containers** (right-click the Docker tray icon → "Switch to Linux containers" if needed).

### Option B — Local (without Docker)

| Tool | Version | Download |
|---|---|---|
| .NET SDK | **8.0** | https://dotnet.microsoft.com/download/dotnet/8.0 |
| Git | Any | https://git-scm.com |

Optional IDEs: Visual Studio 2022, VS Code + C# Dev Kit, or JetBrains Rider.

---

## 3. Option A — Docker Compose (Recommended)

This is the fastest way. Docker builds all four images and wires up networking automatically.

### Step 1 — Clone the repository

```bash
git clone https://github.com/lokeshrookie/MicroserviceTutorial.git
cd MicroserviceTutorial
```

### Step 2 — Build and start all services

```bash
docker-compose up --build
```

> **First run:** Docker pulls the .NET base images (~200 MB). This takes a few minutes once;
> subsequent runs are fast.

### Step 3 — Wait for healthy status

Watch the console. The gateway waits for all three downstream services to pass their health checks before accepting traffic. You'll see lines like:

```
orderservice   | 2026-06-08 ... OrderService starting...
productservice | 2026-06-08 ... ProductService starting...
authservice    | ...
gateway        | Ocelot gateway running...
```

The gateway is ready when you see **"Ocelot gateway running..."**.
This usually takes **30–60 seconds** on first boot.

### Step 4 — Verify

Open a browser or run:

```bash
curl http://localhost:5000/auth
```

Expected response: `404` or `"No Users Created."` — the Gateway is up.

### Stopping

```bash
# Stop and remove containers (data is lost — it's all in-memory)
docker-compose down

# Stop but keep containers
docker-compose stop
```

### Rebuilding after code changes

```bash
docker-compose up --build
```

---

## 4. Option B — Run Locally Without Docker

Run each service in a **separate terminal**. Services must start **in the right order** because the Gateway expects them to be reachable on startup.

> **Note:** When running locally, the Ocelot Gateway is configured to route to Docker hostnames (`productservice`, `orderservice`, `authservice`). You will need to override the base URLs for each service in the Gateway's `ocelot.json` or use the services directly (bypassing the Gateway) for local dev testing.
>
> The simplest local approach is to **call each service directly** on its own port (no Gateway), then use Docker Compose when you want end-to-end testing.

### Step 1 — Clone the repository

```bash
git clone https://github.com/lokeshrookie/MicroserviceTutorial.git
cd MicroserviceTutorial
```

### Step 2 — Start AuthService (Terminal 1)

```bash
cd AuthService
dotnet run
```

Runs at: **http://localhost:5191**
Swagger UI: http://localhost:5191/swagger

### Step 3 — Start ProductService (Terminal 2)

```bash
cd ProductService
dotnet run
```

Runs at: **http://localhost:5234**
Swagger UI: http://localhost:5234/swagger

### Step 4 — Start OrderService (Terminal 3)

```bash
cd OrderService
dotnet run
```

Runs at: **http://localhost:5088**
Swagger UI: http://localhost:5088/swagger

### Step 5 — (Optional) Start Gateway (Terminal 4)

> ⚠️ The Gateway's `ocelot.json` uses Docker DNS hostnames (`productservice`, `authservice`, etc.) which only resolve inside Docker. For local testing it is easier to call each service directly on its port.

If you still want to run the Gateway locally, update `Gateway/ocelot.json` temporarily — replace each `"Host"` value:

| Change from | Change to |
|---|---|
| `"authservice"` | `"localhost"` |
| `"productservice"` | `"localhost"` |
| `"orderservice"` | `"localhost"` |

Also update the `"Port"` values to match each service's local port (5191, 5234, 5088).

Then run:

```bash
cd Gateway
dotnet run
```

Runs at: **http://localhost:5015**

### Local Port Summary

| Service | Local URL | Swagger |
|---|---|---|
| Gateway | http://localhost:5015 | — |
| AuthService | http://localhost:5191 | http://localhost:5191/swagger |
| ProductService | http://localhost:5234 | http://localhost:5234/swagger |
| OrderService | http://localhost:5088 | http://localhost:5088/swagger |

---

## 5. Testing the APIs

All examples below use the **Gateway** at `http://localhost:5000` (Docker Compose).
For local runs without the Gateway, swap in the direct service URL from the table above and keep the `/api/` prefix.

### Step 1 — Login and get a JWT

A default `admin` user is seeded automatically.

```http
POST http://localhost:5000/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "admin"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

Copy the token value. You'll use it as `Bearer <token>` in the Authorization header for all subsequent requests.

---

### Step 2 — Get all products

```http
GET http://localhost:5000/products
Authorization: Bearer <your-token-here>
```

**Response:**
```json
[
  { "id": 1, "name": "Laptop",   "price": 999.90 },
  { "id": 2, "name": "Mouse",    "price": 24.90  },
  { "id": 3, "name": "Keyboard", "price": 49.90  }
]
```

---

### Step 3 — Create a product (Admin only)

```http
POST http://localhost:5000/products
Authorization: Bearer <your-token-here>
Content-Type: application/json

{
  "name": "Monitor",
  "price": 349.99
}
```

**Response:** `201 Created` with the new product including its generated ID.

---

### Step 4 — Update a product (Admin only)

```http
PUT http://localhost:5000/products/1
Authorization: Bearer <your-token-here>
Content-Type: application/json

{
  "name": "Gaming Laptop",
  "price": 1499.99
}
```

---

### Step 5 — Delete a product (Admin only)

```http
DELETE http://localhost:5000/products/1
Authorization: Bearer <your-token-here>
```

**Response:** `204 No Content`

---

### Step 6 — Place an order (validates ProductId via inter-service call)

```http
POST http://localhost:5000/orders
Authorization: Bearer <your-token-here>
Content-Type: application/json

{
  "productId": 2,
  "quantity": 3
}
```

**Response:** `201 Created`
```json
{
  "id": 3,
  "productId": 2,
  "quantity": 3,
  "orderDate": "2026-06-08T01:30:00Z",
  "status": "Placed"
}
```

Try with an **invalid productId** to see validation in action:
```http
POST http://localhost:5000/orders
Authorization: Bearer <your-token-here>
Content-Type: application/json

{
  "productId": 999,
  "quantity": 1
}
```
**Response:** `400 Bad Request` — *"Product with ID 999 does not exist."*

---

### Step 7 — Cancel an order (Admin only)

```http
DELETE http://localhost:5000/orders/1
Authorization: Bearer <your-token-here>
```

**Response:** `200 OK` — order Status changes to `"Cancelled"` (soft delete).

---

### Step 8 — Register a new user

```http
POST http://localhost:5000/auth/register
Content-Type: application/json

{
  "username": "alice",
  "password": "password123",
  "role": "User"
}
```

`"role"` is optional — defaults to `"User"`. Use `"Admin"` to create an admin account.

---

### Using Swagger UI (local dev only)

Each service exposes a Swagger UI in Development mode. Navigate to:

- **AuthService** → http://localhost:5191/swagger
- **ProductService** → http://localhost:5234/swagger
- **OrderService** → http://localhost:5088/swagger

Click **Authorize** (🔓) in the top-right, enter `Bearer <your-token>`, and test endpoints interactively.

### Using VS Code REST Client

If you use the [REST Client extension](https://marketplace.visualstudio.com/items?itemName=humao.rest-client), you can use the `.http` files included in each service folder as a starting point.

---

## 6. Default Credentials & Seed Data

### Users (AuthService)

| Username | Password | Role |
|---|---|---|
| `admin` | `admin` | Admin |

> The admin user is seeded on startup. All other users must be registered via `POST /auth/register`.

### Products (ProductService)

| ID | Name | Price |
|---|---|---|
| 1 | Laptop | $999.90 |
| 2 | Mouse | $24.90 |
| 3 | Keyboard | $49.90 |

### Orders (OrderService)

| ID | ProductId | Quantity | Status |
|---|---|---|---|
| 1 | 1 | 2 | Placed |
| 2 | 2 | 3 | Placed |

> All data resets to these defaults every time you restart the services.

---

## 7. Troubleshooting

### Gateway returns `503 Service Unavailable`

The Gateway starts before downstream services are fully healthy.
**Wait 30–60 seconds** for all health checks to pass, then retry.

```bash
# Check container health status
docker-compose ps
```

All three services should show `healthy` before the Gateway accepts requests.

---

### `401 Unauthorized` on every request

- Make sure you're sending the token as `Authorization: Bearer <token>` (with the word `Bearer ` and a space).
- Tokens expire after **1 hour**. Log in again to get a fresh token.
- Check that the request is going through the Gateway (`localhost:5000`), not directly to a service.

---

### `400 Bad Request` when placing an order

The `productId` you supplied does not exist in ProductService.
- Use `GET /products` first to see valid IDs.
- If you deleted products in the same session, those IDs are gone (in-memory).

---

### Docker build fails with "no space left on device"

Clean up unused Docker resources:

```bash
docker system prune -f
```

---

### Port already in use

If ports `5000–5003` are taken on your machine, edit `docker-compose.yml` and change the **left** side of each port mapping:

```yaml
ports:
  - "6000:80"   # was "5000:80" — now Gateway is on port 6000
```

---

### `productservice` / `orderservice` hostnames not resolving (local dev)

These hostnames only work inside the Docker network. When running services with `dotnet run`, update the Gateway's `ocelot.json` to use `localhost` with the correct port, as described in [Option B Step 5](#step-5--optional-start-gateway-terminal-4).

---

### Check service logs

```bash
# All services
docker-compose logs -f

# One service
docker-compose logs -f gateway
docker-compose logs -f authservice
docker-compose logs -f productservice
docker-compose logs -f orderservice
```

---

## Quick Reference

```bash
# Start (Docker)
docker-compose up --build

# Stop
docker-compose down

# Rebuild one service only
docker-compose up --build productservice

# View logs
docker-compose logs -f

# Check health
docker-compose ps
```
