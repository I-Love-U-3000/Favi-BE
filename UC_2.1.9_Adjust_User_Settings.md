# Use Case 2.1.9: Adjust User Settings

**Module**: Settings / Profile
**Primary Actor**: Authenticated User
**Backend Controller**: `Favi_BE.API.Controllers.ProfilesController`
**Database Tables**: `"Profiles"`, `"SocialLinks"`

---

## 2.1.9.1 Adjust User Settings (Overview)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Adjust User Settings** |
| **Description** | Central hub for managing profile details, account security, and preferences. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User enters the "Settings" or "Edit Profile" section. |
| **Post-condition** | ❖ User updates profile or account settings. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Initialization:**<br>❖ The System fetches the current user's profile and settings.<br>❖ The System provides navigation to Update Profile, Social Links, Privacy, or Delete Account. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|User|
start
:(1) View Settings Hub;
:Choose Function;
split
    -> Update Info;
    :(2.1) Activity\nUpdate Profile Info;
split again
    -> Social Links;
    :(2.2) Activity\nUpdate Social Links;
split again
    -> Privacy;
    :(2.3) Activity\nUpdate Privacy;
split again
    -> Notifications;
    :(2.4) Activity\nConfigure Notifications;
split again
    -> Delete;
    :(2.5) Activity\nDelete Account;
end split
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "SettingsHub" as View
control "ProfilesController" as Controller

User -> View: Open Settings
View -> Controller: GetMyProfile()
activate Controller
Controller --> View: ProfileDto
deactivate Controller
View -> User: Display Settings

opt Update Info
    ref over User, View, Controller: Sequence Update Profile
end

opt Social Links
    ref over User, View, Controller: Sequence Update Social Links
end

opt Delete Account
    ref over User, View, Controller: Sequence Delete Account
end
@enduml
```

---

## 2.1.9.2 Update Profile Information

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Update Profile Information** |
| **Description** | Edit Bio, Name, or Avatar. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks the "Save" button on the Edit Profile form. |
| **Pre-condition** | ❖ User is logged in. |
| **Post-condition** | ❖ Profile information (Name, Bio, Avatar) is updated in the database. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(2.1) | BR1 | **Validation Workflow:**<br>❖ System evaluates input data constraints (Name length, URL formats) (Step 2).<br> **Invalid**: System returns `400 Bad Request` (Step 2.1) and displays error messages (Refer to MSG_ERR_VALIDATION) (Step 6).<br> **Valid**: System moves to step (2.2) to persist changes. |
| (2.2)-(3) | BR2 | **Processing & Storing Rules:**<br>❖ System calls method `UpdateProfile(dto)` (Step 2.2).<br>❖ System executes syntax `UPDATE Profiles SET Name=[Name], ... WHERE Id=[User.ID]` on table “Profiles” (Refer to “Profiles” table in “DB Sheet” file) (Step 3).<br>❖ System validates foreign key constraints (if any). |
| (3.1)-(4) | BR3 | **Displaying Rules:**<br>❖ After updating data, System returns the Entity (Step 3.1).<br>❖ System displays a successful notification (Refer to MSG_SUCCESS_PROFILE_UPDATED).<br>❖ System refreshes the “Profile” view (Refer to “Profile” view in “View Description” file) with the new information (Step 4). |
| (3.2)-(5) | BR_Error | **Exception Handling Rules:**<br>If a system failure occurs, the Global Exception Handler logs the error (Step 3.2) and returns a `500 Internal Server Error`.<br>System shows error (Step 5). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:Edit Profile;
:(1) Click Save;
|System|
:(2) Validate Input;
if (Valid?) then (Yes)
  :(2.2) Proceed;
  |Database|
  :(3) UPDATE "Profiles";
  if (Save Success?) then (Yes)
      |System|
      :(3.1) Return Entity;
      |Authenticated User|
      :(4) Profile Updated;
  else (No)
      |System|
      :(3.2) Log Error & Return 500;
      |Authenticated User|
      :(5) Show Error;
  endif
else (No)
  :(2.1) Return Error;
  |Authenticated User|
  :(6) Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "EditProfileModal" as View
control "ProfilesController" as Controller
entity "Profiles" as DB

User -> View: Click Save
View -> Controller: Update(ProfileUpdateDto)
activate Controller
Controller -> Controller: Validate()
Controller -> DB: Find(userId)
activate DB
DB --> Controller: Entity
deactivate DB
Controller -> DB: Update Properties
Controller -> DB: SaveChanges()
alt Success
    Controller --> View: 200 OK + Updated Profile
    deactivate Controller
    View -> User: Close Modal & Refresh
else Database Error
    Controller --> View: 500 Error
    deactivate Controller
    View -> User: Show Error Message
end
@enduml
```

---

## 2.1.9.3 Update Social Links

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Update Social Links** |
| **Description** | Add external links (Facebook, Twitter) to profile. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks "Add Link" in profile settings. |
| **Pre-condition** | ❖ User has a valid URL to add. |
| **Post-condition** | ❖ New link is saved to "SocialLinks" table.<br>❖ Link appears on the user's public profile. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1)-(2) | BR1 | **Processing & Storing Rules:**<br>❖ User submits the form. System calls method `AddLink(dto)` (Step 1).<br>❖ System inserts a new record into table “SocialLinks” in the database (Refer to “SocialLinks” table in “DB Sheet” file) (Step 2).<br>❖ System links the new record to the current profile via `ProfileId`. |
| (2.1) | BR2 | **Displaying Rules:**<br>❖ System returns the created Link DTO (Step 2.1).<br>❖ The UI adds the new link item to the list in the “EditProfile” view. |
| (2.2) | BR_Error | **Exception Handling Rules:**<br>❖ If a system failure occurs:<br> System logs error (Step 2.2).<br> System returns 500. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:Add Social Link;
|System|
:(1) POST /api/profiles/links;
|Database|
:(2) INSERT INTO "SocialLinks";
if (Success?) then (Yes)
    |System|
    :(2.1) Return LinkDto;
else (No)
    |System|
    :(2.2) Log Error & Return 500;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "AddLinkView" as View
control "ProfilesController" as Controller
entity "SocialLinks" as DB

User -> View: Submit Link
View -> Controller: AddLink(dto)
activate Controller
Controller -> DB: Add Entity
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: Return Created Dto
    deactivate Controller
else Database Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: LogError
    Controller --> View: 500 Error
    deactivate Controller
end
@enduml
```

---

## 2.1.9.5 Delete Account

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Delete Account** |
| **Description** | Permanent account removal. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User initiates deletion flow from Security Settings. |
| **Pre-condition** | ❖ User passes the confirmation challenge (e.g., typing "DELETE"). |
| **Post-condition** | ❖ All user data is permanently deleted from the database.<br>❖ Auth provider record is removed. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1)-(2) | BR1 | **Selecting Rules:**<br>❖ User clicks "Delete Account" (Step 1). System displays a Confirmation Dialog (Refer to MSG_CONFIRM_DELETE_ACCOUNT).<br> **Confirmed**: User accepts permanent data loss (Step 2). System moves to step (3).<br> **Cancelled**: Action aborted. |
| (3)-(4) | BR2 | **Processing & Storing Rules:**<br>❖ System calls method `DeleteProfile(userId)` (Step 3).<br>❖ System executes `DELETE FROM Profiles` (Refer to “Profiles” table in “DB Sheet” file) (Step 4). Cascading logic removes related data in “Follows”, “UserConversations”, etc. |
| (4.1)-(6) | BR3 | **Displaying Rules:**<br>❖ System calls Auth Provider API to delete the identity (Step 4.1).<br>❖ System returns Success (Step 5).<br>❖ System displays a successful notification (Refer to MSG_SUCCESS_ACCOUNT_DELETED).<br>❖ System forces a logout and redirects the user to the “Landing” screen (Refer to “Landing” view in “View Description” file) (Step 6). |
| (4.2)-(7) | BR_Error | **Exception Handling Rules:**<br>❖ If a system failure occurs:<br> System logs error (Step 4.2).<br> System returns 500.<br> Show Error (Step 7). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Request Delete;
:(2) Confirm;
|System|
:(3) DELETE /api/profiles;
|Database|
:(4) DELETE FROM "Profiles";
if (Success?) then (Yes)
    |System|
    :(4.1) Call Auth Provider Delete;
    :(5) Return Success;
    |Authenticated User|
    :(6) Logged Out;
else (No)
    |System|
    :(4.2) Log Error & Return 500;
    |Authenticated User|
    :(7) Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "DeleteAccountSettings" as View
control "ProfilesController" as Controller
entity "AppDbContext" as DB
control "SupabaseAuthService" as Auth

User -> View: Click Delete
View -> Controller: Delete()
activate Controller
Controller -> DB: Remove Profile (Cascade)
activate DB
alt Success
    DB --> Controller: Done
    deactivate DB
    Controller -> Auth: DeleteUser(id)
    Controller --> View: 200 OK
    deactivate Controller
    View -> User: Logout
else Failed
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: Log Error
    Controller --> View: 500 Error
    deactivate Controller
    View -> User: Show Error
end
@enduml
```
