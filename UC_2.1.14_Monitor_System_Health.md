# Use Case 2.1.14: Monitor System Health

**Module**: System Administration
**Primary Actor**: System Administrator / DevOps
**Backend Controller**: `SystemHealthController`
**Infrastructure**: `Prometheus`, `Serilog`, `Seq`

---

## 2.1.14.1 Monitor System Health (Overview)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Monitor System Health** |
| **Description** | Central hub for observing application performance and logs. |
| **Actor** | System Administrator |
| **Trigger** | ❖ Admin enters "System Health" section. |
| **Post-condition** | ❖ Admin views metrics or logs. |

### Business Rules (BR)
| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Initialization:**<br>❖ System checks service statuses (DB, Cache, Storage).<br>❖ System displays "Health Status" (Green/Yellow/Red). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Admin|
start
:(1) View Health Hub;
:Choose Function;
split
    -> Metrics;
    :(2.1) Activity\nView Metrics;
split again
    -> Logs;
    :(2.2) Activity\nView Logs;
end split
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Admin" as User
boundary "HealthHub" as View
control "HealthController" as Controller

User -> View: Open Health
View -> Controller: GetHealthStatus()
activate Controller
Controller --> View: StatusDto
deactivate Controller
View -> User: Display Status

opt Metrics
    ref over User, View, Controller: Sequence View Metrics
end

opt Logs
    ref over User, View, Controller: Sequence View Logs
end
@enduml
```

---

## 2.1.14.2 View Real-time Metrics (CPU, RAM, API Latency)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **View Real-time Metrics** |
| **Description** | Monitor resource usage and performance. |
| **Actor** | Admin |
| **Trigger** | ❖ Admin clicks "Metrics". |

### Business Rules (BR)
| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Data Retrieval:**<br>❖ **Frontend**: `MetricsDashboard`. Polls `healthApi.getMetrics()`.<br>❖ **API**: `GET /api/health/metrics`.<br>❖ **Backend**: `SystemHealthController.GetMetrics`.<br>❖ **Infrastructure**: Queries `Prometheus` HTTP API /metrics endpoint. |
| (3.1) | BR_Error | **Exception:**<br>❖ Returns `503 Service Unavailable` if Prometheus unreachable. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Admin|
start
:(1) View Metrics;
|System|
:(2) GET /api/health/metrics;
|Infrastructure|
:(3) Query Prometheus;
|System|
:(4) Return Data;
|Admin|
:(5) View Charts;
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Admin" as User
boundary "MetricsView" as View
control "HealthController" as Controller
participant "Prometheus" as Infra

User -> View: Get Metrics
View -> Controller: GetMetrics()
activate Controller
Controller -> Infra: Query
activate Infra
Infra --> Controller: Data
deactivate Infra
Controller --> View: Data
deactivate Controller
View -> User: Render Graphs
@enduml
```

---

## 2.1.14.3 View Application Logs (Error/Info/Warn)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **View Application Logs** |
| **Description** | Inspect system logs for troubleshooting. |
| **Actor** | Admin |
| **Trigger** | ❖ Admin clicks "Logs". |

### Business Rules (BR)
| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Log Query:**<br>❖ **Frontend**: `LogExplorer`. Calls `healthApi.getLogs({ level: 'Error' })`.<br>❖ **API**: `GET /api/health/logs?level=Error`.<br>❖ **Backend**: `SystemHealthController.GetLogs`.<br>❖ **Infrastructure**: Queries `Seq` or `ElasticSearch` via HTTP Client. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Admin|
start
:(1) Filter Logs;
|System|
:(2) GET /api/health/logs;
|Database|
:(3) SELECT * FROM "Logs";
|System|
:(4) Return PagedResult;
|Admin|
:(5) Analyze Logs;
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Admin" as User
boundary "LogExplorer" as View
control "HealthController" as Controller
entity "AppLogs" as DB

User -> View: Search 'Error'
View -> Controller: GetLogs(level='Error')
activate Controller
Controller -> DB: Query
activate DB
alt Success
    DB --> Controller: Log Entries
    deactivate DB
    Controller --> View: LogDto
    deactivate Controller
    View -> User: Display Table
else Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: Log Error
    Controller --> View: 500 Error
end
@enduml
```
