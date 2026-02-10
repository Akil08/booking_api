# DocBook: Intelligent Appointment Scheduling API

## ⚠️ Important Note

These projects are entirely original. They are not inspired by any YouTube tutorials, blogs, or copied from anywhere. I conceived the ideas myself, found the problems interesting and worth solving, and chose them specifically because they challenged me in unique ways.

This is one of my favorite projects out of the three I have built.  
My main goal here was not to create a production ready application, but to use it as a personal playground to deeply understand how different technologies connect and work together in practice: how files are linked, how code flows from controllers to services (and sometimes repositories), how functions call one another, and how real world problems like race conditions, abuse prevention, and background automation are solved.

I focused on exploring clean project structure, applying OOP principles and basic SOLID concepts in a practical way, and seeing the trade offs of each decision. Every feature, from transactions to caching, rate limiting to scheduled jobs, was implemented to learn how it would work in real world scenarios, to test, break, experiment, and truly grasp the underlying mechanics.

These projects are learning experiments: intentionally kept simple enough to understand fully, yet realistic enough to reflect actual backend challenges.

## What the Project Is
A .NET 8 Web API for doctor appointment booking with JWT authentication, concurrency-safe slot management, and automated daily reset with priority handling.

## What It Does
- Patients book/cancel slots for today
- Doctor cancels entire day with simulated notifications
- Patients subscribe for priority tomorrow
- Hangfire job at 3 AM resets day and books priority queue first

## What Problem It Solves
- Overbooking under concurrent requests
- Handling doctor cancellations without losing data or allowing invalid bookings
- Fair priority rescheduling for affected patients
- Automated daily operations

## How It Solves It
- Atomic EF Core updates for safe booking count
- Status tracking preserves history
- Priority queue table with FIFO ordering
- Hangfire recurring job for full automation

## Technologies Used
- .NET 8 ASP.NET Core Web API
- PostgreSQL + EF Core
- Hangfire
- JWT Bearer Authentication

## Endpoints
- POST /auth/login - Returns a JWT token for any supplied id and role
- POST /bookings/book - Books a slot for today as the authenticated patient
- POST /bookings/cancel - Cancels the authenticated patient's booking for today
- POST /bookings/doctor/cancel-day - Cancels all bookings for today as the doctor
- POST /bookings/priority/subscribe - Subscribes the authenticated patient for priority booking tomorrow

## Database Tables
- DayStates: Tracks the active day, total capacity, and whether the day is cancelled
  - Id: Primary key
  - Date: The date for the active booking day
  - MaxSlots: Maximum slots allowed for the day
  - BookedCount: Current count of booked slots
  - IsCancelled: Marks the day as cancelled by the doctor
  - UpdatedAt: Last update timestamp for the day state
- Bookings: Stores each patient's booking and cancellation history
  - Id: Primary key
  - PatientId: The patient identifier from the JWT
  - Date: The date of the booking
  - Status: Booked or Cancelled
  - CreatedAt: When the booking was created
  - CancelledAt: When the booking was cancelled
  - CancelledByDoctor: True when cancelled by the doctor
- PrioritySubscribers: FIFO queue for next-day priority booking
  - Id: Primary key
  - PatientId: The subscribed patient identifier
  - CreatedAt: When the subscription was created
