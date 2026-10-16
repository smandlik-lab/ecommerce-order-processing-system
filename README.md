## 🛒 ecommerce-order-processing-system
A backend for an E-commerce Order Processing System. The system should allow customers to place orders, track their status, and support basic order operations.

# DB migration commands
Add-Migration InitialCreate
Update-Database

# 🛒 E-Commerce Backend API (.NET)

A production-ready E-commerce backend system built using ASP.NET Core, following clean layered architecture principles and real-world backend design patterns.

## Local secrets

Do not put credentials in `appsettings.json`. Set the JWT signing key for local development with .NET User Secrets:

```powershell
dotnet user-secrets set "Jwt:Key" "<your-strong-signing-key>"
```

The AWS SDK uses its standard credential chain; configure AWS credentials with a local AWS profile or environment variables.

---