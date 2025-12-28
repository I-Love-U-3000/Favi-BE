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
| (1) | BR1 | **Display:**<br>❖ The **System** displays the full profile details to the **User**.<br>❖ The **System** presents relevant options such as Edit, Privacy, or Delete (for Authenticated Users), or Register (for Guests). |
| (2) | BR2 | **Routing:**<br>❖ Based on the **User's** specific selection, the **System** routes the request to and invokes the specific sub-use cases involved. |

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
| (2)-(3) | BR1 | **Submission & Validation:**<br>❖ The **Frontend** component `RegisterForm` captures inputs and calls `authApi.register({email, password, username})`.<br>❖ The **Backend** `AuthController.Register(dto)` invokes `_profiles.CheckValidUsername(dto.Username)` to validate the input.<br>❖ The **Logic** verifies uniqueness by querying the `Profiles` table. If the username exists, the **System** returns a `409 Conflict` error. |
| (3.1)-(4) | BR2 | **Supabase Registration:**<br>❖ The **Service** `SupabaseAuthService` initiates a call to `POST /auth/v1/signup`, passing the payload `{email, password, data: {username}}`.<br>❖ **Supabase** creates the user in the `auth.users` table and returns `200 OK` with the User Object.<br>❖ If the operation fails, the **System** returns a `400 Bad Request` error. |
| (5) | BR3 | **Profile Sync (Webhook):**<br>❖ **Supabase** triggers a `POST` request to the configured Webhook URI `/api/profilessync/sync`.<br>❖ The **Backend** `ProfilesSyncController.SyncProfile(dto)` receives the `SupabaseUserCreatedDto` payload.<br>❖ The **Database** `ProfileService.CreateProfileAsync` inserts a new row into the `Profiles` table using `Id=user_id`, `Username`, and `DisplayName`. |
| (4.2.2)-(6) | BR4 | **Completion:**<br>❖ The **Backend** `AuthController` returns a `200 OK` response containing the `SupabaseAuthResponse`.<br>❖ The **Frontend** securely stores the session tokens and redirects the **User** to the `/home` page. |

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
| (2)-(3) | BR1 | **Processing:**<br>❖ The **Frontend** initiates `profileApi.updateProfile({displayName, bio, ...})`.<br>❖ The **API** routes the `PUT` request to `/api/profiles`.<br>❖ The **Backend** controller `ProfilesController.Update` delegates the operation to `_profiles.UpdateAsync(userId, dto)`. |
| (3.2)-(4) | BR2 | **Storage:**<br>❖ The **Database** mechanism `UnitOfWork.Profiles.GetByIdAsync(id)` retrieves the entity, updates the specific fields, and calls `UnitOfWork.CompleteAsync()` to persist changes.<br>❖ The **System** automatically updates the `LastActiveAt` timestamp during this process. |
| (3.2.2)-(5) | BR3 | **Completion:**<br>❖ The **System** returns a `200 OK` response including the updated `ProfileResponse`.<br>❖ The **Frontend** updates the Redux store at `user/profile` and displays a success toast to the **User**. |
| (3.2.1)-(6) | BR_Error | **Exception:**<br>❖ If the Profile is not found (which is unlikely in this context), the **System** returns `404 NotFound`.<br>❖ If a database error occurs, the **System** returns `500 Internal Server Error`. |

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
| (2)-(3) | BR1 | **Confirmation:**<br>❖ The **Frontend** displays a confirmation modal asking "Are you sure?".<br>❖ Upon confirmation, the **API** receives a `DELETE` request at `/api/profiles`. |
| (3.2)-(4) | BR2 | **Processing:**<br>❖ The **Backend** controller `ProfilesController.Delete()` executes `_profiles.DeleteAsync(userId)`.<br>❖ The **Database** executes `UnitOfWork.Profiles.Remove(profile)`, which normally performs a hard delete unless Entity Framework is configured for Soft Deletes. |
| (3.2.1)-(5) | BR3 | **Completion:**<br>❖ The **System** responds with `200 OK` and the message "Đã xoá tài khoản.".<br>❖ The **Frontend** invokes the `logout()` function and redirects the **User** to the Login screen. |

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
| (2)-(3) | BR1 | **Search:**<br>❖ The **Frontend** component `SearchInput` debounces the user input and calls `searchApi.search({ query, type: 'user' })`.<br>❖ The **API** receives a `POST` request at `/api/search` with the `SearchRequest` body.<br>❖ The **Backend** `SearchController.Search` calls `_search.SearchAsync`.<br>❖ The **Logic** in `SearchService` executes a query on `Profiles`, finding matches where (`Username` contains query OR `DisplayName` contains query). |
| (3.2)-(4) | BR2 | **Result:**<br>❖ The **System** returns a `200 OK` response containing a `SearchResult`, which is a list of Profiles.<br>❖ The **Frontend** renders the resulting list of `ProfileCard` components. |
| (3.1)-(5) | BR_Error | **Exception:**<br>❖ If the result is empty, the **System** returns `200 OK` with an empty list. If an error occurs, it returns `500`. |

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
| (2)-(3) | BR1 | **Query:**<br>❖ The **API** receives a `GET` request at `/api/profiles/{id}`.<br>❖ The **Backend** controller `ProfilesController.GetById(id)` calls `_profiles.GetEntityByIdAsync(id)`. |
| (4)-(5) | BR2 | **Privacy & Relationships:**<br>❖ The **Privacy** guard `_privacy.CanViewProfileAsync(profile, viewerId)` checks the target's `PrivacyLevel` and the Follow status.<br>❖ The **Service** `_profiles.GetByIdAsync` fetches Follower/Following counts from the `Follows` table.<br>❖ The **System** returns a `200 OK` reply with the `ProfileResponse` data. |
| (5.1)-(6) | BR_Error | **Exception:**<br>❖ If the profile is null, the **System** returns `404 NotFound` with code `PROFILE_NOT_FOUND`.<br>❖ If access is restricted, the **System** returns `403 Forbidden` with code `PROFILE_FORBIDDEN`. |

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
| (2)-(3) | BR1 | **Processing:**<br>❖ The **Frontend** `PrivacySettings` component initiates `updateProfile({ privacyLevel: 'Private' })`.<br>❖ The **API** reuses the endpoint `PUT /api/profiles`.<br>❖ The **Backend** logic in `_profiles.UpdateAsync` maps the `PrivacyLevel` from the DTO to the Entity. |
| (3.2)-(4) | BR2 | **Completion:**<br>❖ The **Database** updates the `Profiles.PrivacyLevel` field.<br>❖ The **System** returns `200 OK` with the updated profile data. |

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
