#!/bin/bash
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiJlM2Q4YzBiMy05MDI4LTRkNTMtYWQ1OS0xM2U4MjkyOWYxMWYiLCJlbWFpbCI6InVzZXIyQHRlc3QuY29tIiwibmJmIjoxNzgwMzk0MTE0LCJleHAiOjE3ODAzOTc3MTQsImlhdCI6MTc4MDM5NDExNCwiaXNzIjoiQXVjdGlvbkVuZ2luZSIsImF1ZCI6IkF1Y3Rpb25FbmdpbmUifQ.n2kIADHDkEffB5AlNNxPCTgViaUnW_jYhSl2qQUqSyw"
AUCTION_ID="e318ac5f-972e-4221-ad95-f8c02c9dfc97"

# Run both requests in parallel
curl -X POST http://localhost:5187/auctions/$AUCTION_ID/bids \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"amount":620}' & 

curl -X POST http://localhost:5187/auctions/$AUCTION_ID/bids \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"amount":630}' &

wait