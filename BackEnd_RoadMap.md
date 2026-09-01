# Prompt: Build the Arabic Real Estate Project Management Backend

You are a senior ASP.NET Core architect.

Build the backend API for a professional internal web application used by an Egyptian real-estate/construction company.

The system replaces an Excel-based process for managing:

* projects
* floors
* expenses
* suppliers
* engineers
* advances
* shareholders
* cash
* company/project storage
* reports
* users
* audit history

The backend must be designed for correctness, financial consistency, maintainability, and future expansion.

---

# 1. Technology Stack

Use:

* ASP.NET Core Web API
* .NET 10
* C#
* Entity Framework Core
* SQL Server
* MediatR
* FluentValidation
* JWT authentication
* ASP.NET Core Authorization
* Serilog
* Swagger / OpenAPI
* BCrypt or ASP.NET Core PasswordHasher for passwords
* xUnit
* FluentAssertions
* Moq only where actually needed

Do not add unnecessary technologies.

Do not use microservices.

Do not use Kubernetes.

Do not use event sourcing.

Do not introduce Redis unless there is an actual measured need.

---

# 2. Architecture

Use a **Clean Architecture modular monolith**.

Solution:

```text
RealEstateManagement.sln

src/
├── RealEstateManagement.API/
├── RealEstateManagement.Application/
├── RealEstateManagement.Domain/
└── RealEstateManagement.Infrastructure/

tests/
├── RealEstateManagement.UnitTests/
└── RealEstateManagement.IntegrationTests/
```

Dependency direction:

```text
API
 ↓
Application
 ↓
Domain

Infrastructure
 ↓
Application
Domain
```

Domain must not depend on infrastructure.

Application must not directly depend on EF Core implementation details.

---

# 3. Domain Organization

Use feature/domain modules inside Application.

```text
Application/
├── Auth/
├── Users/
├── Projects/
├── Floors/
├── Suppliers/
├── Expenses/
├── Cash/
├── Shareholders/
├── Engineers/
├── Advances/
├── Storages/
├── Reports/
└── AuditLogs/
```

Inside a module:

```text
Projects/
├── Commands/
├── Queries/
├── DTOs/
├── Validators/
└── Mappings/
```

Do not create unnecessary files.

---

# 4. Database

Use SQL Server.

Use EF Core migrations.

The database must be normalized.

Do NOT copy Excel sheets directly into tables.

Reports such as:

* project cost report
* floor cost report
* supplier statement
* shareholder statement
* advance statement
* cash report

should be generated from normalized transactional data.

---

# 5. Entities

Required entities:

```text
User
Role

Project
Floor

Supplier

Engineer
ProjectEngineer

Expense

CashStorage
CashTransaction

Shareholder
ShareholderContribution

Advance

Storage
StorageTransaction

AuditLog
```

Do NOT create:

```text
Employee
Partner
Payment
Item
```

unless a later business requirement explicitly requires them.

---

# 6. Users

## User

Properties:

```text
Id
FullName
Username
PasswordHash
Phone
RoleId
IsActive
CreatedAt
LastLoginAt
```

Arabic labels:

```text
معرف المستخدم
الاسم بالكامل
اسم المستخدم
تجزئة كلمة المرور
رقم الهاتف
معرف الدور
نشط
تاريخ الإنشاء
آخر تسجيل دخول
```

---

# 7. Roles

Exactly three initial roles:

```text
CompanyOwner
ProjectOwner
Calculator
```

Arabic:

```text
مالك الشركة
مالك المشروع
مسؤول الحسابات
```

Implement authorization policies.

Do not rely only on frontend hiding.

---

# 8. Projects

## Project

Properties:

```text
Id
Code
Name
Location
Description
Budget
StartDate
ExpectedEndDate
ActualEndDate
Status
ProjectOwnerId
CreatedAt
UpdatedAt
IsActive
```

Arabic:

```text
معرف المشروع
كود المشروع
اسم المشروع
موقع المشروع
وصف المشروع
ميزانية المشروع
تاريخ بداية المشروع
تاريخ الانتهاء المتوقع
تاريخ الانتهاء الفعلي
حالة المشروع
معرف مالك المشروع
تاريخ الإنشاء
تاريخ آخر تعديل
نشط
```

Statuses:

```text
Planning
Active
Completed
Suspended
Cancelled
```

---

# 9. Floors

Properties:

```text
Id
ProjectId
FloorNumber
Name
Description
CreatedAt
```

Arabic:

```text
معرف الدور
معرف المشروع
رقم الدور
اسم الدور
وصف الدور
تاريخ الإنشاء
```

Relationship:

```text
Project 1 ───── * Floors
```

---

# 10. Suppliers

Properties:

```text
Id
Code
Name
Type
Phone
Address
OpeningBalance
Notes
IsActive
CreatedAt
```

Types:

```text
Supplier
ServiceProvider
Contractor
```

Arabic:

```text
مورد
مؤدي خدمة
مقاول
```

Suppliers do not receive advances.

---

# 11. Engineers

## Engineer

```text
Id
Code
FullName
Phone
Email
Specialization
Notes
IsActive
CreatedAt
```

Arabic:

```text
معرف المهندس
كود المهندس
اسم المهندس
رقم الهاتف
البريد الإلكتروني
التخصص
ملاحظات
نشط
تاريخ الإنشاء
```

## ProjectEngineer

```text
Id
ProjectId
EngineerId
Role
StartDate
EndDate
Notes
```

This allows one engineer to work on multiple projects.

---

# 12. Expenses

## Expense

Properties:

```text
Id
ExpenseNumber
ProjectId
FloorId
SupplierId
ExpenseDate
Description
TotalAmount
CreatedByUserId
CreatedAt
UpdatedAt
Notes
Status
```

Arabic:

```text
معرف المصروف
رقم المصروف
معرف المشروع
معرف الدور
معرف المورد
تاريخ المصروف
بيان المصروف
إجمالي قيمة المصروف
معرف المستخدم المنشئ
تاريخ الإنشاء
تاريخ آخر تعديل
ملاحظات
حالة المصروف
```

Do not make `PaidAmount` and `RemainingAmount` authoritative database fields.

Calculate them from related cash transactions.

Rules:

```text
PaidAmount = sum of valid cash payments related to the expense

RemainingAmount = TotalAmount - PaidAmount
```

Validation:

```text
TotalAmount > 0
PaidAmount >= 0
PaidAmount <= TotalAmount
```

Status:

```text
Due
PartiallyPaid
Paid
```

Arabic:

```text
مستحق بالكامل
مدفوع جزئياً
مدفوع بالكامل
```

---

# 13. Cash

## CashStorage

Properties:

```text
Id
Name
Type
OpeningBalance
Location
IsActive
CreatedAt
```

Types:

```text
Company
Project
```

Arabic:

```text
خزينة الشركة
خزينة المشروع
```

For project cash storage, associate it with the project where required.

---

# 14. Cash Transactions

Properties:

```text
Id
TransactionNumber
TransactionDate
Type
Amount
CashStorageId
ProjectId
ExpenseId
AdvanceId
Description
ReferenceNumber
CreatedByUserId
CreatedAt
Notes
```

Types:

```text
ExpensePayment
CashIn
CashOut
AdvanceGiven
AdvanceReturned
OwnerDeposit
OtherIncome
OtherExpense
ShareholderContribution
```

Arabic:

```text
سداد مصروف
إيراد نقدي
مصروف نقدي
صرف عهدة
رد عهدة
إيداع مالك الشركة
إيراد آخر
مصروف آخر
مساهمة مساهم
```

All financial movements are cash.

Do not implement payment gateways.

---

# 15. Shareholders

## Shareholder

```text
Id
Code
Name
Phone
OwnershipPercentage
RequiredContribution
Notes
IsActive
CreatedAt
```

Arabic:

```text
معرف المساهم
كود المساهم
اسم المساهم
رقم الهاتف
نسبة المساهمة
إجمالي المبلغ المطلوب من المساهم
ملاحظات
نشط
تاريخ الإنشاء
```

## ShareholderContribution

```text
Id
ShareholderId
ProjectId
TransactionId
Amount
ContributionDate
Description
CreatedByUserId
CreatedAt
```

Rules:

```text
ContributedAmount = SUM(ShareholderContribution.Amount)

RemainingAmount =
RequiredContribution - ContributedAmount
```

Important:

A shareholder is NOT responsible for or the owner of general transactions.

A shareholder contribution is a specific business event that may generate a cash transaction.

Do not add ShareholderId to generic CashTransaction just to make relationships easier.

---

# 16. Advances

Only engineers can receive advances.

## Advance

```text
Id
AdvanceNumber
EngineerId
ProjectId
IssuedAmount
IssueDate
SettlementDate
Status
Notes
```

Do not attach advances to suppliers.

Do not attach advances to arbitrary users.

Rules:

```text
SettledAmount =
sum of valid settlement/expense records for the advance

RemainingAmount =
IssuedAmount - SettledAmount
```

Statuses:

```text
Open
PartiallySettled
FullySettled
Closed
```

Arabic:

```text
مفتوحة
مسواة جزئياً
مسواة بالكامل
مغلقة
```

---

# 17. Storage

The company has:

1. Company storage.
2. A storage for each project.

## Storage

```text
Id
Name
Type
ProjectId
Location
Description
IsActive
CreatedAt
```

Type:

```text
Company
Project
```

Rules:

* Company storage must have no ProjectId.
* Project storage must belong to exactly one project.
* A project may have one primary storage in MVP.

---

# 18. Storage Transactions

No Items master table.

Store the material information directly in the movement.

Properties:

```text
Id
StorageId
ProjectId
TransactionDate
MaterialName
Unit
Quantity
Type
ReferenceNumber
Description
CreatedByUserId
CreatedAt
```

Types:

```text
Purchase
TransferIn
TransferOut
IssueToProject
Return
Adjustment
```

Arabic:

```text
شراء
تحويل وارد
تحويل صادر
صرف للمشروع
مرتجع
تسوية
```

---

# 19. Entity Relationships

Implement relationships carefully.

Main structure:

```text
User
 └── Role

Project
 ├── ProjectOwner
 ├── Floors
 ├── ProjectEngineers
 ├── Expenses
 ├── Advances
 ├── Storages
 └── CashTransactions

Supplier
 └── Expenses

Engineer
 ├── ProjectEngineers
 └── Advances

Expense
 └── CashTransactions

Shareholder
 └── ShareholderContributions
       └── CashTransaction

Storage
 └── StorageTransactions

CashStorage
 └── CashTransactions
```

---

# 20. Important Financial Integrity Rule

Any operation affecting money must be transactional.

For example, recording an expense payment should execute as one database transaction:

```text
BEGIN TRANSACTION

Validate expense
Validate payment amount
Create cash transaction
Update related state if required
Create audit log

COMMIT
```

If anything fails:

```text
ROLLBACK
```

No partial financial operation is allowed.

---

# 21. Money Types

Use:

```csharp
decimal
```

for all money values.

Recommended SQL type:

```text
decimal(18,2)
```

Do not use:

```text
float
double
```

for financial values.

Quantities can use:

```text
decimal(18,3)
```

---

# 22. Concurrency

Implement optimistic concurrency where financial records may be edited simultaneously.

Do not allow silent overwrites.

Use a row version/concurrency token where appropriate.

Return a meaningful API response such as:

```text
تم تعديل هذا السجل بواسطة مستخدم آخر. برجاء إعادة تحميل الصفحة.
```

---

# 23. Audit Logging

Financial and important business operations must be auditable.

Log:

```text
Create
Update
Delete
Login
Logout
Financial transaction
Expense changes
Shareholder contribution
Advance operations
Storage operations
```

AuditLog:

```text
Id
UserId
Action
EntityName
EntityId
OldValues
NewValues
CreatedAt
IpAddress
```

Do not expose passwords or sensitive authentication information in audit logs.

---

# 24. CQRS / MediatR

Use MediatR for meaningful commands and queries.

Examples:

```text
CreateProjectCommand
UpdateProjectCommand

GetProjectByIdQuery
GetProjectsQuery

CreateExpenseCommand
RecordExpensePaymentCommand
GetExpenseByIdQuery

CreateSupplierCommand
GetSupplierStatementQuery

CreateShareholderContributionCommand
GetShareholderStatementQuery

CreateAdvanceCommand
GetAdvanceStatementQuery

CreateStorageTransactionCommand
GetStorageBalanceQuery
```

Do not create handlers for trivial operations if doing so provides no architectural value.

---

# 25. Validation

Use FluentValidation.

Every command must validate business input.

Examples:

```text
Project budget > 0
Expense total > 0
Expense paid <= total
Shareholder contribution > 0
Advance amount > 0
Storage quantity > 0
Project must exist
Floor must belong to project
Engineer must be assigned to project before receiving advance
```

Return structured validation errors.

---

# 26. DTO Rules

Never expose EF entities directly from API controllers.

Use:

```text
Request DTOs
Response DTOs
```

Example:

```text
CreateExpenseRequest
ExpenseResponse
ExpenseSummaryResponse
SupplierStatementResponse
ProjectFinancialSummaryResponse
```

Avoid leaking internal database structures.

---

# 27. API Endpoints

Use RESTful routes.

Examples:

```text
POST   /api/auth/login
POST   /api/auth/logout

GET    /api/projects
POST   /api/projects
GET    /api/projects/{id}
PUT    /api/projects/{id}
DELETE /api/projects/{id}

GET    /api/projects/{projectId}/floors
POST   /api/projects/{projectId}/floors

GET    /api/suppliers
POST   /api/suppliers
GET    /api/suppliers/{id}

GET    /api/expenses
POST   /api/expenses
GET    /api/expenses/{id}
PUT    /api/expenses/{id}

GET    /api/cash/storages
GET    /api/cash/transactions

GET    /api/shareholders
POST   /api/shareholders
GET    /api/shareholders/{id}
POST   /api/shareholders/{id}/contributions

GET    /api/engineers
POST   /api/engineers
POST   /api/projects/{projectId}/engineers

GET    /api/advances
POST   /api/advances
GET    /api/advances/{id}

GET    /api/storages
POST   /api/storages
GET    /api/storages/{id}/transactions
POST   /api/storages/{id}/transactions
```

Use pagination for large lists.

---

# 28. Query Filters

Support filters where useful.

Examples:

```text
GET /api/expenses?projectId=1
GET /api/expenses?supplierId=5
GET /api/expenses?from=2026-09-01&to=2026-09-30
GET /api/cash/transactions?projectId=1
```

Do not load entire tables into memory.

Filtering, sorting, pagination, and aggregations should execute at the database level.

---

# 29. Financial Queries

Important endpoints should return computed summaries.

Example:

```text
GET /api/projects/{id}/financial-summary
```

Response:

```text
ProjectBudget
TotalExpenses
TotalPaid
TotalOutstanding
CashIn
CashOut
StorageValueIfApplicable
```

Supplier:

```text
GET /api/suppliers/{id}/statement
```

Shareholder:

```text
GET /api/shareholders/{id}/statement
```

Advance:

```text
GET /api/advances/{id}/statement
```

---

# 30. Dashboard API

Create one dashboard endpoint where practical:

```text
GET /api/dashboard/summary
```

Return:

```text
TotalProjects
ActiveProjects
TotalExpenses
TotalSupplierOutstanding
TotalCashBalance
TotalShareholderOutstanding
TotalOutstandingAdvances
```

Also provide data needed for charts.

Avoid making the frontend call 20 endpoints just to render the dashboard.

---

# 31. Error Handling

Implement global exception middleware.

Return standardized responses.

Example:

```json
{
  "success": false,
  "message": "لا يمكن تنفيذ العملية",
  "errors": []
}
```

Validation response:

```json
{
  "success": false,
  "message": "برجاء مراجعة البيانات المدخلة",
  "errors": {
    "amount": [
      "القيمة يجب أن تكون أكبر من صفر"
    ]
  }
}
```

Do not expose stack traces in production.

---

# 32. Logging

Use Serilog.

Log:

* application errors
* API request failures
* authentication failures
* financial operation failures
* important system events

Do not log:

* passwords
* JWT secrets
* sensitive credentials

---

# 33. Database Indexing

Create indexes for frequently queried fields.

At minimum:

```text
Projects.Code
Projects.ProjectOwnerId

Floors.ProjectId

Suppliers.Code
Suppliers.Name

Expenses.ExpenseNumber
Expenses.ProjectId
Expenses.FloorId
Expenses.SupplierId
Expenses.ExpenseDate

CashTransactions.TransactionNumber
CashTransactions.CashStorageId
CashTransactions.ProjectId
CashTransactions.ExpenseId
CashTransactions.TransactionDate

ShareholderContributions.ShareholderId
ShareholderContributions.ProjectId

Advances.EngineerId
Advances.ProjectId
Advances.Status

StorageTransactions.StorageId
StorageTransactions.ProjectId
StorageTransactions.TransactionDate
```

Add composite indexes based on actual query patterns.

---

# 34. Delete Strategy

Do not physically delete financial records casually.

For:

* expenses
* cash transactions
* shareholder contributions
* advances
* storage transactions

prefer cancellation/reversal or soft deletion according to business rules.

Historical financial records must remain traceable.

For simple master data such as suppliers, projects, engineers, use `IsActive` where appropriate.

---

# 35. Seed Data

Create seed data for development:

Roles:

```text
مالك الشركة
مالك المشروع
مسؤول الحسابات
```

Create one development user for each role.

Use secure password hashing.

Never hardcode real company passwords.

---

# 36. Testing

At minimum create tests for:

### Expense calculations

```text
Total = 100000
Paid = 60000
Remaining = 40000
```

### Invalid expense

```text
Paid > Total
```

must fail.

### Shareholder calculation

```text
Required = 500000
Contributed = 300000
Remaining = 200000
```

### Advance calculation

```text
Issued = 50000
Settled = 15000
Remaining = 35000
```

### Authorization

Verify that each role can only perform permitted actions.

### Financial transaction atomicity

Verify rollback when one operation fails.

---

# 37. Local Deployment Target

The final application is intended to run on the company's local network.

Expected deployment:

```text
Company Server
│
├── ASP.NET Core API
│
└── SQL Server
```

Next.js frontend runs against the internal API.

Potential architecture:

```text
Company PC
    ↓
http://server-name
    ↓
Next.js
    ↓
ASP.NET Core
    ↓
SQL Server
```

The backend must support configuration through environment variables/appsettings.

Do not hardcode:

* database credentials
* JWT secret
* server addresses

---

# 38. Security

Implement:

* JWT authentication
* role-based authorization
* secure password hashing
* input validation
* SQL injection protection through EF Core parameterization
* CORS configured for the frontend
* HTTPS where practical
* rate limiting for authentication endpoints where practical
* secure configuration

Never trust frontend authorization.

Backend must enforce permissions.

---

# 39. API Documentation

Enable Swagger/OpenAPI.

Document:

* authentication
* endpoints
* request examples
* response examples
* error responses

All API business messages should be Arabic.

Internal code can remain English.

---

# 40. MVP Implementation Order

Build in this exact order:

```text
1. Solution + architecture
2. Database + EF Core
3. Authentication + roles
4. Projects
5. Floors
6. Suppliers
7. Expenses
8. Cash
9. Shareholders
10. Engineers
11. Advances
12. Storages
13. Dashboard
14. Reports
15. Audit logs
16. Tests
17. Swagger
18. Production configuration
```

---

# 41. Critical Rule

Do not invent additional business entities.

Do not add:

* employees
* partners
* payment gateway
* inventory product catalog
* accounting journal system
* double-entry ledger

unless the requirements later explicitly justify them.

The current goal is:

**Project Management + Expense Tracking + Cash Management + Supplier Balances + Shareholder Contributions + Engineer Advances + Storage Management**

---

# 42. Definition of Done for MVP

The backend is considered functional when the following real scenario works:

```text
Login
 ↓
Create project
 ↓
Create floor
 ↓
Create supplier
 ↓
Create expense
 ↓
Record cash payment
 ↓
Expense remaining is calculated
 ↓
Supplier balance is calculated
 ↓
Project financial summary updates
 ↓
Create shareholder
 ↓
Record shareholder contribution
 ↓
Shareholder remaining updates
 ↓
Create engineer
 ↓
Assign engineer to project
 ↓
Give engineer advance
 ↓
Advance remaining updates
 ↓
Create company/project storage
 ↓
Record material movement
 ↓
Cash/dashboard information updates
```

The system must persist all information in SQL Server and continue working after restarting the API.

Focus on correctness and business logic first. Do not over-engineer.