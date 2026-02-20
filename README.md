# AspireApp1 — University SaaS Platform

> Built by **Harsh Maurya** | B.Tech CSE (6th Semester) | Shambhunath Institute of Engineering and Technology
>
> *Yes, I have 10 backlogs. No, that has not stopped me from building enterprise software. Skill issue? Perhaps. Dedication issue? Absolutely not.*

## What is this?

A multi-tenant, event-driven SaaS platform for higher education built on .NET Aspire.
Featuring clean architecture, CQRS, domain-driven design, Kafka event streaming,
outbox pattern, and enough microservices to make a senior architect cry happy tears.

## Architecture

- **Control Plane** — Tenant provisioning, feature flags, SLA config
- **Data Plane** — Identity, Student, Enrollment, Finance, Academic services
- **Event Plane** — Kafka backbone, outbox relay, schema registry
- **Observability Plane** — Distributed tracing, RED metrics, dashboards
- **Security Plane** — JWT, tenant isolation, field-level encryption

## Tech Stack

| Layer | Technology |
|---|---|
| Orchestration | .NET Aspire 9.3 |
| Auth | OpenIddict 5.8 |
| Gateway | YARP 2.3 |
| Messaging | Apache Kafka |
| Database | PostgreSQL + EF Core 9 |
| CQRS | MediatR 12.4 |
| Caching | Redis |
| Observability | OpenTelemetry |

## Services

- Identity.API — Auth, tenant management
- Student.API — Student lifecycle (state machine)
- Academic.API — Course catalog (coming soon)
- Enrollment.API — Seat reservation (coming soon)
- Finance.API — Billing and invoicing (coming soon)

## Developer

**Harsh Maurya**
Solo Developer, Architect, DevOps Engineer, QA, and Product Manager
(Titles are free when you work alone)

Shambhunath Institute of Engineering and Technology
B.Tech Computer Science Engineering — 3rd Year, 6th Semester

> *Target go-live date: ha ha ha ha ha.................................................*

## Status

![Sprints](https://img.shields.io/badge/Sprint-3%20of%208-blue)
![Build](https://img.shields.io/badge/Build-Passing-green)
![Backlogs](https://img.shields.io/badge/Academic%20Backlogs-10-red)
![Regrets](https://img.shields.io/badge/Regrets-0-brightgreen)
