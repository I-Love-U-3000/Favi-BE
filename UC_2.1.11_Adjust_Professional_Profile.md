# Use Case 2.1.11: Adjust Professional Profile (Creator Tools)

**Module**: Professional Tools
**Primary Actor**: Authenticated User (Creator/Business)
**Backend Controller**: `ProfessionalController`
**Database Tables**: `ProfessionalProfiles`, `Insights`, `AdCampaigns`

---

## 2.1.11.1 Adjust Professional Profile (Overview)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Adjust Professional Profile** |
| **Description** | Central hub for creators to manage professional tools, insights, and advertising. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User accesses the "Professional Dashboard" or "Creator Tools" section. |
| **Post-condition** | ❖ User views insights, manages ads, or toggles professional mode. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Initialization:**<br>❖ The System displays the professional dashboard if the user has a Professional Profile.<br>❖ If not, it offers the option to "Switch to Professional Mode". |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|User|
start
:(1) Open Creator Hub;
:(2) Choose Function;
split
    -> Switch Mode;
    :(2.1) Activity\nSwitch to Pro Mode;
split again
    -> View Insights;
    :(2.2) Activity\nView Dashboard;
split again
    -> Manage Ads;
    :(2.3) Activity\nManage Ad Campaigns;
end split
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "ProfessionalHub" as View
control "ProfessionalController" as Controller

User -> View: Open Tools
View -> Controller: GetStatus()
activate Controller
Controller --> View: ProStatusDto
deactivate Controller
View -> User: Show Options

opt Switch Mode
    ref over User, View, Controller: Sequence Switch Mode
end

opt View Dashboard
    ref over User, View, Controller: Sequence View Dashboard
end

opt Manage Ads
    ref over User, View, Controller: Sequence Manage Ads
end
@enduml
```

---

## 2.1.11.2 Switch to Professional Mode

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Switch to Professional Mode** |
| **Description** | Convert a personal profile to a professional one to unlock insights and tools. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks "Switch to Professional Mode" in settings. |
| **Pre-condition** | ❖ User has a standard personal profile. |
| **Post-condition** | ❖ User profile `IsProfessional` flag is set to True.<br>❖ `ProfessionalProfiles` record is initialized. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Processing:**<br>❖ **Frontend**: `SwitchModeModal` -> Confirm. Calls `proApi.switchMode()`.<br>❖ **API**: `POST /api/professional/switch`.<br>❖ **Backend**: `ProfessionalController.SwitchToProfessional(userId)`.<br>❖ **DB**: `UPDATE Profiles SET IsProfessional=1 WHERE Id=@me`; `INSERT INTO ProfessionalProfiles (Id, Category)`. |
| (3.1) | BR2 | **Completion:**<br>❖ **Response**: `200 OK`.<br>❖ **Frontend**: Refresh user profile. Unlock "Dashboard" link in sidebar. Show "Welcome" modal. |
| (3.2) | BR_Error | **Error:**<br>❖ **DB Error**: `500`. Logged.<br>❖ **Frontend**: "Failed to switch mode". |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Request Switch;
|System|
:(2) POST /api/professional/switch;
|Database|
:(3) UPDATE "Profiles" & INSERT "ProfessionalProfiles";
if (Success?) then (Yes)
  |System|
  :(3.1) Return OK;
  |Authenticated User|
  :(4) Show Welcome;
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
actor "User" as User
boundary "SettingsView" as View
control "ProfessionalController" as Controller
entity "Profiles" as DB

User -> View: Click Switch
View -> Controller: SwitchToProfessional()
activate Controller
Controller -> DB: Update IsProfessional=1
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: 200 OK
    deactivate Controller
    View -> User: Show Welcome Modal
else Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: LogError
    Controller --> View: 500 Error
    View -> User: Show Error
end
@enduml
```

---

## 2.1.11.3 View Professional Dashboard (Insights)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **View Professional Dashboard** |
| **Description** | View real-time reach, engagement, and follower growth metrics. |
| **Actor** | Professional User |
| **Trigger** | ❖ User clicks "Professional Dashboard". |
| **Pre-condition** | ❖ User is in Professional Mode. |
| **Post-condition** | ❖ System displays charts and key performance indicators (KPIs). |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Query:**<br>❖ **Frontend**: `ProDashboard`. Calls `proApi.getInsights({ days: 7 })`.<br>❖ **API**: `GET /api/professional/insights?days=7`.<br>❖ **Backend**: `ProfessionalController.GetInsights`.<br>❖ **DB**: `SELECT SUM(Impressions), SUM(Reach) FROM Insights WHERE ProfileId=@me AND Date >= @startDate`. |
| (4) | BR2 | **Rendering:**<br>❖ **Response**: `200 OK` (InsightsDto).<br>❖ **Frontend**: Renders `Recharts` graphs (Line/Bar) for Reach and Engagement. |
| (4.1) | BR_Error | **Error:**<br>❖ **Server**: `500`.<br>❖ **Frontend**: Shows "Data unavailable" placeholder. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Professional User|
start
:(1) Open Dashboard;
|System|
:(2) GET /api/professional/insights;
|Database|
:(3) SELECT SUM(Metrics) FROM "Insights";
|System|
:(4) Return Data;
|Professional User|
:(5) View Charts;
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "DashboardView" as View
control "ProfessionalController" as Controller
entity "Insights" as DB

User -> View: Open Insights
View -> Controller: GetInsights(7days)
activate Controller
Controller -> DB: Aggregate Metrics
activate DB
alt Success
    DB --> Controller: Data
    deactivate DB
    Controller --> View: InsightsDto
    deactivate Controller
    View -> User: Render Charts
else Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: LogError
    Controller --> View: 500 Error
    View -> User: Show Error Placeholder
end
@enduml
```

---

## 2.1.11.4 Manage Ad Campaigns (Boost Post)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Manage Ad Campaigns** |
| **Description** | Create or monitor paid boosts for posts. |
| **Actor** | Professional User |
| **Trigger** | ❖ User clicks "Boost Post" on a specific post. |
| **Post-condition** | ❖ Ad Campaign created and submitted for review. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Processing:**<br>❖ **Frontend**: `BoostPostModal`. Calls `adsApi.createCampaign({ postId, budget, duration })`.<br>❖ **API**: `POST /api/ads/campaigns`.<br>❖ **Backend**: `AdsController.CreateCampaign`.<br>❖ **DB**: `INSERT INTO AdCampaigns (PostId, Budget, Status='Pending')`. |
| (4) | BR2 | **Payment:**<br>❖ **Mock**: `_paymentGateway.Authorize(amount)`.<br>❖ **Response**: `201 Created`.<br>❖ **Frontend**: Toast "Boost submitted for review". |
| (5) | BR_Error | **Error:**<br>❖ **Payment Fail**: `402 Payment Required`.<br>❖ **Server**: `500`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Professional User|
start
:(1) Configure Boost;
:Submit;
|System|
:(2) POST /api/ads/campaigns;
|Database|
:(3) INSERT "AdCampaigns";
if (Success?) then (Yes)
  |System|
  :(4) Return CampaignDto;
  |Professional User|
  :(5) View "Pending" Status;
else (No)
  |System|
  :(3.1) Log Error;
  |Professional User|
  :(6) Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "BoostPostModal" as View
control "AdsController" as Controller
entity "AdCampaigns" as DB

User -> View: Submit Boost
View -> Controller: CreateCampaign(dto)
activate Controller
Controller -> DB: Insert (Pending)
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: 201 Created
    deactivate Controller
    View -> User: Show Success Toast
else Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: LogError
    Controller --> View: 500 Error
    View -> User: Show Error
end
@enduml
```
