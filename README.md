# Count me in (crapp)

"Count me in" rebuilt as a traditional layered DDD app: an aggregate, application services, a repository over Entity Framework Core, and current state in Postgres. One half of a blog comparison; the sibling repo `countmein-eve` builds the same features event-sourced. Behaviour comes from `/data/projects/count-me-in` (its plan and 19 Given/When/Then scenarios).

## Features

1. Start an interest check (title, pitch, goal, optional closing date) and get a share link id plus an owner key
2. Pitch page: title, pitch, goal, count, goalReached, open, closesOn
3. Count me in (a person id, name, optional email; the same person twice is refused)
4. Change my mind
5. How it's going, owner only: count, goal, goalReached, sign-ups per day, people
6. Close the check, owner only

## Layout

```
src/CountMeIn.Domain          InterestCheck (aggregate root), Participant, domain exceptions, OwnerKey
src/CountMeIn.Application     InterestCheckService (use cases), DTOs, IInterestCheckRepository, IUnitOfWork
src/CountMeIn.Infrastructure  EF Core DbContext, configurations, repository, unit of work, migrations
src/CountMeIn.Api             ASP.NET Core controller, domain exception -> HTTP mapping
tests/                        Domain unit tests, application service tests (in-memory fakes), API tests (real Postgres)
```

Dependencies point inward: Api -> Application + Infrastructure; Infrastructure -> Application -> Domain.

## HTTP API

| | |
|---|---|
| `POST /api/checks` | `{title, pitch, goal, closesOn?}` -> 201 `{checkId, ownerKey}` |
| `GET /api/checks/{id}` | pitch page data |
| `POST /api/checks/{id}/interest` | `{personId, name, email?}` -> 204 |
| `DELETE /api/checks/{id}/interest/{personId}` | change my mind -> 204 |
| `GET /api/checks/{id}/progress` | owner only, header `X-Owner-Key` |
| `POST /api/checks/{id}/close` | owner only, header `X-Owner-Key` |

Refusals are `{ "error": "..." }`: 400 invalid input, 403 not the owner, 404 no such check, 409 closed / already in / not in / already closed.

## Run

Needs .NET 10 and Postgres. The database lives in Docker:

```
docker run -d --name countmein-db -e POSTGRES_PASSWORD=countmein -p 5435:5432 postgres:17
docker exec countmein-db psql -U postgres -c 'create database countmein_crapp'
dotnet run --project src/CountMeIn.Api      # Development applies migrations on startup
dotnet test
```

The connection string is `ConnectionStrings:CountMeIn` (default: localhost port 5435, database `countmein_crapp`). Override with the env var `ConnectionStrings__CountMeIn`. Add a migration with `dotnet tool restore` then `dotnet tool run dotnet-ef migrations add <Name> -p src/CountMeIn.Infrastructure -s src/CountMeIn.Infrastructure -o Persistence/Migrations`.

## Check-in (branch pivot-check-in)

- `POST /api/checks` also takes `capacity` and `doorsOpen`/`doorsClose` (both or neither; open before close).
- Spots are derived, not stored: the first `capacity` participants by sign-up time hold a spot, the rest are the waitlist. Withdrawing deletes the row, so the next person moves up by themselves, and re-joining inserts a new row at the back.
- `POST /api/checks/{id}/check-in` `{personId}`; entry window is open <= now < close. Checked in is a timestamp on the participant.
- `GET /api/checks/{id}/door-list` (owner only): checked in, expected, waitlist. The pitch gains `spotsLeft` and `waitlistCount`.
- `JoinedOn` (a date) became `SignedUpAt` (a timestamp), which gives the ordering; per-day sign-ups are derived from it.

## Releasing unclaimed spots (branch one-more-feature)

From 10 minutes before `doorsClose` (never earlier than `doorsOpen`), spots held by people who haven't checked in are released. They go to waitlisted people who already tried to check in, first come first served by their first attempt; the rest of the released spots stay empty. People who lost a spot are refused with "Your spot went to the waitlist". A hosted service (`SpotReleaseWorker`, every 30 s, `SpotRelease:IntervalSeconds`) does it with no owner action. Checks without a capacity have nothing to release. The door list gains `letInFromWaitlist` and `lostTheirSpot`.

To know who tried first, a refused waitlist check-in now stores `DoorAttemptAt` (first attempt, while the doors are open and before the release). New columns: `participants.DoorAttemptAt/SpotLostAt/LetInAt`, `interest_checks.SpotsReleasedAt/ReleasedSpots`.

### Checks that were already running when this shipped

- **Works:** checks whose release moment is still ahead are released on time by the worker, using the capacity, doors and sign-up order already stored. Spot holders who haven't arrived lose their spot as described.
- **Does not work, and cannot be known:** waitlisted people who tried the door *before* this shipped. Those attempts were refused and nothing was stored, so their order is lost. They get no priority until they try again after the deploy; their attempt time is then the new one. If nobody on the waitlist tries again before the release, the unclaimed spots are released to nobody.
- **Checks already past their release moment at deploy are not released at all.** There is no attempt data to give the spots to, so taking spots away from people for nobody would only do harm. The migration marks them as released with nothing taken away; they behave exactly as before.
- A check-in racing the release by a few milliseconds is not guarded (no row version on the check).

## Venue reveal (branch venue-reveal)

- `PUT /api/checks/{id}/venue` (owner only, `X-Owner-Key`) with `{venue, revealAt}`. It can be set or changed until `revealAt` has passed; after that it is refused ("The venue has already been revealed").
- `GET /api/checks/{id}/venue?personId=...` returns `{revealAt, venue}`. `venue` is null before `revealAt`, and always null for anyone who isn't counted in with a spot. A spot holder, or someone let in from the waitlist, sees it. A waitlisted person sees it from the moment they are let in; someone who changed their mind, lost their spot or never counted in never does. "At that moment" is read as the moment they ask, using the current spot state. The person id is the same unauthenticated id used to count in, as elsewhere in this app.
- The door list shows `venue` and `venueRevealAt`.
- Two nullable columns on `interest_checks` (`Venue`, `VenueRevealAt`).

**Checks already running:** everything works. The columns are null for them, so they simply have no venue until the owner sets one, and they use the same capacity, waitlist and release data as before. Nothing about the past is needed. The one thing to know: a check that is already past the time the owner wants to reveal can still be given a `revealAt` in the past, which reveals at once.

## Design choices

- **Current state only.** Two tables: `interest_checks` and `participants`. Nothing records what used to be true.
- **Changing your mind deletes your participant row.** Being "in" is exactly "having a row", so count, goal check and "already counted in" are all plain queries on current rows. An `IsActive` flag would only be needed if something had to remember withdrawn people; nothing in these rules does. Joining again just inserts a new row.
- **Consequence for "how it's going":** sign-ups per day counts the people who are in now, grouped by the day they joined. The original also showed per-day withdrawals; with current state that history is gone, so it is not reproduced.
- **Aggregate boundary.** `InterestCheck` owns its `Participant`s; callers can't add or remove them except through `CountIn` / `Withdraw`. Rules (closed, blank name, duplicates, owner key) live in the domain methods and raise domain exceptions.
- **Owner key** is generated by the app, shown once, and only its SHA-256 is stored. The pitch DTO has no names or emails; the owner view includes name and email.
- **Race on double sign-up:** a unique index on (check, person) backs the domain check; the unit of work turns the violation into `AlreadyCountedInException`. There is no row-version column, since nothing else on the aggregate is edited concurrently in a way that matters here.
- **Time** comes from `TimeProvider`, so "today" is testable.
- **"Open"** means "not closed". `closesOn` is shown but does not close the check by itself (as in the original).
- Application tests use a hand-written in-memory repository; the API tests run the whole stack on Postgres.
