# Favi Use Case Diagrams

Here are the use case diagrams separated by actor.

## 1. User Use Cases (Regular User)

The core functionalities available to a standard user of the Favi social network.

```plantuml
@startuml Favi_User_UseCases
left to right direction
actor "User" as user

package "Favi System (User Scope)" {
    usecase "Sign In / Sign Up" as UC_Auth
    usecase "Manage Profile" as UC_Profile
    usecase "View Newsfeed & Explore" as UC_Newsfeed
    usecase "Create & Manage Posts" as UC_Post
    usecase "Interact (Like, Comment, Share)" as UC_Interact
    usecase "Chat (1-on-1, Group)" as UC_Chat
    usecase "Manage Collections" as UC_Collection
    usecase "Friend & Follow" as UC_Friend
    usecase "Manage Notifications" as UC_Notif
    usecase "Create & Join Groups" as UC_Group
    usecase "Report Content" as UC_Report
}

user --> UC_Auth
user --> UC_Profile
user --> UC_Newsfeed
user --> UC_Post
user --> UC_Interact
user --> UC_Chat
user --> UC_Collection
user --> UC_Friend
user --> UC_Notif
user --> UC_Group
user --> UC_Report
@enduml
```

## 2. Creator Use Cases (Professional)

Features available to Creators. Creators are Users who have switched to Professional Mode.

```plantuml
@startuml Favi_Creator_UseCases
left to right direction
actor "User" as user
actor "Creator" as creator

user <|-- creator : Inherits

package "Favi System (Creator Scope)" {
    usecase "Switch to Pro Mode" as UC_ProMode
    usecase "View Professional Dashboard" as UC_Dashboard
    usecase "Manage Ad Campaigns" as UC_Ads
}

creator --> UC_ProMode
creator --> UC_Dashboard
creator --> UC_Ads
@enduml
```

## 3. Admin Use Cases (Administrator)

Administrative functionalities for system management and moderation.

```plantuml
@startuml Favi_Admin_UseCases
left to right direction
actor "Admin" as admin

package "Favi System (Admin Scope)" {
    usecase "Sign In (Admin)" as UC_AdminAuth
    usecase "Moderate Users & Content" as UC_Mod
    usecase "View Reports (Admin)" as UC_AdminReport
    usecase "Monitor System Health" as UC_SysHealth
    usecase "View Application Logs" as UC_Logs
}

admin --> UC_AdminAuth
admin --> UC_Mod
admin --> UC_AdminReport
admin --> UC_SysHealth
admin --> UC_Logs
@enduml
```
