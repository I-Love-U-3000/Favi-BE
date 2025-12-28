# Use Case 2.1.3: Adjust Post

**Module**: Content Management
**Primary Actor**: Authenticated User
**Backend Controller**: `PostController`
**Database Tables**: `Posts`, `PostMedia`, `Follows`, `HiddenPosts`

---

## 2.1.3.1 Adjust Post (Overview)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Adjust Post** |
| **Description** | Central hub for post interactions. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User interacts with a Post or Newsfeed. |
| **Post-condition** | ❖ User triggers specific sub-actions. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Display:**<br>❖ The **System** renders the **Post Interface** on the screen, presenting the Post content and available actions to the **User**.<br>❖ The **System** actively checks the **User's** permissions (Ownership or Relationship context) to enable or disable specific buttons (e.g., Edit, Delete). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|User|
start
:(1) Display Post Interface;
:(2) Choose Function;
split
    -> Create;
    :(2.1) Activity\nCreate Post;
split again
    -> Update;
    :(2.2) Activity\nUpdate Post;
split again
    -> Delete;
    :(2.3) Activity\nDelete Post;
split again
    -> Search;
    :(2.4) Activity\nSearch Post;
split again
    -> Newsfeed;
    :(2.5) Activity\nView Newsfeed;
split again
    -> Explore;
    :(2.6) Activity\nView Explore;
split again
    -> Share;
    :(2.7) Activity\nShare Post;
split again
    -> Hide;
    :(2.8) Activity\nHide Post;
split again
    -> React;
    :(2.9) Activity\nReact to Post;
split again
    -> Restore;
    :(2.10) Activity\nRestore Post;
split again
    -> Archive;
    :(2.11) Activity\nArchive Post;
split again
    -> Unarchive;
    :(2.12) Activity\nUnarchive Post;
end split
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "PostView" as View
control "PostController" as Controller

User -> View: Interact
View -> User: Show Options

opt Create
    ref over User, View, Controller: Sequence Create Post
end

opt Update
    ref over User, View, Controller: Sequence Update Post
end

opt Delete
    ref over User, View, Controller: Sequence Delete Post
end

opt Share
    ref over User, View, Controller: Sequence Share Post
end

opt Hide
    ref over User, View, Controller: Sequence Hide Post
end

opt Restore
    ref over User, View, Controller: Sequence Restore Post
end

opt Archive
    ref over User, View, Controller: Sequence Archive Post
end

opt Unarchive
    ref over User, View, Controller: Sequence Unarchive Post
end

opt React
    ref over User, View, Controller: Sequence React to Post
end
@enduml
```

---

## 2.1.3.2 Create Post

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Create Post** |
| **Description** | Publish new content (Text, Image, Video). |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks [Post] button. |
| **Pre-condition** | ❖ Content exists. |
| **Post-condition** | ❖ Post created in DB. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Submission:**<br>❖ The **Frontend** component `CreatePostModal` collects the form data and invokes the `postApi.create(formData)` method to submit the content.<br>❖ The **API** receives a `POST` request at `/api/posts`, containing top-level parameters such as `Caption`, `PrivacyLevel`, `Tags`, `Location`, and `mediaFiles` (Multipart).<br>❖ The **Backend** `PostsController.Create` method extracts the authenticated User ID from the JWT token and delegates the creation logic to `_posts.CreateAsync`. |
| (3.2)-(4) | BR2 | **Persistence:**<br>❖ The **Service** uploads any attached media files to **Cloudinary** and retrieves the remote URLs.<br>❖ The **System** persists the data by inserting records into the `Post`, `PostMedia`, and `PostTag` (Join Table) tables within a single Database Transaction.<br>❖ The `_tags.LinkAsync` method ensures that all referenced tags effectively exist or are created if new. |
| (4.2)-(5) | BR3 | **Completion:**<br>❖ The **System** returns a `201 Created` HTTP response containing the full `PostResponse` DTO.<br>❖ The **Frontend** receives the response and immediately prepends the new post to the local `feed` list for instant feedback. |
| (4.1)-(6) | BR_Error | **Exception:**<br>❖ If the validation fails (e.g., empty content), the **System** returns a `400 Bad Request` error.<br>❖ If the media upload fails, the **System** returns a `400 Bad Request` with the specific error code `MEDIA_UPLOAD_FAILED`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Submit Post;
|System|
:(2) POST /api/posts;
:(3) Validate Content;
if (Valid?) then (Yes)
  |Database|
  :(3.2) INSERT INTO "Posts";
  :(4) INSERT INTO "PostMedia";
  if (Success?) then (Yes)
      |System|
      :(4.2) Return PostDto;
      |Authenticated User|
      :(5) View New Post;
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
boundary "CreatePostView" as View
control "PostController" as Controller
entity "Posts" as DB

User -> View: Submit
View -> Controller: Create(dto)
activate Controller
Controller -> DB: Insert
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: 201 Created
    deactivate Controller
    View -> User: Show Post
else Database Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: LogError(ex)
    Controller --> View: 500 Error
    View -> User: Show Error
end
@enduml
```

---

## 2.1.3.3 Update Post

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Update Post** |
| **Description** | Edit caption, privacy, or media. |
| **Actor** | Authenticated User (Author) |
| **Trigger** | ❖ User clicks Edit. |
| **Post-condition** | ❖ Post updated in DB. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Processing:**<br>❖ The **Frontend** `EditPostForm` captures the changes and calls the `postApi.update(id, { caption })` function.<br>❖ The **API** receives a `PUT` request at `/api/posts/{id}`.<br>❖ The **Backend** controller `PostsController.Update` invokes the business logic via `_posts.UpdateAsync(id, userId, caption)`. |
| (3.2)-(4) | BR2 | **Logic:**<br>❖ The **Service** first verifies **Ownership** by checking if `Post.ProfileId` matches the requesting `userId`. If they do not match, it returns `false`.<br>❖ The **System** updates the `Caption` and `UpdatedAt` fields in the database.<br>❖ The **System** returns a `200 OK` response upon success. |
| (3.2.1)-(5) | BR_Error | **Exception:**<br>❖ If the user is forbidden or the post is not found, the **System** returns a `403 Forbidden` response with the error code `POST_FORBIDDEN_OR_NOT_FOUND`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Edit Post;
|System|
:(2) PUT /api/posts/{id};
|Database|
:(3) Check Owner;
if (Is Owner?) then (Yes)
  :(3.2) UPDATE "Posts";
  if (Success?) then (Yes)
      |System|
      :(3.2.2) Return Dto;
      |Authenticated User|
      :(4) View Updated Post;
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
boundary "EditPostView" as View
control "PostController" as Controller
entity "Posts" as DB

User -> View: Save Changes
View -> Controller: UpdatePost(dto)
activate Controller
Controller -> DB: Update
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: 200 OK
    deactivate Controller
    View -> User: Show Updated Post
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

## 2.1.3.4 Delete Post

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Delete Post** |
| **Description** | Soft delete a post. |
| **Actor** | Authenticated User (Author) |
| **Trigger** | ❖ User clicks Delete. |
| **Post-condition** | ❖ `IsDeleted` = 1. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Processing:**<br>❖ The **API** receives a `DELETE` request at the endpoint `/api/posts/{id}`.<br>❖ The **Backend** controller `PostsController.Delete` triggers the deletion logic via `_posts.DeleteAsync`.<br>❖ The **System** performs a **Soft Delete** by setting `IsDeleted = true` and `DeletedAt = UtcNow`, effectively moving the post to the "Recycle Bin". |
| (3.2)-(4) | BR2 | **Success:**<br>❖ The **System** returns a `200 OK` response with the message "Bài viết đã được chuyển vào thùng rác.".<br>❖ The **Frontend** immediately removes the post component from the feed view. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Delete;
|System|
:(2) DELETE /api/posts/{id};
|Database|
:(3) UPDATE "Posts" SET IsDeleted=1;
if (Success?) then (Yes)
  |System|
  :(3.2) Return OK;
  |Authenticated User|
  :(4) Disappear;
else (No)
  |System|
  :(3.1) Log Error;
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
boundary "PostView" as View
control "PostController" as Controller
entity "Posts" as DB

User -> View: Click Delete
View -> Controller: DeletePost(id)
activate Controller
Controller -> DB: Soft Delete
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: 200 OK
    deactivate Controller
    View -> View: Remove Component
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

## 2.1.3.5 Search Post

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Search Post** |
| **Description** | Search by keyword or AI Semantic meaning. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User enters query in Search Bar. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Keyword Search:**<br>❖ The **API** receives a `POST` request at `/api/search` with the body content `{ Query, Type: 'Post' }`.<br>❖ The **Backend** controller `SearchController.Search` calls `_search.SearchAsync`.<br>❖ The **Database** executes a query to find matches where `Posts.Caption ILIKE %q%` OR `Tags.Name == q`. |
| (4) | BR2 | **Semantic Search:**<br>❖ The **API** receives a `POST` request at `/api/search/semantic`.<br>❖ The **Backend** controller `SearchController.SemanticSearch` delegates to `_search.SemanticSearchAsync`.<br>❖ The **System** generates an embedding for the query and compares it with the `PostEmbeddings` vector table using **Cosine Similarity**. |
| (5) | BR3 | **Result:**<br>❖ The **System** returns a `200 OK` response containing a `SearchResult` object, which includes a list of `PostResponse` items. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Type Query;
|System|
:(2) GET /api/posts/search;
|Database|
:(3) SELECT FullText Search;
:(4) SELECT Vector Similarity (AI);
if (Found?) then (Yes)
  |System|
  :(5) Return Mixed Results;
  |Authenticated User|
  :(6) View List;
else (No)
  |System|
  :(5.1) Return Empty/Error;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "SearchView" as View
control "PostController" as Controller
entity "Posts" as DB

User -> View: Enter Query
View -> Controller: Search(query)
activate Controller
par Keyword Search
    Controller -> DB: ILIKE %query%
    activate DB
    DB --> Controller: KeywordResults
    deactivate DB
else AI Semantic Search
    Controller -> DB: Vector Distance < Threshold
    activate DB
    DB --> Controller: VectorResults
    deactivate DB
end

alt Success
    Controller --> View: List<PostDto>
    deactivate Controller
    View -> User: Display Results
else Error
    Controller -> Controller: LogError
    Controller --> View: 500 Error
    View -> User: Show Error Message
end
@enduml
```

---

## 2.1.3.6 View Post Newsfeed

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **View Post Newsfeed** |
| **Description** | View stream of posts (Personalized Timeline or Public Feed). |
| **Actor** | Authenticated User / Guest |
| **Trigger** | ❖ Open Home Page / Feed. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Routing:**<br>❖ For an **Authenticated User**, the request is routed to `GET /api/posts/feed`, where `PostsController` calls `Release.GetFeedAsync(userId)`.<br>❖ For a **Guest**, the request is routed to `GET /api/posts/guest-feed`, which calls `_posts.GetGuestFeedAsync()`. |
| (4) | BR2 | **Fetching:**<br>❖ The **Feed Logic** queries the `Posts` table where `AuthorId` is in the user's `FollowedIds` OR `AuthorId` is the user themselves, ordering by `CreatedAt` Descending.<br>❖ The **System** enforces **Privacy** rules: `GetFeedAsync` filters valid posts, but the Controller double-checks `_privacy.CanViewPostAsync` for additional safety. |
| (5) | BR3 | **Response:**<br>❖ The **API** returns a `200 OK` response containing a `PagedResult<PostResponse>`.<br>❖ The **Frontend** utilizes Infinite Scroll to populate the list as the user navigates down. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Actor|
start
:(1) Open Feed;
|System|
:(2) GET /api/posts/feed;
|Database|
:(3) SELECT Raw Posts;
|System|
:(4) Apply Privacy Guard;
note right
  Filter based on:
  - IsPublic
  - IsFriend
  - IsOwner
end note
:(5) Return Filtered List;
|Actor|
:(6) Scroll Feed;
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User/Guest" as User
boundary "HomeView" as View
control "PostController" as Controller
participant "PrivacyGuard" as Guard
entity "Posts" as DB

User -> View: Open Home
View -> Controller: GetFeed()
activate Controller
Controller -> DB: Query Posts (Candidate Set)
activate DB
alt Success
    DB --> Controller: List<Post>
    deactivate DB
    Controller -> Guard: Filter(posts, viewerId)
    activate Guard
    Guard -> Guard: Check Privacy Rules
    Guard --> Controller: List<Post> (Filtered)
    deactivate Guard
    Controller --> View: List<PostDto>
    deactivate Controller
    View -> User: Render Feed
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

## 2.1.3.7 View Explore Feed

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **View Explore Feed** |
| **Description** | View trending content. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ Click Explore Tab. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Fetching:**<br>❖ The **API** endpoint `GET /api/posts/explore` receives the request.<br>❖ The **Backend** `PostsController.GetExplore` calls `_posts.GetExploreAsync(userId)`.<br>❖ The **System** queries the `Posts` table, either shuffled or ordered by Engagement (Like/Comment count), excluding any authors already followed by the user to ensure discovery. |
| (3.1) | BR_Error | **Exception:**<br>If no content is available, the **System** returns a `200 OK` response with an empty list. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Open Explore;
|System|
:(2) GET /api/posts/explore;
|Database|
:(3) SELECT * FROM "Posts" ORDER BY Popularity;
:(4) Return List;
|Authenticated User|
:(5) View Trending;
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "ExploreView" as View
control "PostController" as Controller
entity "Posts" as DB

User -> View: Click Explore
View -> Controller: GetExplore()
activate Controller
Controller -> DB: Query Trending (High Engagement)
activate DB
alt Success
    DB --> Controller: List<Post>
    deactivate DB
    Controller --> View: List<PostDto>
    deactivate Controller
    View -> User: Display Trending Grid
else Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: LogError(ex)
    Controller --> View: 500 Error
    View -> User: Show Retry Option
end
@enduml
```

---

## 2.1.3.8 Share Post

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Share Post** |
| **Description** | Share internally (Repost) or externally. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ Click Share. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Internal Repost (Invented):**<br>❖ The **API** receives a `POST` request at `/api/posts/{id}/share`.<br>❖ The **Backend** function `ShareAsync` creates a new `Post` entity with `SharedPostId` set to the original ID.<br>❖ The **System** treats this content as a quote-tweet or simple repost on the user's timeline. |
| (2.1) | BR2 | **External Share:**<br>❖ The **Frontend** generates a shareable link in the format `https://favi.app/posts/{id}` using the ID from the properties.<br>❖ The **System** copies the link to the clipboard or opens the Native Share Sheet on mobile devices. No Backend call is required. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Click Share;
if (Internal?) then (Yes)
  |System|
  :(2) POST /api/posts/share;
  |Database|
  :(3) INSERT "Posts" (SharedId);
  if (Success?) then (Yes)
      |System|
      :(3.2) Return Success;
      |Authenticated User|
      :(4) Show Reposted;
  else (No)
      |System|
      :(3.1) Log Error;
      |Authenticated User|
      :(5) Show Error;
  endif
else (No)
  |System|
  :(2.1) Generate Direct Link;
  |Authenticated User|
  :(3.1) Copy Link;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "ShareModal" as View
control "PostController" as Controller
entity "Posts" as DB

User -> View: Select Share Option
View -> Controller: Share(id, type)
activate Controller
alt Internal Repost
    Controller -> DB: Insert Repost
    activate DB
    alt Success
        DB --> Controller: Success
        deactivate DB
        Controller --> View: 200 OK
        View -> User: Show "Reposted" Toast
    else Error
        activate DB
        DB --> Controller: Exception
        deactivate DB
        Controller -> Controller: LogError
        Controller --> View: 500 Error
        View -> User: Show Error
    end
else External Link
    Controller --> View: Return Link URL
    deactivate Controller
    View -> User: Copy to Clipboard
end
@enduml
```

---

## 2.1.3.9 Hide Post

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Hide Post** |
| **Description** | See less content like this. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ Click "Hide this post". |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Processing (Invented):**<br>❖ The **API** receives a `POST` request at `/api/posts/{id}/hide`.<br>❖ The **Backend** inserts a new record into the `HiddenPosts` table linking the `UserId` and `PostId`.<br>❖ The **System** updates future feed queries to include the clause `WHERE PostId NOT IN (SELECT PostId FROM HiddenPosts)`. |
| (4) | BR2 | **UI:**<br>❖ The **Frontend** optimistically removes the post from the DOM to provide immediate feedback to the user. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Hide Post;
|System|
:(2) POST /api/posts/{id}/hide;
|Database|
:(3) INSERT INTO "HiddenPosts";
if (Success?) then (Yes)
  |System|
  :(3.2) Return OK;
  |Authenticated User|
  :(4) Remove from View;
else (No)
  |System|
  :(3.1) Log Error;
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
boundary "PostView" as View
control "PostController" as Controller
entity "HiddenPosts" as DB

User -> View: Click "Hide"
View -> Controller: Hide(id)
activate Controller
Controller -> DB: Insert Record
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: 200 OK
    deactivate Controller
    View -> View: Remove Card from Feed
else Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: LogError
    Controller --> View: 500 Error
    View -> User: Show Error Message
end
@enduml
```
