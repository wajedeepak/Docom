# Real-Time Queue Update - Quick Reference Guide

## System Overview

Your application uses **Microsoft SignalR** with a **group-based architecture** where:
- Each doctor has a **group** identified by their slug (e.g., `"dr-john-smith"`)
- When events occur (new token, queue advance), messages are **broadcast to all clients in that group**
- Clients receive updates **instantly** via WebSocket (or fallback to polling)

---

## Two Main Scenarios

### 1️⃣ NEW TOKEN ISSUANCE

**When:** Patient clicks "Take Token" button

**Backend Flow:**
```
POST /api/sessions/{sessionId}/tokens
    ↓
TokenService.TakeTokenAsync()
    ├─ Validate doctor & session
    ├─ Calculate position: waitingCount + 1
    ├─ Create Token in DB with:
    │  └─ Status = TokenStatus.Waiting
    │  └─ QueueOrder = position
    │  └─ PublicTokenId = random string (for tracking link)
    └─ Call notifier.NotifyTokenCreatedAsync()
           ↓
QueueNotifier.NotifyTokenCreatedAsync(doctorSlug, tokenNo, position)
    ↓
HubContext.Clients.Group("dr-john-smith").SendAsync("QueueAdvanced", {
  currentToken: 5,
  waitingCount: 12,
  estimatedWaitMinutes: 60
})
```

**Frontend Receives:**
```
conn.on('QueueAdvanced', (data) => {
  // Update displayed waiting count, position, estimated time
  queueState.update(s => ({...s, ...data}))
})
```

**UI Updates:**
- ✅ Doctor's dashboard: Waiting count increases
- ✅ Patient's tracking page: Queue position & wait time updates
- ✅ Other patients: Adjusted positions

---

### 2️⃣ QUEUE STATUS CHANGES

#### Doctor Advances Queue (Next Patient)

**Backend Flow:**
```
POST /api/sessions/{sessionId}/tokens/next
    ↓
TokenService.NextPatientAsync()
    ├─ Mark current token: Status = TokenStatus.Completed
    ├─ Get next waiting token
    ├─ Mark next token: Status = TokenStatus.Serving
    ├─ Update session.CurrentTokenNumber = next.TokenNumber
    └─ Call notifier.NotifyQueueAdvancedAsync()
           ↓
HubContext.Clients.Group("dr-john-smith").SendAsync("QueueAdvanced", {
  currentToken: 6,        ← Now serving #6
  waitingCount: 11,       ← One less waiting
  estimatedWaitMinutes: 55
})
```

**UI Updates:**
- ✅ Doctor's dashboard: Shows "Now Serving: #6"
- ✅ Patient in position #1: Sees "You're next!"
- ✅ All patients: Wait times decrease

#### Doctor Skips Patient

**Backend:**
```
POST /api/sessions/{sessionId}/tokens/skip
    ├─ Mark current: Status = TokenStatus.Skipped
    ├─ Requeue skipped token to end of queue
    ├─ Advance to next patient
    └─ Call notifier.NotifyTokenSkippedAsync()
           ↓
HubContext.Clients.Group("dr-john-smith").SendAsync("TokenSkipped", {
  skippedToken: 5,      ← This one was skipped
  currentToken: 6       ← Now serving this
})
```

#### Session Status Changes

**When:** Doctor pauses/resumes/ends session

```
Backend: SessionService.PauseSessionAsync()
    └─ Call notifier.NotifySessionChangedAsync(doctorSlug, "Paused")
           ↓
HubContext.Clients.Group("dr-john-smith").SendAsync("SessionChanged", {
  status: "Paused"  // or "Active", "Ended"
})
```

**Frontend Behavior:**
```
conn.on('SessionChanged', (data) => {
  // Reload entire queue state from API
  // (Don't do direct update for session status - requires consistency)
  this.loadQueueStateFromAPI()
})
```

---

## Real-Time Events Summary

| Event | Method Called | Data Sent | When Triggered |
|-------|---|---|---|
| **TokenCreated** | `NotifyTokenCreatedAsync()` | `{tokenNumber, queuePosition}` | Patient takes token |
| **QueueAdvanced** | `NotifyQueueAdvancedAsync()` | `{currentToken, waitingCount, estimatedWaitMinutes}` | Doctor clicks "Next" |
| **TokenSkipped** | `NotifyTokenSkippedAsync()` | `{skippedToken, currentToken}` | Doctor clicks "Skip" |
| **SessionChanged** | `NotifySessionChangedAsync()` | `{status}` | Session paused/resumed/ended |

---

## Frontend Architecture

### SignalRService: The Bridge
```typescript
// Establishes WebSocket connection to /hubs/queue
connectToDoctor(slug: string)
  └─ Invokes: JoinDoctorQueue(slug)  // Joins group

// Emits RxJS Subjects when messages arrive
conn.on('QueueAdvanced', ...) → queueAdvanced$.next()
conn.on('TokenCreated', ...) → tokenCreated$.next()
conn.on('TokenSkipped', ...) → tokenSkipped$.next()
conn.on('SessionChanged', ...) → sessionChanged$.next()
```

### Component Subscriptions

**Doctor Dashboard:**
```typescript
// Real-time: Direct API reload
signalR.queueAdvanced$.subscribe(() => loadQueueState())
signalR.tokenCreated$.subscribe(() => loadQueueState())
signalR.tokenSkipped$.subscribe(() => loadQueueState())

// Status change: Full reload (consistency)
signalR.sessionChanged$.subscribe(() => loadQueueState())
```

**Patient Queue Page:**
```typescript
// Real-time: Direct signal update (fast)
signalR.queueAdvanced$.subscribe(event => {
  queueState.update(s => ({
    ...s,
    currentTokenNumber: event.currentToken,
    waitingCount: event.waitingCount,
    estimatedWaitMinutes: event.estimatedWaitMinutes
  }))
})

// Session status change: Full API reload (consistency)
signalR.sessionChanged$.subscribe(() => loadQueueStateFromAPI())

// Fallback: Polling every 2 seconds (if WebSocket fails)
interval(2000).pipe(switchMap(...)).subscribe(...)
```

---

## Code Locations

| Component | File | Purpose |
|-----------|------|---------|
| SignalR Hub | [backend/src/Docom.API/Hubs/QueueHub.cs](backend/src/Docom.API/Hubs/QueueHub.cs) | Manages groups & connections |
| Notifier | [backend/src/Docom.API/Hubs/QueueNotifier.cs](backend/src/Docom.API/Hubs/QueueNotifier.cs) | Broadcasts events to groups |
| Token Service | [backend/src/Docom.Application/Services/TokenService.cs](backend/src/Docom.Application/Services/TokenService.cs) | Creates tokens, advances queue |
| Session Service | [backend/src/Docom.Application/Services/SessionService.cs](backend/src/Docom.Application/Services/SessionService.cs) | Manages session status |
| SignalR Service | [frontend/.../signalr.service.ts](frontend/docom-web/src/app/core/services/signalr.service.ts) | Frontend connection handler |
| Doctor Dashboard | [frontend/.../dashboard.component.ts](frontend/docom-web/src/app/features/doctor/dashboard/dashboard.component.ts) | Displays queue for doctor |
| Queue Page | [frontend/.../queue-page.component.ts](frontend/docom-web/src/app/features/patient/queue-page/queue-page.component.ts) | Displays patient tracking |

---

## Connection Lifecycle

### Doctor Opens Dashboard
```
1. Navigate to /dashboard
2. DoctorDashboard component loads
3. Calls signalRService.connectToDoctor("dr-john-smith")
4. SignalR: connection.start() → WebSocket handshake
5. SignalR: connection.invoke("JoinDoctorQueue", "dr-john-smith")
6. Backend QueueHub adds connection to group "dr-john-smith"
7. Doctor ready to receive real-time updates
```

### Patient Clicks Tracking Link
```
1. Patient takes token: https://docom.in/t/{publicTokenId}
2. TrackingResolver converts publicTokenId to doctor slug
3. Redirect to /{doctorSlug} (queue page)
4. QueuePage component loads
5. Calls signalRService.connectToDoctor(doctorSlug)
6. Same SignalR connection, same group
7. Both doctor and patient in group → same updates
```

### Disconnection
```
When user navigates away:
  signalRService.disconnect()
    ├─ Invoke: LeaveDoctorQueue(slug)  // Leave group
    ├─ connection.stop()                // Close WebSocket
    └─ Cleanup subscriptions
```

---

## Key Design Patterns

### 1. Group-Based Broadcasting
```csharp
// No need to track individual connections
// Just broadcast to group and SignalR handles delivery
_hub.Clients.Group("dr-john-smith").SendAsync("QueueAdvanced", data)
```

### 2. Minimal Payload
```typescript
// Only send delta, not entire queue
{
  currentToken: 6,
  waitingCount: 11,
  estimatedWaitMinutes: 55
}
// Let component fetch full details via API if needed
```

### 3. Fallback Mechanism
```
Primary:      WebSocket (< 50ms)
Fallback 1:   ServerSentEvents (~100ms)
Fallback 2:   LongPolling (2000ms)
Manual Poll:  Every 2 seconds (if WebSocket fails)
```

### 4. Dual Update Strategy
```
Queue advancement:  Fast direct update (just the numbers)
Session status:     Full reload via API (consistency matters)
```

---

## Troubleshooting

### Check if Real-Time is Working

**Browser DevTools:**
```
1. F12 → Network tab
2. Filter: "WS" (WebSocket)
3. Look for: wss://docom.in/api/hubs/queue
4. Status should be: 101 Switching Protocols
```

**Console Logs:**
```
[SignalR] Connected. Joining group: dr-john-smith
[SignalR] QueueAdvanced received: {...}
```

### If Updates Appear After 2 Seconds (Not Instant)
- WebSocket connection failed
- Fallen back to Long Polling (2-second interval)
- Check: CORS headers, SSL certificate, firewall, IIS WebSocket support

### If Messages Don't Arrive
- Connection not established: Check network tab
- Not in correct group: Check browser console for join confirmation
- Backend not sending: Check server logs for notification calls

---

## Summary

**Real-time queue updates are implemented via:**
1. Backend **TokenService** & **SessionService** call **IQueueNotifier**
2. **QueueNotifier** broadcasts to group via SignalR
3. Frontend **SignalRService** receives messages & emits RxJS Subjects
4. **Components** subscribe to subjects & update UI
5. **Fallback**: Polling every 2 seconds if WebSocket unavailable

**Events flow:**
- New Token → `TokenCreated` event → UI shows new position/wait time
- Next Patient → `QueueAdvanced` event → UI shows current token + waiting count
- Skip Patient → `TokenSkipped` event → UI updates queue order
- Pause/Resume → `SessionChanged` event → Full reload via API

All done in **real-time** (< 100ms) for optimal user experience!
