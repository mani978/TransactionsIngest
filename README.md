# Transaction Ingestion Console Application

# Overview

This project implements a .NET console application that ingests transaction data from a mocked JSON feed and stores it in a SQLite database using Entity Framework Core.

The application simulates how a backend system would process a snapshot of transactions from the last 24 hours and maintain both:

The latest state of each transaction.
A history of changes made to those transactions.

The solution also includes automated tests to verify key behaviors such as inserts, updates, and revocations.

# Technology Stack

| Component         | Technology                    |
| ----------------- | ----------------------------- |
| Language          | C#                            |
| Framework         | .NET 10                       |
| ORM               | Entity Framework Core         |
| Database          | SQLite                        |
| Testing Framework | xUnit                         |
| Assertion Library | FluentAssertions              |
| Development Tools | Visual Studio Code / .NET CLI |

# Project Structure

```
TransactionsIngest
│
├── TransactionsIngest.App
│ ├── Data
│ │ └── AppDbContext.cs
│ │
│ ├── Config
│ │ └── TransactionFeedSettings.cs
│ │
│ ├── Models
│ │ ├── Transaction.cs
│ │ └── TransactionAudit.cs
│ │
│ ├── Services
│ │ ├── TransactionIngestionService.cs
│ │ ├── ITransactionFeedService.cs
│ │ ├── MockTransactionFeedService.cs
│ │ └── TransactionIngestionService.cs
│ │
│ ├── Dtos
│ │ ├── IngestionResultDto.cs
│ │ └── TransactionFeedItemDto.cs
│ │
│ ├── MockData
│ │ └── transactions.json
│ │
│ ├── Program.cs
│ └── appsettings.json
│
├── TransactionsIngest.Tests
│ ├── Helpers
│ │ └── FakeTransactionFeedService.cs
│ │
│ └── Services
│ └── TransactionIngestionServiceTests.cs
│
└── README.md
```

# Database Design

The system uses two tables.

## 1. Transactions

This table stores the latest state of each transaction.

| Column                  | Description                                      |
| ----------------------- | ------------------------------------------------ |
| TransactionId           | Business identifier from the feed                |
| CardLast4               | Last four digits of the card number              |
| LocationCode            | Store location code                              |
| ProductName             | Product purchased                                |
| Amount                  | Transaction amount                               |
| TransactionTimeUtc      | Timestamp of the transaction                     |
| Status                  | Current state (Active / Revoked / Finalized)     |
| CreatedAtUtc            | When the record was first created                |
| UpdatedAtUtc            | Last update timestamp                            |
| LastSeenInSnapshotAtUtc | When the transaction last appeared in a snapshot |

## 2. TransactionAudits

This table stores change history.

Every important change creates a record here.

| Column        | Description                           |
| ------------- | ------------------------------------- |
| TransactionId | Associated transaction                |
| ChangeType    | Insert / Update / Revoked / Finalized |
| FieldName     | Field that changed                    |
| OldValue      | Previous value                        |
| NewValue      | New value                             |
| ChangedAtUtc  | When the change occurred              |

# Features Implemented

## Insert New Transactions

If a transaction from the snapshot does not exist in the database:

It is inserted into the Transactions table
An Insert audit record is created

## Update Existing Transactions

If a transaction already exists and any of these fields change:

- card number (last 4 digits)
- location code
- product name
- amount
- timestamp

then:

- the record is updated
- an Update audit record is created for each changed field

## Revocation Detection

If a transaction exists in the database but does not appear in the latest snapshot and is still within the 24-hour window:

Its status becomes Revoked
A Revoked audit record is created

## Finalization

If a transaction becomes older than 24 hours, it may be marked as: Finalized

Finalized transactions represent records that are no longer expected to change.

## Idempotent Processing

Running the ingestion multiple times with the same snapshot does not create duplicates.

```
Example:

First run:
Inserted: 2
Updated: 0
Revoked: 0
Finalized: 0
No Change: 0

Second run:
Inserted: 0
Updated: 0
Revoked: 0
Finalized: 0
No Change: 2
```

## Mock Transaction Feed

Since the real API is unavailable, the application uses a mocked JSON file:
TransactionsIngest.App/MockData/transactions.json

Example:

```json
[
  {
    "transactionId": 1001,
    "cardNumber": "4111111111111111",
    "locationCode": "STO-01",
    "productName": "Wireless Mouse",
    "amount": 19.99,
    "timestamp": "2026-03-10T21:20:00Z"
  },
  {
    "transactionId": 1002,
    "cardNumber": "4000000000000002",
    "locationCode": "STO-02",
    "productName": "USB-C Cable",
    "amount": 25.0,
    "timestamp": "2026-03-10T21:25:00Z"
  }
]
```

# How to Run the Application

Open a terminal in the project root and run:

```
dotnet restore
dotnet build
dotnet run --project TransactionsIngest.App
```

```
Example Output:
Ingestion run completed.
Inserted: 2
Updated: 0
Revoked: 0
Finalized: 0
No Change: 0
```

# Running Automated Tests

The project includes xUnit tests that verify key ingestion behaviors.

To run tests:

```
dotnet test
```

```
Example Output:
Test summary: total: 4
Passed: 4
Failed: 0
```

The tests cover:

```
inserting new transactions
updating transactions when fields change
revoking missing transactions
idempotent ingestion behavior
```

# Design Decisions

## Transaction ID

TransactionId is treated as a business identifier rather than a database primary key.

## Card Data Storage

Only the last 4 digits of the card number are stored to avoid persisting full card numbers.

## Database Transactions

Each ingestion run is wrapped in a single database transaction to ensure data consistency.

## EF Core Code-First

The database schema is created using Entity Framework Core migrations/code-first.

## SQLite

SQLite was selected because it is lightweight and easy to run locally without requiring a server.

# Assumptions

```
The mocked JSON file represents a snapshot of the last 24 hours of transactions.
Missing transactions within the 24-hour window indicate revocations.
Transactions older than 24 hours may be finalized.
The system is designed to run repeatedly without creating duplicate records.
```

# Useful Commands to run in terminal

## Build project

dotnet build

## Run application

dotnet run --project TransactionsIngest.App

## Run tests

dotnet test

---

### End of Content
