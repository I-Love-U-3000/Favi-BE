# Use Case 2.1.7: Chat (Communication)

**Module**: Communication
**Primary Actor**: Authenticated User
**Backend Controller**: `Favi_BE.API.Controllers.ChatController`
**Database Tables**: `"Conversations"`, `"UserConversations"`, `"Messages"`

---

## 2.1.7.1 Chat (Overview)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Chat Overview** |
| **Description** | Central hub for real-time communication (Inbox, Direct Messages, Group Chats). |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks the "Chat" icon. |
| **Post-condition** | ❖ User manages conversations or messages. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Initialization:**<br>❖ The System fetches the user's active conversations sorted by recent activity.<br>❖ The System connects to the Real-time SignalR Hub to receive live updates. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|User|
start
:(1) Open Chat Hub;
:Choose Function;
split
    -> View Inbox;
    :(2.1) Activity\nView Conversations;
split again
    -> New DM;
    :(2.2) Activity\nCreate Chat (DM);
split again
    -> New Group;
    :(2.3) Activity\nCreate Group Chat;
split again
    -> Send/Reply;
    :(2.4) Activity\nSend Message;
split again
    -> Unsend;
    :(2.5) Activity\nDelete Message;
split again
    -> Search;
    :(2.6) Activity\nSearch History;
split again
    -> Mark Read;
    :(2.7) Activity\nMark Chat Read;
end split
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "ChatHub" as View
control "ChatController" as Controller

User -> View: Open Chat
View -> Controller: GetConversations()
activate Controller
Controller --> View: List<ConversationDto>
deactivate Controller
View -> User: Display Inbox

opt Create DM
    ref over User, View, Controller: Sequence Create Chat
end

opt Create Group
    ref over User, View, Controller: Sequence Create Group
end

opt Send Message
    ref over User, View, Controller: Sequence Send Message
end

opt Unsend
    ref over User, View, Controller: Sequence Unsend Message
end
@enduml
```

---

## 2.1.7.2 Create Chat (Direct Message)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Create Chat (Direct Message)** |
| **Description** | Start a new 1-on-1 chat. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks the [btnMessage] on another user's profile. |
| **Pre-condition** | ❖ Target user accepts messages (privacy settings allow). |
| **Post-condition** | ❖ A new conversation is created (or existing one retrieved).<br>❖ System navigates to the Chat Window. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Conversation Discovery:**<br>❖ System calls method `GetOrCreateDm(targetId)`.<br>❖ System queries table “UserConversations” in the database to find a common `ConversationId` (Type=DM) between [User.ID] and [Target.ID].<br> **If Found**: System retrieves the existing ID and moves to step (3.1).<br> **If Not Found**: System moves to step (3.2) to create a new conversation record in “Conversations” table and inserts links in “UserConversations” (Steps 4-5). |
| (5.1)-(6) | BR3 | **Displaying Rules:**<br>❖ After obtaining the ID (Step 5.1), System displays a “ChatWindow” screen (Refer to “ChatWindow” view in “View Description” file) for the specific conversation (Step 6).<br>❖ System initiates the connection to the SignalR hub for real-time updates. |
| (5.2) | BR_Error | **Exception Handling Rules:**<br>❖ If a system failure occurs during creation:<br> System logs the error (Step 5.2).<br> System returns `500 Internal Server Error`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Click Message Button;
|System|
:(2) Check Existing DM;
|Database|
:(3) Query "UserConversations";
if (Found Existing?) then (Yes)
  |System|
  :(3.1) Return ConversationId;
else (No)
  |Database|
  :(3.2) Prepare Creation;
  :(4) INSERT INTO "Conversations";
  :(5) INSERT INTO "UserConversations";
  if (Save Success?) then (Yes)
      |System|
      :(5.1) Return New Id;
  else (No)
    |System|
    :(5.2) Log Error Return 500;
    stop
  endif
endif
|Authenticated User|
:(6) Open Chat Window;
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "ProfileDetailView (Mock)" as View
control "ChatController" as Controller
entity "Conversations" as Entity

User -> View: Click Message
activate View
View -> Controller: GetOrCreateDm(targetId)
activate Controller
Controller -> Entity: Check Existing DM
activate Entity
Entity --> Controller: Result
deactivate Entity

alt Found
  Controller --> View: Return Existing ConversationId
else Not Found
  Controller -> Entity: Create New Conversation
  activate Entity
  alt Success
      Entity --> Controller: Success
      deactivate Entity
      Controller --> View: Return New ConversationId
  else Database Error
      activate Entity
      Entity --> Controller: Exception
      deactivate Entity
      Controller -> Controller: LogError(ex)
      Controller --> View: Return Error (500)
  end
end
deactivate Controller
View -> User: Navigate to ChatWindow
deactivate View
@enduml
```

---

## 2.1.7.3 Create Group Chat

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Create Group Chat** |
| **Description** | Create a room with multiple users. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User initiates "New Group" and selects members. |
| **Pre-condition** | ❖ At least 2 other users are selected. |
| **Post-condition** | ❖ A group conversation record is created.<br>❖ All selected users are added as participants. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1)-(2) | BR1 | **Selecting Rules:**<br>❖ User navigates to “Create Group” form (Refer to “GroupCreation” view in “View Description” file).<br>❖ User selects members from the friend list (Step 1) and enters a Group Name (Step 2).<br>❖ User clicks the `[btnCreate]` button. system moves to step (3). |
| (3) | BR2 | **Group Validation Rule:**<br>❖ System validates the participant count.<br> **Invalid**: If Count < 2, System displays an error message (Refer to MSG_ERR_MIN_MEMBERS) (Step 3.1).<br> **Valid**: System moves to step (3.2). |
| (3.2)-(5) | BR3 | **Processing & Storing Rules:**<br>❖ System calls method `CreateGroup(dto)` (Step 3.2).<br>❖ System inserts a new record into table “Conversations” (Refer to “Conversations” table in “DB Sheet” file) with `Type` = 1 (Group) (Step 4).<br>❖ System inserts multiple records into “UserConversations” table for each selected member (Step 5). |
| (5.1)-(6) | BR4 | **Displaying Rules:**<br>❖ After creation, System returns Summary (Step 5.1).<br>❖ System displays the “ChatWindow” screen (Refer to “ChatWindow” view in “View Description” file) for the new group (Step 6).<br>❖ System sends a System Message to the chat: "Group created by [User]". |
| (5.2)-(7) | BR_Error | **Exception Handling Rules:**<br>❖ If a system failure occurs:<br> System logs the error (Step 5.2).<br> System returns `500 Internal Server Error`.<br> System shows error to user (Step 7). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Select Members;
:(2) Enter Name & Create;
|System|
:(3) Validate Members (> 1);
if (Valid?) then (No)
  :(3.1) Show Error;
  stop
else (Yes)
  :(3.2) POST /api/chat/group;
  |Database|
  :(4) INSERT INTO "Conversations";
  :(5) Batch INSERT "UserConversations";
  if (Save Success?) then (Yes)
      |System|
      :(5.1) Return Summary;
      |Authenticated User|
      :(6) Open Group Chat;
  else (No)
      |System|
      :(5.2) Log Error & Return 500;
      |Authenticated User|
      :(7) Show Error;
  endif
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "NewGroupView" as View
control "ChatController" as Controller
entity "AppDbContext" as DB

User -> View: Submit Form
View -> Controller: CreateGroup(name, ids)
activate Controller
Controller -> DB: Add Conversation
Controller -> DB: Add Participants
Controller -> DB: SaveChanges()
alt Success
    DB --> Controller: Success
    deactivate Controller
    Controller --> View: GroupDto
    deactivate Controller
    View -> User: Navigate to Room
else Database Error
    DB --> Controller: Exception
    Controller -> Controller: LogError(ex)
    Controller --> View: Error (500)
    deactivate Controller
    View -> User: Show Error
end
@enduml
```

---

## 2.1.7.4 Reply Message / Send Message

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Reply Message / Send Message** |
| **Description** | Sending a text message. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User types text and clicks [btnSend] inside a chat window. |
| **Pre-condition** | ❖ Conversation exists.<br>❖ User is a participant of the conversation. |
| **Post-condition** | ❖ Message is saved to database.<br>❖ Other participants receive the message (Real-time). |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1)-(2) | BR1 | **Submitting Rules:**<br>When the user types a message and hits Enter/Send (Step 1), the system performs an Optimistic UI update (Step 2) to show the message immediately as "Sending". |
| (3)-(5) | BR2 | **Processing & Storing Rules:**<br>❖ System calls method `SendMessage(content, convId)` (Step 3).<br>❖ System inserts a new record into table “Messages” (Refer to “Messages” table in “DB Sheet” file) (Step 4).<br>❖ System updates `LastMessageAt` in table “Conversations” (Step 5).<br>❖ System broadcasts the message via SignalR to other participants. |
| (5.1)-(6) | BR3 | **Displaying Rules:**<br>❖ The UI receives the `MessageDto` acknowledgement (Step 5.1).<br>❖ The UI updates the message status from "Sending..." to "Sent" (Refer to “MessageStatus” view in “View Description” file) (Step 6). |
| (5.2)-(7) | BR_Error | **Exception Handling Rules:**<br>❖ If a system failure occurs:<br> System logs error (Step 5.2).<br> System returns `500 Error`.<br> UI marks the message as "Failed" (red icon) (Step 7). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Send Message;
|System|
:(2) Optimistic Update UI;
:(3) POST /api/chat/messages;
|Database|
:(4) INSERT INTO "Messages";
:(5) UPDATE "Conversations";
if (Save Success?) then (Yes)
    |System|
    :(5.1) Return MessageDto;
    |Authenticated User|
    :(6) Status changes to "Sent";
else (No)
    |System|
    :(5.2) Log Error & Return 500;
    |Authenticated User|
    :(7) Status changes to "Failed";
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "ChatWindowView" as View
control "ChatController" as Controller
entity "Messages" as DB_Msg
entity "Conversations" as DB_Conv

User -> View: Send "Hello"
View -> View: Add Bubble (Pending)
View -> Controller: SendMessage(id, "Hello")
activate Controller
Controller -> DB_Msg: Add Message
Controller -> DB_Conv: Update Timestamp
activate DB_Conv
alt Success
    DB_Conv --> Controller: Done
    deactivate DB_Conv
    Controller --> View: MessageDto (Real ID)
    deactivate Controller
    View -> View: Update Bubble (Sent)
else Error
    DB_Conv --> Controller: Exception
    deactivate DB_Conv
    Controller -> Controller: LogError(ex)
    Controller --> View: Error (500)
    deactivate Controller
    View -> View: Update Bubble (Failed)
end
@enduml
```

---

## 2.1.7.5 Delete Chat / Message (Unsend)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Delete Chat / Message (Unsend)** |
| **Description** | Unsend a specific message. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User long-presses a message and selects "Unsend". |
| **Pre-condition** | ❖ Message was sent by the user.<br>❖ Time elapsed is within the allowable limit (e.g., 15 mins). |
| **Post-condition** | ❖ Message content is removed or marked as deleted in the database. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2) | BR2 | **Unsend Eligibility Check:**<br>❖ System/Frontend checks the timestamp of the message.<br> **Time > 15m**: The action is disabled or rejected (Refer to MSG_ERR_UNSEND_TIMEOUT) (Step 2.1).<br> **Time <= 15m**: System proceeds to step (2.2). |
| (2.2)-(3) | BR3 | **Processing & Storing Rules:**<br>❖ System calls method `DeleteMessage(msgId)` (Step 2.2).<br>❖ System performs a Soft Delete updates the record in table “Messages” (Refer to “Messages” table in “DB Sheet” file) setting `IsDeleted` = True or `Content` = NULL (Step 3). |
| (3.1)-(4) | BR4 | **Displaying Rules:**<br>❖ System returns OK (Step 3.1).<br>❖ Message disappears from view or content is replaced with "Message Unsent" (Step 4). |
| (3.2)-(5) | BR_Error | **Exception Handling Rules:**<br>❖ If a system failure occurs:<br> System logs the error (Step 3.2).<br> System returns `500 Internal Server Error`.<br> Show Error (Step 5). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Select Unsend;
|System|
:(2) Validate Time (<15m);
if (Valid?) then (No)
  :(2.1) Reject;
  stop
else (Yes)
  :(2.2) DELETE /api/chat/messages/{id};
  |Database|
  :(3) DELETE FROM "Messages" WHERE Id=@id;
  if (Success?) then (Yes)
    |System|
    :(3.1) Return OK;
    |Authenticated User|
    :(4) Message disappears;
  else (No)
    |System|
    :(3.2) Log Error & Return 500;
    |Authenticated User|
    :(5) Show Error;
  endif
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "MessageBubble" as View
control "ChatController" as Controller
entity "Messages" as DB

User -> View: Click Unsend
View -> Controller: DELETE /api/chat/messages/{id}
activate Controller
Controller -> DB: Delete Record
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: 200 OK
    deactivate Controller
    View -> View: Remove Component
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

## 2.1.7.6 Search Chat History

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Search Chat History** |
| **Description** | Search within conversations. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User types a keyword in the chat search bar. |
| **Pre-condition** | ❖ User has existing conversations. |
| **Post-condition** | ❖ System displays messages matching the keyword. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(4) | BR1 | **Querying Rules:**<br>❖ System calls method `SearchMessages(query)` (Step 2).<br>❖ System executes syntax `SELECT * FROM Messages WHERE Content LIKE %[query]%` on table “Messages” (Step 3).<br>❖ System joins with “UserConversations” to ensure the user has access to those messages (Step 4). |
| (5)-(6) | BR2 | **Displaying Rules:**<br>❖ System returns results (Step 5).<br>❖ System groups results by Conversation.<br>❖ System displays the “SearchResults” list (Refer to “SearchResults” view in “View Description” file) (Step 6). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Input Keyword;
|System|
:(2) GET /api/chat/search?q=Plan;
|Database|
:(3) Query "Messages";
:(4) JOIN "UserConversations";
|System|
:(5) Return Results;
|Authenticated User|
:(6) View Results;
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "ChatSearchView" as View
control "ChatController" as Controller
entity "Messages" as DB

User -> View: Search "Plan"
View -> Controller: Search("Plan")
activate Controller
Controller -> DB: Execute Like Query
activate DB
DB --> Controller: Results
deactivate DB
Controller --> View: List<MessageSearchResult>
deactivate Controller
View -> User: Display List
@enduml
```

---

## 2.1.7.7 Mark Chat as Read

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Mark Chat as Read** |
| **Description** | Update read status when opening a chat. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks to open a conversation. |
| **Pre-condition** | ❖ Conversation has unread messages. |
| **Post-condition** | ❖ The "LastReadMessageId" is updated to the latest message.<br>❖ Unread indicator disappears. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(4) | BR1 | **Processing & Storing Rules:**<br>❖ When entering the screen, System calls method `MarkAsRead(convId)` (Step 2).<br>❖ System updates table “UserConversations” setting `LastReadMessageId` to the latest message ID (Steps 3-4).<br>❖ System triggers a badge update on the client side. |
| (4.1) | BR_Error | **Exception Handling Rules:**<br>❖ If a system failure occurs:<br> System logs the error (Step 4.1).<br> System returns `500 Internal Server Error`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Open Chat;
|System|
:(2) Background Call MarkAsRead;
|Database|
:(3) UPDATE "UserConversations";
:(4) SET LastReadMessageId = @latest;
if (Success?) then (Yes)
    stop
else (No)
    |System|
    :(4.1) Log Error;
    stop
endif
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "ChatWindow" as View
control "ChatController" as Controller
entity "UserConversations" as DB

User -> View: Open
View -> Controller: MarkAsRead(id, msgId)
activate Controller
Controller -> DB: Update Pivot Table
activate DB
alt Success
    DB --> Controller: Done
    deactivate DB
    Controller --> View: 204 No Content
    deactivate Controller
else Database Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: Log Error
    Controller --> View: 500 Error
    deactivate Controller
end
@enduml
```

---

## 2.1.7.8 Leave Group Chat

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Leave Group Chat** |
| **Description** | Exit a group. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User selects "Leave Group" from group settings. |
| **Pre-condition** | ❖ User is currently a member of the group. |
| **Post-condition** | ❖ User is removed from the participant list.<br>❖ User no longer receives messages from this group. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2) | BR1 | **Leave Confirmation Logic:**<br>❖ System displays a Warning Dialog (Refer to MSG_CONFIRM_LEAVE).<br> **Confirmed**: User accepts consequences. System moves to step (3).<br> **Cancelled**: The dialog closes; action aborted. |
| (3)-(4) | BR2 | **Processing & Storing Rules:**<br>❖ System calls method `LeaveGroup(convId)` (Step 3).<br>❖ System deletes the record from table “UserConversations” (Refer to “UserConversations” table in “DB Sheet” file) for the current user and target group (Step 4). |
| (4.1)-(5) | BR3 | **Displaying Rules:**<br>❖ After leave, System returns success (Step 4.1).<br>❖ System redirects user to Inbox (Step 5). |
| (4.2)-(6) | BR_Error | **Exception Handling Rules:**<br>❖ If a system failure occurs:<br> System logs the error (Step 4.2).<br> System returns `500 Internal Server Error`.<br> Show Error (Step 6). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Leave Group;
:(2) Confirm;
|System|
:(3) POST /api/chat/{id}/leave;
|Database|
:(4) DELETE FROM "UserConversations";
if (Success?) then (Yes)
    |System|
    :(4.1) Return Success;
    |Authenticated User|
    :(5) Redirect to Inbox;
else (No)
    |System|
    :(4.2) Log Error & Return 500;
    |Authenticated User|
    :(6) Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "GroupInfoView" as View
control "ChatController" as Controller
entity "UserConversations" as DB

User -> View: Leave
View -> Controller: LeaveGroup(id)
activate Controller
Controller -> DB: Remove Member
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
