# Use Case 2.1.10: Supervise Content (Reporting)

**Module**: Supervision / Moderation
**Primary Actor**: Authenticated User
**Backend Controller**: `Favi_BE.API.Controllers.ReportsController`
**Database Tables**: `"Reports"`

---

## 2.1.10.1 Supervise Content (Overview)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Supervise Content** |
| **Description** | Central hub for content moderation and reporting. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User accesses reporting functions or support center. |
| **Post-condition** | ❖ User submits reports or tracks report status. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Initialization:**<br>❖ The System allows users to flag content for review.<br>❖ The System provides a history view of submitted reports. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|User|
start
:(1) Supervise Content;
:Choose Function;
split
    -> Report Content;
    :(2.1) Activity\nReport Post/User;
split again
    -> View History;
    :(2.2) Activity\nView Report History;
end split
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "ContentHub" as View
control "ReportsController" as Controller

User -> View: Interact
View -> User: Display Options

opt Report
    ref over User, View, Controller: Sequence Report Content
end

opt View History
    ref over User, View, Controller: Sequence View History
end
@enduml
```

---

## 2.1.10.2 Report Post / Comment / User

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Report Post / Comment / User** |
| **Description** | Submit a report against a violation. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks "Report" from the content options menu. |
| **Pre-condition** | ❖ Target content exists.<br>❖ User is allowed to view the content. |
| **Post-condition** | ❖ A "Report" record is created in the database.<br>❖ Admins are notified (if configured). |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Submission:**<br>❖ **Frontend**: `ReportDialog`. User selects reason. Calls `reportApi.create({ targetId, targetType, reason })`. |
| (2)-(4) | BR2 | **Processing:**<br>❖ **API**: `POST /api/reports`.<br>❖ **Backend**: `ReportsController.Create(dto)`. Checks `_privacy.CanReport`.<br>❖ **DB**: `INSERT INTO Reports (ReporterId, TargetId, Reason, Status='Pending')`. |
| (4.1)-(5) | BR3 | **Completion:**<br>❖ **Response**: `201 Created`.<br>❖ **Frontend**: Shows "Report submitted" confirmation. Dialog closes. |
| (4.2)-(6) | BR_Error | **Error:**<br>❖ **Invalid**: `400`.<br>❖ **Server**: `500`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:Select Report Option;
|System|
:Show Reason Dialog;
|Authenticated User|
:(1) Select Reason & Submit;
|System|
:(2) POST /api/reports;
:(3) Validate Request;
|Database|
:(4) INSERT INTO "Reports";
if (Save Success?) then (Yes)
  |System|
  :(4.1) Return ReportResponse;
  |Authenticated User|
  :(5) Show "Thanks for reporting";
else (No)
  |System|
  :(4.2) Log Error & Return 500;
  |Authenticated User|
  :(6) Show Error Message;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "ReportDialog (Mock)" as View
control "ReportsController" as Controller
entity "Reports" as Entity

User -> View: Select Reason & Submit
activate View
View -> Controller: Create(ReportDto)
activate Controller
Controller -> Controller: Validate (PrivacyGuard)
Controller -> Entity: Insert Report (Status=Pending)
activate Entity
alt Success
    Entity --> Controller: Return Created Entity
    deactivate Entity
    Controller --> View: Return OK (200)
    deactivate Controller
    View --> User: Show Success Toast
else Database Error
    activate Entity
    Entity --> Controller: Exception
    deactivate Entity
    Controller -> Controller: LogError(ex)
    Controller --> View: Return Error (500)
    deactivate Controller
    View --> User: Show Error Message
end
deactivate View
@enduml
```

---

## 2.1.10.3 View My Report History

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **View My Report History** |
| **Description** | Track status of submitted reports. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User navigates to the Help/Support center. |
| **Pre-condition** | ❖ User has submitted reports previously. |
| **Post-condition** | ❖ System displays a list of reports with their current status. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Query:**<br>❖ **Frontend**: `SupportCenter`. Calls `reportApi.getMyReports()`.<br>❖ **API**: `GET /api/reports/my`.<br>❖ **Backend**: `ReportsController.GetMyReports`.<br>❖ **DB**: `SELECT * FROM Reports WHERE ReporterId=@me`. |
| (4)-(5) | BR2 | **Display:**<br>❖ **Response**: `200 OK` (List).<br>❖ **Frontend**: Renders list using badges: "Open" (Green), "Closed" (Gray). |
| (6) | BR_Error | **Error:**<br>❖ **Server**: `500`. Logged. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Open Support Inbox;
|System|
:(2) GET /api/reports/my;
|Database|
:(3) SELECT * FROM "Reports" 
WHERE ReporterId=@me;
|System|
:(4) Return List;
|Authenticated User|
:(5) View Status;
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "SupportInboxView" as View
control "ReportsController" as Controller
entity "Reports" as DB

User -> View: Open Page
View -> Controller: GetMyReports()
activate Controller
Controller -> DB: Query by ReporterId
activate DB
DB --> Controller: List<Report>
deactivate DB
Controller --> View: List<ReportResponse>
deactivate Controller
View -> User: Render List
@enduml
```
