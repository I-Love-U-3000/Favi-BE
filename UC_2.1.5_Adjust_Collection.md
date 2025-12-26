# Use Case 2.1.5: Adjust Collection

**Module**: User Lists / Bookmarks
**Primary Actor**: Authenticated User
**Backend Controller**: `Favi_BE.API.Controllers.CollectionsController`
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
| (2)-(3) | BR1 | **Validation & Upload Process:**<br>❖ The System receives the collection name and an optional cover image file from the User.<br>❖ If an image file is present, the System authenticates with the Cloudinary service and uploads the file to a designated container.<br>❖ The System retrieves the secure, persistent URL of the uploaded image for storage (Step 3). |
| (4) | BR2 | **Persistence & Default Privacy:**<br>❖ The System inserts a new record into the `Collections` table, storing the Name, the secure Image URL (if any), and the `OwnerId`.<br>❖ The System sets the default visibility `IsPrivate` to `true`, ensuring the collection is personal by default unless changed (Step 4). |
| (5) | BR3 | **UI Real-time Update:**<br>❖ Upon successful creation, the System returns the complete `CollectionDto`.<br>❖ The UI immediately prepends the new collection card to the top of the grid view without requiring a full page reload (Step 5). |
| (4.1) | BR_Error | **Exception Handling:**<br>❖ If the Cloudinary upload fails: The System logs the vendor-specific error and aborts the creation process.<br>❖ If the Database insert fails: The System logs the SQL exception and returns a 500 status code.<br>❖ The UI displays a distinct error message advising the user to retry or check their connection (Step 6). |

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
| (2)-(3) | BR1 | **Validation & Ownership Verification:**<br>❖ The System verifies that the requesting User is the legitimate Owner of the target collection.<br>❖ If a new cover image is uploaded, the System performs the Cloudinary upload process similar to creation (Step 3). |
| (4) | BR2 | **Update Logic:**<br>❖ The System executes an update on the `Collections` table, modifying only the changed fields (`Name`, `Privacy`, or `CoverImageUrl`).<br>❖ The System updates the `UpdatedAt` timestamp to reflect the modification (Step 4). |
| (5) | BR3 | **UI Synchronization:**<br>❖ The UI receives the updated DTO and immediately refreshes the collection card's visual elements (Name, Image) to reflect the changes (Step 5). |
| (4.1) | BR_Error | **Exception Handling:**<br>❖ If any step fails (Auth, Upload, or DB):<br> The System logs the complete stack trace.<br> The System returns a 500 error.<br> The UI notifies the user that the update could not be saved (Step 6). |

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
| (2)-(3) | BR1 | **Safe Deletion Protocol:**<br>❖ The System performs a strict ownership check to ensure unauthorized users cannot delete collections.<br>❖ Instead of a physical delete, the System performs a "Service Soft Delete" by setting `IsDeleted = 1` in the `Collections` table (Step 3). |
| (4) | BR2 | **UI Feedback:**<br>❖ Upon receiving a plain 200 OK success response, the UI utilizes a client-side filter to permanently remove the deleted item from the DOM (Step 4). |
| (3.1) | BR_Error | **Exception Reporting:**<br>❖ The System captures any database constraints or connection errors.<br>❖ The System logs the error with high severity.<br>❖ The UI presents a clear "Delete Failed" message to the User (Step 5). |

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
| (2)-(3) | BR1 | **Reaction Toggling Logic:**<br>❖ The System receives the toggle request.<br>❖ The System checks the `CollectionReactions` table for an existing record matching UserID and CollectionID.<br>❖ **If Exists:** The System deletes the record (Unreact).<br>❖ **If Not Exists:** The System inserts a new record (React) (Step 3). |
| (4) | BR2 | **Count & State Synchronization:**<br>❖ The System recalculates (or increments/decrements) the cached `ReactionCount` for the Collection.<br>❖ The UI toggles the heart icon's visual state (Active Red vs Inactive Grey) to provide instant feedback (Step 4). |
| (3.1) | BR_Error | **Failure Recovery:**<br>❖ If the database transaction fails:<br> The System logs the error.<br> The UI actively reverts the heart icon to its previous state to ensure data consistency (Step 6). |

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
| (2)-(3) | BR1 | **Item Linkage Logic:**<br>❖ The System accepts the request to link a specific Post ID to a Collection ID.<br>❖ The System strictly verifies that the Authenticated User is the Owner of the target Collection.<br>❖ The System inserts a unique record into `CollectionItems` (Step 3). |
| (3.2) | BR2 | **Duplicate Prevention:**<br>❖ The System pre-checks for existing links. If the Post is already in the Collection, the System returns a 409 Conflict (or 200 OK with no-op) to prevent duplicate entries (Step 3.2). |
| (4) | BR3 | **User Confirmation:**<br>❖ The UI displays a temporary toast notification "Saved to [Collection Name]" confirming the action was successful (Step 4). |
| (3.1) | BR_Error | **Exception Handling:**<br>❖ If the operation fails: The System logs the specific error code and the UI displays a generic "Save Failed" alert (Step 5). |

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
