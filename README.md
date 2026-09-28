# CompliCore

**Multi-tenant compliance management platform for Saudi businesses.**
Track iqamas, work permits, and business licenses in one place, and get warned before anything expires.

[![CI](https://github.com/EBRA04/CompliCore/actions/workflows/ci.yml/badge.svg)](https://github.com/EBRA04/CompliCore/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)
![Docker](https://img.shields.io/badge/Docker-ready-2496ED)

---

## Overview

Companies in Saudi Arabia juggle dozens of expiring documents: employee iqamas, passports, work permits, commercial registrations, municipal licenses. A missed renewal means fines and disruption.

CompliCore gives each company a private workspace to record every document and its expiry date, see at a glance what is valid, what is expiring soon, and what has already lapsed, and get automatic reminders. Every change is recorded in an audit trail.

## Features

**Multi-tenant workspaces**
Every company registers into its own isolated workspace. One company can never see, edit, or delete another company's data — enforced by EF Core global query filters on every tenant-owned table, and proven by automated tests: a model-reflection test asserts every tenant entity has a filter configured, and separate integration tests confirm cross-tenant requests against employees and compliance items return 404.

**Employee and company compliance tracking**
Record employees with their iqama, passport, and work permit details, plus company-level documents such as the Commercial Registration, municipal license, and Civil Defense certificate.

**Automatic status**
Every item is classified as **Valid**, **Expiring** (within 60 days), or **Expired**, computed fresh from the expiry date on every request — never stored, never stale. Filter by status, type, or employee.

**Reminders and notifications**
A background job runs on startup and every 24 hours, creating in-app notifications at 60, 30, and 7 days before expiry, and once on expiry. A unique index makes it idempotent — running it twice never creates duplicates — and renewing a document restarts the cycle for the new date.

**Audit trail**
Every create, update, and delete on Employees, Compliance Items, and Users is logged with who did it, when, and the old/new value of every changed field. The log is append-only — the database rejects any attempt to modify or delete an existing entry.

**Dashboard**
Counts of valid, expiring, and expired items, plus the next ten documents due to expire.

**Roles**

- **Admin**: full read/write, manages team members, reads the audit log
- **Viewer**: read-only access for managers and auditors

## Core services

| Service          | What it does                                                                                                               |
| ---------------- | -------------------------------------------------------------------------------------------------------------------------- |
| Authentication   | Company registration, login, JWT tokens carrying the tenant and role                                                       |
| Users            | Admins add and remove team members and assign roles                                                                        |
| Employees        | Employee records with search and pagination                                                                                |
| Compliance items | Typed expiry records with computed status, filtering, and renewal (update the expiry date; history lives in the audit log) |
| Audit log        | Append-only, field-level history of every change                                                                           |
| Reminders        | Scheduled background job generating expiry notifications                                                                   |
| Notifications    | In-app alerts with read/unread tracking                                                                                    |
| Dashboard        | Summary counts and upcoming expirations                                                                                    |

## Tech stack

| Area            | Technology                                                                    |
| --------------- | ----------------------------------------------------------------------------- |
| Backend         | ASP.NET Core Web API (.NET 10), C#                                            |
| Data            | Entity Framework Core, PostgreSQL 16                                          |
| Auth            | JWT bearer tokens, BCrypt password hashing                                    |
| Background work | .NET `BackgroundService`                                                      |
| Testing         | xUnit, Testcontainers (integration tests against a real PostgreSQL container) |
| DevOps          | Docker, Docker Compose, GitHub Actions                                        |

## Multi-tenancy strategy

| Approach                              | Pros                                                  | Cons                                 |
| ------------------------------------- | ----------------------------------------------------- | ------------------------------------ |
| Database per tenant                   | Strongest isolation                                   | Heavy ops, migrations × N            |
| Schema per tenant                     | Good isolation, one DB                                | Migrations per schema                |
| **Shared schema + TenantId (chosen)** | Simplest, one migration, scales to many small tenants | Isolation depends on code discipline |

The tenant comes only from the JWT's `tenant_id` claim — never the request body, route, or headers. A global query filter silently appends `WHERE TenantId = <current tenant>` to every query, and a `SaveChangesAsync` override blocks any write that doesn't match. Other tenants' records return **404**, not 403, so their existence is never revealed.

## Getting started

**Prerequisite:** [Docker](https://www.docker.com/) with Compose.

```bash
git clone https://github.com/EBRA04/CompliCore.git
cd CompliCore
cp .env.example .env        # edit POSTGRES_PASSWORD and JWT_KEY
docker compose up --build
```

| What              | Where                           |
| ----------------- | ------------------------------- |
| API               | http://localhost:8080           |
| API docs (Scalar) | http://localhost:8080/scalar/v1 |
| Health check      | http://localhost:8080/health    |

## API overview

All endpoints are under `/api` and require a JWT except registration, login, and health.

| Area              | Endpoints                                                                                                       |
| ----------------- | --------------------------------------------------------------------------------------------------------------- |
| Auth              | `POST /auth/register`, `POST /auth/login`, `GET /auth/me`                                                       |
| Users (Admin)     | `GET /users`, `POST /users`, `DELETE /users/{id}`                                                               |
| Employees         | `GET /employees`, `GET /employees/{id}`, `POST`, `PUT`, `DELETE`                                                |
| Compliance items  | `GET /compliance-items` (filter by status, type, employeeId, companyOnly), `GET /{id}`, `POST`, `PUT`, `DELETE` |
| Notifications     | `GET /notifications`, `POST /notifications/{id}/read`                                                           |
| Audit log (Admin) | `GET /audit-logs` (filter by `entityName`)                                                                      |
| Dashboard         | `GET /dashboard`                                                                                                |

Explore everything interactively via the Scalar UI.

### Example

```http
POST /api/compliance-items
Authorization: Bearer <token>

{
  "employeeId": "a3f1...",
  "type": "Iqama",
  "referenceNumber": "2412345678",
  "issueDate": "2025-03-01",
  "expiryDate": "2026-11-15"
}
```

```json
{
  "type": "Iqama",
  "title": "Iqama",
  "expiryDate": "2026-11-15",
  "status": "Expiring",
  "daysRemaining": 55
}
```

## Running the tests

```bash
cd Backend
dotnet test
```

Docker must be running — integration tests spin up a real, throwaway PostgreSQL container per test class via Testcontainers. Covers tenant isolation across employees and compliance items, role-based authorization, expiry status boundary math, and a reflection test proving every tenant-owned entity has a query filter configured.

## Design decisions

| Decision                               | Reason                                                                  |
| -------------------------------------- | ----------------------------------------------------------------------- |
| Shared schema + TenantId               | Simplest to build; teaches query filters directly                       |
| Tenant only from JWT claim             | Client can never spoof it                                               |
| Status computed at query time          | Never stale; no background job needed for correctness                   |
| Reminder worker inside the API process | One image, one container; unique DB index keeps duplicate runs harmless |
| 404 for other-tenant ids               | Never reveals that a record exists                                      |

## License

MIT
