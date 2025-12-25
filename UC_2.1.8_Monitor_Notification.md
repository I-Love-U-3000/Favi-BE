# Use Case 2.1.8: Monitor Notification

**Module**: Notifications
**Primary Actor**: Authenticated User
**Backend Controller**: `Favi_BE.API.Controllers.NotificationsController`
**Database Tables**: `"Notifications"`

---

## 2.1.8.1 Monitor Notification (View List)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Monitor Notification (View List)** |
| **Description** | View the notification stream. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks the [iconBell] on the Navigation Bar. |
| **Pre-condition** | ❖ User is logged in. |
| **Post-condition** | ❖ System displays the notification dropdown/page with recent alerts. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Selecting Rules:**<br>When the user clicks the Bell Icon on the navbar, the system toggles the Notification Center dropdown/page. |
| (2) | BR2 | **Querying Rules:**<br>The system calls `NotificationsController.GetNotifications` (`GET /api/notifications`) to fetch the latest alerts. |
| (3) | BR3 | **Querying Rules:**<br>The database executes a `SELECT` query on the `Notifications` table, filtering for records where `RecipientId` is the current user, ordered by time. |
| (4) | BR4 | **Displaying Rules:**<br>The system returns a `PagedResult` containing notification DTOs. |
| (5) | BR5 | **Displaying Rules:**<br>The UI renders the notification list. |
| (5.1) | BR5.1 | **Displaying Rules (Unread):**<br>If `IsRead` is false, the item is displayed with a highlighted background (e.g., light blue) to indicate it is new. |
| (5.2) | BR5.2 | **Displaying Rules (Read):**<br>If `IsRead` is true, the item is displayed with a standard/white background. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Click Bell Icon;
|System|
:(2) GET /api/notifications;
|Database|
:(3) SELECT * FROM "Notifications";
|System|
:(4) Return PagedResult;
|Authenticated User|
:(5) Processing Display;
if (IsRead?) then (Yes)
  :(5.1) Show White BG;
else (No)
  :(5.2) Show Highlight BG;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "NavbarView (Mock)" as View
control "NotificationsController" as Controller
entity "Notifications" as Entity

User -> View: Click [iconBell]
activate View
View -> Controller: GetNotifications(paging)
activate Controller
Controller -> Entity: Query (RecipientId = Me)
activate Entity
Entity --> Controller: Return List
deactivate Entity
Controller --> View: Return PagedResult
deactivate Controller
View --> User: Render List

opt Highlight Unread
    View -> View: Check IsRead
    note right: If false, apply highlight
end
deactivate View
@enduml
```

---

## 2.1.8.3 Mark Notification as Read

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Mark Notification as Read** |
| **Description** | Click a notification to read it. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks on an unread notification item. |
| **Pre-condition** | ❖ Notification exists and is unread. |
| **Post-condition** | ❖ Notification status becomes "Read".<br>❖ User is redirected to the relevant content. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Selecting Rules:**<br>When the user clicks on a specific notification item, the system handles the read confirmation and navigation. |
| (2) | BR2 | **Processing Rules:**<br>The system calls `NotificationsController.MarkAsRead` (`PUT /api/notifications/{id}/read`) for that specific notification. |
| (3) | BR3 | **Storing Rules:**<br>The database updates the record in the `Notifications` table, setting the `IsRead` column to `TRUE`. |
| (4) | BR4 | **Displaying Rules:**<br>The system returns a success status (200 OK) to acknowledge the update. |
| (5) | BR5 | **Processing Rules:**<br>The UI reads the `TargetUrl` or resource parameters from the notification and redirects the browser to the relevant content (e.g., specific Post, Comment, or Profile). |
| (6) | BR_Error | **Exception Handling Rules:**<br>If a system failure occurs, the Global Exception Handler logs the error and returns a `500 Internal Server Error`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Click Item;
|System|
:(2) PUT /api/notifications/{id}/read;
:Determine Redirect URL;
|Database|
:(3) UPDATE "Notifications" SET IsRead=TRUE;
if (Update Success?) then (Yes)
    |System|
    :(4) Return Success;
    |Authenticated User|
    :(5) Navigate to Target;
else (No)
    |System|
    :Log Error;
    :Return Error (500);
    |Authenticated User|
    :Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "NotificationItem (Mock)" as View
control "NotificationsController" as Controller
entity "Notifications" as Entity

User -> View: Click Item
activate View
View -> Controller: MarkAsRead(id)
activate Controller
Controller -> Entity: Update IsRead=true
activate Entity
alt Success
    Entity --> Controller: Success
    deactivate Entity
    Controller --> View: Return Success (200)
    deactivate Controller
    View -> View: Navigate to Target
else Database Error
    activate Entity
    Entity --> Controller: Exception
    deactivate Entity
    Controller -> Controller: LogError(ex)
    Controller --> View: Error (500)
    deactivate Controller
    View -> User: Show Error
end
deactivate View
@enduml
```

---

## 2.1.8.4 Mark All Notifications as Read

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Mark All Notifications as Read** |
| **Description** | Bulk clear unread status. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks the "Mark all as read" icon. |
| **Pre-condition** | ❖ There are unread notifications. |
| **Post-condition** | ❖ All user's notifications are updated to "Read".<br>❖ Badge count is reset to 0. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Selecting Rules:**<br>User clicks the "Mark all as read" button/icon in the header. |
| (2) | BR2 | **Submitting Rules:**<br>System calls `NotificationsController.MarkAllAsRead()`. |
| (3) | BR3 | **Storing Rules:**<br>SQL: `UPDATE "Notifications" SET "IsRead" = true WHERE "RecipientProfileId" = @me`. |
| (4) | BR4 | **Displaying Rules:**<br>Frontend clears the red badge count on the Bell Icon immediately. |
| (5) | BR_Error | **Exception Handling Rules:**<br>If a system failure occurs, the Global Exception Handler logs the error and returns a `500 Internal Server Error`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:Click Mark All;
|System|
:PUT /api/notifications/read-all;
|Database|
:UPDATE "Notifications" ...;
if (Update Success?) then (Yes)
    |System|
    :Return OK;
    |Authenticated User|
    :Badge Disappears;
else (No)
    |System|
    :Log Error;
    :Return Error (500);
    |Authenticated User|
    :Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
actor "Authenticated User" as User
participant "NotificationCenterHeader" as View
participant "NotificationsController" as Controller
participant "Notifications" as DB

User -> View: Click Checkmark
View -> Controller: MarkAllAsRead()
activate Controller
Controller -> DB: Bulk Update
activate DB
alt Success
    DB --> Controller: Done
    deactivate DB
    Controller --> View: OK
    deactivate Controller
    View -> View: Set UnreadCount = 0
else Database Error
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: LogError(ex)
    Controller --> View: Error (500)
    deactivate Controller
    View -> User: Show Error
end
@enduml
```

---

## 2.1.8.5 Delete Notification

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Delete Notification** |
| **Description** | Remove an item from history. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User uses the delete action/swipe on a notification. |
| **Pre-condition** | ❖ Notification exists in the list. |
| **Post-condition** | ❖ The notification record is deleted from the database.<br>❖ Item is removed from the UI. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Selecting Rules:**<br>User swipes left on an item or uses the delete menu options. |
| (2) | BR2 | **Submitting Rules:**<br>System calls `NotificationsController` DELETE endpoint. |
| (3) | BR3 | **Storing Rules:**<br>System deletes the row from `"Notifications"`. |
| (4) | BR_Error | **Exception Handling Rules:**<br>If a system failure occurs, the Global Exception Handler logs the error and returns a `500 Internal Server Error`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:Swipe Delete;
|System|
:DELETE /api/notifications/{id};
|Database|
:DELETE FROM "Notifications" WHERE Id=@id;
if (Success?) then (Yes)
  |System|
  :Return OK;
else (No)
  |System|
  :Log Error;
  :Return 500;
  |Authenticated User|
  :Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
actor "Authenticated User" as User
participant "NotificationItem" as View
participant "NotificationsController" as Controller
participant "Notifications" as DB

User -> View: Delete Action
View -> Controller: Delete API
activate Controller
Controller -> DB: Remove
activate DB
alt Success
    DB --> Controller: Done
    deactivate DB
    Controller --> View: OK
    deactivate Controller
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

## 2.1.8.6 Configure Notification Preferences

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Configure Notification Preferences** |
| **Description** | Toggle types of alerts. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User navigates to Settings -> Notifications. |
| **Pre-condition** | ❖ User is logged in. |
| **Post-condition** | ❖ User's preference logic is updated in the database. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Displaying Rules:**<br>User enters "Settings -> Notifications". Displays list of toggles (Likes, Comments, Follows). |
| (2) | BR2 | **Selecting Rules:**<br>User toggles "Email Notifications for Likes" to OFF. |
| (3) | BR3 | **Submitting Rules:**<br>System calls `ProfilesController.UpdateSettings`. |
| (4) | BR4 | **Storing Rules:**<br>Updates the JSONB settings column or specific columns in `"Profiles"` table. |
| (5) | BR_Error | **Exception Handling Rules:**<br>If a system failure occurs, the Global Exception Handler logs the error and returns a `500 Internal Server Error`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:Toggle Switch;
|System|
:PATCH /api/profiles/settings;
|Database|
:UPDATE "Profiles" SET Settings...;
if (Success?) then (Yes)
  |System|
  :Return Updated Settings;
else (No)
  |System|
  :Log Error;
  :Return 500;
  |Authenticated User|
  :Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
actor "Authenticated User" as User
participant "NotificationSettingsView" as View
participant "ProfilesController" as Controller
participant "Profiles" as DB

User -> View: Toggle "Email for Likes"
View -> Controller: UpdateSettings(dto)
activate Controller
Controller -> DB: Save Changes
activate DB
alt Success
    DB --> Controller: Done
    deactivate DB
    Controller --> View: OK
    deactivate Controller
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
