import csv

fp_file = r"c:\Users\tophu\source\repos\Favi-BE\favi_function_points.csv"
out_file = r"c:\Users\tophu\source\repos\Favi-BE\favi_user_stories.csv"

def generate_stories():
    rows = []
    with open(fp_file, 'r', encoding='utf-8') as f:
        reader = csv.reader(f)
        rows = list(reader)
        
    # Helpers
    def get_actor(main, sub):
        sub_l = sub.lower()
        main_l = main.lower()
        if "admin" in main_l: return "Administrator"
        if "professional" in main_l or "ads" in sub_l: return "Creator (Professional User)"
        if "group" in main_l:
            if "manage" in sub_l or "moderate" in sub_l: return "Group Admin"
            return "Group Member"
        if "guest" in main_l or "sign in" in main_l or "sign up" in sub_l: return "Guest/User"
        return "Authenticated User"

    def get_details(main, sub, actor):
        # Heuristics for Description & Solution
        desc = ""
        sol = ""
        
        main_l = main.lower()
        sub_l = sub.lower()
        
        # Identity
        if "sign in" in sub_l:
            desc = "Users need to access their account to use personalized features."
            sol = "Implement Login API (JWT), validate credentials, handle errors (401), store token in client."
        elif "sign up" in sub_l:
            desc = "New users need to create an account to join the platform."
            sol = "Implement Registration API, validate input (email/pass strength), hash password, create User record."
        elif "reset password" in sub_l:
            desc = "Users may forget passwords and need recovery options."
            sol = "Implement Forgot Password flow (Email OTP/Link), verify OTP, allow password update."
        elif "log out" in sub_l:
            desc = "Users need to securely end their session."
            sol = "Clear client-side LocalStorage/Cookies, optionally blacklist token on server."
            
        # Social
        elif "post" in sub_l:
            if "create" in sub_l:
                desc = "Users want to share moments with text, images, or video."
                sol = "Create Post API (Multipart/Form-data), upload media to Cloudinary, save metadata to DB."
            elif "update" in sub_l:
                desc = "Users want to correct or modify their published content."
                sol = "Update Post API, validate ownership, update DB records."
            elif "delete" in sub_l:
                desc = "Users want to remove content they no longer want shared."
                sol = "Soft-delete Post API (IsDeleted=1), remove from feeds."
            elif "view" in sub_l:
                desc = "Users want to see updates from friends or trending content."
                sol = "Implement GetNewsfeed/GetExplore APIs with pagination (cursor-based), filter by privacy."
            elif "search" in sub_l:
                desc = "Users want to find specific content using keywords or semantic meaning."
                sol = "Integrate Vector Search (Qdrant) or Text Search (Postgres ILIKE), return ranked results."
            elif "react" in sub_l:
                desc = "Users want to express emotions on content."
                sol = "Toggle Reaction API, update counts, send notification to owner."
            elif "hide" in sub_l:
                desc = "Users want to hide specific post."
                sol = "Add to HiddenPosts table, exclude from query."
            elif "archive" in sub_l or "restore" in sub_l:
                desc = "Users want to archive specific post."
                sol = "Toggle IsArchived flag."
        
        # Profile
        elif "profile" in main_l:
            if "view" in sub_l:
                desc = "Users want to see their own or others' information."
                sol = "GetProfile API, return DTO with follower counts, bio, avatar."
            elif "update" in sub_l or "adjust" in sub_l:
                desc = "Users want to customize their online persona."
                sol = "UpdateProfile API, handle avatar upload, update DB columns."
            elif "search" in sub_l:
                desc = "Users want to find other people."
                sol = "Search User API by name/email."
            elif "privacy" in sub_l:
                desc = "Users want to control who sees their content."
                sol = "Update PrivacySettings column (JSON/Fields) in User table."
                
        # Chat
        elif "chat" in main_l:
            if "send" in sub_l:
                desc = "Users want to communicate privately in real-time."
                sol = "Implement SignalR Hub/Socket, save message to DB, push to receiver."
            elif "create" in sub_l:
                desc = "Users want to start a conversation."
                sol = "Create Conversation API, add members."
            elif "view" in sub_l:
                desc = "Users want to see past messages."
                sol = "Get Messages API with pagination."
                
        # Admin
        elif "admin" in main_l:
            desc = "Admins need tools to manage the platform health and content."
            sol = "Implement Admin APIs with Role Checks (Authorize(Role=Admin)). Dashboard UI for charts/logs."
            
        else:
            desc = f"Allow {actor} to perform {sub} to achieve their goals."
            sol = "Implement corresponding Frontend UI and Backend API Controller methods."
            
        return desc, sol

    # 1. Build Mapping from SRS
    srs_file = r"c:\Users\tophu\source\repos\Favi-BE\Favi_SRS.docx.md"
    req_map = {}
    
    try:
        with open(srs_file, 'r', encoding='utf-8') as f:
            for line in f:
                line = line.strip()
                # Look for TOC lines like: [2.1.1.1 Sign In (Email/Password)](#...)
                if line.startswith("[2.1.") and "](" in line:
                    # Extract ID and Name
                    # Format: [2.1.1.1 Sign In (Email/Password)	8](#...)
                    content = line.split("](") [0] # [2.1.1.1 Sign In...	8
                    content = content.replace("[", "")
                    
                    # Split ID and Rest
                    parts = content.split(" ", 1)
                    if len(parts) == 2:
                        req_id = parts[0]
                        rest = parts[1]
                        
                        # Remove page number (usually \tNUM or just space NUM at end)
                        # Heuristic: Split by \t if exists
                        if "\t" in rest:
                            req_name = rest.split("\t")[0]
                        else:
                            # Try to strip trailing number if meaningful
                            # Regex or rsplit?
                            # For now, just rstrip digits
                            import re
                            req_name = re.sub(r'\s+\d+$', '', rest).strip()
                            
                        req_map[req_name.lower().strip()] = req_id
    except Exception as e:
        print(f"Warning: Could not read SRS for mapping: {e}")

    user_stories = []
    
    for i, row in enumerate(rows):
        if i == 0: continue
        if not row: continue
        if "TỔNG CỘNG" in row[1]: break
        
        stt = row[0]
        main = row[1]
        sub = row[2]
        
        # Determine Req ID
        # Try exact match on Sub
        r_id = req_map.get(sub.lower())
        
        # If not found, try fuzzy or fallbacks
        if not r_id:
            # Maybe the sub in CSV doesn't have the parent prefix?
            # In CSV: "Sign In (Email/Password)" -> SRS: "Sign In (Email/Password)" (Matches)
            # In CSV: "Create Post" -> SRS: "Create Post"
            pass
            
        if not r_id:
             r_id = "N/A"
        
        # Generate Data
        actor = get_actor(main, sub)
        desc, sol = get_details(main, sub, actor)
        
        action = sub.lower()
        benefit = "I can use the system effectively"
        
        if "manage" in action: benefit = "keep data organized"
        elif "view" in action: benefit = "access information"
        elif "create" in action: benefit = "add new content"
        
        story = f"As a {actor}, I want to {sub}, so that {benefit}."
        
        user_stories.append([
            f"US-{stt.zfill(3)}",
            r_id,
            story,
            desc,
            sol
        ])
        
    # Write CSV
    with open(out_file, 'w', encoding='utf-8-sig', newline='') as f:
        writer = csv.writer(f)
        writer.writerow(["ID", "Requirement ID", "User Story", "Description", "Solution"])
        writer.writerows(user_stories)
        
    print(f"Generated {len(user_stories)} user stories with mapping.")

if __name__ == "__main__":
    generate_stories()
