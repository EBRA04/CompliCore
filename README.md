# CompliCore

**Multi-tenant compliance management platform for Saudi businesses.**
Track iqamas, work permits, and business licenses in one place, attach the paperwork, and get warned before anything expires.

[![CI](https://github.com/EBRA04/CompliCore/actions/workflows/ci.yml/badge.svg)](https://github.com/EBRA04/CompliCore/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)
![Docker](https://img.shields.io/badge/Docker-ready-2496ED)

---

## Overview

Companies in Saudi Arabia juggle dozens of expiring documents: employee iqamas, passports, work permits, commercial registrations, municipal licenses. A missed renewal means fines and disruption.

CompliCore gives each company a private workspace to record every document and its expiry date, upload scans, and see at a glance what is valid, what is expiring soon, and what has already lapsed. Reminders are generated automatically, and every change is recorded in an audit trail.

<!-- Add screenshots to docs/screenshots and uncomment -->
<!--
![Dashboard](docs/screenshots/dashboard.png)
![Compliance items](docs/screenshots/items.png)
-->

## Features

**Multi-tenant workspaces**
Every company signs up to its own isolated workspace. One company can never see, edit, or download another company's data, and automated tests verify it for every resource type.

**Employee and company compliance tracking**
Record employees with their iqama, passport, and work permit details, plus company-level documents such as the Commercial Registration, municipal license, and Civil Defense certificate.

**Automatic status**
Every item is instantly classified as **Valid**, **Expiring** (within 60 days), or **Expired**, with the days remaining. Filter and sort by urgency to see what needs attention first.

**Document storage**
Attach PDF, JPG, or PNG scans to any item (up to 5 MB). Files are stored per company and downloadable only by authorized users.

**Reminders and notifications**
A background job runs daily and creates notifications at 60, 30, and 7 days before expiry, and again when an item has expired. Renew a document and its reminders start over for the new date.

**Audit trail**
Every create, update, and delete is logged with who did it, when, and exactly what changed (for example, the old and new expiry date). The log is append-only.

**Dashboard**
Counts of valid, expiring, and expired items, plus the next ten documents due to expire.

**Roles**
- **Admin**: full access, manages users, reads the audit log
- **Viewer**: read-only access for managers and auditors

## Core services

| Service | What it does |
|---|---|
| Authentication | Company registration, login, JWT tokens carrying the company and role |
| Users | Admins add and remove team members and assign roles |
| Employees | Employee records with search and pagination |
| Compliance items | Typed expiry records (iqama, passport, work permit, licenses) with status calculation, filters, and renewal history |
| Documents | Secure upload, list, download, and delete of scanned paperwork |
| Audit log | Append-only history of every change, per company |
| Reminders | Scheduled job that generates expiry notifications |
| Notifications | In-app alerts with read/unread tracking |
| Dashboard | Summary counts and upcoming expirations |

## Tech stack

| Area | Technology |
|---|---|
| Backend | ASP.NET Core Web API (.NET 10), C# |
| Data | Entity Framework Core, PostgreSQL 16 |
| Auth | JWT bearer tokens, BCrypt password hashing |
| Background work | .NET `BackgroundService` |
| Testing | xUnit, Testcontainers (integration tests against a real PostgreSQL) |
| DevOps | Docker, Docker Compose, GitHub Actions |
| Frontend | React, Vite, Tailwind CSS |

## Getting started

**Prerequisite:** [Docker](https://www.docker.com/) with Compose.

```bash
git clone https://github.com/EBRA04/CompliCore.git
cd CompliCore
cp .env.example .env        # then edit the values
docker compose up --build
```

| What | Where |
|---|---|
| API | http://localhost:8080 |
| Swagger UI (development mode) | http://localhost:8080/swagger |
| Health check | http://localhost:8080/health |

**Demo data:** in development mode the app seeds two sample companies. Log in with the demo credentials listed below to explore.

| Company | Email | Password |
|---|---|---|
| _(fill in from your seeder)_ | _demo@example.com_ | _(password)_ |

**Frontend**

```bash
cd Frontend
npm install
npm run dev                 # http://localhost:5173
```

## API overview

All endpoints are under `/api` and require a JWT except registration, login, and health.

| Area | Endpoints |
|---|---|
| Auth | `POST /auth/register`, `POST /auth/login`, `GET /auth/me` |
| Users (Admin) | `GET /users`, `POST /users`, `DELETE /users/{id}` |
| Employees | `GET /employees`, `GET /employees/{id}`, `POST`, `PUT`, `DELETE` |
| Compliance items | `GET /compliance-items` (filter by status, type, employee), `GET /{id}`, `POST`, `PUT`, `DELETE` |
| Documents | `POST /compliance-items/{id}/documents`, `GET /documents/{id}/download`, `DELETE /documents/{id}` |
| Notifications | `GET /notifications`, `POST /notifications/{id}/read`, `POST /notifications/read-all` |
| Dashboard | `GET /dashboard` |
| Audit log (Admin) | `GET /audit-logs` |

Explore everything interactively in Swagger UI.

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
  "employeeName": "Mohammed Khan",
  "expiryDate": "2026-11-15",
  "status": "Expiring",
  "daysRemaining": 55
}
```

## Running the tests

```bash
dotnet test Backend/CompliCore.Tests
```

Integration tests run the real API against a real PostgreSQL database in a throwaway container (Docker required). They cover tenant isolation for every resource, role permissions, expiry status rules, document upload validation, audit logging, and reminder generation.

## Roadmap

- Email and SMS delivery for reminders
- PostgreSQL Row-Level Security as an extra isolation layer
- S3-compatible document storage
- Refresh tokens

## License

MIT
