# Use Case 2.1.4: Adjust Comment

**Module**: Interaction / Engagement
**Primary Actor**: Authenticated User
**Backend Controller**: `CommentsController`
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
| (1) | BR1 | **Display Logic:**<br>❖ The **System** renders the Comment Thread component as part of the Post Detail view.<br>❖ The **System** evaluates the current **User's** permissions (Author/Admin) to dynamically enable or disable action buttons (Reply, Edit, Delete) for each comment. |

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
| (2)-(3) | BR1 | **Submission:**<br>❖ The **Frontend** `CommentInput` component captures the text and calls `commentApi.create({ postId, content })`.<br>❖ The **API** receives a `POST` request at `/api/comments` with the `CreateCommentRequest` body.<br>❖ The **Backend** `CommentsController.Create` validates the request and invokes `_comments.CreateAsync`. |
| (3.2)-(4) | BR2 | **Persistence:**<br>❖ The **Database** inserts a new record into the `Comments` table with `ParentId` set to NULL.<br>❖ The **NotificationService** triggers a notification if the **Author** is different from the Post Owner. |
| (4.2)-(5) | BR3 | **Completion:**<br>❖ The **System** returns a `200 OK` response containing the `CommentResponse`.<br>❖ The **Frontend** immediately appends the new comment to the list for display. |
| (4.1)-(6) | BR_Error | **Exception:**<br>❖ If validation fails (e.g., empty content), the **System** returns `400 Bad Request`. If a Database error occurs, it returns `500`. |

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
| (2)-(3) | BR1 | **Submission:**<br>❖ The **Frontend** calls `commentApi.create({ postId, content, parentCommentId })` with the target Parent ID.<br>❖ The **API** request is handled by `POST /api/comments` (Reused endpoint).<br>❖ The **Backend** explicitly verifies the existence of the `ParentCommentId`. |
| (3.2) | BR2 | **Persistence:**<br>❖ The **Database** inserts the `Comment` record with the `ParentId` populated.<br>❖ The **Logic** supports nested threading (depending on UI implementation) or single-level nesting. |
| (3.2.2)-(4) | BR3 | **Completion:**<br>❖ The **System** returns `200 OK` with the `CommentResponse`.<br>❖ The **Frontend** inserts the new reply under the parent comment in the thread. |

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
| (2)-(3) | BR1 | **Processing:**<br>❖ The **API** receives a `PUT` request at `/api/comments/{id}`.<br>❖ The **Backend** `CommentsController.Update` calls `_comments.UpdateAsync`.<br>❖ The **Logic** strictly verifies that `AuthorId` matches the `UserId` of the requester. It then updates the Content. |
| (3.2)-(4) | BR2 | **Persistence:**<br>❖ The **Database** updates the `Comments` table record.<br>❖ The **System** returns `200 OK` with the updated DTO. |
| (3.2.1) | BR_Error | **Exception:**<br>❖ If the comment is Not Found or the user is Forbidden, the **System** returns `404 Not Found` (merged error code for security). |

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
| (2)-(3) | BR1 | **Processing:**<br>❖ The **API** receives a `DELETE` request at `/api/comments/{id}`.<br>❖ The **Backend** `CommentsController.Delete` invokes `_comments.DeleteAsync`.<br>❖ The **Logic** verifies that the user is the Owner. |
| (3.2) | BR2 | **Persistence:**<br>❖ The **Database** performs a Soft Delete by setting `IsDeleted = 1`.<br>❖ The **System** returns `200 OK` with the message "Đã xoá bình luận.". |
| (3.2.2)-(4) | BR3 | **UI:**<br>❖ The **Frontend** removes the comment element from the DOM. |

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
