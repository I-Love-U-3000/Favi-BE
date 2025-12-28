# Use Case 2.1.13: Admin User-Level Moderation

**Module**: Administration / Moderation
**Primary Actor**: System Administrator
**Backend Controller**: `AdminController`
**Database Tables**: `Reports`, `Profiles`

---

## 2.1.13.1 Admin Moderation (Overview)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Admin Moderation** |
| **Description** | Central hub for reviewing and acting on user reports. |
| **Actor** | System Administrator |
| **Trigger** | ❖ Admin enters the "Moderation Dashboard". |
| **Post-condition** | ❖ Admin views pending reports or takes action. |

### Business Rules (BR)
| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Initialization:**<br>❖ System loads list of relevant Reports (Status=Pending).<br>❖ System displays summary stats (Total Open, High Priority). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Admin|
start
:(1) View Mod Dashboard;
:Choose Function;
split
    -> View Reports;
    :(2.1) Activity\nView All Reports;
split again
    -> Resolve;
    :(2.2) Activity\nAccept/Deny Report;
end split
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Admin" as User
boundary "ModHub" as View
control "AdminController" as Controller

User -> View: Open Dashboard
View -> Controller: GetModStats()
activate Controller
Controller --> View: StatsDto
deactivate Controller
View -> User: Display Actions

opt View Reports
    ref over User, View, Controller: Sequence View Reports
end

opt Resolve
    ref over User, View, Controller: Sequence Accept/Deny
end
@enduml
```

---

## 2.1.13.2 View all user report

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **View all user report** |
| **Description** | List all reports against users or content. |
| **Actor** | System Administrator |
| **Trigger** | ❖ Admin clicks "Reports" tab. |

### Business Rules (BR)
| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Query:**<br>❖ **Frontend**: `AdminReports`. Calls `adminApi.getReports(filter)`.<br>❖ **API**: `GET /api/admin/reports`.<br>❖ **Backend**: `AdminController.GetReports`.<br>❖ **DB**: `SELECT * FROM Reports r JOIN Profiles p ON r.ReporterId = p.Id`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Admin|
start
:(1) Open Reports;
|System|
:(2) GET /api/admin/reports;
|Database|
:(3) SELECT * FROM "Reports";
|System|
:(4) Return List;
|Admin|
:(5) View Grid;
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Admin" as User
boundary "ReportListView" as View
control "AdminController" as Controller
entity "Reports" as DB

User -> View: View List
View -> Controller: GetAllReports(filter)
activate Controller
Controller -> DB: Select
activate DB
alt Success
    DB --> Controller: List
    deactivate DB
    Controller --> View: PagedResult
    deactivate Controller
    View -> User: Render Grid
else Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: LogError
    Controller --> View: 500 Error
end
@enduml
```

---

## 2.1.13.3 Accept/Deny user report

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Accept/Deny user report** |
| **Description** | Resolve a report by banning/warning (Accept) or dismissing (Deny). |
| **Actor** | System Administrator |
| **Trigger** | ❖ Admin selects action on a report. |

### Business Rules (BR)
| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1)-(2) | BR1 | **Process:**<br>❖ **Frontend**: Click "Ban User & Resolve". Calls `adminApi.resolve({ reportId, action: 'Ban' })`.<br>❖ **API**: `POST /api/admin/reports/{id}/resolve`.<br>❖ **Backend**: `AdminController.ResolveReport`.<br>❖ **DB**: Transaction:<br> 1. `UPDATE Reports SET Status='Resolved'.`<br> 2. `INSERT INTO UserModerations (Ban)...` |
| (3) | BR2 | **Audit:**<br>❖ `INSERT INTO AuditLogs (AdminId, Action, Timestamp)`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Admin|
start
:(1) Select Action;
if (Accept?) then (Yes)
  :(2) Ban User / Delete Content;
else (No)
  :(3) Dismiss Report;
endif
|System|
:(4) POST /api/admin/reports/resolve;
|Database|
:(5) UPDATE "Reports";
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Admin" as User
boundary "ReportDetailView" as View
control "AdminController" as Controller
entity "Reports" as DB

User -> View: Click Resolve (Accept/Deny)
View -> Controller: ResolveReport(id, action)
activate Controller
Controller -> DB: Update Status
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: OK
else Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: LogError
    Controller --> View: 500 Error
end
@enduml
```
