# Repository Task Status

## Completed / Implemented

- [x] Authentication endpoints for registration, login, and retrieving the authenticated user's profile.
- [x] JWT bearer authentication configuration; order endpoints require authentication.
- [x] Order API operations for creating, viewing, cancelling, filtering, and updating order status.
- [x] EF Core persistence with SQL Server, entity models, code first and migrations.
- [x] Layered architecture using controllers, services, repositories, DTOs, and dependency injection.
- [x] Generic and specialized repository implementations and a Unit of Work.
- [x] AWS SQS order processing: enqueue pending orders and consume messages in a hosted background service.
- [x] Structured application logging using `ILogger` and configured logging levels/scopes.
- [x] Distributed trace context is included in request logging scopes: `TransId`/`TraceId` use the W3C trace ID, `SpanId` and `ParentId` come from the active `Activity`, `ConnectionId` comes from the ASP.NET Core connection, and `X-Correlation-ID` is returned to callers.
- [x] Correlation middleware is registered in the HTTP pipeline, and the `CorrelatedClient` HttpClient is configured with the correlation delegating handler. HttpClientFactory's standard diagnostics propagate W3C trace context for outbound requests made through this named client.
- [x] Copilot instructions describing architecture, conventions, secret handling, testing, migrations, and AI guardrails.
- [x] Unit tests include 24 xUnit controller tests across authentication and order controllers, using Moq and FluentAssertions.
- [x] All 26 focused correlation middleware/handler tests passed, covering trace/span/parent/connection IDs in logging scope and outbound correlation-ID propagation.

## Identified Gaps / Follow-up Work

- [ ] Replace the JWT signing key currently present in tracked `appsettings.json` with a secret-based configuration; rotate the exposed key if it has been used.
- [ ] Replace plain SHA-256 password hashing with a password-specific, salted password hashing implementation.
- [ ] Review authorization for administrative order operations; role-based authorization was not evident in the inspected controller.
- [ ] Connect any future outbound HTTP integrations through `IHttpClientFactory.CreateClient("CorrelatedClient")` to use the configured correlation handler and standard W3C trace propagation. No outbound HTTP integrations or tracing exporter/backend are currently configured.
- [ ] Verify README feature claims (including payment, email, and recommendations) against implementation; they were not confirmed as wired into the inspected API.
