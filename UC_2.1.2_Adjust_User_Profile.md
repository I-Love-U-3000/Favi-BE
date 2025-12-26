# Use Case 2.1.2: Adjust User Profile

**Module**: User Management
**Primary Actor**: User (Guest / Authenticated)
**Backend Controller**: `AuthController`, `ProfilesController`, `ProfilesSyncController`
**Database Tables**: `Profiles`, `Follows`, `Supabase Auth`

---

## 2.1.2.1 Adjust User Profile (Overview)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Adjust User Profile** |
| **Description** | General profile management dashboard allows user to view their profile and trigger creation, updates, deletion, or privacy settings. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User navigates to "My Profile". |
| **Post-condition** | ❖ User performs one of the sub-actions. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Display:**<br>❖ System displays profile details.<br>❖ System presents options: Edit, Privacy, Delete (if Auth), or Register (if Guest). |
| (2) | BR2 | **Routing:**<br>❖ Based on user selection, system invokes specific sub-use cases. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|User|
start
:(1) Display Profile View;
:(2) Choose Function;
split
    -> Create (Sign Up);
    :(2.1) Activity\nCreate User Profile;
split again
    -> Update;
    :(2.2) Activity\nUpdate User Profile;
split again
    -> Delete;
    :(2.3) Activity\nDelete User Profile;
split again
    -> Privacy;
    :(2.4) Activity\nManage Privacy;
split again
    -> Search;
    :(2.5) Activity\nSearch Profiles;
end split
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "ProfileView" as View
control "ProfilesController" as Controller

User -> View: Open Profile
View -> Controller: GetMyProfile()
activate Controller
Controller --> View: ProfileDto
deactivate Controller
View -> User: Display Dashboard

opt Create / Sign Up
    ref over User, View, Controller: Sequence Create User Profile
end

opt Update
    ref over User, View, Controller: Sequence Update User Profile
end

opt Delete
    ref over User, View, Controller: Sequence Delete User Profile
end

opt Privacy
    ref over User, View, Controller: Sequence Manage Privacy
end
@enduml
```

---

## 2.1.2.2 Create User Profile (Sign Up)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Create User Profile (Sign Up)** |
| **Description** | Register a new account via Supabase (Backend Proxy). Profile is created via Webhook. |
| **Actor** | Guest |
| **Trigger** | ❖ User clicks [btnRegister] on the Sign Up Screen. |
| **Pre-condition** | ❖ Email must not already exist in Supabase. |
| **Post-condition** | ❖ User created in Supabase.<br>❖ Profile synced to local DB.<br>❖ User receives session tokens. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Submission:**<br>❖ User submits registration form (Step 1).<br>❖ System calls `Register(RegisterDto)` (Step 2).<br>❖ System checks Username validity (Step 3). |
| (3.1)-(4) | BR2 | **Supabase Registration:**<br>❖ System calls `Supabase.RegisterAsync` (Step 4).<br> **Fail**: Return 400 (Step 4.1).<br> **Success**: Proceed to Step (4.2). |
| (5) | BR3 | **Profile Sync (Webhook):**<br>❖ Supabase triggers Webhook `SyncProfile` (Step 5).<br>❖ System creates `Profile` record (Step 5.1). |
| (4.2.2)-(6) | BR4 | **Completion:**<br>❖ Backend returns `SupabaseAuthResponse` (Step 4.2.2).<br>❖ Client redirects (Step 6). |
| (3.1)-(7) | BR_Error | **Exception:**<br>If Registration fails: Log Error (Step 4.1). Show Error (Step 7). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Guest|
start
:(1) Submit Sign Up;
|System|
:(2) POST /api/auth/register;
:(3) Check Username Availability;
if (Available?) then (No)
  :(3.1) Return Conflict;
  stop
else (Yes)
  |Supabase Auth|
  :(4) Register User;
  if (Success?) then (Yes)
      |System|
      :(4.2) Return Success;
      fork
        |System|
        :(4.2.1) Return 200 OK to Client;
        |Guest|
        :(6) Redirect Home;
      fork again
        |Supabase Auth|
        :(5) Trigger "SyncProfile" Webhook;
        |System|
        :(5.1) INSERT INTO "Profiles";
      end fork
  else (No)
      |System|
      :(4.1) Log Error;
      |Guest|
      :(7) Show Error;
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
boundary "RegisterScreen" as View
control "AuthController" as Controller
control "ProfilesSyncController" as Webhook
participant "Supabase API" as External
entity "Profiles" as DB_Profile

User -> View: Submit(Email, Username, Pass)
View -> Controller: Register(dto)
activate Controller

Controller -> Controller: CheckUsername(username)
alt Username Taken
    Controller --> View: 409 Conflict
else Valid
    Controller -> External: RegisterAsync(email, pass, meta)
    activate External

    par Supabase Auth Response
        External --> Controller: SupabaseAuthResponse
        Controller --> View: 200 OK (Tokens)
        View -> User: Redirect Home
    else Webhook Sync
        External -> Webhook: POST /api/profilessync/sync
        activate Webhook
        Webhook -> DB_Profile: Insert Profile
        activate DB_Profile
        DB_Profile --> Webhook: Success
        deactivate DB_Profile
        Webhook --> External: 200 OK
        deactivate Webhook
    end

    deactivate External
    deactivate Controller
end
@enduml

```

---

## 2.1.2.3 Update User Profile

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Update User Profile** |
| **Description** | Edit Avatar, Bio, or Cover Photo. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks Save Changes in Edit Mode. |
| **Post-condition** | ❖ Profile updated in DB. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Processing:**<br>❖ System calls `UpdateProfile(dto)` (Step 2).<br>❖ System validates input (Step 3). |
| (3.2)-(4) | BR2 | **Storage:**<br>❖ System updates `Profiles` table (Step 3.2).<br>❖ System changes `UpdatedAt` timestamp (Step 4). |
| (3.2.2)-(5) | BR3 | **Completion:**<br>❖ Return Updated Dto (Step 3.2.2).<br>❖ UI Reflects changes (Step 5). |
| (3.2.1)-(6) | BR_Error | **Exception:**<br> DB Error: Log (Step 3.2.1). Return 500. Show Error (Step 6). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Submit Changes;
|System|
:(2) PUT /api/profiles/me;
|Database|
:(3) UPDATE "Profiles";
if (Success?) then (Yes)
  |System|
  :(3.2) Return Updated Profile;
  |Authenticated User|
  :(4) Show Success;
else (No)
  |System|
  :(3.1) Log Error & Return 500;
  |Authenticated User|
  :(5) Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "EditProfileView" as View
control "ProfilesController" as Controller
entity "Profiles" as DB

User -> View: Save Changes
View -> Controller: UpdateProfile(dto)
activate Controller
Controller -> DB: Update
activate DB
alt Success
    DB --> Controller: Done
    deactivate DB
    Controller --> View: 200 OK
    deactivate Controller
    View -> User: Show Updated Profile
else Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: LogError
    Controller --> View: 500 Error
    View -> User: Show Error
end
@enduml
```

---

## 2.1.2.4 Delete User Profile

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Delete User Profile** |
| **Description** | Soft delete the user account. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User confirms "Deactivate Account". |
| **Post-condition** | ❖ `IsDeleted` = True in DB.<br>❖ Supabase User disabled/deleted. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Confirmation:**<br>❖ User requests deletion (Step 1).<br>❖ System calls `Info` to confirm intent (Step 2). |
| (3.2)-(4) | BR2 | **Processing:**<br>❖ System calls `DeleteUser()` (Step 3.2).<br>❖ System updates `Profiles` -> `IsDeleted=1` (Step 4). |
| (3.2.1)-(5) | BR3 | **Completion:**<br>❖ System logs out user (Step 3.2.1).<br>❖ Redirect to Login (Step 5). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Request Deactivation;
|System|
:(2) DELETE /api/auth/user;
|Database|
:(3) UPDATE "Profiles" SET IsDeleted=1;
if (Success?) then (Yes)
  |System|
  :(3.2) Sign Out User;
  :(3.2.1) Return OK;
  |Authenticated User|
  :(4) Redirect Login;
else (No)
  |System|
  :(3.1) Log Error;
  |Authenticated User|
  :(5) Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
control "AuthController" as Controller
entity "Profiles" as DB
participant "Supabase" as Auth

User -> Controller: Delete Account
activate Controller
Controller -> DB: Soft Delete Profile
activate DB
DB --> Controller: Success
deactivate DB
Controller -> Auth: Disable/Delete User
Controller --> User: 200 OK (Logout)
deactivate Controller
@enduml
```

---

## 2.1.2.5 Search User Profile

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Search User Profile** |
| **Description** | Find users by name. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User types in search bar. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Search:**<br>❖ System calls `SearchProfiles(query)` (Step 2).<br>❖ System executes LIKE query on “Profiles” (Step 3). |
| (3.2)-(4) | BR2 | **Result:**<br>❖ System returns List (Step 3.2).<br>❖ UI shows results (Step 4). |
| (3.1)-(5) | BR_Error | **Exception:**<br>DB Error (Step 3): Log (Step 3.1). Show Error (Step 5). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Type Query;
|System|
:(2) GET /api/profiles/search?q=...;
|Database|
:(3) SELECT FROM "Profiles" WHERE Name LIKE...;
if (Success?) then (Yes)
  |System|
  :(3.2) Return Results;
  |Authenticated User|
  :(4) Show List;
else (No)
  |System|
  :(3.1) Log Error;
  |Authenticated User|
  :(5) Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
control "ProfilesController" as Controller
entity "Profiles" as DB

User -> Controller: Search(query)
activate Controller
Controller -> DB: LIKE Query
activate DB
alt Success
    DB --> Controller: List<Profile>
    Controller --> User: List<ProfileDto>
else Error
    DB --> Controller: Exception
    Controller --> User: 500 Error
end
deactivate DB
@enduml
```

---

## 2.1.2.6 View Other User Profile

### Use Case Description
| Attribute | Details |
| :---: | :---: |
| **Name** | **View Other User Profile** |
| **Description** | View details of another user. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks on a username/avatar. |
| **Pre-condition** | ❖ Target user exists. |
| **Post-condition** | ❖ Profile details displayed. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Query:**<br>❖ System calls `GetProfile(username)` (Step 2).<br>❖ System queries “Profiles” table (Step 3). |
| (4)-(5) | BR2 | **Relationships:**<br>❖ System checks “Follows” table (Step 4).<br>❖ System returns Dto (Step 5). |
| (5.1)-(6) | BR_Error | **Exception:**<br>If DB Fail (Step 3): Log (Step 3.1). Return 500. Show Error (Step 6). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Click User Link;
|System|
:(2) GET /api/profiles/{username};
|Database|
:(3) SELECT FROM "Profiles";
if (Found?) then (Yes)
  :(4) Check "Follows" Status;
  |System|
  :(5) Return Profile Dto;
  |Authenticated User|
  :(6) View Profile;
else (No)
  |System|
  :(3.1) Return 404 Not Found;
  |Authenticated User|
  :(7) Show 404;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
control "ProfilesController" as Controller
entity "Profiles" as DB
entity "Follows" as DB_Follow

User -> Controller: GetProfile(username)
activate Controller
Controller -> DB: FindByUsername
activate DB
alt Found
    DB --> Controller: Profile
    Controller -> DB_Follow: CheckFollowStatus
    DB_Follow --> Controller: Status
    Controller --> User: ProfileDto
else Not Found
    DB --> Controller: null
    Controller --> User: 404 Not Found
end
deactivate DB
@enduml
```

---

## 2.1.2.7 Manage Privacy Settings

### Use Case Description
| Attribute | Details |
| :---: | :---: |
| **Name** | **Manage Privacy Settings** |
| **Description** | Toggle Public/Private profile status. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User toggles privacy switch. |
| **Post-condition** | ❖ `IsPrivate` column updated. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Processing:**<br>❖ User toggles switch (Step 1).<br>❖ System calls `UpdatePrivacy(bool)` (Step 2).<br>❖ System Updates DB (Step 3). |
| (3.2)-(4) | BR2 | **Completion:**<br>❖ Return OK (Step 3.2).<br>❖ Show Toast (Step 4). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Toggle Privacy;
|System|
:(2) PATCH /api/profiles/privacy;
|Database|
:(3) UPDATE "Profiles" SET IsPrivate=val;
if (Success?) then (Yes)
  |System|
  :(3.2) Return OK;
  |Authenticated User|
  :(4) Show Success;
else (No)
  |System|
  :(3.1) Log Error;
  |Authenticated User|
  :(5) Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
control "ProfilesController" as Controller
entity "Profiles" as DB

User -> Controller: SetPrivacy(isPrivate)
activate Controller
Controller -> DB: Update
activate DB
DB --> Controller: Success
deactivate DB
Controller --> User: 200 OK
deactivate Controller
@enduml
```
