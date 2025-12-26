# Use Case 2.1.12: Adjust Community Group (Advanced Governance)

**Module**: Communities / Groups
**Primary Actor**: Authenticated User
**Backend Controller**: `GroupsController`
**Database Tables**: `Groups`, `GroupMembers`, `GroupPosts`, `GroupJoinRequests`

---

## 2.1.12.1 Adjust Group (Overview)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Adjust Group** |
| **Description** | Central hub for interacting with community groups. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User enters a Group Page. |
| **Post-condition** | ❖ User views content, joins/leaves, or moderates the group. |

### Business Rules (BR)
| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Initialization:**<br>❖ System loads Group details and permissions based on the user's role (Admin, Moderator, Member, Guest).<br>❖ System displays options relevant to the role (e.g., "Settings" for Admins). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|User|
start
:(1) View Group Hub;
:Choose Function;
split
    -> Create;
    :(2.1) Activity\nCreate Group;
split again
    -> Settings;
    :(2.2) Activity\nUpdate Settings;
split again
    -> Join/Leave;
    :(2.3) Activity\nJoin/Leave Group;
split again
    -> Requests;
    :(2.4) Activity\nManage Requests;
split again
    -> Members;
    :(2.5) Activity\nManage Members;
split again
    -> Moderation;
    :(2.6) Activity\nModerate Content;
end split
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "GroupHub" as View
control "GroupsController" as Controller

User -> View: Open Group
View -> Controller: GetDetails()
activate Controller
Controller --> View: GroupDto + Permissions
deactivate Controller
View -> User: Display Actions

opt Create
    ref over User, View, Controller: Sequence Create Group
end

opt Settings
    ref over User, View, Controller: Sequence Update Settings
end

opt Join/Leave
    ref over User, View, Controller: Sequence Join/Leave
end
@enduml
```

---

## 2.1.12.2 Create Group

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Create Group** |
| **Description** | Establish a new community. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks "Create Group". |
| **Post-condition** | ❖ Group created, User becomes Admin. |

### Business Rules (BR)
| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Creation:**<br>❖ System validates Name and Privacy (Public/Private) (Step 2).<br>❖ System inserts `Groups` record (Step 3).<br>❖ System adds the Creator to `GroupMembers` as ROLE_ADMIN. |
| (4) | BR_Error | **Exception:**<br>❖ Log Error, Return 500, Show Error. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|User|
start
:(1) Submit Form;
|System|
:(2) POST /api/groups;
|Database|
:(3) INSERT "Groups" & "GroupMembers" (Admin);
if (Success?) then (Yes)
  |System|
  :(4) Return Id;
  |User|
  :(5) Redirect to Group;
else (No)
  :(3.1) Log Error;
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
boundary "CreateGroupView" as View
control "GroupsController" as Controller
entity "Groups" as DB

User -> View: Create(dto)
View -> Controller: CreateGroup(dto)
activate Controller
Controller -> DB: Insert Group & Member
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: GroupDto
    deactivate Controller
    View -> User: Open Group
else Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: Log Error
    Controller --> View: 500 Error
    View -> User: Show Error
end
@enduml
```

---

## 2.1.12.3 Update Group Settings (Rules, Privacy)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Update Group Settings** |
| **Description** | Modify rules, privacy, or cover photo. |
| **Actor** | Group Admin |
| **Trigger** | ❖ Admin saves changes in Settings. |

### Business Rules (BR)
| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Update Logic:**<br>❖ System checks Admin Permissions (Step 2).<br>❖ System updates `Groups` table (Step 3). |
| (4) | BR2 | **Display:**<br>❖ UI refreshes Group Info. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Admin|
start
:(1) Update Info;
|System|
:(2) PUT /api/groups/{id};
|Database|
:(3) UPDATE "Groups";
if (Success?) then (Yes)
  :(3.1) Return OK;
else (No)
  :(3.2) Log Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Admin" as User
boundary "GroupSettings" as View
control "GroupsController" as Controller
entity "Groups" as DB

User -> View: Save
View -> Controller: UpdateGroup(id, dto)
activate Controller
Controller -> DB: Update
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: 200 OK
    deactivate Controller
    View -> User: Show Success
else Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: Log Error
    Controller --> View: 500 Error
    View -> User: Show Error
end
@enduml
```

---

## 2.1.12.4 Join / Leave Group

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Join / Leave Group** |
| **Description** | Request access or exit a group. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks Join or Leave. |

### Business Rules (BR)
| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2) | BR1 | **Type Check:**<br>❖ If Public: Add directly to `GroupMembers`.<br>❖ If Private: Add to `GroupJoinRequests`. |
| (3) | BR2 | **Leaving:**<br>❖ Remove from `GroupMembers`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|User|
start
:Click Action;
if (Join?) then (Yes)
  |System|
  :(2) Check Privacy;
  if (Public?) then (Yes)
    :(3) INSERT "GroupMembers";
  else (No)
    :(3.1) INSERT "GroupJoinRequests";
  endif
else (Leave)
  :(4) DELETE "GroupMembers";
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "GroupHeader" as View
control "GroupsController" as Controller
entity "GroupMembers" as DB

User -> View: Click Join
View -> Controller: JoinGroup(id)
activate Controller
Controller -> DB: Add Member/Request
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: Status (Joined/Pending)
    deactivate Controller
    View -> User: Update Button
else Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: Log Error
    Controller --> View: 500 Error
end
@enduml
```

---

## 2.1.12.5 Manage Join Requests

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Manage Join Requests** |
| **Description** | Approve or Reject pending members. |
| **Actor** | Group Admin / Moderator |

### Business Rules (BR)
| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Decision:**<br>❖ Approve: Move from `Requests` to `Members`.<br>❖ Reject: Delete from `Requests`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Admin|
start
:(1) Approve/Reject;
|System|
:(2) POST /api/groups/requests;
|Database|
:(3) UPDATE "GroupMembers" / DELETE "Requests";
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Admin" as User
boundary "RequestsView" as View
control "GroupsController" as Controller
entity "GroupJoinRequests" as DB

User -> View: Click Approve
View -> Controller: ApproveRequest(id)
activate Controller
Controller -> DB: Move to Members
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: OK
else Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: Log Error
    Controller --> View: 500 Error
end
@enduml
```

---

## 2.1.12.6 Manage Group Members (Ban/Mute)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Manage Group Members** |
| **Description** | Kick, Ban, or Promote members. |
| **Actor** | Group Admin |

### Business Rules (BR)
| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Action:**<br>❖ Admin selects member and action (Kick/Ban).<br>❖ System updates `GroupMembers` (Role or Status). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Admin|
start
:(1) Select Member Action;
|System|
:(2) PUT /api/groups/members;
|Database|
:(3) UPDATE "GroupMembers";
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Admin" as User
boundary "MemberListView" as View
control "GroupsController" as Controller
entity "GroupMembers" as DB

User -> View: Ban User
View -> Controller: BanMember(id)
activate Controller
Controller -> DB: Update Status=Banned
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: OK
else Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: Log Error
    Controller --> View: 500 Error
end
@enduml
```

---

## 2.1.12.7 Moderate Group Content (Approve/Reject Queue)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Moderate Group Content** |
| **Description** | Review posts held for moderation. |
| **Actor** | Group Admin / Moderator |

### Business Rules (BR)
| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Review:**<br>❖ Admin views pending posts.<br>❖ Approve: `Posts.Status` = Active.<br>❖ Reject: `Posts.IsDeleted` = 1. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Moderator|
start
:(1) Review Post;
if (Approve?) then (Yes)
  :(2) Set Active;
else (No)
  :(3) Delete;
endif
|Database|
:(4) UPDATE "Posts";
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Moderator" as User
boundary "ModQueueView" as View
control "GroupsController" as Controller
entity "Posts" as DB

User -> View: Approve Post
View -> Controller: ApprovePost(id)
activate Controller
Controller -> DB: Update Status=Active
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: OK
else Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: Log Error
    Controller --> View: 500 Error
end
@enduml
```
