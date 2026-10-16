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