# Architecture — Low Level Design (LLD)
## University Management System (UMS)

---

## 1. Clean Architecture — Layer Contract

Every service follows an identical 4-layer structure. Dependency direction is strictly inward:

```
API  →  Application  →  Domain
         ↓
    Infrastructure  →  Domain
```

| Layer | Allowed Dependencies | Forbidden Dependencies |
|-------|---------------------|----------------------|
| Domain | None (pure C#) | Application, Infrastructure, API |
| Application | Domain only | Infrastructure, API |
| Infrastructure | Domain, Application interfaces | API |
| API | Application, Infrastructure (DI only) | Domain direct construction |

The Aegis `LayerMatrixRule` and `DomainIsolationRule` enforce this at CI time.

---

## 2. Shared Kernel (`UMS.SharedKernel`)

Provides primitives shared across all services. Must remain thin — domain logic does NOT belong here.

```
UMS.SharedKernel/
├── BaseEntity.cs          ← Id (Guid), TenantId, CreatedAt, UpdatedAt, concurrency token
├── IAggregateRoot.cs      ← interface: IReadOnlyList<IDomainEvent> DomainEvents, ClearDomainEvents()
├── DomainEvent.cs         ← abstract record: EventId, OccurredAt, TenantId
├── IDomainEvent.cs        ← marker interface
├── IDomainException.cs    ← marker interface
├── ITenantedEvent.cs      ← interface: Guid TenantId
├── OutboxMessage.cs       ← Id, EventType, Payload (JSON), OccurredAt, ProcessedAt?
├── PagedResult.cs         ← Items, TotalCount, Page, PageSize
├── ITenantContext.cs      ← Guid TenantId { get; }
└── Extensions.cs          ← general extensions
```

**Dependency rule:** SharedKernel has zero project references. Services reference it via NuGet or project reference.

---

## 3. Service Defaults (`AspireApp1.ServiceDefaults`)

Provides uniform cross-cutting configuration for all services:

```
ServiceDefaults/
├── Extensions.cs
│   ├── AddServiceDefaults()     ← registers OTEL, Serilog, health checks, resilience
│   ├── ConfigureOpenTelemetry() ← traces (Jaeger OTLP) + metrics (Prometheus)
│   ├── AddDefaultHealthChecks() ← liveness + readiness endpoints
│   └── MapDefaultEndpoints()    ← /health, /alive, /ready
└── SerilogExtensions.cs         ← Serilog enrichment, Seq sink
```

All 9 service `Program.cs` files call `builder.AddServiceDefaults()` as the first line.

---

## 4. Identity Service

### 4.1 Layer Breakdown

```
Identity.Domain/
├── Aggregates/
│   ├── ApplicationUser.cs       ← User aggregate: password hash, lock, verification
│   ├── Tenant.cs                ← Tenant aggregate: features, user limit, status
│   └── VerificationToken.cs     ← Token aggregate: purpose, expiry, used flag
├── Events/
│   ├── UserRegisteredEvent.cs
│   ├── UserEmailVerifiedEvent.cs
│   ├── UserDeactivatedEvent.cs
│   ├── UserLoginSucceededEvent.cs
│   ├── UserLoginFailedEvent.cs
│   ├── UserLockedOutEvent.cs
│   ├── EmailVerificationRequestedEvent.cs
│   ├── PasswordResetRequestedEvent.cs
│   ├── PasswordResetCompletedEvent.cs
│   ├── TenantProvisionedEvent.cs
│   ├── TenantSuspendedEvent.cs
│   └── TenantUpgradedEvent.cs
├── Exceptions/
│   ├── UserAlreadyExistsException.cs
│   ├── TenantNotFoundException.cs
│   ├── TenantAlreadyExistsException.cs
│   ├── TenantUserLimitExceededException.cs
│   ├── SelfRegistrationDisabledException.cs
│   ├── ExpiredVerificationTokenException.cs
│   └── InvalidVerificationTokenException.cs
└── Interfaces/
    ├── IUserRepository.cs
    ├── ITenantRepository.cs
    ├── IVerificationTokenRepository.cs
    └── IAuditLogger.cs

Identity.Application/
├── Commands/
│   ├── RegisterCommand + Handler + Validator
│   ├── ValidateCredentialsCommand + Handler + Validator
│   ├── ForgotPasswordCommand + Handler + Validator
│   ├── ResetPasswordCommand + Handler + Validator
│   ├── VerifyEmailCommand + Handler + Validator
│   ├── ResendVerificationCommand + Handler + Validator
│   ├── ProvisionTenantCommand + Handler
│   └── DeactivateUserCommand + Handler
├── DTOs/
│   └── PasswordCheckResult.cs
└── Behaviours/
    └── ValidationBehavior.cs    ← FluentValidation MediatR pipeline behaviour

Identity.Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs  ← EF Core, global tenant filter, outbox
│   ├── Configurations/          ← Fluent API configs per entity
│   ├── Migrations/              ← 7 migrations (latest: AddVerificationTokens)
│   └── Repositories/
│       ├── UserRepository.cs
│       ├── TenantRepository.cs
│       ├── AuditLogRepository.cs
│       └── VerificationTokenRepository.cs
├── Interceptors/
│   └── DomainEventDispatcherInterceptor.cs  ← publishes domain events post-save
├── OpenIddict/
│   └── PasswordGrantHandler.cs  ← custom grant handler
├── Seeder/
│   └── IdentitySeeder.cs        ← seeds default tenant + admin
└── DependencyInjection.cs

Identity.API/
├── Endpoints/
│   ├── AuthEndpoints.cs         ← /connect/token, /connect/refresh
│   ├── TenantEndpoints.cs       ← /tenants (provision, list)
│   └── RegionHealthEndpoints.cs
├── Middleware/
│   └── GlobalExceptionMiddleware.cs
└── Program.cs
```

### 4.2 Kafka Events Published

| Event | When |
|-------|------|
| `UserRegisteredEvent` | User successfully registers |
| `UserEmailVerifiedEvent` | Email verification completed |
| `EmailVerificationRequestedEvent` | Registration or resend triggered |
| `PasswordResetRequestedEvent` | Forgot password flow initiated |
| `PasswordResetCompletedEvent` | Password successfully reset |
| `UserLoginSucceededEvent` | Successful authentication |
| `UserLoginFailedEvent` | Failed login attempt |
| `TenantProvisionedEvent` | New tenant created |

---

## 5. Academic Service

```
Academic.Domain/
├── AcademicCalendar.cs       ← aggregate: semester, year, active flag
├── Course.cs                 ← aggregate: code, credits, status (Draft/Published)
├── Department.cs             ← aggregate: name, HOD, status
├── Programme.cs              ← aggregate: name, department, duration
├── Curriculum.cs             ← aggregate: programme + list of CurriculumEntry
└── Events/
    ├── CoursePublishedEvent.cs
    ├── DepartmentCreatedEvent.cs
    └── ProgrammeActivatedEvent.cs

Academic.Application/
├── Commands/
│   ├── CreateCourseCommand / Handler / Validator
│   ├── UpdateCourseCommand / Handler
│   ├── PublishCourseCommand / Handler
│   ├── CreateDepartmentCommand / Handler / Validator
│   ├── UpdateDepartmentCommand / Handler / Validator
│   ├── CreateProgrammeCommand / Handler / Validator
│   ├── UpdateProgrammeCommand / Handler
│   ├── CreateAcademicCalendarCommand / Handler / Validator
│   ├── ActivateAcademicCalendarCommand / Handler
│   ├── AddCourseToCurriculumCommand / Handler / Validator
│   └── RemoveCurriculumEntryCommand / Handler
└── Queries/
    ├── GetCourseByIdQuery / Handler
    ├── GetCoursesByDepartmentQuery / Handler
    ├── GetDepartmentByIdQuery / Handler
    ├── GetAllDepartmentsQuery / Handler
    ├── GetProgrammeByIdQuery / Handler
    ├── GetProgrammesByDepartmentQuery / Handler
    ├── GetCurriculumByProgrammeQuery / Handler
    ├── GetAcademicCalendarByIdQuery
    ├── GetAllAcademicCalendarsQuery
    └── GetActiveCalendarQuery / Handler

Academic.Infrastructure/
├── AcademicDbContext.cs     ← includes global tenant filter + outbox
├── Repositories/            ← per-aggregate repositories
├── AcademicOutboxRelayService.cs
└── AcademicEventsConsumer.cs  ← consumes events from other services
```

---

## 6. Student Service

```
Student.Domain/
├── Student.cs               ← aggregate: personal info, status, enrollment
├── StudentStatus.cs         ← enum: Active, Suspended, Graduated, Withdrawn
└── Events/
    ├── StudentCreatedEvent.cs
    ├── StudentDetailsUpdatedEvent.cs
    └── StudentStatusChangedEvent.cs

Student.Application/
├── Commands/
│   ├── CreateStudentCommand
│   ├── AdmitStudentCommand
│   └── StudentLifecycleCommands (Suspend, Graduate, Withdraw, Reinstate)
└── Queries/
    ├── GetAllStudentsQuery (paged)
    └── GetStudentByIdQuery
```

---

## 7. Attendance Service

```
Attendance.Domain/
├── AttendanceRecord.cs      ← aggregate: student, course, date, status
├── AttendanceSummary.cs     ← aggregate: student, course, period, percentage
├── CondonationRequest.cs    ← aggregate: request, status, approver
└── AttendanceEnums.cs       ← AttendanceStatus, CondonationStatus

Attendance.Application/
├── Commands/
│   ├── MarkAttendanceCommand / Handler / Validator
│   └── CondonationCommands (Create, Approve, Reject)
└── Queries/
    ├── AttendanceRecordQueries (by student/course/date)
    ├── AttendanceSummaryQueries
    └── CondonationQueries

Attendance.Infrastructure/
├── AttendanceDbContext.cs   ← Unit of Work pattern
├── AttendanceUnitOfWork.cs
├── Repositories/
└── AttendanceIntegrationTests.cs
```

---

## 8. Examination Service

```
Examination.Domain/
├── ExamSchedule.cs          ← aggregate: course, date, room, status
├── MarksEntry.cs            ← aggregate: student, exam, marks, grade
├── HallTicket.cs            ← aggregate: student, exam schedule
└── ResultCard.cs            ← aggregate: student, semester, CGPA

Examination.Application/
├── Commands/
│   ├── CreateExamScheduleCommand
│   ├── EnterMarksCommand / Handler
│   └── MarksWorkflowCommands (Approve, Lock)
└── Queries/
    └── (exam schedules, marks, hall tickets, results)
```

---

## 9. Fee Service

```
Fee.Domain/
├── FeeStructure.cs          ← aggregate: programme, year, components
├── FeePayment.cs            ← aggregate: student, structure, amount, status
└── Scholarship.cs           ← aggregate: student, amount, criteria

Fee.Application/
├── Commands/
│   ├── CreateFeeStructureCommand / Handler
│   ├── FeePaymentCommands (Create, Confirm, Refund)
│   └── CreateScholarshipCommand / Handler
└── Queries/
    └── (structures, payments, scholarships by student)
```

---

## 10. Faculty Service

```
Faculty.Domain/
├── Faculty.cs               ← aggregate: personal info, designation, department
├── CourseAssignment.cs      ← aggregate: faculty + course + semester
└── Publication.cs           ← aggregate: title, authors, journal, year

Faculty.Application/
├── Commands/
│   ├── FacultyCommands (Create, Update, SetMaintenance)
│   ├── CourseAssignmentCommands (Assign, Revoke)
│   └── PublicationCommands (Add, Update, Remove)
└── Queries/
    └── FacultyQueries, CourseAssignmentQueries, PublicationQueries
```

---

## 11. Hostel Service

```
Hostel.Domain/
├── Hostel.cs                ← aggregate: name, capacity, warden
├── Room.cs                  ← aggregate: hostel, number, type, capacity
├── RoomAllotment.cs         ← aggregate: student, room, from/to dates
└── HostelComplaint.cs       ← aggregate: student, category, status

Hostel.Application/
├── Commands/
│   ├── CreateHostelCommand / Handler / Validator
│   ├── CreateRoomCommand / Handler / Validator
│   ├── AllocateRoomCommand / Handler / Validator
│   ├── VacateRoomCommand / Handler
│   ├── UpdateWardenCommand / Handler / Validator
│   ├── RaiseComplaintCommand / Handler / Validator
│   └── UpdateComplaintStatusCommand / Handler
└── Queries/
    └── GetHostels, GetRooms, GetAllotments, GetComplaints, GetStudentAllotment
```

---

## 12. Notification Service

```
Notification.Domain/
├── NotificationLog.cs       ← aggregate: channel, recipient, template, status
├── NotificationTemplate.cs  ← aggregate: event type, subject, body (HTML)
└── NotificationPreference.cs ← aggregate: student, channel opt-ins

Notification.Infrastructure/
├── Channels/
│   ├── EmailChannel.cs      ← SMTP dispatch via INotificationChannel
│   └── SmsChannel.cs        ← SMS stub
├── Consumers/               ← Kafka consumers per source service
│   ├── AcademicEventsConsumer.cs
│   ├── ExaminationEventsConsumer.cs
│   ├── FeeEventsConsumer.cs
│   ├── IdentityEventsConsumer.cs
│   └── StudentEventsConsumer.cs
└── NotificationDispatcher.cs ← routes event → template → channel
```

---

## 13. Governance Layer (Aegis)

```
Aegis.Core/
├── RuleEngine.cs            ← orchestrates all rules, produces RuleViolation list
├── RuleEngineBuilder.cs     ← fluent builder for rule registration
├── IRule.cs                 ← interface: string Id, RuleResult Evaluate(ArchitectureModel)
├── ArchitectureModel.cs     ← parsed snapshot of all assemblies/namespaces/references
├── ArchitectureModelBuilder.cs
├── Rules/
│   ├── LayerMatrixRule.cs          ← enforce Domain→App→Infra→API direction
│   ├── DomainIsolationRule.cs      ← domain must not reference infra/API
│   ├── CircularDependencyRule.cs   ← no cycles
│   ├── CrossServiceDirectReferenceRule.cs ← services must not P2P reference
│   ├── SharedKernelIsolationRule.cs ← SharedKernel stays thin
│   ├── TenantIsolationRule.cs      ← all entities must carry TenantId
│   ├── ApiLayerRule.cs             ← no domain logic in API layer
│   ├── ApplicationLayerRule.cs     ← no infra imports in Application
│   ├── ConsumerGroupScopingRule.cs ← each service gets unique Kafka consumer group
│   ├── EventSchemaCompatibilityRule.cs ← event contracts checked against schema dir
│   ├── LoggingContractRule.cs      ← services must use structured logging
│   ├── MediatorPatternRule.cs      ← commands/queries must go through MediatR
│   ├── RegionAffinityRule.cs       ← multi-region routing checks
│   ├── ResiliencePolicyRule.cs     ← HTTP clients must have retry/circuit breaker
│   └── StaticMutableStateRule.cs   ← no static mutable state in services
├── Snapshot/
│   ├── SnapshotStore.cs     ← persists architecture model to JSON
│   ├── SnapshotDiffer.cs    ← detects drift between snapshots
│   └── PersistedSnapshot.cs
└── Renderers/
    ├── IReportRenderer.cs
    ├── JsonReportRenderer.cs
    ├── TextReportRenderer.cs
    └── CompactReportRenderer.cs

Ums.Cli/
├── Commands/
│   ├── GovernCommands.cs    ← govern verify all/single, snapshot create/diff
│   ├── AiCommands.cs        ← AI-assisted analysis (ClaudeClient)
│   ├── ContextCommands.cs   ← context window inspection
│   └── GitCommands.cs       ← git integration
└── Adapters/
    ├── VerifyBoundariesAdapter.cs
    ├── VerifyDependenciesAdapter.cs
    ├── VerifyEventContractsAdapter.cs
    ├── VerifyRegionAdapter.cs
    ├── VerifyResilienceAdapter.cs
    ├── VerifyTenantAdapter.cs
    └── SnapshotAdapter.cs
```

---

## 14. Cross-Cutting Patterns

### Outbox Pattern (per service)

```
1. Command Handler
   ├── mutates aggregate (domain event raised)
   ├── saves aggregate to DB
   └── saves OutboxMessage (serialized event) to DB
       (same EF Core SaveChanges transaction)

2. OutboxRelayService (BackgroundService per service)
   ├── polls OutboxMessages WHERE ProcessedAt IS NULL
   ├── publishes to Kafka
   └── marks as processed
```

### Domain Event Dispatch

```
DomainEventDispatcherInterceptor (EF Core SaveChanges interceptor)
   ├── after SaveChanges succeeds
   ├── collects IDomainEvent from aggregates
   └── publishes via MediatR (in-process, for same-service handlers)
       (cross-service events go via OutboxRelayService → Kafka)
```

### Validation Pipeline

```
HTTP Request
    → API Endpoint
    → MediatR.Send(command)
    → ValidationBehavior (pipeline behaviour)
        → FluentValidation.ValidateAsync()
        → throws ValidationException if invalid
    → Command Handler
```

### Error Handling

```
GlobalExceptionMiddleware (registered in all API projects)
    ├── catches DomainException → 422 Unprocessable Entity
    ├── catches ValidationException → 400 Bad Request
    ├── catches NotFoundException → 404 Not Found
    └── catches Exception → 500 Internal Server Error
        (logs full exception via Serilog)
```

---

## 15. API Boundary Contracts

### Standard Response Shapes

```csharp
// Success (GET single)
{ "data": { ... } }

// Success (GET list)
{ "items": [...], "totalCount": N, "page": P, "pageSize": S }

// Error
{ "type": "...", "title": "...", "status": 400, "errors": { ... } }
```

### Health Endpoints (all services)

```
GET /health          → { "status": "Healthy", "services": { "db": "Healthy" } }
GET /health/region   → { "region": "...", "tenantId": "..." }
```

### Authentication Flow

```
POST /connect/token
  Body: grant_type=password&username=X&password=Y&tenant_id=Z
  → 200 { access_token, refresh_token, expires_in }

POST /connect/token
  Body: grant_type=refresh_token&refresh_token=X
  → 200 { access_token, refresh_token, expires_in }
```
