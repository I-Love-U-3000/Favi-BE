# Use Case 2.1.1: Sign In

**Module**: Authentication
**Primary Actor**: User (Guest / Authenticated)
**Backend Controller**: `AuthController`
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
| (2)-(3) | BR1 | **Submission:**<br>❖ **Frontend**: `LoginForm` calls `authApi.login({email, password})`.<br>❖ **API Call**: `POST /api/auth/login` with Body: `LoginDto { Email, Password }`.<br>❖ **Backend**: `AuthController.Login(dto)` invokes `_supabase.LoginAsync(email, password)`.<br>❖ **Service**: `SupabaseAuthService` sends `POST /auth/v1/token?grant_type=password` to Supabase Auth Server. |
| (3.1)-(4) | BR2 | **Validation (External):**<br>❖ **Supabase**: Verifies credentials against `auth.users` table.<br> **Invalid**: Returns `400 Bad Request` ("invalid_grant").<br> **Valid**: Returns `200 OK` with `access_token`, `refresh_token`, `user` object. |
| (5.2)-(6) | BR3 | **Completion:**<br>❖ **Backend**: `SupabaseAuthService` deserializes JSON to `SupabaseAuthResponse`.<br>❖ **Controller**: Returns `200 OK (SupabaseAuthResponse)`.<br>❖ **Frontend**: `authSlice` stores tokens in `localStorage`/`Redux State`. Redirects to `/home`. |
| (5.1)-(7) | BR_Error | **Exception Handling:**<br>❖If Supabase returns `400`: `AuthController` returns `401 Unauthorized` `{ code: "INVALID_CREDENTIALS", message: "Email hoặc mật khẩu không đúng." }`.<br>❖ **Frontend**: Displays error message toast to user. |

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
| (1)-(3) | BR1 | **Initiation:**<br>❖ **Frontend**: `LoginScreen` calls `supabase.auth.signInWithOAuth({ provider: 'google' })`.<br>❖ **SDK**: Redirects browser to `https://<project-ref>.supabase.co/auth/v1/authorize?provider=google`.<br>❖ **User Action**: User consents on Google Consent Screen. |
| (3.2)-(4) | BR2 | **Callback & Sync:**<br>❖ **Supabase**: Redirects to `SiteURL` with `access_token` and `refresh_token` in URL fragment.<br>❖ **Frontend**: `SupabaseAuthProvider` detects `onAuthStateChange`.<br>❖ **Backend (Sync)**: Supabase Webhook triggers `POST /api/profilessync/sync` (if configured) or Client calls `POST /api/auth/sync` (optional fallback).<br>❖ **Web App**: Persists session to `localStorage`. |
| (3.1)-(5) | BR_Error | **Exception:**<br>❖ **Frontend**: If `error` param present in URL or user cancels, show "Login Cancelled" notification. |

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
| (2)-(3) | BR1 | **Request:**<br>❖ **Frontend**: `ForgotPasswordForm` calls `supabase.auth.resetPasswordForEmail(email, { redirectTo: '.../reset-password' })`.<br>❖ **Supabase**: Sends generic recovery email with Magic Link.<br>❖ **Note**: No Backend API call needed for this step (purely client-SDK mediated). |
| (4) | BR2 | **Completion:**<br>❖ **User Interaction**: Clicks link in email -> Redirects to App with `access_token` (type=recovery).<br>❖ **Frontend**: Detects `recovery` event. Shows `ResetPasswordScreen`.<br>❖ **Action**: User enters new password. Frontend calls `supabase.auth.updateUser({ password: newPassword })`. |

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
| (1)-(2) | BR1 | **Action:**<br>❖ **Frontend**: User clicks Logout button.<br>❖ **SDK**: Calls `supabase.auth.signOut()`.<br>❖ **Cleanup**: Removes `sb-<ref>-auth-token` from LocalStorage. Clears Redux/Context state. |
| (3) | BR2 | **Redirect:**<br>❖ **Router**: Detects unauthenticated state. Redirects to `/login`. |

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
