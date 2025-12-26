# Use Case 2.1.6: Adjust Friend (Connections)

**Module**: Connections / Social Graph
**Primary Actor**: Authenticated User
**Backend Controller**: `Favi_BE.API.Controllers.ProfilesController`
**Database Tables**: `"Follows"`, `"Profiles"`, `"UserModerations"`

---

## 2.1.6.1 Adjust Friend (Overview)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Adjust Friend** |
| **Description** | Central hub for managing social connections (Followers, Followings, Suggestions, Blocked Users). |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User enters the "Friends" or "Network" section. |
| **Post-condition** | ❖ User Views lists or executes management actions. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Initialization & Navigation:**<br>❖ The System loads the default connection list (usually "Followers" or "Following").<br>❖ The User can switch tabs to view differents lists (Suggestions, Blocked).<br>❖ The System enables actions (Follow, Unfollow, Block) based on the context of each profile card. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|User|
start
:(1) View Friend Hub;
:Choose Function;
split
    -> View/Switch Tabs;
    :(2.1) Activity\nView Friend Lists;
split again
    -> Add/Follow;
    :(2.2) Activity\nAdd Friend / Follow;
split again
    -> Delete/Unfollow;
    :(2.3) Activity\nDelete Friend / Unfollow;
split again
    -> Search;
    :(2.4) Activity\nSearch Friend;
split again
    -> Block/Unblock;
    :(2.5) Activity\nBlock/Unblock User;
split again
    -> Suggestions;
    :(2.6) Activity\nView Suggestions;
end split
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "FriendHub" as View
control "ProfilesController" as Controller

User -> View: Open Connections
View -> Controller: GetFollowers/Followings()
activate Controller
Controller --> View: List<ProfileDto>
deactivate Controller
View -> User: Display Default List

opt Add/Follow
    ref over User, View, Controller: Sequence Add Friend
end

opt Delete/Unfollow
    ref over User, View, Controller: Sequence Delete Friend
end

opt Search
    ref over User, View, Controller: Sequence Search Friend
end

opt Block
    ref over User, View, Controller: Sequence Block Friend
end
@enduml
```

---

## 2.1.6.2 Add Friend / Follow User

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Add Friend / Follow User** |
| **Description** | The Authenticated User initiates a connection with another user. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User navigates to another user's profile.<br>❖ User clicks the [btnFollow] button. |
| **Pre-condition** | ❖ User is logged in.<br>❖ Target user is not blocked by the actor. |
| **Post-condition** | ❖ A new record is added to the "Follows" table.<br>❖ The button state changes to "Following" or "Requested". |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Validation Workflow:**<br>❖ The selected data will be checked by table “UserModerations” and corresponding “Permissions” in the database (Refer to “UserModerations” table in “DB Sheet” file) to check if there are any constraints (e.g., Blocked users).<br>❖ System calls method `PrivacyGuard.CanFollowAsync(followerId, followeeId)`.<br> If the action is **Allowed**: System moves to step (4).<br> If the action is **Forbidden**: System moves to step (3.1) to return `403 Forbidden`. System displays an error message (Refer to MSG_ERR_BLOCKED or MSG_ERR_FORBIDDEN) (Step 3.2). |
| (3.2) | BR2 | **Storing Rules:**<br>❖ When the validations pass, System will move to step (4) to send data to database by method `Follow(targetId)`.<br>❖ System stores connection information in table “Follows” in the database (Refer to “Follows” table in “DB Sheet” file) with `FollowerId` = [User.ID] and `FolloweeId` = [Target.ID]. |
| (3.2.1)-(5) | BR3 | **Displaying Rules:**<br>❖ System returns a `200 OK` success status (Step 4.1).<br>❖ System displays a successful notification (Refer to MSG_SUCCESS_FOLLOW) and updates the button state.<br>❖ The UI button changes to “Following” to reflect the new association (Step 5). |
| (3.2.4.2)-(6) | BR_Error | **Exception Handling Rules:**<br>❖ If a system failure occurs:<br> System logs the error (Step 4.2).<br> System returns `500 Internal Server Error`.<br> System displays "Follow Failed" message (Step 6). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Click [btnFollow];
|System|
:(2) Call PrivacyGuard.CanFollowAsync();
|Database|
:(3) Check "UserModerations";
|System|
if (Can Follow?) then (No)
  :(3.1) Return Forbidden (403);
  |Authenticated User|
  :(4) Show Error MSG;
else (Yes)
  |Database|
  :(3.2) INSERT INTO "Follows";
  if (Save Success?) then (Yes)
    |System|
    :(3.2.1) Return Success (200 OK);
    |Authenticated User|
    :(5) Button updates to "Following";
  else (No)
    |System|
    :(3.2.2) Log Error & Return 500;
    |Authenticated User|
    :(6) Show "Follow Failed";
  endif
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "ProfileDetailView" as View
boundary "[btnFollow]" as Button
control "ProfilesController" as Controller
control "PrivacyGuard" as Service
entity "Follows (Table)" as DB

User -> View: View Profile
View -> Button: Render(State="Not Following")
User -> Button: Click()
Button -> Controller: Follow(targetId)
activate Controller
Controller -> Service: CanFollowAsync(me, target)
Service -> DB: Check Block Lists
Service --> Controller: true
Controller -> DB: Add(new FollowEntity)
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> Button: 200 OK
    deactivate Controller
    Button -> Button: SetState("Following")
    View --> User: Update UI
else Database Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: LogError(ex)
    Controller --> Button: 500 Internal Server Error
    deactivate Controller 
    Button --> User: Show Error Toast
end
@enduml
```

---

## 2.1.6.3 Delete Friend (Unfriend) / Unfollow

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Delete Friend (Unfriend) / Unfollow** |
| **Description** | The Authenticated User removes an existing connection. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks the "Following" button or "Unfollow" option on a target profile. |
| **Pre-condition** | ❖ User is currently following the target user. |
| **Post-condition** | ❖ The record is removed from the "Follows" table.<br>❖ The button state reverts to "Follow". |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1)-(2) | BR1 | **Confirmation Logic:**<br>❖ When the user clicks "Unfollow" (Step 1), System displays a Confirmation Dialog (Refer to “ConfirmationModal” view in “View Description” file) asking user to confirm (Step 2).<br> **On Cancel**: The user clicks "Cancel" (Step 2.1). The dialog closes, and the system moves to end state (Flow stops).<br> **On Confirm**: The user clicks "Confirm" (Step 2.2). System moves to step (3) to execute removal. |
| (3)-(5) | BR2 | **Processing & Storing Rules:**<br>❖ System calls method `ProfilesController.Unfollow(targetId)` (Step 3).<br>❖ The input data will be checked by table “Follows” in the database (Refer to “Follows” table in “DB Sheet” file) (Step 4).<br>❖ System deletes the corresponding records from table “Follows” where `FollowerId` matches current user and `FolloweeId` matches target (Step 5). |
| (5.1)-(6) | BR3 | **Displaying Rules:**<br>❖ After deleting data, System returns Success (Step 5.1).<br>❖ System displays a successful notification (Refer to MSG_SUCCESS_UNFOLLOW).<br>❖ The UI resets the button state from “Following” back to “Follow” (Step 6). |
| (5.2)-(7) | BR_Error | **Exception Handling Rules:**<br>❖ If a system failure occurs:<br> System logs the error and returns 500 (Step 5.2).<br> System displays "Unfollow Failed" message (Step 7). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Click "Unfollow";
:(2) Confirm Dialog displays;
if (Confirm?) then (No)
  :(2.1) Click "Cancel";
  stop
else (Yes)
  |Authenticated User|
  :(2.2) Click "Confirm";
  |System|
  :(3) Call DELETE /api/profiles/follow;
  |Database|
  :(4) Find Record;
  :(5) DELETE Record;
  if (Delete Success?) then (Yes)
      |System|
      :(5.1) Return Success;
      |Authenticated User|
      :(6) Button resets to "Follow";
  else (No)
      |System|
      :(5.2) Log Error & Return 500;
      |Authenticated User|
      :(7) Show "Unfollow Failed";
  endif
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "ProfileDetailView (Mock)" as View
control "ProfilesController" as Controller
entity "Follow (Table)" as Entity

User -> View: Click "Unfollow"
activate View
View --> User: Display Confirmation Dialog

alt Cancel
    User -> View: Click "Cancel"
    View --> User: Close Dialog
else Confirm
    User -> View: Click "Confirm"
    View -> Controller: Unfollow(targetId)
    activate Controller
    Controller -> Entity: Remove Record
    activate Entity
    
    alt Success
        Entity --> Controller: Success
        deactivate Entity
        Controller --> View: Return Success (200)
        View --> User: Reset Button to "Follow"
    else Database Error
        activate Entity
        Entity --> Controller: Exception
        deactivate Entity
        Controller -> Controller: LogError(ex)
        Controller --> View: Return Error (500)
        View --> User: Show Error Message
    end
    deactivate Controller
end
deactivate View
@enduml
```

---

## 2.1.6.4 Search Friend

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Search Friend** |
| **Description** | The user searches specifically within their network or globally. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User focuses on the [txtSearch] input field.<br>❖ User types a keyword (name or username). |
| **Pre-condition** | ❖ User is on the "Friends" screen or Navigation bar. |
| **Post-condition** | ❖ System displays a list of profiles matching the search query. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2) | BR1 | **Validation Logic:**<br>❖ The input data from `[txtSearch]` is validated.<br> **Invalid (Empty/Null)**: If input length is 0, the system ignores the request or displays a tooltip (Refer to MSG_WARN_EMPTY_SEARCH).<br> **Valid**: If input is valid, System moves to step (3). |
| (2.2)-(3) | BR2 | **Querying Rules:**<br>❖ System calls method `SearchController.SearchPeople(query)`.<br>❖ System queries data in the table “Profiles” in the database (Refer to “Profiles” table in “DB Sheet” file) with syntax `SELECT * FROM Profiles WHERE Name LIKE %[query]%`. |
| (4)-(5) | BR3 | **Displaying Rules:**<br>❖ After getting matched data, system displays a “SearchResults” list (Refer to “SearchResults” view in “View Description” file).<br>❖ The system renders a list of `ProfileDto` objects for the user to select. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Type keyword in Search Bar;
|System|
:(2) Validate Input (Length > 0);
if (Valid?) then (No)
  :(2.1) Stop / Ignore;
  stop
else (Yes)
  :(2.2) GET /api/search/people?q=...;
  |Database|
  :(3) SELECT * FROM "Profiles" WHERE Name LIKE %q%;
  |System|
  :(4) Return List;
  |Authenticated User|
  :(5) View Search Results;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "NavBarView" as View
boundary "[txtSearch]" as Input
control "SearchController" as Controller
entity "Profiles (Table)" as DB

User -> Input: Type "Alice"
Input -> Input: Debounce(500ms)
Input -> Controller: SearchPeople("Alice")
activate Controller
Controller -> DB: Query(Name contains "Alice")
activate DB
DB --> Controller: List<Profile>
deactivate DB
Controller --> View: List<SearchResultDto>
deactivate Controller
View --> User: Display Dropdown List
@enduml
```

---

## 2.1.6.5 Block Friend / User

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Block Friend / User** |
| **Description** | Block a user to prevent all interaction. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User selects "Block" from the profile options menu. |
| **Pre-condition** | ❖ Target user is not already blocked. |
| **Post-condition** | ❖ A "Block" record is added to "UserModerations".<br>❖ Any existing "Follows" relationships are deleted. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1)-(3) | BR1 | **Selecting Rules & Confirmation:**<br>❖ User invokes the "Block" command from the profile menu (Step 1).<br>❖ System displays a Warning Dialog (Refer to MSG_CONFIRM_BLOCK) (Step 2).<br> **Confirmed**: User clicks Confirm (Step 3). System moves to step (4). |
| (4)-(6) | BR2 | **Processing & Storing Rules:**<br>❖ System calls method `BlockUser(targetId)` (Step 4).<br>❖ System stores block information in table “UserModerations” in the database (Refer to “UserModerations” table in “DB Sheet” file) with Type='Block' (Step 5).<br>❖ System deletes any related records in table “Follows” (Refer to “Follows” table in “DB Sheet” file) to ensure no connection remains (Step 6). |
| (6.1)-(7) | BR3 | **Displaying Rules:**<br>❖ After processing, System returns Success (Step 6.1).<br>❖ System displays a successful notification (Refer to MSG_SUCCESS_BLOCK).<br>❖ System redirects the user to the Home Screen or updates the view to hide the content (Refer to “Home” view in “View Description” file) (Step 7). |
| (6.2)-(8) | BR_Error | **Exception Handling Rules:**<br>❖ If a system failure occurs:<br> System logs the error and returns 500 (Step 6.2).<br> System displays Error message (Step 8). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Select "Block" from Menu;
|System|
:(2) Show Warning;
|Authenticated User|
:(3) Confirm;
|System|
:(4) POST /api/users/block;
|Database|
:(5) INSERT INTO "UserModerations";
:(6) DELETE FROM "Follows";
if (Success?) then (Yes)
  |System|
  :(6.1) Return Success;
  |Authenticated User|
  :(7) Redirect to Home;
else (No)
  |System|
  :(6.2) Log Error & Return 500;
  |Authenticated User|
  :(8) Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "ProfileOptionsSheet" as View
boundary "BlockConfirmModal" as Modal
control "ProfilesController" as Controller
entity "UserModerations" as DB_Mod
entity "Follows" as DB_Follow

User -> View: Click "Block"
View -> Modal: Show()
User -> Modal: Click "Confirm Block"
Modal -> Controller: BlockUser(targetId)
activate Controller
Controller -> DB_Mod: Add Block Record
Controller -> DB_Follow: Remove Mutual Follows
activate DB_Follow
activate DB_Follow
alt Success
    DB_Follow --> Controller: Done
    deactivate DB_Follow
    Controller --> Modal: 200 OK
    deactivate Controller
    Modal -> User: Show "Blocked" Toast
    Modal -> View: Redirect Home
else Database Error
    activate DB_Follow
    DB_Follow --> Controller: Exception
    deactivate DB_Follow
    Controller -> Controller: Log Error
    Controller --> Modal: 500 Error
    deactivate Controller
    Modal -> User: Show Error Action
end
@enduml
```

---

## 2.1.6.6 Unblock User

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Unblock User** |
| **Description** | Restore ability to interact. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks [btnUnblock] in the Blocked Users list. |
| **Pre-condition** | ❖ Target user is currently in the blocked list. |
| **Post-condition** | ❖ The "Block" record is removed from "UserModerations".<br>❖ User disappears from the blocked list. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Processing & Storing Rules:**<br>❖ System calls method `UnblockUser(targetId)` (Step 2).<br>❖ System deletes the corresponding "Block" record from table “UserModerations” in the database (Refer to “UserModerations” table in “DB Sheet” file) matched by `[User.ID]` and `[Target.ID]` (Step 3). |
| (3.1)-(4) | BR2 | **Displaying Rules:**<br>❖ After deleting the block record, System returns Success (Step 3.1).<br>❖ System displays a successful notification (Refer to MSG_SUCCESS_UNBLOCK).<br>❖ System updates the "Blocked Users" list view (Refer to “BlockedList” view in “View Description” file) by removing the unblocked user item (Step 4). |
| (3.2)-(5) | BR_Error | **Exception Handling Rules:**<br>❖ If a system failure occurs:<br> System logs the error and returns 500 (Step 3.2).<br> System displays Error message (Step 5). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:Go to Blocked List;
:(1) Click "Unblock";
|System|
:(2) DELETE /api/users/block/{id};
|Database|
:(3) DELETE FROM "UserModerations";
if (Success?) then (Yes)
    |System|
    :(3.1) Return Success;
    |Authenticated User|
    :(4) User removed from list;
else (No)
    |System|
    :(3.2) Log Error & Return 500;
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
actor "Authenticated User" as User
boundary "BlockedListView" as View
control "ProfilesController" as Controller
entity "UserModerations" as DB

User -> View: Click [btnUnblock]
View -> Controller: Unblock(targetId)
activate Controller
Controller -> DB: Delete Record
activate DB
alt Success
    DB --> Controller: Done
    deactivate DB
    Controller --> View: 200 OK
    deactivate Controller
    View -> View: Remove Item from List
else Database Error
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

---

## 2.1.6.7 View Friend Suggestions

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **View Friend Suggestions** |
| **Description** | System recommends users to follow. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User visits the Feed or Friends tab.<br>❖ System identifies low connection count or relevant signals. |
| **Pre-condition** | ❖ User is logged in. |
| **Post-condition** | ❖ a list of suggested profiles is displayed to the user. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(4) | BR1 | **Querying Rules:**<br>❖ System calls method `ProfilesController.GetRecommendations()` (Step 2).<br>❖ System queries data in the table “Profiles” in the database (Step 3).<br>❖ System filters out users already in “Follows” table (Step 4).<br>❖ System orders the results by [Strategy: MutualFriends/Random]. |
| (5)-(7) | BR2 | **Displaying Rules:**<br>❖ System processes data (Randomize/Rank) (Step 5).<br>❖ System returns list (Step 6).<br>❖ System displays a “Suggestions” widget (Refer to “Suggestions” view in “View Description” file) filled with candidate profiles (Step 7). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:View Feed / Friends Tab;
:(1) System identifies trigger;
|System|
:(2) GetRecommendations();
|Database|
:(3) SELECT * FROM "Profiles";
:(4) EXCEPT SELECT * FROM "Follows";
|System|
:(5) Randomize / Rank;
:(6) Return List;
|Authenticated User|
:(7) See Suggestions;
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "FeedView" as View
control "ProfilesController" as Controller
entity "Profiles" as DB

User -> View: Scroll to Widget
View -> Controller: GetRecommendations()
activate Controller
Controller -> DB: Query Candidates
activate DB
DB --> Controller: Raw List
deactivate DB
Controller -> Controller: Filter Existing Follows
Controller --> View: List<ProfileDto>
deactivate Controller
View --> User: Render Cards
@enduml
```
