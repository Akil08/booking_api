# DocBook: Intelligent Appointment Scheduling API

A .NET 10 Web API for doctor appointment booking with JWT authentication, concurrency-safe slot management, and automated daily reset with priority handling.

---

## What the Project Does

- **Patients** can book/cancel slots for today
- **Doctor** can cancel entire day with simulated notifications
- **Patients** can subscribe for priority booking tomorrow
- **Hangfire job** at 3 AM resets day and books priority queue first

---

## What Problem It Solves

- Overbooking under concurrent requests
- Handling doctor cancellations without losing data
- Fair priority rescheduling for affected patients
- Automated daily operations

---

## How It Solves It

- Atomic EF Core updates for safe booking count
- Status tracking preserves history
- Priority queue table with FIFO ordering
- Hangfire recurring job for full automation

---

## What I Learned

- How race conditions occur under concurrent booking requests and how to solve them using atomic database updates — and   understanding which concurrency strategy fits which situation
- How to use Hangfire for scheduling recurring background jobs, including separating its connection string and triggering jobs manually for testing


## AI Tools Used

- Used Claude as a pair programmer throughout — planning features, deciding project structure, and figuring out the implementation approach
- Pair-programmed the core application logic collaboratively
- Used it to write unit tests and set up the GitHub Actions CI/CD pipeline


## What I'd Improve

- Replace mock email notifications with real email delivery (e.g., SendGrid or SMTP)
- Add idempotency keys on booking endpoints to prevent duplicate bookings from retried or double-fired requests

## Technologies Used

- .NET 10 ASP.NET Core Web API
- PostgreSQL + EF Core
- Hangfire (Background Jobs)
- JWT Bearer Authentication
- xUnit + Moq (Unit Testing)
- GitHub Actions (CI/CD)

---

## Prerequisites

- .NET 10 SDK
- PostgreSQL installed and running
- Postman (for API testing)

---

## Setup & Run

### 1. Clone the Repository

```bash
git clone https://github.com/Akil08/booking_api.git
cd booking_api
```

### 2. Configure Database Connection

Edit `appointment_api/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=appointment_api;Username=postgres;Password=YOUR_PASSWORD",
  "HangfireConnection": "Host=localhost;Port=5432;Database=appointment_api;Username=postgres;Password=YOUR_PASSWORD"
}
```

### 3. Configure JWT Secret

In `appsettings.json`, ensure the key is at least 32 characters:

```json
"Jwt": {
  "Issuer": "appointment_api",
  "Audience": "appointment_api",
  "Key": "super_secret_key_12345678901234567890"
}
```

### 4. Run the Application

```bash
cd appointment_api
dotnet run
```

The API will be available at: `http://localhost:5000`

---

## API Endpoints

| Method | Endpoint | Role | Description |
|--------|----------|------|-------------|
| POST | `/auth/login` | Public | Get JWT token |
| POST | `/bookings/book` | Patient | Book a slot for today |
| POST | `/bookings/cancel` | Patient | Cancel your booking |
| POST | `/bookings/doctor/cancel-day` | Doctor | Cancel all bookings for today |
| POST | `/bookings/priority/subscribe` | Patient | Subscribe for priority tomorrow |

---

## Testing with Postman

### Step 1: Get JWT Token

**Request:**
```
POST http://localhost:5000/auth/login
Content-Type: application/json

{
  "id": 1,
  "role": "patient"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6..."
}
```

⚠️ **Copy this token** for subsequent requests.

---

### Step 2: Book Appointment (Patient)

**Request:**
```
POST http://localhost:5000/bookings/book
Authorization: Bearer YOUR_PATIENT_TOKEN
Content-Type: application/json
```

**Response:**
```json
{
  "bookingId": 1,
  "message": "Booked"
}
```

---

### Step 3: Cancel Appointment (Patient)

**Request:**
```
POST http://localhost:5000/bookings/cancel
Authorization: Bearer YOUR_PATIENT_TOKEN
Content-Type: application/json
```

**Response:**
```json
{
  "message": "Booking cancelled"
}
```

---

### Step 4: Subscribe for Priority (Patient)

**Request:**
```
POST http://localhost:5000/bookings/priority/subscribe
Authorization: Bearer YOUR_PATIENT_TOKEN
Content-Type: application/json
```

**Response:**
```json
{
  "message": "Subscribed for priority booking"
}
```

---

### Step 5: Cancel Day (Doctor)

**First, get a Doctor token:**
```
POST http://localhost:5000/auth/login
Content-Type: application/json

{
  "id": 100,
  "role": "doctor"
}
```

**Then cancel the day:**
```
POST http://localhost:5000/bookings/doctor/cancel-day
Authorization: Bearer YOUR_DOCTOR_TOKEN
Content-Type: application/json
```

**Response:**
```json
{
  "message": "Day cancelled and patients notified"
}
```

**Check Terminal:** You'll see mock email logs for affected patients.

---

### Step 6: Verify Booking Fails on Cancelled Day

**Request:**
```
POST http://localhost:5000/bookings/book
Authorization: Bearer YOUR_PATIENT_TOKEN
Content-Type: application/json
```

**Response:**
```json
{
  "message": "No slots available"
}
```

---

## Run Unit Tests

```bash
cd appointment_api.Tests
dotnet test
```

**Expected Output:**
```
Passed!  - Failed: 0, Passed: 4, Skipped: 0
```

---

## Hangfire Dashboard

Access the background job dashboard at:

```
http://localhost:5000/hangfire
```

- **Recurring Jobs:** View the daily reset job (runs at 3 AM UTC)
- **Succeeded Jobs:** View executed job history
- **Trigger Button:** Manually trigger the daily reset for testing

---

## Database Tables

### DayStates
| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| Date | date | The booking date |
| MaxSlots | int | Maximum slots allowed |
| BookedCount | int | Current booked count |
| IsCancelled | bool | Cancelled by doctor |
| UpdatedAt | datetime | Last update timestamp |

### Bookings
| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| PatientId | int | Patient identifier |
| Date | date | Booking date |
| Status | enum | Booked or Cancelled |
| CreatedAt | datetime | When created |
| CancelledAt | datetime? | When cancelled |
| CancelledByDoctor | bool | Doctor cancellation flag |

### PrioritySubscribers
| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| PatientId | int | Patient identifier |
| CreatedAt | datetime | Subscription timestamp |

---

## CI/CD Pipeline

This project uses **GitHub Actions** for automated testing on every push.

**Workflow:** `.github/workflows/tests.yml`

- Runs on: `push` and `pull_request` to `main`
- Steps: Restore → Build → Test
- View results: **Actions** tab on GitHub

---

## Project Structure

```
booking_api/
├── appointment_api/
│   ├── Controllers/        # API endpoints
│   ├── Data/               # DbContext
│   ├── DTOs/               # Request/Response models
│   ├── Models/             # Database entities
│   ├── Services/           # Business logic
│   ├── Program.cs          # App configuration
│   └── appsettings.json    # Configuration
├── appointment_api.Tests/  # Unit tests
├── .github/workflows/      # CI/CD pipeline
└── booking_api.sln
```

---

