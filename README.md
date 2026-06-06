# AuctionEngine

AuctionEngine is an ASP.NET Core backend for running timed auctions.

It supports:

- User registration and login with ASP.NET Identity and JWT bearer tokens.
- Creating auction items.
- Listing active auctions.
- Placing bids on auctions.
- Bid validation for closed auctions, expired auctions, own-auction bids, non-positive amounts, and bids below the current highest bid.
- EF Core optimistic concurrency on the auction's `CurrentHighestBid` value.
- Redis caching for the current highest bid.
- SignalR notifications for new bids and closed auctions.
- A background service that closes expired auctions and creates invoices for winning bids.
- Unit tests for bidding and validation logic.

## Project Structure

- `AuctionEngine.API`: Minimal API endpoints, authentication setup, SignalR hub, hosted auction closer, and static test page.
- `AuctionEngine.Core`: Entities, service interfaces, bid validation, and bid placement service.
- `AuctionEngine.Infrastructure`: EF Core `DbContext`, PostgreSQL repository, Redis highest-bid cache, and migrations.
- `AuctionEngine.Tests`: xUnit tests.

## Requirements

- .NET SDK that supports `net10.0`.
- PostgreSQL.
- Redis.

The default local settings expect:

- PostgreSQL at `localhost:5432`.
- Database name: `AuctionEngine`.
- PostgreSQL user: `postgres`.
- PostgreSQL password: `postgres`.
- Redis at `localhost:6379`.

## Configuration

The database connection is in `AuctionEngine.API/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=5432;Database=AuctionEngine;User Id=postgres;Password=postgres;"
}
```

Development settings in `AuctionEngine.API/appsettings.Development.json` include:

```json
"Jwt": {
  "Key": "development-signing-key",
  "Issuer": "AuctionEngine",
  "Audience": "AuctionEngine"
},
"ConnectionStrings": {
  "Redis": "localhost:6379"
}
```

Use a different JWT key outside local development.

## Install

Restore dependencies:

```bash
dotnet restore
```

Apply the EF Core migrations:

```bash
dotnet ef database update \
  --project AuctionEngine.Infrastructure \
  --startup-project AuctionEngine.API
```

If `dotnet ef` is not installed:

```bash
dotnet tool install --global dotnet-ef
```

## Run

Start the API:

```bash
dotnet run --project AuctionEngine.API
```

The HTTP launch profile uses:

```text
http://localhost:5187
```

Open the SignalR test page in a browser:

```text
http://localhost:5187/index.html
```

## Use The API

### Register

```bash
curl -X POST http://localhost:5187/register \
  -H "Content-Type: application/json" \
  -d '{"email":"seller@test.com","password":"Password123!"}'
```

### Login

```bash
curl -X POST http://localhost:5187/login \
  -H "Content-Type: application/json" \
  -d '{"email":"seller@test.com","password":"Password123!"}'
```

The response contains a JWT:

```json
{
  "token": "..."
}
```

Use the token in authenticated requests:

```bash
export TOKEN="paste-token-here"
```

### Create An Auction

```bash
curl -X POST http://localhost:5187/auctions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "title": "Keyboard",
    "description": "Mechanical keyboard",
    "startingPrice": 50,
    "endTime": "2026-06-07T12:00:00Z"
  }'
```

The response includes the auction `id`.

### List Active Auctions

```bash
curl http://localhost:5187/auctions
```

### Get One Auction

```bash
curl http://localhost:5187/auctions/{auctionId}
```

### Place A Bid

Log in as a user who did not create the auction, then place a bid:

```bash
curl -X POST http://localhost:5187/auctions/{auctionId}/bids \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"amount":75}'
```

Bid responses can include:

- `201 Created`: bid accepted.
- `400 Bad Request`: bid failed validation.
- `401 Unauthorized`: missing or invalid JWT.
- `404 Not Found`: auction does not exist.
- `409 Conflict`: another bid changed the auction at the same time.

## SignalR

The SignalR hub is available at:

```text
/hubs/auction
```

Clients can call:

```text
JoinAuction(Guid auctionId)
```

The server sends:

- `NewBid` when a bid is accepted.
- `AuctionClosed` when the background service closes an expired auction.

The page at `wwwroot/index.html` connects to the hub, joins an auction group, and displays bid and close events.

## Background Auction Closing

`AuctionCLoserService` runs inside the API process.

Every 30 seconds it:

- Finds up to 50 auctions where `EndTime <= DateTime.UtcNow` and `IsClosed == false`.
- Finds the highest bid for each expired auction.
- Creates an invoice when there is a winning bidder.
- Marks the auction as closed.
- Sends an `AuctionClosed` SignalR event to clients in that auction group.

## Tests

Run the test suite:

```bash
dotnet test
```

The tests cover bid validation and bid placement behavior.

## Notes

- The API uses PostgreSQL through EF Core and Npgsql.
- Redis stores the highest bid value with a two-hour absolute expiration.
- `CurrentHighestBid` is configured as an EF Core concurrency token.
- The repository includes `test_concurrency.sh`, but it contains a hard-coded token and auction id. Update those values before using it locally.
