# Fintech Payment Processing POC

## 1. Overview

A proof-of-concept fintech payment processing system that demonstrates real-world payment workflows including SWIFT MT103 to ISO 20022 pacs.008 conversion, domain-driven design patterns, and event-driven architecture. The system processes payments between accounts, automatically generates ISO 20022 compliant XML messages, and tracks payment lifecycle through domain events.

## 2. Architecture & Design Patterns

### Architecture: Clean Architecture (Layered Architecture)

The solution follows **Clean Architecture** principles with clear separation of concerns across four layers:

- **Domain Layer** (`FintechPaymentPOC.Domain`): Core business entities, value objects, and domain events
  - Entities: `Account`, `Transaction`
  - Value Objects: `SwiftMtxPayment`, `Party`, `MTXPayment`
  - Domain Events: `PaymentRequestedEvent`, `IDomainEvent`, `IDomainEventHandler`

- **Application Layer** (`FintechPaymentPOC.Application`): Business logic and use cases
  - Services: `PaymentService`, `MT103Parser`, `PaymentEnrichmentService`, `Pacs008Generator`
  - DTOs: `PaymentRequestDto`, `PaymentResponseDto`, `AccountDto`
  - Interfaces: `IPaymentService`, `IPaymentRepository`, `IDomainEventDispatcher`

- **Infrastructure Layer** (`FintechPaymentPOC.Infrastructure`): Data access and external services
  - Data: `PaymentDbContext`, `DatabaseSeeder`
  - Repositories: `PaymentRepository`
  - Events: `DomainEventDispatcher`, `PaymentEventHandler`, `NotificationEventHandler`, `AuditEventHandler`

- **API Layer** (`FintechPaymentPOC.API`): HTTP endpoints and application configuration
  - Controllers: `PaymentsController`, `AccountsController`
  - Configuration: `Program.cs`

### Design Patterns Used

1. **Domain-Driven Design (DDD)**
   - Domain events dispatched automatically on `SaveChangesAsync`
   - Rich domain model with business logic in entities
   - Value objects for immutable data structures

2. **Repository Pattern**
   - `IPaymentRepository` abstracts data access
   - `PaymentRepository` implements EF Core data access

3. **Event-Driven Architecture**
   - Domain events (`PaymentRequestedEvent`) published when entities change
   - Event handlers (`PaymentEventHandler`, `NotificationEventHandler`, `AuditEventHandler`) process events asynchronously
   - Automatic event dispatch via `PaymentDbContext.SaveChangesAsync`

4. **Dependency Injection**
   - All services registered in `Program.cs`
   - Constructor injection throughout

5. **Unit of Work Pattern**
   - `PaymentDbContext` manages transactions and domain event dispatch
   - Database transactions ensure atomicity

6. **Canonical Data Model Pattern**
   - `SwiftMtxPayment` serves as intermediate model between MT103 and pacs.008
   - Enables conversion between different message formats

## 3. Conversion Example

### SWIFT MT103 → ISO 20022 pacs.008 Conversion

The POC demonstrates conversion from legacy SWIFT MT103 format to modern ISO 20022 pacs.008 XML format.

**Input (MT103):**
```
:20:TRX123456
:23B:CRED
:32A:250925USD1000,
:50K:/123456789
JOHN DOE
:59:/987654321
JANE DOE
:71A:OUR
```

**Process:**
1. **Parse MT103** → Extract tags (`:20:`, `:32A:`, `:50K:`, `:59:`, `:71A:`)
2. **Convert to MTX** → Create `SwiftMtxPayment` canonical model
3. **Enrich & Validate** → Add missing fields, validate business rules
4. **Generate pacs.008** → Create ISO 20022 XML structure

**Output (pacs.008 XML):**
```xml
<Document xmlns="urn:iso:std:iso:20022:tech:xsd:pacs.008.001.12">
  <FIToFICstmrCdtTrf>
    <GrpHdr>
      <MsgId>TRX123456</MsgId>
      <CreDtTm>2025-09-25T10:30:00.000Z</CreDtTm>
      <NbOfTxs>1</NbOfTxs>
    </GrpHdr>
    <CdtTrfTxInf>
      <InstdAmt Ccy="USD">1000.00</InstdAmt>
      <Dbtr>
        <Nm>JOHN DOE</Nm>
      </Dbtr>
      <Cdtr>
        <Nm>JANE DOE</Nm>
      </Cdtr>
    </CdtTrfTxInf>
  </FIToFICstmrCdtTrf>
</Document>
```

**Why MTX is Required:**
- MT103 uses flat tag-based format → pacs.008 uses hierarchical XML
- MT103 has optional fields → pacs.008 requires mandatory elements
- MT103 uses different data formats → MTX normalizes to ISO standards
- MTX enables seamless conversion between legacy and modern formats

## 4. How to Run

### Prerequisites
- .NET 9.0 SDK or later
- Visual Studio 2022 / VS Code / Rider (optional)

### Steps

1. **Clone/Navigate to the repository:**
   ```bash
   cd fintech-poc
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build the solution:**
   ```bash
   dotnet build
   ```

4. **Run the API:**
   ```bash
   cd FintechPaymentPOC.API
   dotnet run
   ```

5. **Access Swagger UI:**
   - Navigate to: `https://localhost:7082/swagger` (or `http://localhost:5108/swagger`)
   - The API will be available with interactive documentation

6. **Database:**
   - Uses in-memory database (seeded with sample accounts on startup)
   - No additional database setup required

## 5. APIs Overview

### Account Management

#### `POST /api/accounts`
Creates a new account with initial balance.
- **Request:** `{ "name": "John Doe", "initialBalance": 1000.00, "currency": "USD" }`
- **Response:** Created account details with generated ID

#### `GET /api/accounts/{id}`
Retrieves account details by ID.
- **Response:** Account information including balance and currency

#### `GET /api/accounts/{id}/transactions`
Gets all transactions for a specific account (sent and received).
- **Response:** List of transactions with sender/receiver details

### Payment Processing

#### `POST /api/payments`
Creates and processes a payment between two accounts.
- **Request:** `{ "senderAccountId": "guid", "receiverAccountId": "guid", "amount": 100.00, "currency": "USD" }`
- **Process:** Validates → Creates transaction → Converts to pacs.008 XML → Processes payment → Updates balances
- **Response:** Payment status, transaction ID, MTX payment details, and **pacs.008 XML**
- **Features:** Automatic balance updates, domain event processing, ISO 20022 XML generation

### SWIFT Conversion

#### `POST /api/swift/mt103/convert`
Converts SWIFT MT103 message to ISO 20022 pacs.008 XML.
- **Request:** `{ "mt103Text": ":20:TRX123456\n:32A:250925USD1000,\n..." }`
- **Process:** Parse MT103 → Convert to MTX → Enrich & Validate → Generate pacs.008
- **Response:** pacs.008 XML, MTX details, conversion status (RECEIVED → TRANSFORMED → SENT)

## Key Features

- ✅ **Domain-Driven Design** with domain events
- ✅ **Event-Driven Architecture** with automatic event dispatch
- ✅ **Database Transactions** ensuring atomicity
- ✅ **SWIFT MT103 → ISO 20022 Conversion** pipeline
- ✅ **Account Management** with balance tracking
- ✅ **Payment Processing** with automatic balance updates
- ✅ **ISO 20022 pacs.008 XML** generation
- ✅ **Comprehensive Logging** for audit trail
- ✅ **Swagger Documentation** for API exploration

## Technology Stack

- **.NET 9.0** - Latest .NET framework
- **Entity Framework Core** - ORM with in-memory database
- **ASP.NET Core** - Web API framework
- **Swagger/OpenAPI** - API documentation
- **Clean Architecture** - Layered architecture pattern
- **Domain-Driven Design** - DDD principles

## Project Structure

```
FintechPaymentPOC/
├── FintechPaymentPOC.Domain/          # Domain layer (entities, value objects, events)
├── FintechPaymentPOC.Application/     # Application layer (services, DTOs, interfaces)
├── FintechPaymentPOC.Infrastructure/  # Infrastructure layer (data access, event handlers)
└── FintechPaymentPOC.API/            # API layer (controllers, configuration)
```

## Notes

- The system uses **in-memory database** - data is lost on application restart
- Domain events are dispatched automatically when `SaveChangesAsync` is called
- All payments automatically generate pacs.008 XML for ISO 20022 compliance
- Event handlers process payments, send notifications, and create audit logs

