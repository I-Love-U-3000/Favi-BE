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
| (1) | BR1 | **Initialization & Navigation:**<br>❖ The **System** loads the default connection list (typically "Followers" or "Following").<br>❖ The **User** can switch tabs to view different lists, such as Suggestions or Blocked Users.<br>❖ The **System** enables context-sensitive actions (Follow, Unfollow, Block) based on the relationship state of each profile card. |

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
| (2)-(3) | BR1 | **Validation Workflow:**<br>❖ The **Frontend** `ProfileHeader` checks the local state (isBlocked/isFollowing) and calls `profileApi.follow(targetId)`.<br>❖ The **API** receives a `POST` request at `/api/profiles/follow/{targetId}`.<br>❖ The **Backend** `ProfilesController.Follow(targetId)` invokes `_privacy.CanFollowAsync(followerId, followeeId)`.<br>❖ The **Logic** checks the `UserModerations` table for any 'Block' relationship.<br> If blocked, the **System** throws a `FriendshipException` ("Cannot follow blocked user") and returns `403 Forbidden`.<br> If allowed, the **System** proceeds to storage operation. |
| (3.2) | BR2 | **Storage:**<br>❖ The **Backend** executes `_profiles.FollowAsync(followerId, targetId)`.<br>❖ The **Database** inserts a new record into `Follows` table with `FollowerId`, `FolloweeId`, and `CreatedAt` timestamp.<br>❖ The **NotificationService** triggers a notification via `_notificationService.NotifyFollow(targetId, followerId)`, inserting a record into the `Notifications` table. |
| (3.2.1)-(5) | BR3 | **Completion:**<br>❖ The **System** returns a `200 OK` response.<br>❖ The **Frontend** updates the `isFollowing` state to `true`, changes the button text to "Following", and displays a success toast (MSG_SUCCESS_FOLLOW). |
| (3.2.4.2)-(6) | BR_Error | **Exception Handling:**<br>❖ If a Database error occurs (e.g., UniqueConstraint), the **System** returns `409 Conflict`.<br>❖ For generic errors, the **System** returns `500 Internal Server Error`.<br>❖ The **Frontend** catches the error, reverts the button state, and displays a "Follow Failed" toast. |

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
| (1)-(2) | BR1 | **Confirmation:**<br>❖ The **Frontend** displays a `ConfirmationModal` ("Are you sure you want to unfollow?") when the user clicks "Unfollow".<br>❖ Upon confirmation, the **Frontend** initiates a call to `profileApi.unfollow(targetId)`. |
| (3)-(5) | BR2 | **Processing:**<br>❖ The **API** receives a `DELETE` request at `/api/profiles/follow/{targetId}`.<br>❖ The **Backend** `ProfilesController.Unfollow(targetId)` invokes `_profiles.UnfollowAsync(currentUserId, targetId)`.<br>❖ The **Database** executes a deletion against the `Follows` table where `FollowerId` and `FolloweeId` match the request.<br>❖ The **Logic** verifies the record exists before attempting deletion. |
| (5.1)-(6) | BR3 | **Completion:**<br>❖ The **System** returns `200 OK` on success.<br>❖ The **Frontend** dispatches the `unfollowSuccess` Redux action, reverts the button state to "Follow", and displays a success message (MSG_SUCCESS_UNFOLLOW). |
| (5.2)-(7) | BR_Error | **Error Handling:**<br>❖ If the relationship is not found, the **System** returns `404 Not Found`.<br>❖ For server-side errors, the **System** returns `500` and logs nature of the failure via `Serilog`.<br>❖ The **Frontend** displays an "Unfollow Failed" toast notification. |

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
| (2) | BR1 | **Frontend Validation:**<br>❖ The **Frontend** `FriendSearchInput` component applies a 500ms debounce to the input.<br>❖ If the query length is less than 2 characters, the **System** abuts the request.<br>❖ Otherwise, it dispatches `searchUser(query)`. |
| (2.2)-(3) | BR2 | **Processing:**<br>❖ The **API** processes a `POST` request to `/api/search` with the body `{ query: "abc", type: "User" }`.<br>❖ The **Backend** `SearchController.Search(dto)` invokes `_searchService.SearchUsersAsync(query)`.<br>❖ The **Database** executes a query `SELECT * FROM Profiles` matching `DisplayName` or `Username`.<br>❖ The **Logic** refines the results by filtering out blocked users via a check against `UserModerations`. |
| (4)-(5) | BR3 | **Result:**<br>❖ The **System** returns `200 OK` with a `PagedResult<ProfileDto>`.<br>❖ The **Frontend** updates the `searchResults` state and renders the `UserList`.<br>❖ If no matches are found, the **Frontend** displays a "No users found" message. |

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
| (1)-(3) | BR1 | **Confirmation:**<br>❖ The **Frontend** displays a `BlockWarningModal` when the user selects "Block" from the `UserProfileMenu`.<br>❖ Upon explicit confirmation ("Confirm Block"), the **Frontend** calls `profileApi.blockUser(targetId)`. |
| (4)-(6) | BR2 | **Processing:**<br>❖ The **API** receives a `POST` request at `/api/user-moderation/block` with the `{ targetId }`.<br>❖ The **Backend** `UserModerationController.Block(dto)` invokes `_moderation.BlockAsync(userId, targetId)`.<br>❖ The **Database** performs two operations: <br> 1. `INSERT INTO UserModerations` with `Type='Block'`.<br> 2. `DELETE FROM Follows` to remove any mutual follow status. |
| (6.1)-(7) | BR3 | **Completion:**<br>❖ The **System** returns `200 OK`.<br>❖ The **Frontend** immediately redirects the user to Home (if on the profile page) or hides the user's content.<br>❖ The **System** invalidates specific user cache keys. |
| (6.2)-(8) | BR_Error | **Exception:**<br>❖ If the user is already blocked, the **System** returns `409 Conflict`.<br>❖ For server warnings, it returns `500`.<br>❖ The **Frontend** displays an error toast. |

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
| (2)-(3) | BR1 | **Processing:**<br>❖ The **Frontend** `BlockedUsersList` initiates a call to `profileApi.unblock(targetId)` upon clicking "Unblock".<br>❖ The **API** request goes to `DELETE /api/user-moderation/block/{targetId}`.<br>❖ The **Backend** `UserModerationController.Unblock` calls `_moderation.UnblockAsync`.<br>❖ The **Database** deletes the matching record from `UserModerations` where `Type='Block'`. |
| (3.1)-(4) | BR2 | **Completion:**<br>❖ The **System** returns `200 OK`.<br>❖ The **Frontend** optimistically removes the user from the `BlockedUsersList` and displays a "User unblocked" toast. |
| (3.2)-(5) | BR_Error | **Exception:**<br>❖ If the block record is not found, the **System** returns `404 Not Found`.<br>❖ For generic failures, it returns `500`. |

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
| (2)-(4) | BR1 | **Data Fetching:**<br>❖ The **Frontend** `RightSidebar` component calls `profileApi.getRecommendations()` when mounted.<br>❖ The **API** receives a `GET` request at `/api/profiles/recommendations?limit=5`.<br>❖ The **Backend** `ProfilesController.GetRecommendations` delegates to `_recommendationEngine.GetSuggestedUsersAsync`.<br>❖ The **Database** executes a query to `SELECT` random profiles not already followed by the user. |
| (5)-(7) | BR2 | **Rendering:**<br>❖ The **System** returns `200 OK` containing a list of `ProfileDto`.<br>❖ The **Frontend** renders `SuggestionCard` components for each user in the list. |

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
