# Use Case 2.1.5: Adjust Collection

**Module**: User Lists / Bookmarks
**Primary Actor**: Authenticated User
**Backend Controller**: `CollectionsController`
**Database Tables**: `"Collections"`, `"CollectionItems"`, `"CollectionReactions"`

---

## 2.1.5.1 Adjust Collection (Overview)

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Adjust Collection** |
| **Description** | Central hub for managing personal saved lists. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User enters the "Saved" or "Collections" section. |
| **Post-condition** | ❖ User manages collections or items within them. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (1) | BR1 | **Initialization Logic:**<br>❖ The System retrieves all collections owned by the Authenticated User, as well as collections they are following or have reacted to.<br>❖ The UI evaluates ownership permissions for each collection card to enable or disable specific management actions (Create, Update, Delete are restricted to Owners). |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|User|
start
:(1) View Collections;
:Choose Function;
split
    -> Create;
    :(2.1) Activity\nCreate Collection;
split again
    -> Update;
    :(2.2) Activity\nUpdate Collection;
split again
    -> Delete;
    :(2.3) Activity\nDelete Collection;
split again
    -> React/Unreact;
    :(2.4) Activity\nReact to Collection;
split again
    -> Add Post;
    :(2.5) Activity\nAdd Post to Collection;
end split
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "CollectionsView" as View
control "CollectionsController" as Controller

User -> View: Open Saved
View -> Controller: GetMyCollections()
activate Controller
Controller --> View: List<CollectionDto>
deactivate Controller
View -> User: Display Grid

opt Create
    ref over User, View, Controller: Sequence Create Collection
end

opt Update
    ref over User, View, Controller: Sequence Update Collection
end

opt Delete
    ref over User, View, Controller: Sequence Delete Collection
end

opt React
    ref over User, View, Controller: Sequence React to Collection
end
@enduml
```

---

## 2.1.5.2 Create Collection

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Create Collection** |
| **Description** | Create a new list with optional cover image. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks "New Collection". |
| **Pre-condition** | ❖ Name is provided. |
| **Post-condition** | ❖ Collection Created with Image URL. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Submission:**<br>❖ **Frontend**: `CreateCollectionModal` calls `collectionApi.create(formData)`.<br>❖ **API**: `POST /api/collections`.<br>❖ **Backend**: `CollectionsController.Create`. |
| (3.1) | BR2 | **Upload:**<br>❖ **Service**: `_collections.CreateAsync` calls `_cloudinary.TryUploadAsync` if cover image provided.<br>❖ **Logic**: Returns URL. |
| (4) | BR3 | **Persistence:**<br>❖ **DB**: Insert `Collection` { `OwnerId`, `Name`, `CoverUrl`, `IsPrivate=true` }.<br>❖ **Response**: `200 OK` with `CollectionResponse`. |
| (4.1) | BR_Error | **Exception:**<br>❖ Upload Error: `400 Bad Request`. DB Error: `500`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Input Name & Image;
|System|
:(2) POST /api/collections;
if (Has Image?) then (Yes)
  :(3) Upload to Cloudinary;
  if (Upload Success?) then (Yes)
    :(3.1) Get URL;
  else (No)
    :(3.2) Log/Return Error;
    stop
  endif
endif
|Database|
:(4) INSERT INTO "Collections";
if (Success?) then (Yes)
  |System|
  :(4.2) Return Dto;
  |Authenticated User|
  :(5) View New Collection;
else (No)
  |System|
  :(4.1) Log Error;
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
actor "User" as User
boundary "CreateModal" as View
control "CollectionsController" as Controller
participant "ICloudinaryService" as Cloudinary
entity "Collections" as DB

User -> View: Submit(Name, File)
View -> Controller: Create(dto)
activate Controller
alt Has Image
    Controller -> Cloudinary: UploadImage(File)
    activate Cloudinary
    Cloudinary --> Controller: ImageUrl
    deactivate Cloudinary
end
Controller -> DB: Insert Record
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: 201 Created
    deactivate Controller
    View -> User: Show in Grid
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

## 2.1.5.3 Update Collection

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Update Collection** |
| **Description** | Edit name, privacy, or cover image. |
| **Actor** | Authenticated User (Owner) |
| **Trigger** | ❖ User clicks Edit on generic collection settings. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Processing:**<br>❖ **API**: `PUT /api/collections/{id}`.<br>❖ **Backend**: `CollectionsController.Update` calls `_collections.UpdateAsync`.<br>❖ **Logic**: Verifies Owner. Uploads new image if provided. |
| (4) | BR2 | **Persistence:**<br>❖ **DB**: Update `Collections`.<br>❖ **Response**: `200 OK` with updated details. |
| (4.1) | BR_Error | **Exception:**<br>❖ Forbidden/Not Found: `404 Not Found`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Edit Details;
|System|
:(2) PUT /api/collections/{id};
|Database|
:(3) Check Owner;
if (Owner?) then (Yes)
  if (New Image?) then (Yes)
      |System|
      :(3.1) Upload Cloudinary;
  endif
  |Database|
  :(4) UPDATE "Collections";
  if (Success?) then (Yes)
      |System|
      :(4.2) Return Dto;
      |Authenticated User|
      :(5) View Updated;
  else (No)
      |System|
      :(4.1) Log Error;
      |Authenticated User|
      :(6) Show Error;
  endif
else (No)
  :(3.2) Return 403;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "EditModal" as View
control "CollectionsController" as Controller
participant "IPhotoService" as Cloudinary
entity "Collections" as DB

User -> View: Save Changes
View -> Controller: Update(id, dto)
activate Controller
Controller -> DB: Check Owner
activate DB
DB --> Controller: OK
deactivate DB
alt New Image
    Controller -> Cloudinary: Upload
    Cloudinary --> Controller: Url
end
Controller -> DB: Update Fields
activate DB
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: 200 OK
    deactivate Controller
    View -> User: Refresh View
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

## 2.1.5.4 Delete Collection

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Delete Collection** |
| **Description** | Remove a collection (Soft Delete). |
| **Actor** | Authenticated User (Owner) |
| **Trigger** | ❖ User clicks Delete. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Processing:**<br>❖ **API**: `DELETE /api/collections/{id}`.<br>❖ **Backend**: `CollectionsController.Delete`.<br>❖ **Logic**: Verifies Owner. |
| (4) | BR2 | **Persistence:**<br>❖ **DB**: `_uow.Collections.Remove(collection)`.<br>❖ **Response**: `204 No Content`. |
| (3.1) | BR_Error | **Exception:**<br>❖ Forbidden: `403 Forbidden` { message: "NOT_OWNER" }. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Delete;
|System|
:(2) DELETE /api/collections/{id};
|Database|
:(3) Check Owner;
if (Owner?) then (Yes)
  :(3.1) UPDATE IsDeleted=1;
  if (Success?) then (Yes)
      |System|
      :(3.3) Return OK;
      |Authenticated User|
      :(4) Removed from List;
  else (No)
      |System|
      :(3.2) Log Error;
      |Authenticated User|
      :(5) Show Error;
  endif
else (No)
  :(3.0) Return 403;
endif
stop
@enduml
```

**Sequence Diagram**
```plantuml
@startuml
autonumber
actor "User" as User
boundary "View" as View
control "CollectionsController" as Controller
entity "Collections" as DB

User -> View: Confirm Delete
View -> Controller: Delete(id)
activate Controller
Controller -> DB: Check Owner
activate DB
alt Owner
    DB --> Controller: OK
    Controller -> DB: Update IsDeleted
    alt Success
        DB --> Controller: Done
        deactivate DB
        Controller --> View: 200 OK
        deactivate Controller
        View -> View: Remove Item
    else Error
        activate DB
        DB --> Controller: Exception
        deactivate DB
        Controller -> Controller: LogError
        Controller --> View: 500 Error
        View -> User: Show Error
    end
else Forbidden
    activate DB
    DB --> Controller: Stop
    deactivate DB
    Controller --> View: 403 Forbidden
    View -> User: Freezen
end
@enduml
```

---

## 2.1.5.5 React to Collection

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **React to Collection** |
| **Description** | Like/Follow a public collection. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks Heart icon. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Processing:**<br>❖ **API**: `POST /api/collections/{id}/reactions` { type }.<br>❖ **Backend**: `ToggleReactionAsync`.<br>❖ **Logic**: Checks if Reaction exists. |
| (4) | BR2 | **Toggle:**<br>❖ **DB**: If exists -> Remove. If new -> Add.<br>❖ **Response**: `200 OK` { removed: bool, type: string }. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Toggle Heart;
|System|
:(2) POST /api/collections/{id}/react;
|Database|
:(3) Check Reaction;
if (Exists?) then (Yes)
  :(3.1) DELETE (Unreact);
else (No)
  :(3.2) INSERT (React);
endif
if (Success?) then (Yes)
  |System|
  :(4) Return Status;
  |Authenticated User|
  :(5) Update Icon;
else (No)
  |System|
  :(3.3) Log Error;
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
actor "User" as User
boundary "CardView" as View
control "CollectionsController" as Controller
entity "CollectionReactions" as DB

User -> View: Click Heart
View -> Controller: React(id)
activate Controller
Controller -> DB: Check Exists
activate DB
alt Exists
    DB --> Controller: Yes
    Controller -> DB: Delete
else Not Exists
    DB --> Controller: No
    Controller -> DB: Insert
end
alt Success
    DB --> Controller: Success
    deactivate DB
    Controller --> View: 200 OK
    deactivate Controller
    View -> User: Toggle Icon Color
else Error
    activate DB
    DB --> Controller: Exception
    deactivate DB
    Controller -> Controller: LogError
    Controller --> View: 500 Error
    View -> User: Revert Icon
end
@enduml
```

---

## 2.1.5.6 Add Post to Collection

### Use Case Description
| Attribute | Details |
| :--- | :--- |
| **Name** | **Add Post to Collection** |
| **Description** | Save a post to a specific collection. |
| **Actor** | Authenticated User |
| **Trigger** | ❖ User clicks Save on a post and selects collection. |

### Business Rules (BR)

| Activity | BR Code | Description |
| :---: | :---: | :--- |
| (2)-(3) | BR1 | **Processing:**<br>❖ **API**: `POST /api/collections/{id}/posts/{postId}`.<br>❖ **Backend**: `CollectionsController.AddPost`.<br>❖ **Logic**: `_collections.AddPostAsync`. Checks Owner. |
| (3.2) | BR2 | **Persistence:**<br>❖ **DB**: Insert `CollectionItems` { `CollectionId`, `PostId` }.<br>❖ **Check**: If exists -> Skip/Return.<br>❖ **Response**: `200 OK`. |
| (3.1) | BR_Error | **Exception:**<br>❖ Not Owner / Invalid: `403 Forbidden`. |

### Diagrams

**Activity Diagram**
```plantuml
@startuml
|Authenticated User|
start
:(1) Select Collection;
|System|
:(2) POST /api/collections/{id}/items;
|Database|
:(3) Check Exists;
if (Exists?) then (Yes)
  |System|
  :(3.1) Return Conflict;
  stop
else (No)
  :(3.2) INSERT INTO "CollectionItems";
  if (Success?) then (Yes)
      |System|
      :(3.2.2) Return Success;
      |Authenticated User|
      :(4) Show Saved Toast;
  else (No)
      |System|
      :(3.2.1) Log Error;
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
actor "User" as User
boundary "SaveDialog" as View
control "CollectionsController" as Controller
entity "CollectionItems" as DB

User -> View: Select Collection
View -> Controller: AddItem(id, item)
activate Controller
Controller -> DB: Check Exists
activate DB
alt Not Exists
    DB --> Controller: No
    Controller -> DB: Insert
    alt Success
        DB --> Controller: Success
        deactivate DB
        Controller --> View: 200 OK
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
else Exists
    activate DB
    DB --> Controller: Yes
    deactivate DB
    Controller --> View: 409 Conflict
    View -> User: Show "Already Saved"
end
@enduml
```
