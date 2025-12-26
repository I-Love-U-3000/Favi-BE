# Use Case 2.1.1: Sign In

**Module**: Authentication
**Primary Actor**: User (Guest / Authenticated)
**Backend Controller**: `Favi_BE.API.Controllers.AuthController`
**Database Tables**: `Profiles` (Read-Only via Sync), Supabase Auth (External)

---

## 2.1.1.1 Sign In (Email/Password)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Sign In (Email/Password)** |
| **Description** | Authenticate user using Supabase credentials. |
| **Actor** | Guest |
| **Trigger** | ❖ User clicks [btnLogin] on the Login Screen. |
| **Pre-condition** | ❖ User account exists in Supabase. |
| **Post-condition** | ❖ System returns Supabase Access/Refresh Tokens.<br>❖ User is redirected to Home. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Submission:**<br>❖ User submits credentials (Step 1).<br>❖ System calls `Login(LoginDto)` (Step 2).<br>❖ System forwards request to Supabase Auth (Step 3). |
| (3.1)-(4) | BR2 | **Validation (External):**<br>❖ Supabase verifies credentials (Step 3).<br> **Invalid**: Returns 400/401 (Step 3.1).<br> **Valid**: Returns Session/Tokens (Step 4). |
| (5.2)-(6) | BR3 | **Completion:**<br>❖ Backend returns `SupabaseAuthResponse` (Step 5.2).<br>❖ UI saves tokens (Step 6). |
| (5.1)-(7) | BR_Error | **Exception Handling:**<br>If Supabase unavailable: Log Error (Step 5.1), Return 500, Show Error (Step 7). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Guest|
start
:(1) Submit Credentials;
|System|
:(2) POST /api/auth/login;
|Supabase Auth|
:(3) Verify /token?grant_type=password;
if (Valid?) then (No)
  :(3.1) Return 400/401 Error;
  |System|
  :(3.1.1) Return Unauthorized;
  |Guest|
  :(4) Show Error;
  stop
else (Yes)
  :(3.2) Return Access & Refresh Tokens;
  |System|
  :(5) Process Response;
  if (Success?) then (Yes)
      :(5.2) Return 200 OK (Tokens);
      |Guest|
      :(6) Save Tokens & Redirect;
  else (No)
      |System|
      :(5.1) Log Error;
      |Guest|
      :(7) Show System Error;
  endif
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Guest" as User
boundary "LoginScreen" as View
control "AuthController" as Controller
participant "SupabaseAuthService" as Service
participant "Supabase API" as External

User -> View: Submit(Email, Pass)
activate View
View -> Controller: Login(dto)
activate Controller
Controller -> Service: LoginAsync(email, pass)
activate Service
Service -> External: POST /token?grant_type=password
activate External

alt Invalid Credentials
    External --> Service: 400 Bad Request
    Service --> Controller: null
    Controller --> View: 401 Unauthorized
    View -> User: Show Error
else Valid Credentials
    External --> Service: 200 OK (Session)
    deactivate External
    Service --> Controller: SupabaseAuthResponse
    deactivate Service
    Controller --> View: 200 OK
    deactivate Controller
    View -> View: Save Tokens
    View -> User: Redirect Home
end
deactivate View
@enduml
```

---

## 2.1.1.2 Sign In (OAuth - Google)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Sign In (OAuth - Google)** |
| **Description** | Authenticate via Google using Supabase Client SDK. |
| **Actor** | Guest |
| **Trigger** | ❖ User clicks [Sign in with Google]. |
| **Pre-condition** | ❖ Google Auth enabled in Supabase Project. |
| **Post-condition** | ❖ Supabase Session created.<br>❖ Webhook syncs Profile (if new). |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1)-(3) | BR1 | **Initiation:**<br>❖ User clicks Google Login (Step 1).<br>❖ Client SDK redirects to Supabase/Google (Step 2).<br>❖ User grants permission (Step 3). |
| (3.2)-(4) | BR2 | **Callback:**<br>❖ Google redirects back to App with Tokens (Step 3.2).<br>❖ Client SDK persists Session (Step 4). |
| (3.1)-(5) | BR_Error | **Exception:**<br>If User denies/Provider fails: Log Error (Step 3.1). Show Error (Step 5). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Guest|
start
:(1) Click "Sign in with Google";
|Supabase Auth (Client SDK)|
:(2) Redirect to Google OAuth;
|Google Provider|
:(3) Request Permissions;
if (User Grants?) then (No)
  :(3.1) Return Error / Denied;
  |Guest|
  :(5) Show Error;
  stop
else (Yes)
  :(3.2) Return ID Token;
  |Supabase Auth (Client SDK)|
  :(4) Create Session & Persist Tokens;
  |Guest|
  :(6) Redirect to Home;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Guest" as User
boundary "LoginScreen" as View
participant "Supabase SDK" as Client
participant "Google API" as Provider

User -> View: Click Google
View -> Client: signInWithOAuth(Google)
Client -> View: Redirect URL
View -> Provider: Navigate to Auth Page
Provider -> User: Prompt Consent
User -> Provider: Accept
Provider -> View: Callback (Tokens)
View -> Client: setSession(Tokens)
View -> User: Redirect Home
@enduml
```

---

## 2.1.1.4 Reset Password

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Reset Password (Creative)** |
| **Description** | Reset password via Supabase recovery flow. |
| **Actor** | Guest |
| **Trigger** | ❖ User clicks "Forgot Password". |
| **Pre-condition** | ❖ Email exists in Supabase. |
| **Post-condition** | ❖ Password updated. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Request:**<br>❖ User enters Email (Step 1).<br>❖ System calls `ResetPassword(email)` (Creative) (Step 2).<br>❖ Supabase sends email (Step 3). |
| (4) | BR2 | **Completion:**<br>❖ System returns OK (Step 4). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Guest|
start
:(1) Request Password Reset;
|System|
:(2) POST /api/auth/reset-password;
|Supabase Auth|
:(3) Trigger Recovery Email;
if (Success?) then (Yes)
  |System|
  :(3.2) Return OK;
  |Guest|
  :(4) Check Email;
else (No)
  |System|
  :(3.1) Return Error;
  |Guest|
  :(5) Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Guest" as User
control "AuthController" as Controller
participant "Supabase API" as External

User -> Controller: Request Reset
Controller -> External: POST /recover
External --> Controller: OK
Controller --> User: 200 OK
@enduml
```

---

## 2.1.1.6 Logout

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Logout** |
| **Description** | Clear session. |
| **Actor** | Authenticated User |
| **Note** | **Client-Side Only**. No Backend Endpoint found in codebase. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Action:**<br>User clicks Logout. |
| (2) | BR2 | **Cleanup:**<br>Client clears LocalStorage/Cookies. Redirects to Login. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Click Logout;
:(2) Clear Tokens (Local Storage);
:(3) Redirect to Login;
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "Client App" as View

User -> View: Click Logout
View -> View: Clear Tokens (LocalStorage)
View -> User: Redirect Login
@enduml
```
