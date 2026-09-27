# Distributed Order System

A distributed order processing demo built with:

- .NET 10
- ASP.NET Core Web API
- SQL Server
- RabbitMQ
- Docker
- Entity Framework Core

## Features

- Concurrent inventory deduction
- SQL transaction
- RabbitMQ producer / consumer
- Manual ACK / NACK
- Message redelivery
- Idempotent consumer
- Database-backed idempotency
- Transactional message processing

## Experiments

### 1. Concurrent inventory deduction

10 concurrent requests against 5 items of stock.

Result:
- 5 requests succeeded
- 5 requests failed
- No overselling

### 2. RabbitMQ message redelivery

Tested manual ACK and NACK.

When the consumer fails before ACK:
- Message remains unacknowledged
- Message is redelivered after consumer restart

### 3. Idempotent consumer

Used EventId + unique database index.

Sending the same event twice results in:
- First message processed
- Second message detected as duplicate
- Business operation is not repeated
