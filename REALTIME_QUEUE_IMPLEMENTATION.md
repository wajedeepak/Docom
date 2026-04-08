# Real-Time Queue Update Implementation

## Overview

Your system uses **Microsoft SignalR** with a **group-based broadcast pattern** to send real-time queue updates to all connected clients watching a specific doctor's queue.

---

## Architecture Components

### 1. **Backend: SignalR Hub** 
**File:** [QueueHub.cs](backend/src/Docom.API/Hubs/QueueHub.cs)

```csharp
public class QueueHub : Hub
{
    // Patients/Doctors join group named by doctor's slug
    public async Task JoinDoctorQueue(string doctorSlug)
        => await Groups.AddToGroupAsync(Context.ConnectionId, doctorSlug.ToLowerInvariant());

    // Leave group when navigating away
    public async Task LeaveDoctorQueue(string doctorSlug)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, doctorSlug.ToLowerInvariant());
}
```

**Key Concept:** 
- Each **doctor** has a **group** named by their slug (e.g., `"dr-john-smith"`)
- When patients/doctors connect, they `JoinDoctorQueue("dr-john-smith")`
- All messages sent to this group are received by **all connected clients in that group**

---

### 2. **Backend: Queue Notifier Service**
**File:** [QueueNotifier.cs](backend/src/Docom.API/Hubs/QueueNotifier.cs)

Implements `IQueueNotifier` interface - broadcasts 4 types of real-time events:

```csharp
public Task NotifyTokenCreatedAsync(string doctorSlug, int tokenNumber, int queuePosition)
    => _hub.Clients.Group(doctorSlug.ToLowerInvariant())
           .SendAsync("TokenCreated", new { tokenNumber, queuePosition });

public Task NotifyQueueAdvancedAsync(string doctorSlug, int currentToken, int waitingCount)
    => _hub.Clients.Group(doctorSlug.ToLowerInvariant())
           .SendAsync("QueueAdvanced", new { currentToken, waitingCount, estimatedWaitMinutes = waitingCount * 5 });

public Task NotifyTokenSkippedAsync(string doctorSlug, int skippedToken, int currentToken)
    => _hub.Clients.Group(doctorSlug.ToLowerInvariant())
           .SendAsync("TokenSkipped", new { skippedToken, currentToken });

public Task NotifySessionChangedAsync(string doctorSlug, string status)
    => _hub.Clients.Group(doctorSlug.ToLowerInvariant())
           .SendAsync("SessionChanged", new { status });
```

---

## Flow 1: New Token Issuance

### Patient Takes Token from Link

```
PATIENT ACTION: Clicks "Take Token" on queue page
      ↓
[TokensController.cs - POST /api/sessions/{sessionId}/tokens]
      ↓
[TokenService.TakeTokenAsync(doctorSlug, patientDto)]
      
      Steps:
      1. Validate doctor is active
      2. Get active session
      3. Calculate waiting count
      4. Create new Token entity in database
      5. Generate public token ID
      6. ⭐ NOTIFY via IQueueNotifier
      ↓
[QueueNotifier.NotifyTokenCreatedAsync(doctorSlug, tokenNumber, queuePosition)]
      ↓
[SignalR HubContext broadcasts to group "doctor-slug"]
      ↓
REAL-TIME MESSAGE TO ALL CLIENTS:
{
  currentToken: 5,
  waitingCount: 12,
  estimatedWaitMinutes: 60
}
      ↓
[Frontend SignalRService receives "QueueAdvanced" event]
      ↓
[DoctorDashboard + QueuePage components subscribe & update UI]
```

### Key Code: Token Creation

**File:** [TokenService.cs - TakeTokenAsync()](backend/src/Docom.Application/Services/TokenService.cs#L38-L70)

```csharp
public async Task<TakeTokenResponseDto> TakeTokenAsync(string doctorSlug, TakeTokenDto dto)
{
    var doctor = await _doctorRepo.GetBySlugAsync(doctorSlug)
        ?? throw new KeyNotFoundException("Doctor not found.");

    // Validation...
    var waitingCount = await _tokenRepo.GetWaitingCountAsync(session.Id);

    // Create token in database
    var token = await _tokenRepo.CreateAsync(new Token
    {
        SessionId = session.Id,
        PatientName = dto.PatientName?.Trim(),
        PhoneNumber = dto.PhoneNumber?.Trim(),
        Status = TokenStatus.Waiting,           // New tokens start as 'Waiting'
        QueueOrder = waitingCount + 1,          // Queue order calculated atomically
        PublicTokenId = GeneratePublicTokenId() // Unique tracking ID
    });

    var position = waitingCount + 1;

    // ⭐ SEND REAL-TIME NOTIFICATION
    await _notifier.NotifyTokenCreatedAsync(doctorSlug, token.TokenNumber, position);

    return new TakeTokenResponseDto(
        TokenId: token.Id,
        TokenNumber: token.TokenNumber,
        CurrentTokenNumber: session.CurrentTokenNumber,
        QueuePosition: position,
        EstimatedWaitMinutes: position * 5,
        PublicTokenId: token.PublicTokenId   // Patient uses this for tracking
    );
}
```

---

## Flow 2: Queue Status Changes

### Doctor Advances Queue (NextPatient)

```
DOCTOR ACTION: Clicks "Next" button to serve next patient
      ↓
[TokensController.cs - POST /api/sessions/{sessionId}/tokens/next]
      ↓
[TokenService.NextPatientAsync(sessionId, doctorId)]
      
      Steps:
      1. Fetch current serving token (if exists)
      2. Mark current as TokenStatus.Completed
      3. Get next waiting token
      4. Mark next as TokenStatus.Serving
      5. Update session.CurrentTokenNumber = next.TokenNumber
      6. ⭐ NOTIFY all clients about advancement
      ↓
[QueueNotifier.NotifyQueueAdvancedAsync(doctorSlug, next.TokenNumber, waitingCount)]
      ↓
[SignalR broadcasts to group "doctor-slug"]
      ↓
REAL-TIME MESSAGE:
{
  currentToken: 6,        // Now serving token #6
  waitingCount: 11,       // One less patient waiting
  estimatedWaitMinutes: 55
}
      ↓
[All connected clients update their UI]
      - Doctor's dashboard: Shows token #6 now being served
      - Patient in position #1: Sees they're next
      - Other patients: Adjusted wait times
```

### Key Code: Queue Advancement

**File:** [TokenService.cs - NextPatientAsync()](backend/src/Docom.Application/Services/TokenService.cs#L75-L100)

```csharp
public async Task<TokenDto> NextPatientAsync(int sessionId, int doctorId)
{
    var session = await GetOwnedActiveSessionAsync(sessionId, doctorId);

    // Mark current patient as completed
    var current = await _tokenRepo.GetCurrentServingAsync(sessionId);
    if (current != null)
    {
        current.Status = TokenStatus.Completed;
        current.ServedAt = DateTime.UtcNow;
        await _tokenRepo.UpdateAsync(current);
    }

    // Get next waiting patient
    var next = await _tokenRepo.GetNextWaitingAsync(sessionId)
        ?? throw new InvalidOperationException("No more patients in queue.");

    // Serve the next patient
    next.Status = TokenStatus.Serving;    // Change status to Serving
    await _tokenRepo.UpdateAsync(next);

    // Update session with new current token
    session.CurrentTokenNumber = next.TokenNumber;
    await _sessionRepo.UpdateAsync(session);

    // Get waiting count for notification
    var waitingCount = await _tokenRepo.GetWaitingCountAsync(sessionId);
    var doctor = await _doctorRepo.GetByIdAsync(doctorId);

    // ⭐ BROADCAST TO ALL CLIENTS IN GROUP
    await _notifier.NotifyQueueAdvancedAsync(
        doctor!.Slug, 
        next.TokenNumber,  // New current token
        waitingCount       // Updated count
    );

    return MapToDto(next);
}
```

---

### Other Queue Status Changes

#### 1. Pause Session
**File:** [SessionService.cs - PauseSessionAsync()](backend/src/Docom.Application/Services/SessionService.cs#L45-L55)

```csharp
public async Task<SessionDto> PauseSessionAsync(int sessionId, int doctorId)
{
    var session = await GetOwnedSessionAsync(sessionId, doctorId);
    session.Status = SessionStatus.Paused;
    await _sessionRepo.UpdateAsync(session);

    var doctor = await _doctorRepo.GetByIdAsync(doctorId);
    await _notifier.NotifySessionChangedAsync(doctor!.Slug, "Paused");
    // ↑ Tells patients: "Queue is paused, can't take or advance tokens"

    return MapToDto(session);
}
```

#### 2. Skip Patient
**File:** [TokenService.cs - SkipPatientAsync()](backend/src/Docom.Application/Services/TokenService.cs#L117-L150)

```csharp
// Mark current as skipped
current.Status = TokenStatus.Skipped;
await _tokenRepo.UpdateAsync(current);

// Move skipped token to end of queue
await _tokenRepo.RequeueSkippedAsync(current.Id, sessionId);

// Move to next patient
var next = await _tokenRepo.GetNextWaitingAsync(sessionId);
next.Status = TokenStatus.Serving;
await _tokenRepo.UpdateAsync(next);

// ⭐ NOTIFY: Token #X was skipped, now serving token #Y
await _notifier.NotifyTokenSkippedAsync(
    doctor!.Slug,
    current.TokenNumber,     // The skipped one
    next?.TokenNumber ?? session.CurrentTokenNumber  // Now being served
);
```

#### 3. Resume Session
```csharp
session.Status = SessionStatus.Active;
await _sessionRepo.UpdateAsync(session);
await _notifier.NotifySessionChangedAsync(doctor!.Slug, "Active");
// ↑ Tells patients: "Queue is active again"
```

---

## Frontend: How Clients Receive & React

### SignalRService: Message Listener
**File:** [signalr.service.ts](frontend/docom-web/src/app/core/services/signalr.service.ts)

```typescript
conn.on('QueueAdvanced', (data: QueueAdvancedEvent) => {
  console.log('[SignalR] QueueAdvanced received:', data);
  this.queueAdvanced$.next(data);  // Emit to all subscribers
});

conn.on('TokenCreated', (data: TokenCreatedEvent) => {
  console.log('[SignalR] TokenCreated received:', data);
  this.tokenCreated$.next(data);
});

conn.on('TokenSkipped', (data: any) => {
  console.log('[SignalR] TokenSkipped received:', data);
  this.tokenSkipped$.next(data);
});

conn.on('SessionChanged', (data: SessionChangedEvent) => {
  console.log('[SignalR] SessionChanged received:', data);
  this.sessionChanged$.next(data);
});
```

### Doctor Dashboard: Reacts to Queue Changes
**File:** [dashboard.component.ts](frontend/docom-web/src/app/features/doctor/dashboard/dashboard.component.ts)

```typescript
// Subscribe to real-time queue advancement events
signalR.queueAdvanced$.subscribe(() => {
  console.log('Queue advanced! Refreshing...');
  this.loadQueueState(this.activeSession().id);
});

// Subscribe to new token events
signalR.tokenCreated$.subscribe(() => {
  console.log('New token created! Refreshing...');
  this.loadQueueState(this.activeSession().id);
});

// Load queue state from API
private loadQueueState(sessionId: number) {
  this.sessionService.getQueueState(sessionId).subscribe(state => {
    // Update Angular signals
    this.currentToken.set(state.currentTokenNumber);
    this.waitingCount.set(state.waitingTokens.length);
    this.tokens.set(state.waitingTokens);
  });
}
```

### Patient Queue Page: Dual Update Strategy
**File:** [queue-page.component.ts](frontend/docom-web/src/app/features/patient/queue-page/queue-page.component.ts)

```typescript
// ⭐ REAL-TIME UPDATE (Fast path)
signalR.queueAdvanced$.subscribe((event) => {
  // Update immediately without API call
  queueState.update(s => ({
    ...s,
    currentTokenNumber: event.currentToken,
    waitingCount: event.waitingCount,
    estimatedWaitMinutes: event.estimatedWaitMinutes
  }));
});

// FULL RELOAD (Consistency check)
signalR.sessionChanged$.subscribe(() => {
  // Session status changed (paused/resumed/ended)
  // Reload entire queue state from API
  this.loadQueueState();
});

// FALLBACK: Polling (Every 2 seconds)
interval(2000).pipe(
  switchMap(() => this.sessionService.getQueueState(this.doctorSlug())),
  takeUntilDestroyed()
).subscribe(state => {
  // Update if real-time somehow failed
  queueState.set(state);
});
```

---

## Real-Time Event Summary

| Event | Triggered | Data Sent | Used By |
|-------|-----------|-----------|---------|
| **TokenCreated** | Patient takes token | `{ tokenNumber, queuePosition }` | Doctor dashboard, other patients |
| **QueueAdvanced** | Doctor clicks "Next" | `{ currentToken, waitingCount, estimatedWaitMinutes }` | All connected clients |
| **TokenSkipped** | Doctor clicks "Skip" | `{ skippedToken, currentToken }` | All connected clients |
| **SessionChanged** | Session paused/resumed/ended | `{ status }` | Doctor & patients (triggers full reload) |

---

## Connection Flow

### 1. Doctor Starts Monitoring Queue
```
Doctor opens queue page
    ↓
[App loads DoctorDashboard component]
    ↓
[Component calls SignalRService.connectToDoctor("dr-john-smith")]
    ↓
[SignalR connects to wss://docom.in/api/hubs/queue]
    ↓
[Invokes: conn.invoke('JoinDoctorQueue', 'dr-john-smith')]
    ↓
[Backend QueueHub.JoinDoctorQueue() adds connection to group]
    ↓
Doctor is now in group "dr-john-smith" and receives all broadcasts
```

### 2. Patient Clicks Tracking Link
```
Patient clicks link: https://docom.in/t/{publicTokenId}
    ↓
[TrackingResolver resolves publicTokenId to doctor slug]
    ↓
[Redirects to queue page with doctor slug]
    ↓
[QueuePage connects via SignalRService.connectToDoctor(slug)]
    ↓
Patient joins same group as doctor
    ↓
Both receive same real-time updates
```

---

## Why It Works

### ✅ Advantages
1. **Instant updates**: < 100ms latency via WebSocket
2. **Group-based**: No need to broadcast to all users, only relevant doctor's queue
3. **Low bandwidth**: Only small JSON messages with deltas
4. **Automatic fallback**: WebSocket → ServerSentEvents → Long Polling
5. **Reconnection handling**: Auto-rejoin group on reconnect

### ⚠️ Current Production Issue
In production, **WebSocket may fail**, causing fallback to **Long Polling every 2 seconds**:
- Updates appear delayed (2-second minimum)
- Higher server load
- Visible UI lag

**Solution:** Ensure IIS WebSocket support is enabled (see separate guide)

---

## Message Flow Example

```
TIME: 14:32:50
  Doctor clicks "Next" button on dashboard
    ↓
  [Backend] TokenService.NextPatientAsync() executes
        - Completes current token #5
        - Starts serving token #6
        - Waiting count: 11
    ↓
  [Backend] QueueNotifier.NotifyQueueAdvancedAsync('dr-john', 6, 11)
    ↓
  [SignalR Hub] Broadcasts to group 'dr-john':
    {
      type: "QueueAdvanced",
      currentToken: 6,
      waitingCount: 11,
      estimatedWaitMinutes: 55
    }
    ↓
TIME: 14:32:50 + 15ms (via WebSocket)
  [Browser #1] Doctor's dashboard receives message
    - DashboardComponent.queueAdvanced$.subscribe() fires
    - Calls loadQueueState()
    - currentToken signal updates to 6
    - UI re-renders: "Now Serving: Token #6"
    ↓
TIME: 14:32:50 + 18ms (via WebSocket)
  [Browser #2] Patient's tracking page receives message
    - QueuePageComponent.queueAdvanced$.subscribe() fires
    - queueState updates directly
    - waitingCount updates from 12 to 11
    - estimatedWaitMinutes updates from 60 to 55
    - UI shows: "Wait time: ~55 minutes" ← UPDATE VISIBLE
```

---

## Testing Real-Time Updates

### Browser DevTools Check
```
1. Open F12 → Network tab
2. Filter by "WS" (WebSocket)
3. Look for: wss://docom.in/api/hubs/queue
4. Click as activity happens
5. Messages tab shows real-time data
```

### Console Logging
```
[SignalR] Connected. Joining group: dr-john-smith
[SignalR] QueueAdvanced received: {currentToken: 6, waitingCount: 11, ...}
[SignalR] TokenCreated received: {tokenNumber: 101, queuePosition: 5}
```

---

## Summary

Your real-time system architecture:

1. **Backend**: Services call `IQueueNotifier` when queue changes
2. **SignalR Hub**: Broadcasts to group named by doctor slug  
3. **Frontend**: Subscribes to RxJS subjects (`queueAdvanced$`, `tokenCreated$`, etc)
4. **Components**: React to signals and update Angular state/signals
5. **Fallback**: 2-second polling if WebSocket unavailable

This ensures all clients watching the same doctor's queue see updates within milliseconds.
