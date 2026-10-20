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
- [x] Copilot instructions describing architecture, conventions, secret handling, testing, migrations, and AI guardrails.
- [x] Unit tests: 24 xUnit controller tests across authentication and order controllers, using Moq and FluentAssertions. Counted from source; not run as part of this review.

## Identified Gaps / Follow-up Work

- [ ] Replace the JWT signing key currently present in tracked `appsettings.json` with a secret-based configuration; rotate the exposed key if it has been used.
- [ ] Replace plain SHA-256 password hashing with a password-specific, salted password hashing implementation.
- [ ] Review authorization for administrative order operations; role-based authorization was not evident in the inspected controller.
- [ ] Register `CorrelationIdMiddleware` in the HTTP pipeline. The middleware and outbound delegating handler exist, but the middleware is not wired in `Program.cs`.
- [ ] Verify README feature claims (including payment, email, and recommendations) against implementation; they were not confirmed as wired into the inspected API.
