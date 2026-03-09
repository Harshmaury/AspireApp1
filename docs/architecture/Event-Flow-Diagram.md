# Event Flow Diagram
## University Management System (UMS)

---

## 1. Event Architecture Overview

All inter-service events flow through Kafka using the **Transactional Outbox Pattern**. There are no synchronous service-to-service HTTP calls.

```
Producer Service
│
├── Command Handler mutates aggregate
├── Domain event raised on aggregate
├── EF Core SaveChanges (atomic):
│   ├── Saves aggregate changes
│   └── Saves OutboxMessage (serialized event JSON)
│
└── OutboxRelayService (BackgroundService)
    ├── Polls OutboxMessages WHERE processed_at IS NULL
    ├── Wraps in KafkaEventEnvelope { EventType, Payload, TenantId, OccurredAt }
    ├── Produces to Kafka topic
    └── Marks OutboxMessage.ProcessedAt = now()
            │
            ▼
         Kafka Topic (3 partitions)
            │
            ▼
Consumer Service
    └── KafkaConsumerBase
        ├── Subscribes to topic
        ├── Deserializes KafkaEventEnvelope
        ├── Routes to event handler by EventType
        └── Commits offset on success
```

---

## 2. Kafka Topics

| Topic | Partitions | Producers | Consumers |
|-------|-----------|-----------|-----------|
| `identity-events` | 3 | Identity | Notification |
| `student-events` | 3 | Student | Notification |
| `academic-events` | 3 | Academic | Notification |
| `attendance-events` | 3 | Attendance | — |
| `examination-events` | 3 | Examination | Notification |
| `fee-events` | 3 | Fee | Notification |
| `faculty-events` | 3 | Faculty | — |
| `hostel-events` | 3 | Hostel | — |
| `notification-events` | 3 | Notification | — |

---

## 3. Event Catalog

### Identity Events (`identity-events`)

| Event | Trigger | Payload Fields |
|-------|---------|---------------|
| `UserRegistered` | POST /register | UserId, Email, TenantId, OccurredAt |
| `EmailVerificationRequested` | Register or resend | UserId, Email, Token, TenantId |
| `UserEmailVerified` | POST /verify-email | UserId, Email, TenantId |
| `UserDeactivated` | Admin deactivates | UserId, TenantId |
| `UserLoginSucceeded` | Successful auth | UserId, IpAddress, TenantId |
| `UserLoginFailed` | Failed auth | Email, IpAddress, TenantId |
| `UserLockedOut` | Too many failures | UserId, TenantId |
| `PasswordResetRequested` | POST /forgot-password | UserId, Token, TenantId |
| `PasswordResetCompleted` | POST /reset-password | UserId, TenantId |
| `TenantProvisioned` | POST /tenants | TenantId, Name, Plan |
| `TenantSuspended` | Admin suspends | TenantId |
| `TenantUpgraded` | Plan change | TenantId, NewPlan |

### Student Events (`student-events`)

| Event | Trigger | Payload Fields |
|-------|---------|---------------|
| `StudentCreated` | POST /students | StudentId, Name, Programme, TenantId |
| `StudentDetailsUpdated` | PATCH /students/:id | StudentId, ChangedFields, TenantId |
| `StudentStatusChanged` | Admit/Suspend/Graduate | StudentId, OldStatus, NewStatus, TenantId |

### Academic Events (`academic-events`)

| Event | Trigger | Payload Fields |
|-------|---------|---------------|
| `CoursePublished` | PublishCourseCommand | CourseId, Code, Credits, TenantId |
| `DepartmentCreated` | CreateDepartmentCommand | DeptId, Name, TenantId |
| `ProgrammeActivated` | UpdateProgrammeCommand | ProgrammeId, Name, TenantId |

### Fee Events (`fee-events`)

| Event | Trigger | Payload Fields |
|-------|---------|---------------|
| `FeePaymentConfirmed` | Confirm payment | PaymentId, StudentId, Amount, TenantId |
| `ScholarshipGranted` | Approve scholarship | ScholarshipId, StudentId, Amount, TenantId |

### Examination Events (`examination-events`)

| Event | Trigger | Payload Fields |
|-------|---------|---------------|
| `ExamSchedulePublished` | Publish schedule | ScheduleId, CourseId, Date, TenantId |
| `MarksApproved` | Approve marks | MarksEntryId, StudentId, CourseId, TenantId |
| `ResultCardGenerated` | Generate result | ResultCardId, StudentId, CGPA, TenantId |

### Faculty Events (`faculty-events`)

| Event | Trigger | Payload Fields |
|-------|---------|---------------|
| `FacultyCreated` | CreateFacultyCommand | FacultyId, Name, Department, TenantId |
| `CourseAssigned` | AssignCourseCommand | FacultyId, CourseId, Semester, TenantId |

### Hostel Events (`hostel-events`)

| Event | Trigger | Payload Fields |
|-------|---------|---------------|
| `RoomAllocated` | AllocateRoomCommand | AllotmentId, StudentId, RoomId, TenantId |
| `RoomVacated` | VacateRoomCommand | AllotmentId, StudentId, TenantId |
| `ComplaintRaised` | RaiseComplaintCommand | ComplaintId, StudentId, Category, TenantId |

---

## 4. Notification Service — Event Routing

```
Kafka Consumer (Notification.Infrastructure)
│
├── IdentityEventsConsumer
│   ├── UserRegistered          → send "Welcome + Verify Email" email
│   ├── EmailVerificationRequested → send verification link email
│   ├── PasswordResetRequested  → send password reset link email
│   └── UserLockedOut           → send account locked notification
│
├── StudentEventsConsumer
│   ├── StudentCreated          → send enrollment confirmation
│   └── StudentStatusChanged    → send status update notification
│
├── AcademicEventsConsumer
│   └── CoursePublished         → notify enrolled students
│
├── FeeEventsConsumer
│   └── FeePaymentConfirmed     → send payment receipt
│
└── ExaminationEventsConsumer
    └── ExamSchedulePublished   → notify students of exam schedule
```

---

## 5. End-to-End Flow: Student Registration

```
Student registers via Identity service
    │
    ▼
POST /identity/register (Identity.API)
    │ RegisterCommand → MediatR
    │
    ▼
RegisterCommandHandler
    ├── Creates ApplicationUser aggregate
    ├── Creates VerificationToken
    ├── Raises EmailVerificationRequestedEvent (on aggregate)
    ├── SaveChanges:
    │   ├── INSERT ApplicationUser
    │   ├── INSERT VerificationToken
    │   └── INSERT OutboxMessage (serialized event)
    └── Returns success
    │
    ▼
Identity OutboxRelayService (background)
    ├── Picks up OutboxMessage
    ├── Produces to `identity-events`: EmailVerificationRequested
    └── Marks processed
    │
    ▼
Notification IdentityEventsConsumer
    ├── Receives EmailVerificationRequested
    ├── Loads NotificationTemplate for "email-verification"
    ├── Dispatches via EmailChannel (SMTP)
    └── Logs to NotificationLog
```

---

## 6. Event Envelope Format (KafkaEventEnvelope)

```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "eventType": "EmailVerificationRequested",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "occurredAt": "2026-03-07T12:35:00Z",
  "payload": {
    "userId": "...",
    "email": "student@university.edu",
    "token": "...",
    "purpose": "EmailVerification"
  }
}
```

---

## 7. Governance: Event Contract Rules

The Aegis `EventSchemaCompatibilityRule` validates:
- All published events match the schema in `src/.ums/event-schemas/`
- No breaking changes (removed fields, type changes) in existing events
- New optional fields are allowed (additive)
- Snapshots (`baseline-latest.snap.json`) track event schema drift across deploys
