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
| (2)-(3) | BR1 | **Submission:**<br>❖ The **Frontend** `LoginForm` captures the credentials and initiates a call to `authApi.login({email, password})`.<br>❖ The **API** receives a `POST` request at `/api/auth/login` containing the `LoginDto` payload.<br>❖ The **Backend** method `AuthController.Login(dto)` delegates the authentication logic to `_supabase.LoginAsync(email, password)`.<br>❖ The `SupabaseAuthService` sends a `POST` request to the external Supabase Auth Server at `/auth/v1/token` with `grant_type=password`. |
| (3.1)-(4) | BR2 | **Validation (External):**<br>❖ **Supabase** verifies the provided credentials against its `auth.users` table.<br> If **Invalid**, Supabase returns `400 Bad Request` with the error "invalid_grant".<br> If **Valid**, Supabase returns `200 OK` containing the `access_token`, `refresh_token`, and the `user` object. |
| (5.2)-(6) | BR3 | **Completion:**<br>❖ The **Backend** service `SupabaseAuthService` deserializes the JSON response into a `SupabaseAuthResponse` object.<br>❖ The **Controller** returns a `200 OK` response wrapping the auth data.<br>❖ The **Frontend** `authSlice` securely stores the tokens in `localStorage` or `Redux State` and redirects the **User** to the `/home` page. |
| (5.1)-(7) | BR_Error | **Exception Handling:**<br>❖ If Supabase returns a `400` error, the `AuthController` translates this to a `401 Unauthorized` response with the message "Email hoặc mật khẩu không đúng.".<br>❖ The **Frontend** displays an error message toast to the **User**. |

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
| (1)-(3) | BR1 | **Initiation:**<br>❖ The **Frontend** `LoginScreen` calls the SDK method `supabase.auth.signInWithOAuth({ provider: 'google' })`.<br>❖ The **SDK** redirects the browser to the Supabase authorization endpoint `https://<project-ref>.supabase.co/auth/v1/authorize?provider=google`.<br>❖ The **User** consents to the login on the Google Consent Screen. |
| (3.2)-(4) | BR2 | **Callback & Sync:**<br>❖ Top completion, **Supabase** redirects back to the `SiteURL` with the `access_token` and `refresh_token` in the URL fragment.<br>❖ The **Frontend** `SupabaseAuthProvider` detects the session change via `onAuthStateChange`.<br>❖ The **Backend** receives a Supabase Webhook event to `POST /api/profilessync/sync` (if configured) or the Client manually calls `POST /api/auth/sync` as a fallback to sync the profile.<br>❖ The **Web App** persists the session to `localStorage`. |
| (3.1)-(5) | BR_Error | **Exception:**<br>❖ If the `error` parameter is present in the URL or the user cancels the operation, the **Frontend** displays a "Login Cancelled" notification. |

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
| (2)-(3) | BR1 | **Request:**<br>❖ The **Frontend** `ForgotPasswordForm` calls `supabase.auth.resetPasswordForEmail(email, { redirectTo: '.../reset-password' })`.<br>❖ **Supabase** sends a generic recovery email containing a Magic Link to the user.<br>❖ **Note**: No Backend API call is needed for this step as it is handled entirely by the Client SDK. |
| (4) | BR2 | **Completion:**<br>❖ The **User** clicks the link in the email and is redirected back to the App with an `access_token` (type=recovery).<br>❖ The **Frontend** detects the `recovery` event and displays the `ResetPasswordScreen`.<br>❖ After the **User** enters a new password, the **Frontend** calls `supabase.auth.updateUser({ password: newPassword })` to finalize the change. |

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
| (1)-(2) | BR1 | **Action:**<br>❖ The **Frontend** detects the user clicking the Logout button and calls `supabase.auth.signOut()`.<br>❖ The **SDK** cleans up the session by removing the `sb-<ref>-auth-token` from `LocalStorage` and clearing the Redux/Context state. |
| (3) | BR2 | **Redirect:**<br>❖ The **Router** detects the unauthenticated state and redirects the **User** to the `/login` page. |

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
