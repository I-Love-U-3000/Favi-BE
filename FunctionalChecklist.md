# Functional Checklist for Favi-BE

This document tracks the implementation status of key features across different domains of the application. It's used for progress tracking and future planning.

---

## 👤 User Features

Features available to the end-user.

- [x] **Authentication**
  - [x] User Registration (Email/Password)
  - [x] User Login
  - [x] Token Refresh
- [x] **Profile Management**
  - [x] Create Profile (on registration)
  - [x] View Profile (self and others)
  - [x] Update Profile (Display Name, Bio, Avatar, etc.)
  - [x] Manage Social Links
- [x] **Social Graph**
  - [x] Follow a user
  - [x] Unfollow a user
  - [x] View Followers list
  - [x] View Following list
- [x] **Post Management**
  - [x] Create a new post (with caption and tags)
  - [x] View a single post: Media + Tags + Comments + Reactions
  - [x] Update post caption, tag, content, media
  - [x] Delete a post
  - [x] Upload media (images/videos) to a post
  - [x] Remove media from a post
  - [x] Post privacy settings (Public, Followers, Private)        (NEW)
- [x] **Content Interaction**
  - [x] React to a post (Like, Love, etc.)
  - [x] Comment on a post
  - [x] Reply to a comment
  - [x] Update a comment
  - [x] Delete a comment
  - [x] Reaction summary                                           (NEW)
- [x] **Content Organization (Collections)**
  - [x] Create a collection
  - [x] Update collection details
  - [x] Delete a collection
  - [x] Add a post to a collection
  - [x] Remove a post from a collection
  - [x] View posts within a collection
- [x] **Content Discovery**
  - [x] Personal Feed (posts from followed users)
  - [x] Explore Page (discover new content)
  - [x] View posts by a specific tag
- [x] **Search**
  - [x] Basic keyword search for users, posts, and tags
- [x] **Content Moderation**
  - [x] Report a post, user, or comment

---

## 👑 Admin Features

Features for administrators and moderators.

- [x] **Report Management**
  - [x] View all submitted reports
  - [x] Filter reports (by reporter, target, etc.)
  - [x] Update status of a report (e.g., Approved, Rejected)
- [ ] **Content Moderation (2nd Level Review)**
  - [ ] Dashboard to review content flagged by AI
  - [ ] Take action on flagged content (e.g., remove post, suspend user)
- [ ] **User Management**
  - [ ] View all users
  - [ ] Ban/Suspend a user account
  - [ ] Delete a user account
- [ ] **Analytics & Monitoring**
  - [ ] Dashboard with key metrics (DAU, MAU, new users, etc.)
  - [ ] Content trends analysis
  - [ ] System health monitoring

---

## 🤖 AI Features

Features powered by Artificial Intelligence.

- [ ] **Search Enhancement**
  - [ ] Semantic Search for posts (understanding intent and context, not just keywords)
  - [ ] Image-based search (find similar images)
- [ ] **Content Moderation**
  - [ ] Automatic red-flagging of potentially harmful content (text and images)
  - [ ] Spam detection in comments and posts
- [ ] **Recommendation Engine**
  - [ ] Advanced content recommendations for the "Explore" page
  - [ ] User recommendations ("Who to follow")
- [ ] **Content Understanding**
  - [ ] Automatic tagging of posts based on image content

---

#THIS IS NOT INCLUDED IN THE DIAGRAMS

## ⚙️ System & Infrastructure

Core backend functionalities and infrastructure concerns.

- [x] **Database**
  - [x] Schema definition and management
  - [x] Database migrations
- [x] **API**
  - [x] RESTful API endpoints for all user features
  - [x] Authentication & Authorization (JWT-based)
- [x] **Privacy Control**
  - [x] Privacy Guard for checking access rights (view profiles, posts, etc.)
- [x] **File Storage**
  - [x] Integration with a cloud storage provider (e.g., Cloudinary) for media files
- [ ] **Notifications**
  - [ ] System for sending real-time notifications (e.g., new follower, comment, reaction)
  - [ ] Notification preferences for users
- [x] **Content Distribution**
  - [x] Logic for serving Feed and Explore content
- [ ] **CI/CD**
  - [ ] Automated build and deployment pipeline
- [ ] **Containerization**
  - [x] Dockerfile for the application
  - [x] Docker Compose for local development environment


--- 
Implemented Features:

diagrams/sequence/user/authentication.plantuml
diagrams/sequence/user/profile_management.plantuml
diagrams/sequence/user/social_graph.plantuml
diagrams/sequence/user/post_management.plantuml
diagrams/sequence/user/content_interaction.plantuml
diagrams/sequence/user/content_organization.plantuml
diagrams/sequence/user/content_discovery.plantuml
diagrams/sequence/user/search.plantuml
diagrams/sequence/user/content_moderation_user_report.plantuml
diagrams/sequence/admin/admin_report_management.plantuml

Planned Features (Not Yet Implemented):

diagrams/sequence/admin/admin_user_management.plantuml
diagrams/sequence/admin/admin_analytics_and_monitoring.plantuml
diagrams/sequence/ai/ai_search_enhancement.plantuml
diagrams/sequence/ai/ai_content_moderation.plantuml
diagrams/sequence/ai/ai_recommendation_engine.plantuml
diagrams/sequence/ai/ai_content_understanding.plantuml