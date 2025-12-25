# Use Case 2.1.10: Supervise Content (Reporting)

**Module**: Supervision / Moderation
**Primary Actor**: Authenticated User
**Backend Controller**: `Favi_BE.API.Controllers.ReportsController`
**Database Tables**: `"Reports"`

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
| (5) | BR5 | **Displaying Rules:**<br>The system returns the created `ReportResponse` object to the client. |
| (6) | BR6 | **Displaying Rules:**<br>The UI displays a "Thank you" toast message, informing the user that the report has been received. |
| (7) | BR_Error | **Exception Handling Rules:**<br>If a system failure occurs, the Global Exception Handler logs the error and returns a `500 Internal Server Error`. |

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
  :(5) Return ReportResponse;
  |Authenticated User|
  :(6) Show "Thanks for reporting";
else (No)
  |System|
  :(5a) Log Error;
  :(5b) Return Error (500);
  |Authenticated User|
  :(6a) Show Error Message;
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
| (1) | BR1 | **Selecting Rules:**<br>User goes to Help Center -> "My Support Requests". |
| (2) | BR2 | **Querying Rules:**<br>System calls `ReportsController.GetMyReports()`.<br>SQL: `SELECT * FROM "Reports" WHERE "ReporterId" = @me`. |
| (3) | BR3 | **Displaying Rules:**<br>System displays list. Items show Status (Pending/Resolved). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:Open Support Inbox;
|System|
:GET /api/reports/my;
|Database|
:SELECT * FROM "Reports" 
WHERE ReporterId=@me;
|System|
:Return List;
|Authenticated User|
:View Status;
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
actor "Authenticated User" as User
participant "SupportInboxView" as View
participant "ReportsController" as Controller
participant "Reports" as DB

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
