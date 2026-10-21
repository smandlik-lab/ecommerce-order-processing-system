---
name: ecommerce-api-development
description: "Implement or update features in this .NET 10 e-commerce API. Use for controller, service, repository, EF Core, AWS SQS, authentication, observability, CORS, documentation, and xUnit test work that must follow this repository's architecture."
argument-hint: "Describe the feature or bug to implement"
---

# E-commerce API Development

Use this workflow for repository-aware feature implementation. Follow `.github/copilot-instructions.md` for always-on project conventions; this skill adds a repeatable implementation and verification process.

## Workflow

1. Inspect the relevant controller, service interface and implementation, repository, DTOs/models, dependency registrations in `Program.cs`, configuration, and tests before editing.
2. Verify whether the requested behavior is actually exposed and wired into the application. Do not infer functionality from a model, DTO, service method, README claim, or configuration key alone.
3. If a material product or API behavior is ambiguous, ask before choosing behavior. Otherwise state any small assumptions and implement the narrowest complete change.
4. Preserve the current layered structure:
   - Controllers handle HTTP binding, responses, and authorization.
   - Services own business workflows.
   - Repositories and `AppDbContext` own persistence.
   - DTOs define API and queue contracts; do not expose EF entities from API routes.
5. Register new services and handlers using existing dependency-injection patterns in `Program.cs`. Preserve existing authentication, CORS, correlation, OpenTelemetry metrics, Prometheus, and SQS wiring unless the requested feature requires a deliberate change.
6. For EF model changes, create/update migrations using EF tooling and keep the snapshot consistent. Do not apply migrations to a configured database unless explicitly requested.
7. Add or update focused xUnit tests, following the existing Moq/FluentAssertions conventions. Run the narrowest relevant test selection; expand validation only when needed.
8. Update directly related README or API request documentation. Describe only implemented and wired behavior, and distinguish metrics from trace export.
9. Review the diff for scope, errors, and whitespace. Report changed files, validation actually run, and any remaining limitations.

## Repository-Specific Checks

- Target framework: `net10.0`; test project: `EcommerceOrderProcessing.Tests`.
- API entry point and dependency registration: `Program.cs`.
- Order messaging uses `IOrderQueueService`, `OrderQueueService`, and the hosted `OrderQueueConsumerService`; preserve their message contract.
- OpenTelemetry currently exports ASP.NET Core, HttpClient, and runtime **metrics** to Prometheus at `/metrics`. No trace-span exporter/backend is configured.
- `CorrelationIdMiddleware` and the named `CorrelatedClient` are the existing correlation/W3C context integration points.
- The CORS policy is allow-listed; use trusted origins and do not combine unrestricted origins with credentials.
- Product/category/cart entities exist, but do not claim related management routes exist unless a controller exposes them.
- Payment status, email, and recommendations must not be represented as integrated features without verifying active routes and implementations.
- Do not put secrets in tracked configuration. Use User Secrets or environment-specific secret providers.
