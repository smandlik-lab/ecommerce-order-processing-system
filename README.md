# 🛒 Ecommerce-order-processing-system
A backend for an E-commerce Order Processing System. The system should allow customers to place orders, track their status, and support basic order operations.


# E-Commerce Backend API (.NET)

An e-commerce backend API built with ASP.NET Core using a layered architecture.

## 📌 Overview

The repository currently implements JWT authentication endpoints, authenticated order operations, EF Core persistence, and asynchronous order completion through AWS SQS. The feature list below distinguishes API endpoints from service methods and domain models so it does not imply unfinished integrations are available.

## Features

### 🔐 Authentication and User Management

**Available API endpoints:**

- Register a user (`POST /api/Auth/register`).
- Log in and receive a JWT (`POST /api/Auth/login`).
- Retrieve the authenticated user's profile (`GET /api/Auth/me`).

**Service methods without API endpoints:** profile updates, password changes, email changes, forgot-password handling, and logout have service/interface methods, but no corresponding controller routes are exposed.

### 📦 Orders

- Create an order from the authenticated user's existing cart. The service checks product availability, reduces stock, removes the cart items, persists the order, and enqueues it for asynchronous processing.
- Retrieve the authenticated user's order list or a specific order.
- Cancel an eligible order.
- List orders and filter by status.
- Update order status.
- Process pending orders through an AWS SQS consumer; the background worker changes pending orders to completed.

### 🧱 Product, Category, and Cart Models

Product, category, cart, and cart-item entities are defined and persisted through EF Core. The repository does not currently expose product/category CRUD endpoints or add/remove/update-cart-item endpoints. The existing order-creation flow reads cart contents.

### 📈 Observability

- OpenTelemetry metrics for ASP.NET Core requests, `HttpClient` requests, and .NET runtime measurements.
- Prometheus metrics scrape endpoint at `/metrics`, with Prometheus and Grafana support described below.
- Correlation ID and W3C trace-context details in logging scopes (see Distributed Tracing Status).

### Not currently implemented

- Email confirmation or notification delivery. A Resend configuration entry is not an email-sending implementation.

---


```powershell
dotnet user-secrets set "Jwt:Key" "<your-strong-signing-key>"
```

The AWS SDK uses its standard credential chain; configure AWS credentials with a local AWS profile or environment variables.

---

## Distributed Tracing Status

The request pipeline registers `CorrelationIdMiddleware`. It uses an incoming `X-Correlation-ID` or generates one, sets `HttpContext.TraceIdentifier`, returns the ID in the response header, and adds structured trace context to the logging scope:

- `TransId` and `TraceId` are the active W3C `Activity` trace ID.
- `SpanId` and `ParentId` are the active span ID and its parent span ID.
- `ConnectionId` is the ASP.NET Core connection identifier.

The named `CorrelatedClient` HttpClient is configured with `CorrelationIdDelegatingHandler` to propagate `X-Correlation-ID`. HttpClientFactory's built-in diagnostics propagate the W3C trace context on outbound requests. Use `IHttpClientFactory.CreateClient("CorrelatedClient")` for future outbound integrations to preserve this behavior.

The application currently has no outbound HTTP integrations or distributed trace exporter/backend configured. Middleware and outbound correlation propagation are covered by unit tests; OpenTelemetry metrics are exported separately as described below.

e.g. 
info: EcommerceOrderProcessing.Services.Implementations.AuthService[0]
      => SpanId:4549f289bc1fa154, TraceId:bfc6b59e7bea3ef9c1055f30c1d8d8a2, ParentId:0000000000000000 => ConnectionId:0HNPEHIO8CKA7 => RequestPath:/api/Auth/login RequestId:0HNPEHIO8CKA7:00000001 => System.Collections.Generic.Dictionary`2[System.String,System.Object] => EcommerceOrderProcessing.Controllers.AuthController.Login (EcommerceOrderProcessing)
      User logged in successfully. Email: string@gmail.com, UserId: 1
---

## Observability: OpenTelemetry Metrics, Prometheus, and Grafana

The API configures OpenTelemetry metrics in `Program.cs` and exposes a Prometheus scrape endpoint at `/metrics`. The configured instrumentations collect:

- ASP.NET Core incoming HTTP request metrics, including request counts and durations.
- Outgoing `HttpClient` request metrics.
- .NET runtime metrics, including memory, garbage collection, and thread-pool measurements.

The scrape endpoint is available at `http://localhost:5190/metrics` when using the HTTP launch profile, or at `https://localhost:7204/metrics` when using the HTTPS launch profile.

The repository includes `prometheus.yml`, configured to scrape every 15 seconds with the `ecommerce-order-api` job, and `docker-compose.yml` to start Prometheus and Grafana:

```powershell
docker compose up -d
```

- Prometheus UI: `http://localhost:9090`
- Grafana UI: `http://localhost:3000`

The Compose file starts Prometheus and Grafana, not the API; run the API separately using one of the launch profiles above. The Compose file sets Grafana's initial login to `admin` / `admin`. Change these credentials before exposing Grafana beyond a local development environment. The Compose setup does not provision a Grafana Prometheus data source or dashboards; configure a Prometheus data source in Grafana using `http://prometheus:9090`.

When the API runs directly on the host, `prometheus.yml` currently targets `host.docker.internal:7204`. The application's HTTPS profile uses TLS on port 7204; ensure the Prometheus target's scheme and port match the API profile you run. If scraping the HTTP profile instead, target `host.docker.internal:5190`.

This setup exports metrics, not distributed trace spans. The correlation-ID and W3C trace-context logging described above are separate from metric collection.

---

## CORS Configuration

`Program.cs` defines and applies the `_myAllowSpecificOrigins` CORS policy. The policy currently allows browser requests from:

- `http://localhost:3000`
- `https://your-frontend-app.com` (example placeholder; replace it with the deployed frontend's exact origin)

The policy allows any request header and HTTP method, which permits headers such as `Authorization` used with bearer tokens. It does not enable cross-origin credentials (`AllowCredentials` is not configured), and it does not use `AllowAnyOrigin`. Replace the example origin with the trusted production frontend origin before deployment.

The CORS middleware runs after routing and before authentication, authorization, and controller endpoint execution. CORS controls which browser origins may read cross-origin responses; it is not a replacement for authentication or authorization.

---

## 🧠 Architecture

The project follows a clean layered architecture:

- Controllers → Handle HTTP requests
- Services → Business logic layer
- Repositories → Data access layer
- Unit of Work → Manages transactions across repositories
- DTOs → Data transfer objects
- Entity Framework Core → ORM

### 🔥 Key Design Patterns

- Generic Repository Pattern
- Repository Pattern
- Unit of Work Pattern
- Dependency Injection
- Separation of Concerns

---

## 🛠 Tech Stack

- ASP.NET Core Web API
- Entity Framework Core
- SQL Server

---

## ⚙️ Key Highlights

- Implemented Generic Repository Pattern to reduce code duplication
- Designed scalable data access layer using Unit of Work

---

## ▶️ How to Run

1. Clone the repository  
2. Update `appsettings.json` (add your configuration and API keys)
3. Apply migrations:
```bash
dotnet ef database update
```
4. Run the project:
```bash
dotnet run
```

## DB migration commands
Add-Migration InitialCreate
Update-Database

---

## 📌 Future Improvements
- AI-powered recommendation system 🤖
- Frontend integration
- Email verification (OTP)