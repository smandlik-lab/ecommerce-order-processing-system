# 🛒 Ecommerce-order-processing-system
A backend for an E-commerce Order Processing System. The system should allow customers to place orders, track their status, and support basic order operations.


#  E-Commerce Backend API (.NET)

A production-ready E-commerce backend system built using ASP.NET Core, following clean layered architecture principles and real-world backend design patterns.

## 📌 Overview

This project simulates a real-world e-commerce backend system with advanced features beyond basic CRUD operations.

It includes authentication, product management, cart handling, order processing, payment integration, and email notifications.

The system is designed with scalability and maintainability in mind, using modern backend architecture patterns such as Generic Repository, Repository Pattern, and Unit of Work.

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

The application currently has no outbound HTTP integrations or tracing exporter/backend configured. Middleware and outbound correlation propagation are covered by unit tests; this setup provides request correlation and W3C context propagation, but does not itself export traces to a monitoring system.

e.g. 
info: EcommerceOrderProcessing.Services.Implementations.AuthService[0]
      => SpanId:4549f289bc1fa154, TraceId:bfc6b59e7bea3ef9c1055f30c1d8d8a2, ParentId:0000000000000000 => ConnectionId:0HNPEHIO8CKA7 => RequestPath:/api/Auth/login RequestId:0HNPEHIO8CKA7:00000001 => System.Collections.Generic.Dictionary`2[System.String,System.Object] => EcommerceOrderProcessing.Controllers.AuthController.Login (EcommerceOrderProcessing)
      User logged in successfully. Email: string@gmail.com, UserId: 1
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
- Built recommendation system based on user behavior

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