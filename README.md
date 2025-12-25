# Favi Social Network - Backend Use Cases & Business Logic

This document outlines the comprehensive functional requirements and business capabilities of the Favi backend system. It unifies core social networking features, advanced administrative controls, and a visionary set of ecosystem expansions inspired by major platforms into a single, detailed specifications list.

## 2.1 Use Case Description

### 2.1.1 Sign In Use Case
*   2.1.1.1 Sign In (Email/Password)
*   2.1.1.2 Sign In (OAuth - Google/Facebook)
*   2.1.1.3 Forgot Password
*   2.1.1.4 Reset Password
*   2.1.1.5 Refresh Session Token
*   2.1.1.6 Logout (Invalidate Token)

### 2.1.2 Adjust User Profile Use Case
*   2.1.2.1 Adjust User Profile
*   2.1.2.2 Create User Profile (Sign Up)
*   2.1.2.3 Update User Profile (Avatar, Bio, Cover Photo)
*   2.1.2.4 Delete User Profile (Soft Delete/Deactivation)
*   2.1.2.5 Search User Profile (Keyword)
*   2.1.2.6 View Other User Profile
*   2.1.2.7 Manage Privacy Settings (Public/Friends Only)

### 2.1.3 Adjust Post Use Case
*   2.1.3.1 Adjust Post
*   2.1.3.2 Create Post (Text, Image, Video)
*   2.1.3.3 Update Post (Edit caption, privacy settings)
*   2.1.3.4 Delete Post
*   2.1.3.5 Search Post (Hashtags, Full-text)
*   2.1.3.6 View Post Newsfeed (Personalized Feed)
*   2.1.3.7 View Explore Feed (Trending Content)
*   2.1.3.8 Share Post (Internal/External)
*   2.1.3.9 Hide Post (See less like this)

### 2.1.4 Adjust Comment Use Case
*   2.1.4.1 Adjust Comment
*   2.1.4.2 Create Comment
*   2.1.4.3 Update Comment
*   2.1.4.4 Delete Comment
*   2.1.4.5 Reply to Comment (Threaded Comments)
*   2.1.4.6 Like Comment
*   2.1.4.7 View Comment Threads

### 2.1.5 Adjust Collection Use Case
*   2.1.5.1 Adjust Collection (Saved Posts)
*   2.1.5.2 Create Collection
*   2.1.5.3 Update Collection (Rename, Privacy)
*   2.1.5.4 Delete Collection
*   2.1.5.5 Search Collection
*   2.1.5.6 Add Post to Collection
*   2.1.5.7 Remove Post from Collection

### 2.1.6 Adjust Friend Use Case
*   2.1.6.1 Adjust Friend
*   2.1.6.2 Add Friend / Follow User
*   2.1.6.3 Delete Friend (Unfriend) / Unfollow
*   2.1.6.4 Search Friend / Followers / Following
*   2.1.6.5 Block Friend / User
*   2.1.6.6 Unblock User
*   2.1.6.7 View Friend Suggestions (Mutual Friends)

### 2.1.7 Chat Use Case
*   2.1.7.1 Chat
*   2.1.7.2 Create Chat (Direct Message)
*   2.1.7.3 Create Group Chat
*   2.1.7.4 Reply Message
*   2.1.7.5 Delete Chat / Message (Unsend)
*   2.1.7.6 Search Chat History
*   2.1.7.7 Mark Chat as Read/Unread
*   2.1.7.8 Leave Group Chat

### 2.1.8 Monitor Notification Use Case
*   2.1.8.1 Monitor Notification (Real-time push)
*   2.1.8.2 View Notification History
*   2.1.8.3 Mark Notification as Read
*   2.1.8.4 Mark All Notifications as Read
*   2.1.8.5 Delete Notification
*   2.1.8.6 Configure Notification Preferences

### 2.1.9 Adjust User Settings Use Case
*   2.1.9.1 Adjust User Settings
*   2.1.9.2 Update Privacy Settings
*   2.1.9.3 Update Language/Region
*   2.1.9.4 View Blocked Users List
*   2.1.9.5 Manage Connected Devices

### 2.1.10 Supervise Content Use Case (User Level)
*   2.1.10.1 Supervise Content
*   2.1.10.2 Report Post / Comment / User
*   2.1.10.3 View My Report History
*   2.1.10.4 Search My Reports
*   2.1.10.5 Appeal Content Removal

### 2.1.11 Adjust Shared Album Use Case (Collaborative Memory Ecosystem)
*   *Business Logic*: Collaborative spaces with dynamic permissions (Owner/Contributor/Viewer) and smart merging of photos from different devices based on timestamps/GPS.
*   2.1.11.1 Adjust Shared Album
*   2.1.11.2 Create Shared Album
*   2.1.11.3 Invite Contributor to Album
*   2.1.11.4 Manage Contributor Permissions (Promote/Demote)
*   2.1.11.5 Upload Media to Shared Album (Smart Merge)
*   2.1.11.6 Remove Media from Shared Album
*   2.1.11.7 Leave Shared Album
*   2.1.11.8 Search Shared Albums

### 2.1.12 Adjust Professional Profile Use Case (Creator Tools)
*   *Business Logic*: Tools for creators including real-time insights reach/engagement and Role-Based Access Control (RBAC) to delegate page management without sharing passwords.
*   2.1.12.1 Adjust Professional Profile
*   2.1.12.2 Switch to Professional Mode
*   2.1.12.3 View Professional Dashboard (Insights)
*   2.1.12.4 View Audience Retention Metrics
*   2.1.12.5 Delegate Access (Add Editor/Moderator)
*   2.1.12.6 Remove Delegate
*   2.1.12.7 Manage Ad Campaigns (Boost Post)

### 2.1.13 Adjust Community Group Use Case (Advanced Governance)
*   *Business Logic*: Autonomous micro-societies with automated rule enforcement, approval queues, and gamification (badges like "Rising Star").
*   2.1.13.1 Adjust Group
*   2.1.13.2 Create Group
*   2.1.13.3 Update Group Settings (Rules, Privacy)
*   2.1.13.4 Join / Leave Group
*   2.1.13.5 Manage Join Requests
*   2.1.13.6 Manage Group Members (Ban/Mute)
*   2.1.13.7 Assign Group Badges (Manual/Auto)
*   2.1.13.8 Moderate Group Content (Approve/Reject Queue)
*   2.1.13.9 Configure Auto-Flagging Rules

### 2.1.14 Adjust Marketplace Use Case (Local Commerce)
*   *Business Logic*: Hyper-local C2C trading with Geo-fencing search (PostGIS), contextual chat rooms for transactions, and fraud detection heuristics.
*   2.1.14.1 Adjust Marketplace Listing
*   2.1.14.2 Create Listing (Item details, Price, Location)
*   2.1.14.3 Search Listings (Geo-fencing/Radius)
*   2.1.14.4 Contact Seller (Contextual Chat Creation)
*   2.1.14.5 Mark Item as Sold/Pending
*   2.1.14.6 Rate Seller / Buyer
*   2.1.14.7 Report Suspicious Listing

### 2.1.15 Adjust Event Use Case (Lifecycle Management)
*   *Business Logic*: Events with RSVP state machines (Invited->Going->Checked-in), JWT-based QR ticketing, and post-event engagement loops.
*   2.1.15.1 Adjust Event
*   2.1.15.2 Create Event
*   2.1.15.3 Update Event Details
*   2.1.15.4 RSVP to Event (Interested/Going/Cant Go)
*   2.1.15.5 Manage Guest List
*   2.1.15.6 Generate Ticket QR Code
*   2.1.15.7 Check-in Attendee  (Scan QR)
*   2.1.15.8 Upload to Event Gallery (Post-Event)

### 2.1.16 Adjust Memory Use Case (Nostalgia Engine)
*   *Business Logic*: Emotional engagement triggers filtering out negative memories (based on sentiment/block lists) and synthesizing stories associated with dates.
*   2.1.16.1 Adjust Memory Settings
*   2.1.16.2 View "On This Day" Feed
*   2.1.16.3 Share Memory to Story
*   2.1.16.4 Hide Memory (Specific Person)
*   2.1.16.5 Hide Memory (Specific Date Range)
*   2.1.16.6 Create "Year in Review" Recap

### 2.1.17 Monitor System Health Use Case (Admin)
*   2.1.17.1 Monitor System Health
*   2.1.17.2 View Real-time Metrics (CPU, RAM, API Latency)
*   2.1.17.3 View Application Logs (Error/Info/Warn)
*   2.1.17.4 View Audit Trail (Admin Actions)
*   2.1.17.5 Export System Logs
*   2.1.17.6 Configure Alert Thresholds

### 2.1.18 Manage Content Governance Use Case (Admin)
*   2.1.18.1 Manage Content Governance
*   2.1.18.2 Manage Keyword Blacklist (CRUD)
*   2.1.18.3 View Flagged Content Queue
*   2.1.18.4 Resolve Flagged Content (Safe/Remove)
*   2.1.18.5 Execute Bulk Actions (Delete Multiple Posts)
*   2.1.18.6 Manage Spam Filters

### 2.1.19 Manage Access Use Case (Super Admin)
*   2.1.19.1 Manage Access
*   2.1.19.2 Manage Staff Account Roles
*   2.1.19.3 Revoke User Sessions (Force Logout)
*   2.1.19.4 Manage IP Blacklist
*   2.1.19.5 View Access Logs by IP
*   2.1.19.6 Lock/Unlock User Account

### 2.1.20 Analyze Growth Use Case (Business Intelligence)
*   2.1.20.1 Analyze Growth
*   2.1.20.2 View User Growth Report (DAU/MAU)
*   2.1.20.3 View Retention Cohorts
*   2.1.20.4 View Feature Usage Heatmaps
*   2.1.20.5 Generate Revenue/Ad Reports
