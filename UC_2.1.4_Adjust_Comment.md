# Use Case 2.1.4: Adjust Comment

**Module**: Interaction / Engagement
**Primary Actor**: Authenticated User
**Backend Controller**: `Favi_BE.API.Controllers.CommentsController`
**Database Tables**: `"Comments"`

---

## 2.1.4.1 Adjust Comment (View Comment)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Adjust Comment** |
| **Description** | View/Manage a specific comment item. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User views a comment in a thread. |
| **Post-condition** | ❖ Comment is displayed with Reply/Like/Delete options. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Display Logic:**<br>❖ The System renders the Comment Thread component as part of the Post Detail view.<br>❖ The System evaluates the current user's permissions (Author/Admin) to dynamically enable or disable action buttons (Reply, Edit, Delete) for each comment. |

### Diagrams

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|User|
start
:(1) Display Comment Thread;
:Choose Function;
split
    -> Create;
    :(2.1) Activity\nCreate Comment;
split again
    -> Reply;
    :(2.2) Activity\nReply Comment;
split again
    -> Update;
    :(2.3) Activity\nUpdate Comment;
split again
    -> Delete;
    :(2.4) Activity\nDelete Comment;
end split
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "PostDetail" as View
control "CommentsController" as Controller

User -> View: View Post
View -> Controller: GetComments()
activate Controller
Controller --> View: List<Comment>
deactivate Controller
View -> User: Display Thread

opt Create
    ref over User, View, Controller: Sequence Create Comment
end

opt Reply
    ref over User, View, Controller: Sequence Reply Comment
end

opt Delete
    ref over User, View, Controller: Sequence Delete Comment
end
@enduml
```

---

## 2.1.4.2 Create Comment

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Create Comment** |
| **Description** | Add a comment to a post. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User types text and clicks Send. |
| **Pre-condition** | ❖ Post exists and accepts comments. |
| **Post-condition** | ❖ Comment record created. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Submission & Validation:**<br>❖ The User submits the comment text via the input field (Step 1).<br>❖ The System receives the `CreateCommentDto` containing the text and `PostId`.<br>❖ The System validates that the content is not empty and does not exceed the maximum character limit (Step 3). |
| (3.2)-(4) | BR2 | **Persistence & Linking:**<br>❖ The System creates a new record in the `Comments` table with the current timestamp.<br>❖ The System establishes a foreign key link to the target `PostId` and `UserId` (Step 4). |
| (4.2)-(5) | BR3 | **UI Update:**<br>❖ Upon success, the System returns the full `CommentDto` including the new ID and Author info.<br>❖ The UI appends the new comment to the bottom of the list without refreshing the page (Step 5). |
| (4.1)-(6) | BR_Error | **Exception Handling:**<br>❖ If the database insert fails (Step 4):<br> The System logs the exception details.<br> The System returns a 500 Internal Server Error.<br> The UI displays a toast message "Failed to post comment" (Step 6). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Submit Comment;
|System|
:(2) POST /api/comments;
:(3) Validate Content;
if (Valid?) then (Yes)
  |Database|
  :(3.2) INSERT INTO "Comments";
  :(4) Link Post;
  if (Success?) then (Yes)
      |System|
      :(4.2) Return Dto;
      |Authenticated User|
      :(5) View Comment;
  else (No)
      |System|
      :(4.1) Log Error;
      |Authenticated User|
      :(6) Show Error;
  endif
else (No)
  :(3.1) Return Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "PostDetail" as View
control "CommentsController" as Controller
entity "Comments" as DB

User -> View: Submit
View -> Controller: AddComment(dto)
activate Controller
Controller -> DB: Insert
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: 201 Created
    deactivate Controller
    View -> User: Show Comment
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

## 2.1.4.3 Reply to Comment

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Reply to Comment** |
| **Description** | Respond to an existing comment. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks Reply on a specific comment. |
| **Post-condition** | ❖ New Comment created with `ParentCommentId`. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Reply Processing:**<br>❖ The System receives the reply request targeting a specific `ParentCommentId`.<br>❖ The System verifies that the Parent Comment exists and is not deleted (Step 3). |
| (3.2) | BR2 | **Hierarchical Storage:**<br>❖ The System inserts a new record into the `Comments` table, populating the `ParentId` column to establish the threading relationship (Step 3.2). |
| (3.2.2)-(4) | BR3 | **Threaded Display:**<br>❖ The System returns the created `CommentDto`.<br>❖ The UI renders the new comment indented or nested immediately under the parent comment to visualize the conversation flow (Step 4). |
| (3.2.1)-(5) | BR_Error | **Exception Handling:**<br>❖ If the database operation fails:<br> Log the error stack trace.<br> Return a 500 error.<br> Show a "Reply failed" notification to the User (Step 5). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Submit Reply;
|System|
:(2) POST /api/comments/{id}/reply;
|Database|
:(3) Check Parent Exists;
if (Exists?) then (Yes)
  :(3.2) INSERT with ParentId;
  if (Success?) then (Yes)
      |System|
      :(3.2.2) Return Dto;
      |Authenticated User|
      :(4) View Thread;
  else (No)
      |System|
      :(3.2.1) Log Error;
      |Authenticated User|
      :(5) Show Error;
  endif
else (No)
  :(3.1) Return 404;
  stop
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "CommentThread" as View
control "CommentsController" as Controller
entity "Comments" as DB

User -> View: Submit Reply
View -> Controller: AddReply(parentId, dto)
activate Controller
Controller -> DB: Check Parent
activate DB
alt Parent Exists
    DB --> Controller: Exists
    Controller -> DB: Insert Reply
    alt Success
        DB --> Controller: Success
        deactivate DB
        Controller --> View: 201 Created
        deactivate Controller
        View -> User: Show Nested Reply
    else Error
        activate DB
        DB --> Controller: Exception
        deactivate DB
        Controller -> Controller: LogError
        Controller --> View: 500 Error
        View -> User: Show Error
    end
else Not Found
    activate DB
    DB --> Controller: Null
    deactivate DB
    Controller --> View: 404 Not Found
    View -> User: Show Error
end
@enduml
```

---

## 2.1.4.4 Update Comment

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Update Comment** |
| **Description** | Edit the content of an existing comment. |
| **Actor** | Authenticated User (Author) |
| **Trigger** | ❖ User clicks Edit on their comment. |
| **Post-condition** | ❖ Comment text updated. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Update Logic:**<br>❖ The System receives the edited text and the Comment ID.<br>❖ The System performs a strict ownership check to ensure only the original Author (or Admin) can modify the content (Step 3). |
| (3.2)-(4) | BR2 | **Persistence:**<br>❖ The System executes an `UPDATE` command on the `Comments` table to modify the `Content` column and update the `UpdatedAt` timestamp (Step 3.2).<br>❖ The System returns the updated `CommentDto` (Step 4). |
| (3.2.1) | BR_Error | **Exception Handling:**<br>❖ If the database update fails:<br> Log the error.<br> Return a 500 error.<br> The UI notifies the user and retains the edit mode/old content (Step 3.2.1). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Edit Comment;
|System|
:(2) PUT /api/comments/{id};
|Database|
:(3) Check Owner;
if (Is Owner?) then (Yes)
  :(3.2) UPDATE "Comments";
  if (Success?) then (Yes)
      |System|
      :(4) Return Dto;
      |Authenticated User|
      :(5) View Updated;
  else (No)
      |System|
      :(3.2.1) Log Error;
      |Authenticated User|
      :(6) Show Error;
  endif
else (No)
  :(3.1) Return 403;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "CommentView" as View
control "CommentsController" as Controller
entity "Comments" as DB

User -> View: Save Edit
View -> Controller: UpdateComment(id, dto)
activate Controller
Controller -> DB: Check Owner
activate DB
alt Owner & Exists
    DB --> Controller: OK
    Controller -> DB: Update Content
    alt Success
        DB --> Controller: Success
        deactivate DB
        Controller --> View: 200 OK
        deactivate Controller
        View -> User: Update Text
    else Error
        activate DB
        DB --> Controller: Exception
        deactivate DB
        Controller -> Controller: LogError
        Controller --> View: 500 Error
        View -> User: Show Error
    end
else Forbidden/NotFound
    activate DB
    DB --> Controller: Fail
    deactivate DB
    Controller --> View: 403/404
    View -> User: Show Error
end
@enduml
```

---

## 2.1.4.5 Delete Comment

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Delete Comment** |
| **Description** | Remove a comment. |
| **Actor** | Authenticated User (Author/Admin) |
| **Trigger** | ❖ User clicks Delete. |
| **Post-condition** | ❖ Comment deleted (Soft). |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Deletion Logic:**<br>❖ The System receives a DELETE request for a specific Comment ID.<br>❖ The System validates that the requester is the owner of the comment or an Administrator (Step 3). |
| (3.2) | BR2 | **Soft Deletion:**<br>❖ Unlike a hard delete, the System updates the `IsDeleted` flag to `1` in the `Comments` table to preserve history (Step 3.2). |
| (3.2.2)-(4) | BR3 | **UI Removal:**<br>❖ Upon success (200 OK), the UI removes the comment node (and potentially its children) from the DOM immediately (Step 4). |
| (3.2.1)-(5) | BR_Error | **Exception Handling:**<br>❖ If the operation fails:<br> Log the error.<br> Return 500.<br> Show "Delete failed" error to the User (Step 5). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Delete;
|System|
:(2) DELETE /api/comments/{id};
|Database|
:(3) Check Owner;
if (Is Owner?) then (Yes)
  :(3.2) Soft Delete;
  if (Success?) then (Yes)
      |System|
      :(3.2.2) Return OK;
      |Authenticated User|
      :(4) Removed;
  else (No)
      |System|
      :(3.2.1) Log Error;
      |Authenticated User|
      :(5) Show Error;
  endif
else (No)
  :(3.1) Return 403;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "PostDetail" as View
control "CommentsController" as Controller
entity "Comments" as DB

User -> View: Click Delete
View -> Controller: Delete(id)
activate Controller
Controller -> DB: Check Owner
activate DB
alt Owner & Exists
    DB --> Controller: OK
    Controller -> DB: Update IsDeleted=1
    alt Success
        DB --> Controller: Done
        deactivate DB
        Controller --> View: 200 OK
        deactivate Controller
        View -> View: Remove Comment
    else Error
        activate DB
        DB --> Controller: Exception
        deactivate DB
        Controller -> Controller: LogError
        Controller --> View: 500 Error
        View -> User: Show Error
    end
else Forbidden/NotFound
    activate DB
    DB --> Controller: Fail
    deactivate DB
    Controller --> View: 403/404
    View -> User: Show Error
end
@enduml
```
