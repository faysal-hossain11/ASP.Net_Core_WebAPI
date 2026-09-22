# 📝 Task Management API (.NET 8)

A production-ready RESTful API built with **.NET 8**, **Entity Framework Core**, and **PostgreSQL (Neon Cloud)**, following Clean Architecture principles and Repository-Service pattern.

---

## 🚀 Features

- **Authentication & Authorization**: Secure JWT-based auth with HMAC-SHA512 password hashing.
- **User-Specific Data Isolation**: Multi-tenant task management where users can only access their own tasks.
- **Global Exception Handling**: Centralized middleware returning standardized JSON error responses.
- **Interactive API Documentation**: Swagger/OpenAPI with built-in JWT Authorization header support.
- **Unit Testing**: Automated testing coverage using **xUnit** and **Moq**.

---

## 🛠️ Tech Stack

- **Framework**: .NET 8 Web API
- **Database**: PostgreSQL (Neon Cloud) via Entity Framework Core
- **Security**: JWT Bearer Authentication, HMAC-SHA512
- **Testing**: xUnit, Moq
- **Documentation**: Swagger / OpenAPI

---

## 🚦 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL Database Instance (or Neon Cloud)

### Environment Setup
Add your database connection string and JWT configurations in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=YOUR_HOST;Database=YOUR_DB;Username=YOUR_USER;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
  },
  "Jwt": {
    "Key": "YOUR_512_BIT_SECRET_KEY_HERE",
    "Issuer": "TaskApi",
    "Audience": "TaskApiUsers"
  }
}
