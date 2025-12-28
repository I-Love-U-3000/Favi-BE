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
| (1) | BR1 | **Display:**<br>❖ System displays Post Interface.<br>❖ Options enabled based on Ownership/Context. |

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
| (2)-(3) | BR1 | **Submission:**<br>❖ **Frontend**: `CreatePostModal` calls `postApi.create(formData)`.<br>❖ **API**: `POST /api/posts`. Top-level params: `Caption`, `PrivacyLevel`, `Tags`, `Location`, `mediaFiles` (Multipart).<br>❖ **Backend**: `PostsController.Create` extracts User ID from JWT. Calls `_posts.CreateAsync`. |
| (3.2)-(4) | BR2 | **Persistence:**<br>❖ **Service**: Uploads media to Cloudinary (if any).<br>❖ **DB**: Inserts `Post`, `PostMedia`, `PostTag` (Join Table) in a Transaction.<br>❖ **Tags**: `_tags.LinkAsync` ensures tags exist or created. |
| (4.2)-(5) | BR3 | **Completion:**<br>❖ **Response**: `201 Created` with `PostResponse`.<br>❖ **Frontend**: Prepend new post to `feed` list. |
| (4.1)-(6) | BR_Error | **Exception:**<br>❖ Validation Error (Empty content): `400 Bad Request`.<br>❖ Upload Fail: `400 Bad Request` { code: "MEDIA_UPLOAD_FAILED" }. |

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
| (2)-(3) | BR1 | **Processing:**<br>❖ **Frontend**: `EditPostForm` calls `postApi.update(id, { caption })`.<br>❖ **API**: `PUT /api/posts/{id}`.<br>❖ **Backend**: `PostsController.Update` calls `_posts.UpdateAsync(id, userId, caption)`. |
| (3.2)-(4) | BR2 | **Logic:**<br>❖ **Ownership**: Service checks if `Post.ProfileId == userId`. If not -> `false`.<br>❖ **Update**: Modifies `Caption`, `UpdatedAt`.<br>❖ **Response**: `200 OK`. |
| (3.2.1)-(5) | BR_Error | **Exception:**<br>❖ Forbidden/Not Found: Returns `403 Forbidden` { code: "POST_FORBIDDEN_OR_NOT_FOUND" }. |

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
| (2)-(3) | BR1 | **Processing:**<br>❖ **API**: `DELETE /api/posts/{id}`.<br>❖ **Backend**: `PostsController.Delete` calls `_posts.DeleteAsync`.<br>❖ **Logic**: Sets `IsDeleted = true`, `DeletedAt = UtcNow`. Moves to "Recycle Bin". |
| (3.2)-(4) | BR2 | **Success:**<br>❖ **Response**: `200 OK` { message: "Bài viết đã được chuyển vào thùng rác." }.<br>❖ **Frontend**: Removes post from feed instantly. |

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
| (2)-(3) | BR1 | **Keyword Search:**<br>❖ **API**: `POST /api/search` { `Query`, `Type`: `Post` }.<br>❖ **Backend**: `SearchController.Search` -> `_search.SearchAsync`.<br>❖ **DB**: `Posts.Caption ILIKE %q%` OR `Tags.Name == q`. |
| (4) | BR2 | **Semantic Search:**<br>❖ **API**: `POST /api/search/semantic`.<br>❖ **Backend**: `SearchController.SemanticSearch` -> `_search.SemanticSearchAsync`.<br>❖ **Logic**: Generates embedding for query, compares with `PostEmbeddings` vector table using Cosine Similarity. |
| (5) | BR3 | **Result:**<br>❖ **Response**: `200 OK` with `SearchResult` (List of PostResponse). |

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
| (2)-(3) | BR1 | **Routing:**<br>❖ **Authenticated**: `GET /api/posts/feed`. `PostsController` calls `Release.GetFeedAsync(userId)`.<br>❖ **Guest**: `GET /api/posts/guest-feed`. Calls `_posts.GetGuestFeedAsync()`. |
| (4) | BR2 | **Fetching:**<br>❖ **Feed Logic**: Queries `Posts` where `AuthorId` IN (FollowedIds) OR `AuthorId` == Me. Ordered by `CreatedAt` Desc.<br>❖ **Privacy**: `GetFeedAsync` already filters visible posts, but Controller double-checks `_privacy.CanViewPostAsync` for safety. |
| (5) | BR3 | **Response:**<br>❖ **API**: `200 OK` with `PagedResult<PostResponse>`.<br>❖ **Frontend**: Infinite scroll populates list. |

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
| (2)-(3) | BR1 | **Fetching:**<br>❖ **API**: `GET /api/posts/explore`.<br>❖ **Backend**: `PostsController.GetExplore` calls `_posts.GetExploreAsync(userId)`.<br>❖ **Logic**: Queries `Posts` shuffled or ordered by Engagement (Like/Comment count), excluding Followed authors (discovery purpose). |
| (3.1) | BR_Error | **Exception:**<br>Returns `200 OK` (Empty) if no content. |

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
| (2)-(3) | BR1 | **Internal Repost (Invented):**<br>❖ **API**: `POST /api/posts/{id}/share`.<br>❖ **Backend**: `ShareAsync` creates a new Post with `SharedPostId = originId`.<br>❖ **Content**: Acts as a quote-tweet or simple repost. |
| (2.1) | BR2 | **External Share:**<br>❖ **Frontend**: Generates link `https://favi.app/posts/{id}` using the ID from props.<br>❖ **Action**: Copies to clipboard or opens Native Share Sheet (Mobile). No Backend call required. |

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
| (2)-(3) | BR1 | **Processing (Invented):**<br>❖ **API**: `POST /api/posts/{id}/hide`.<br>❖ **Backend**: Inserts into `HiddenPosts` table (UserId, PostId).<br>❖ **Feed**: Future feed queries: `WHERE PostId NOT IN (SELECT PostId FROM HiddenPosts)`. |
| (4) | BR2 | **UI:**<br>❖ **Frontend**: Optimistically removes post from DOM. |

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


