# Use Case 2.1.9: Adjust User Settings

**Module**: Settings / Profile
**Primary Actor**: Authenticated User
**Backend Controller**: `Favi_BE.API.Controllers.ProfilesController`
**Database Tables**: `"Profiles"`, `"SocialLinks"`

---

## 2.1.9.1 Adjust User Settings (Profile Update)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Adjust User Settings (Profile Update)** |
| **Description** | Edit Bio, Name, or Avatar. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks the "Edit Profile" button. |
| **Pre-condition** | ❖ User is logged in. |
| **Post-condition** | ❖ Profile information (Name, Bio, Avatar) is updated in the database. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Submitting Rules:**<br>When the user changes their details and clicks "Save" in the Edit Profile modal, the system initiates the update process. |
| (2) | BR2 | **Validation Rules:**<br>The system validates the input data (e.g., Name length, valid URL formats). |
| (2.1) | BR2.1 | **Displaying Rules (Invalid):**<br>If validation fails, the system returns a `400 Bad Request` and displays specific error messages to the user. |
| (2.2) | BR2.2 | **Processing Rules (Valid):<br>If validation passes, the system proceeds to step (3) to persist the changes. |
| (3) | BR3 | **Storing Rules:**<br>The database executes an `UPDATE` command on the `Profiles` table for the current user's record with the new values. |
| (4) | BR4 | **Displaying Rules:**<br>The system returns the updated `Profile` entity/DTO. |
| (5) | BR5 | **Displaying Rules:**<br>The UI updates the profile view with the new information and closes the modal (or shows a "Saved" toast). |
| (6) | BR6 | **Displaying Rules:**<br>Show Error. |

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
      :(4) Return Entity;
      |Authenticated User|
      :(5) Profile Updated;
  else (No)
      |System|
      :Log Error;
      :Return Error (500);
      |Authenticated User|
      :Show Error;
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
actor "Authenticated User" as User
participant "EditProfileModal" as View
participant "ProfilesController" as Controller
participant "Profiles" as DB

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
| (1) | BR1 | **Selecting Rules:**<br>User clicks "Add Link" in the Edit Profile screen. |
| (2) | BR2 | **Displaying Rules:**<br>System shows "Add Link" form: `[txtPlatform]`, `[txtUrl]`. |
| (3) | BR3 | **Submitting Rules:**<br>User enters data and confirms. System calls `ProfilesController.AddLink(dto)`. |
| (4) | BR4 | **Storing Rules:**<br>System inserts a new record into `"SocialLinks"` table with `ProfileId = @me`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:Add Social Link;
|System|
:POST /api/profiles/links;
|Database|
:INSERT INTO "SocialLinks";
|System|
:Return LinkDto;
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
actor "Authenticated User" as User
participant "AddLinkView" as View
participant "ProfilesController" as Controller
participant "SocialLinks" as DB

User -> View: Submit Link
View -> Controller: AddLink(dto)
activate Controller
Controller -> DB: Add Entity
activate DB
DB --> Controller: Success
deactivate DB
Controller --> View: Return Created Dto
deactivate Controller
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
| (1) | BR1 | **Selecting Rules:**<br>When the user clicks the "Delete Account" button in settings, the system asks for confirmation. |
| (2) | BR2 | **Processing Rules:**<br>Upon confirmation, the system calls `ProfilesController.Delete` (`DELETE /api/profiles`). |
| (3) | BR3 | **Storing Rules:**<br>The database permanently deletes the `Profiles` record (and cascades deletes to associated data like `Follows`, `UserConversations`). |
| (4) | BR4 | **Processing Rules:**<br>The system calls the Authentication Provider (e.g., Auth0/Firebase) to remove the user's login identity. |
| (5) | BR5 | **Displaying Rules:**<br>The system returns a success message indicating the account is deleted. |
| (6) | BR6 | **Displaying Rules:**<br>The UI forcibly logs the user out and redirects them to the Landing/Login page. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Request Delete;
:Confirm;
|System|
:(2) DELETE /api/profiles;
|Database|
:(3) DELETE FROM "Profiles";
|System|
:(4) Call Auth Provider Delete;
:(5) Return Success;
|Authenticated User|
:(6) Logged Out;
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
actor "Authenticated User" as User
participant "DeleteAccountSettings" as View
participant "ProfilesController" as Controller
participant "AppDbContext" as DB
participant "SupabaseAuthService" as Auth

User -> View: Click Delete
View -> Controller: Delete()
activate Controller
Controller -> DB: Remove Profile (Cascade)
activate DB
DB --> Controller: Done
deactivate DB
Controller -> Auth: DeleteUser(id)
Controller --> View: 200 OK
deactivate Controller
View -> User: Logout
@enduml
```
