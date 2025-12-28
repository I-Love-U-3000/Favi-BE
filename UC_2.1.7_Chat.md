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
| (2)-(3) | BR1 | **Discovery & Creation:**<br>❖ **Frontend**: User clicks "Message" on Profile. Calls `chatApi.createDm(targetId)`.<br>❖ **API**: `POST /api/chat/dm` Body: `{ targetId }`.<br>❖ **Backend**: `ChatController.GetOrCreateDm` calls `_chatService.GetDmByParticipants(userId, targetId)`.<br>❖ **DB**: `SELECT ConversationId FROM UserConversations WHERE UserId IN (@user, @target) GROUP BY ConversationId HAVING COUNT(*)=2`.<br> **If Exists**: Returns existing `ConversationDto`.<br> **If New**: `INSERT INTO Conversations (Type=DM)`; `INSERT INTO UserConversations` for both users. |
| (5.1)-(6) | BR2 | **Navigation:**<br>❖ **Response**: `200 OK` (ConversationDto).<br>❖ **Frontend**: Redirects to `/messages/{conversationId}`.<br>❖ **SignalR**: Client invokes `Hub.JoinGroup(conversationId)` for real-time events. |
| (5.2) | BR_Error | **Exception:**<br>❖ **Error**: `500 Server Error`.<br>❖ **Frontend**: Show error toast. |

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
| (1)-(3) | BR1 | **Submission:**<br>❖ **Frontend**: `CreateGroupModal`. User selects >1 friend. Clicks "Create".<br>❖ **Validation**: Local check: `selected.length >= 2`.<br>❖ **API**: `POST /api/chat/group` Body: `{ name: "Team", memberIds: [1, 2] }`. |
| (3.2)-(5) | BR2 | **Processing:**<br>❖ **Backend**: `ChatController.CreateGroup(dto)` calls `_chatService.CreateGroupAsync`.<br>❖ **DB**: <br> 1. `INSERT INTO Conversations (Type=Group, Name=...)` -> Get `Id`.<br> 2. `INSERT INTO UserConversations` for Creator (Role=Admin) and Members (Role=Member). |
| (5.1)-(6) | BR3 | **Completion & Notify:**<br>❖ **Response**: `201 Created` (ConversationDto).<br>❖ **SignalR**: Backend broadcasts `ReceiveNewConversation` to all members via `IHubContext`.<br>❖ **Frontend**: Creator navigates to new chat. Members see new chat appear in sidebar. |
| (5.2)-(7) | BR_Error | **Exception:**<br>❖ **Validation**: If < 2 members, API returns `400 Bad Request`.<br>❖ **Frontend**: Displays "Select at least 2 members". |

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
| (1)-(2) | BR1 | **Optimistic Sending:**<br>❖ **Frontend**: `MessageInput`. User hits Enter.<br>❖ **Local State**: Adds temporary message object (`status='sending'`) to list.<br>❖ **API**: `POST /api/chat/messages` Body: `{ conversationId, content }`. |
| (3)-(5) | BR2 | **Processing:**<br>❖ **Backend**: `ChatController.SendMessage` calls `_chatService.SendMessageAsync`.<br>❖ **DB**: `INSERT INTO Messages (ConversationId, SenderId, Content)`. Updates `Conversations.LastMessageAt`.<br>❖ **Real-time**: Calls `_hubContext.Clients.Group(convId).SendAsync("ReceiveMessage", msgDto)`. |
| (5.1)-(6) | BR3 | **Confirmation:**<br>❖ **Response**: `201 Created` (MessageDto).<br>❖ **Frontend**: Replaces temporary object with real `MessageDto` (`status='sent'`). |
| (5.2)-(7) | BR_Error | **Error:**<br>❖ **Frontend**: Fails? Set message status to `failed` (Red retry icon).<br>❖ **Retry Logic**: User can click to retry API call. |

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
| (2) | BR1 | **Validation:**<br>❖ **Frontend**: Checks `msg.sentAt`. If > 15 mins, "Unsend" option hidden/disabled.<br>❖ **Action**: `chatApi.unsendMessage(msgId)`. |
| (2.2)-(3) | BR2 | **Processing:**<br>❖ **API**: `DELETE /api/chat/messages/{id}`.<br>❖ **Backend**: `ChatController.DeleteMessage`. Verifies `SenderId == CurrentUserId`. Check Timestamp.<br>❖ **DB**: `UPDATE Messages SET IsDeleted=1, Content=NULL WHERE Id=@id`.<br>❖ **Real-time**: Broadcasts `MessageDeleted` event via SignalR. |
| (3.1)-(4) | BR3 | **UI Update:**<br>❖ **Frontend**: Receives `MessageDeleted` event or API success.<br>❖ **Action**: Replaces content with "Message unsent" italic text. |
| (3.2)-(5) | BR_Error | **Exception:**<br>❖ **Timeout**: If backend check fails (>15m), return `400 Bad Request`.<br>❖ **Frontend**: Show "Cannot unsend old messages". |

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
| (2)-(4) | BR1 | **Search:**<br>❖ **Frontend**: `ChatSidebar` search input. Calls `chatApi.searchMessages(query)`.<br>❖ **API**: `GET /api/chat/search?q={query}`.<br>❖ **Backend**: `ChatController.Search`.<br>❖ **DB**: `SELECT * FROM Messages m JOIN UserConversations uc ON m.ConversationId = uc.ConversationId WHERE uc.UserId = @me AND m.Content LIKE %query%`. |
| (5)-(6) | BR2 | **Result:**<br>❖ **Response**: `200 OK` (List of MessageDto).<br>❖ **Frontend**: Displays results grouped by Conversation. Clicking jumps to message context. |

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
| (2)-(4) | BR1 | **Processing:**<br>❖ **Frontend**: `useEffect` on chat mount. Calls `chatApi.markRead(conversationId, lastMessageId)`.<br>❖ **API**: `POST /api/chat/{id}/read` Body: `{ messageId }`.<br>❖ **Backend**: `ChatController.Read`.<br>❖ **DB**: `UPDATE UserConversations SET LastReadMessageId = @msgId WHERE UserId=@me AND ConversationId=@convId`.<br>❖ **SignalR**: Notify other clients (optional, for "Read Receipts"). |
| (4.1) | BR_Error | **Error:**<br>❖ Silent failure (non-critical). Logged in backend. |

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
| (2) | BR1 | **Confirmation:**<br>❖ **Frontend**: Click "Leave Group". Show Warning Modal.<br>❖ **Action**: `chatApi.leaveGroup(convId)`. |
| (3)-(4) | BR2 | **Processing:**<br>❖ **API**: `POST /api/chat/{id}/leave`.<br>❖ **Backend**: `ChatController.LeaveGroup`.<br>❖ **DB**: `DELETE FROM UserConversations WHERE UserId=@me AND ConversationId=@id`.<br>❖ **System Message**: `INSERT INTO Messages (Content="User left", Type=System)`.<br>❖ **SignalR**: Broadcast "UserLeft" event to remaining members. |
| (4.1)-(5) | BR3 | **Completion:**<br>❖ **Response**: `200 OK`.<br>❖ **Frontend**: Redirects to Inbox root. Removes chat from list. |
| (4.2)-(6) | BR_Error | **Exception:**<br>❖ **Error**: `500`.<br>❖ **Frontend**: Show detailed error message. |

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
