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
| (1) | BR1 | **Initialization:**<br>❖ The **System** allows users to flag content for review.<br>❖ The **System** provides a history view of submitted reports. |

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
| (1) | BR1 | **Submission:**<br>❖ The **Frontend** shows a `ReportDialog` where the user selects a reason.<br>❖ Upon confirmation, it calls `reportApi.create({ targetId, targetType, reason })`. |
| (2)-(4) | BR2 | **Processing:**<br>❖ The **API** receives a `POST` request at `/api/reports`.<br>❖ The **Backend** `ReportsController.Create(dto)` first checks `_privacy.CanReport`.<br>❖ The **Database** inserts a new record into `Reports` with `ReporterId`, `TargetId`, `Reason`, and sets `Status='Pending'`. |
| (4.1)-(5) | BR3 | **Completion:**<br>❖ The **System** returns `201 Created`.<br>❖ The **Frontend** displays a "Report submitted" confirmation and closes the dialog. |
| (4.2)-(6) | BR_Error | **Error:**<br>❖ If request is **Invalid**, the **System** returns `400`.<br>❖ For **Server** errors, it returns `500`. |

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
| (2)-(3) | BR1 | **Query:**<br>❖ The **Frontend** `SupportCenter` calls `reportApi.getMyReports()`.<br>❖ The **API** receives `GET /api/reports/my`.<br>❖ The **Backend** `ReportsController.GetMyReports` executes the query.<br>❖ The **Database** selects all records from `Reports` where `ReporterId` matches the current user. |
| (4)-(5) | BR2 | **Display:**<br>❖ The **System** returns `200 OK` with the list.<br>❖ The **Frontend** renders the list using badges to indicate status (e.g., "Open" in Green, "Closed" in Gray). |
| (6) | BR_Error | **Error:**<br>❖ If a **Server** error occurs, the **System** returns `500` and logs it. |

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
