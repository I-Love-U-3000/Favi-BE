# Use Case 2.1.7: Chat (Communication)

**Module**: Communication
**Primary Actor**: Authenticated User
**Backend Controller**: `Favi_BE.API.Controllers.ChatController`
**Database Tables**: `"Conversations"`, `"UserConversations"`, `"Messages"`

---

## 2.1.7.1 Chat (View Conversations)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Chat (View Conversations)** |
| **Description** | The user views their inbox containing all active conversations. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks the [iconChat] in the Navigation Bar. |
| **Pre-condition** | ❖ User is logged in. |
| **Post-condition** | ❖ System displays the "ChatList" view with sorted conversations. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Selecting Rules:**<br>When the user clicks the Chat Icon in the navigation bar, the system opens the Chat List interface. |
| (2) | BR2 | **Querying Rules:**<br>The system calls `ChatController.GetConversations` (`GET /api/chat/conversations`) to retrieve the user's active chats. |
| (3) | BR3 | **Querying Rules:**<br>The database queries the `UserConversations` table, joining with `Conversations` and `Profiles` to get the latest message and recipient details. |
| (4) | BR4 | **Displaying Rules:**<br>The system returns a list of conversation summaries (DTOs) to the frontend. |
| (5) | BR5 | **Displaying Rules:**<br>The UI renders the list of conversations (Inbox), showing the last message and timestamp for each. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Click Chat Icon;
|System|
:(2) Call GET /api/chat/conversations;
|Database|
:(3) SELECT * FROM "UserConversations";
|System|
:(4) Return List;
|Authenticated User|
:(4) View Inbox;
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "Authenticated User" as User
boundary "ChatListView (Mock)" as View
control "ChatController" as Controller
entity "UserConversations" as Entity

User -> View: Open Chat
activate View
View -> Controller: GetConversations()
activate Controller
Controller -> Entity: Query UserConversations
activate Entity
Entity --> Controller: Return List
deactivate Entity
Controller --> View: Return List<SummaryDto>
deactivate Controller
View --> User: Render List
deactivate View
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
| (1) | BR1 | **Selecting Rules:**<br>When the user clicks the "Message" button on another user's profile, the system checks for an existing conversation. |
| (2) | BR2 | **Querying Rules:**<br>The system calls `ChatController.GetOrCreateDm` (`POST /api/chat/dm/{targetId}`) with the target user's ID. |
| (3) | BR3 | **Querying Rules:**<br>The database checks the `UserConversations` table to see if a direct message (DM) conversation already exists between these two users. |
| (3.1) | BR3.1 | **Displaying Rules (Existing):**<br>If a conversation exists, the system returns its `ConversationId`, and the UI opens that chat window directly. |
| (3.2) | BR3.2 | **Processing Rules (New):**<br>If no conversation exists, the system proceeds to step (4) to create a new one. |
| (4) | BR4 | **Storing Rules:**<br>The database inserts a new record into the `Conversations` table (Type=DM). |
| (5) | BR5 | **Storing Rules:**<br>The database inserts two records into `UserConversations`, linking both the current user and the target user to the new conversation. |
| (6) | BR6 | **Displaying Rules:**<br>The system returns the new `ConversationId`. |
| (7) | BR7 | **Displaying Rules:**<br>The UI opens the newly created chat window, allowing the user to send the first message. |

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
  else (No)
    |System|
    :Log Error;
    :Return Error (500);
    stop
  endif
endif
|System|
:(6) Return Summary;
|Authenticated User|
:(7) Open Chat Window;
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
| (1) | BR1 | **Selecting Rules:**<br>User selects "New Group" and checks multiple users from the friend list. |
| (2) | BR2 | **Submitting Rules:**<br>User enters Group Name and clicks [btnCreate]. |
| (3) | BR3 | **Validating Rules:**<br>System checks member count (Must be > 1 other person). |
| (4) | BR4 | **Storing Rules:**<br>System calls `ChatController.CreateGroup`.<br>Inserts into `"Conversations"` (`Type = 1` for Group).<br>Inserts into `"UserConversations"` for **every** selected member + Creator. |
| (5) | BR5 | **Displaying Rules:**<br>System redirects to the new Group Chat room. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:Select Members;
:Enter Name & Create;
|System|
:POST /api/chat/group;
|Database|
:INSERT INTO "Conversations";
:Batch INSERT "UserConversations";
if (Save Success?) then (Yes)
    |System|
    :Return Summary;
    |Authenticated User|
    :Open Group Chat;
else (No)
    |System|
    :Log Error;
    :Return Error (500);
    |Authenticated User|
    :Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
actor "Authenticated User" as User
participant "NewGroupView" as View
participant "ChatController" as Controller
participant "AppDbContext" as DB

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
| (1) | BR1 | **Submitting Rules:**<br>When the user types a message and hits Enter/Send, the system triggers the send process. |
| (2) | BR2 | **Processing Rules:**<br>The frontend performs an Optimistic UI update, showing the message as "Sending..." immediately. |
| (2.1) | BR2.1 | **Processing Rules:**<br>The system calls `ChatController.SendMessage` (`POST /api/chat/messages`) with the content and `ConversationId`. |
| (3) | BR3 | **Storing Rules:**<br>The database inserts a new record into the `Messages` table with the current timestamp and sender ID. |
| (4) | BR4 | **Storing Rules:**<br>The database updates the `Conversations` table, setting `LastMessageAt` to the current time to bump the conversation to the top. |
| (5) | BR5 | **Displaying Rules:**<br>The system returns the full `MessageDto` (including the generated ID). |
| (6) | BR6 | **Displaying Rules:**<br>The UI updates the message status from "Sending..." to "Sent" (or displays a delivery tick). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Send Message;
|System|
:Optimistic Update UI;
:(2) POST /api/chat/messages;
|Database|
:(3) INSERT INTO "Messages";
:(4) UPDATE "Conversations";
if (Save Success?) then (Yes)
    |System|
    :(5) Return MessageDto;
    |Authenticated User|
    :(6) Status changes to "Sent";
else (No)
    |System|
    :Log Error;
    :Return Error (500);
    |Authenticated User|
    :Status changes to "Failed";
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
actor "Authenticated User" as User
participant "ChatWindowView" as View
participant "ChatController" as Controller
participant "Messages" as DB_Msg
participant "Conversations" as DB_Conv

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
| (1) | BR1 | **Selecting Rules:**<br>User long-presses a message bubble and selects "Unsend". |
| (2) | BR2 | **Validation Rules:**<br>Frontend checks if message timestamp < 15 minutes. |
| (3) | BR3 | **Submitting Rules:**<br>System calls `ChatController` to delete/soft-delete. |
| (4) | BR4 | **Storing Rules:**<br>Backend removes the record from `"Messages"` table or sets `Content` to NULL. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:Select Unsend;
|System|
:DELETE /api/chat/messages/{id};
|Database|
:DELETE FROM "Messages" WHERE Id=@id;
if (Success?) then (Yes)
  |System|
  :Return OK;
  |Authenticated User|
  :Message disappears;
else (No)
  |System|
  :Log Error;
  :Return 500;
  |Authenticated User|
  :Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
actor "Authenticated User" as User
participant "MessageBubble" as View
participant "ChatController" as Controller
participant "Messages" as DB

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
| (1) | BR1 | **Selecting Rules:**<br>User types in the Search Bar within the Chat section. |
| (2) | BR2 | **Searching Rules:**<br>System calls `ChatController` with query string. |
| (3) | BR3 | **Querying Rules:**<br>SQL: `SELECT * FROM "Messages" WHERE Content LIKE %q% AND ConversationId IN (Select Id from UserConversations where ProfileId = @me)`. |
| (4) | BR4 | **Displaying Rules:**<br>System displays list of messages grouped by Conversation. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:Search "Plan";
|System|
:GET /api/chat/search?q=Plan;
|Database|
:Query "Messages" 
JOIN "UserConversations";
|System|
:Return Results;
|Authenticated User|
:View Results;
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
actor "Authenticated User" as User
participant "ChatSearchView" as View
participant "ChatController" as Controller
participant "Messages" as DB

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
| (1) | BR1 | **Selecting Rules:**<br>User clicks on a Conversation to open it. |
| (2) | BR2 | **Submitting Rules:**<br>System calls `ChatController.MarkAsRead(convId, lastMsgId)`. |
| (3) | BR3 | **Storing Rules:**<br>System updates `"UserConversations"` table: sets `LastReadMessageId` to the ID of the newest message in that chat. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:Open Chat;
|System|
:Background Call MarkAsRead;
|Database|
:UPDATE "UserConversations" 
SET LastReadMessageId = @latest;
if (Success?) then (Yes)
    stop
else (No)
    :Log Error;
    :Return 500;
    stop
endif
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
actor "Authenticated User" as User
participant "ChatWindow" as View
participant "ChatController" as Controller
participant "UserConversations" as DB

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
| (1) | BR1 | **Selecting Rules:**<br>User clicks "Group Info" -> "Leave Group". |
| (2) | BR2 | **Displaying Rules:**<br>System displays Warning: "You won't receive further messages". (Refer to MSG Confirm 2). |
| (3) | BR3 | **Submitting Rules:**<br>User confirms. System calls `ChatController.LeaveGroup`. |
| (4) | BR4 | **Storing Rules:**<br>System deletes the `UserConversation` record for this user and this group. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:Leave Group;
:Confirm;
|System|
:POST /api/chat/{id}/leave;
|Database|
:DELETE FROM "UserConversations";
if (Success?) then (Yes)
    |System|
    :Return Success;
    |Authenticated User|
    :Redirect to Inbox;
else (No)
    |System|
    :Log Error;
    :Return 500;
    |Authenticated User|
    :Show Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
actor "Authenticated User" as User
participant "GroupInfoView" as View
participant "ChatController" as Controller
participant "UserConversations" as DB

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
