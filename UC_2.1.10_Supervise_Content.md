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
| (1) | BR1 | **Submitting Rules:**<br>When the user selects a reason from the "Report" dialog and clicks "Submit", the system captures the input. |
| (2) | BR2 | **Processing Rules:**<br>The system calls `ReportsController.Create` (`POST /api/reports`) with the `ReportDto` containing the target ID and reason. |
| (3) | BR3 | **Validation Rules:**<br>The system validates the request (e.g., checking if the user is allowed to report this content via `PrivacyGuard`). |
| (4) | BR4 | **Storing Rules:**<br>The database creates a new record in the `Reports` table with `Status = Pending` for admin review. |
| (4.1) | BR5 | **Displaying Rules:**<br>The system returns the created `ReportResponse` object to the client (Step 4.1). |
| (5) | BR6 | **Displaying Rules:**<br>The UI displays a "Thank you" toast message, informing the user that the report has been received (Step 5). |
| (4.2)-(6) | BR_Error | **Exception Handling Rules:**<br>If a system failure occurs, the Global Exception Handler logs the error (Step 4.2) and returns a `500 Internal Server Error`.<br>System shows error (Step 6). |

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
| (2)-(3) | BR1 | **Querying Rules:**<br>❖ System calls method `ReportsController.GetMyReports()`.<br>❖ System queries data in the table “Reports” in the database (Refer to “Reports” table in “DB Sheet” file) with syntax `SELECT * FROM Reports WHERE ReporterId = [User.ID]`.<br>❖ System includes Status information (Pending, Resolved). |
| (4)-(5) | BR2 | **Display Workflow:**<br>❖ System displays a “ReportList” screen (Refer to “ReportList” view in “View Description” file).<br>❖ System renders the items with status badges:<br> **Pending**: Displayed with "Open" badge (Refer to “BadgeOpen” style).<br> **Resolved**: Displayed with "Closed" badge (Refer to “BadgeClosed” style). |
| (6) | BR_Error | **Exception Handling Rules:**<br>❖ If a system failure occurs:<br> System logs the error.<br> System returns `500 Internal Server Error`. |

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
