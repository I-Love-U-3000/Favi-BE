# Use Case 2.1.8: Monitor Notification

**Module**: Notifications
**Primary Actor**: Authenticated User
**Backend Controller**: `Favi_BE.API.Controllers.NotificationsController`
**Database Tables**: `"Notifications"`

---

## 2.1.8.1 Monitor Notification (Overview)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Monitor Notification** |
| **Description** | Central hub for viewing and managing system alerts and notifications. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks the "Bell" icon. |
| **Post-condition** | ❖ User views or manages notifications. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Initialization:**<br>❖ The System fetches the user's notification stream, highlighting unread items.<br>❖ The System provides controls to Mark as Read, Delete, or Configure preferences. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|User|
start
:(1) View Notification Hub;
:Choose Function;
split
    -> View List;
    :(2.1) Activity\nView Notifications;
split again
    -> Read;
    :(2.2) Activity\nMark as Read;
split again
    -> Mark All;
    :(2.3) Activity\nMark All Read;
split again
    -> Delete;
    :(2.4) Activity\nDelete Notification;
split again
    -> Configure;
    :(2.5) Activity\nConfigure Preferences;
end split
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "NotificationHub" as View
control "NotificationsController" as Controller

User -> View: Open Notifications
View -> Controller: GetNotifications()
activate Controller
Controller --> View: PagedResult<NotificationDto>
deactivate Controller
View -> User: Display List

opt Mark Read
    ref over User, View, Controller: Sequence Mark Read
end

opt Mark All Read
    ref over User, View, Controller: Sequence Mark All Read
end

opt Delete
    ref over User, View, Controller: Sequence Delete
end
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
| (2)-(3) | BR1 | **Processing & Storing Rules:**<br>❖ When user selects an item, System calls method `MarkAsRead(id)` (Step 2).<br>❖ System updates data in the table “Notifications” in the database (Refer to “Notifications” table in “DB Sheet” file) setting `IsRead` = `True` (Step 3). |
| (3.1)-(4) | BR2 | **Displaying Rules:**<br>❖ System returns Success (Step 3.1).<br>❖ System validates the `TargetUrl` contained in the notification data.<br>❖ System redirects browser to the specific content page (e.g., Post Details or User Profile) (Refer to “PostDetail” view in “View Description” file) (Step 4). |
| (3.2)-(5) | BR_Error | **Exception Handling Rules:**<br>❖ If a system failure occurs:<br> System logs the error (Step 3.2).<br> System returns `500 Internal Server Error`.<br> Show Error (Step 5). |

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
    :(3.1) Return Success;
    |Authenticated User|
    :(4) Navigate to Target;
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
| (2)-(3) | BR1 | **Storing Rules:**<br>❖ User clicks "Mark All as Read". System calls method `MarkAllAsRead()` (Step 2).<br>❖ System executes syntax `UPDATE Notifications SET IsRead=1 WHERE RecipientId=[User.ID]` on table “Notifications” (Refer to “Notifications” table in “DB Sheet” file) (Step 3). |
| (3.1)-(4) | BR2 | **Displaying Rules:**<br>❖ System returns success confirmation (Step 3.1).<br>❖ System clears the "Unread Badge" count on the Navigation Bar (Refer to “NavBar” view in “View Description” file) (Step 4). |
| (3.2)-(5) | BR_Error | **Exception Handling Rules:**<br>❖ If a system failure occurs:<br> System logs error (Step 3.2).<br> System returns 500.<br> Show Error (Step 5). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Click Mark All;
|System|
:(2) PUT /api/notifications/read-all;
|Database|
:(3) UPDATE "Notifications" ...;
if (Update Success?) then (Yes)
    |System|
    :(3.1) Return OK;
    |Authenticated User|
    :(4) Badge Disappears;
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
boundary "NotificationCenterHeader" as View
control "NotificationsController" as Controller
entity "Notifications" as DB

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
| (2)-(3) | BR1 | **Storing Rules:**<br>❖ User selects Delete option. System calls method `DeleteNotification(id)` (Step 2).<br>❖ System deletes the record from table “Notifications” in the database (Refer to “Notifications” table in “DB Sheet” file) (Step 3). |
| (3.1) | BR2 | **Displaying Rules:**<br>❖ System returns `200 OK` (Step 3.1).<br>❖ System removes the item from the list view immediately. |
| (3.2)-(4) | BR_Error | **Exception Handling Rules:**<br>❖ If a system failure occurs:<br> System logs error (Step 3.2).<br> System returns 500.<br> Show Error (Step 4). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Swipe Delete;
|System|
:(2) DELETE /api/notifications/{id};
|Database|
:(3) DELETE FROM "Notifications" WHERE Id=@id;
if (Success?) then (Yes)
  |System|
  :(3.1) Return OK;
else (No)
  |System|
  :(3.2) Log Error & Return 500;
  |Authenticated User|
  :(4) Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "NotificationItem" as View
control "NotificationsController" as Controller
entity "Notifications" as DB

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
| (2)-(3) | BR1 | **Storing Rules:**<br>❖ User changes a toggle value (e.g., "Email For Likes"). System calls method `UpdateSettings(dto)` (Step 2).<br>❖ System updates the `Settings` JSON column in table “Profiles” (Refer to “Profiles” table in “DB Sheet” file) for the current user (Step 3). |
| (3.1) | BR2 | **Displaying Rules:**<br>❖ System returns updated settings (Step 3.1).<br>❖ System displays a successful notification (Refer to MSG_SUCCESS_SETTINGS_SAVED). |
| (3.2)-(4) | BR_Error | **Exception Handling Rules:**<br>❖ If a system failure occurs:<br> System logs error (Step 3.2).<br> System returns 500.<br> Show Error (Step 4). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Toggle Switch;
|System|
:(2) PATCH /api/profiles/settings;
|Database|
:(3) UPDATE "Profiles" SET Settings...;
if (Success?) then (Yes)
  |System|
  :(3.1) Return Updated Settings;
else (No)
  |System|
  :(3.2) Log Error & Return 500;
  |Authenticated User|
  :(4) Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "NotificationSettingsView" as View
control "ProfilesController" as Controller
entity "Profiles" as DB

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
