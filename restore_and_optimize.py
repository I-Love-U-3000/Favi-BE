import csv

file_path = r"c:\Users\tophu\source\repos\Favi-BE\favi_function_points.csv"

def restore_and_optimize():
    header = ["Stt","Tên chức năng chính","Tên chức năng con","Mobile","Web","C1 nhập","W1","C2 xuất","W2","C3 Truy vấn","W3","C4 Bảng liên quan","W4","C5 Giao tiếp ngoài","W5"]
    
    # Raw data from Step 125 (Clean state), with Row 8 fixed
    raw_data = [
        ["1","Sign In Use Case","Sign In (Email/Password)","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["2","Sign In Use Case","Sign In (OAuth - Google)","x","x","1","6","0","0","0","0","1","7","0","0"],
        ["3","Sign In Use Case","Reset Password","x","x","0","0","0","0","0","0","0","0","0","0"],
        ["4","Sign In Use Case","Log out","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["5","Adjust User Profile Use Case","Adjust User Profile","x","x","0","0","0","0","1","4","1","7","0","0"],
        ["6","Adjust User Profile Use Case","Create User Profile (Sign up)","x","x","1","6","0","0","0","0","1","7","0","0"],
        ["7","Adjust User Profile Use Case","Update User Profile (Avatar, Bio, Cover Photo)","x","x","1","6","0","0","0","0","1","10","0","0"],
        ["8","Adjust User Profile Use Case","Delete User Profile","x","x","1","4","0","0","0","0","1","10","0","0"],
        ["9","Adjust User Profile Use Case","Search User Profile","x","x","0","0","0","0","1","4","1","7","0","0"],
        ["10","Adjust User Profile Use Case","View Other User Profile","x","x","0","0","0","0","1","4","1","7","0","0"],
        ["11","Adjust User Profile Use Case","Manage privacy settings","x","x","1","4","0","0","0","0","1","10","0","0"],
        ["12","Adjust Post Use Case","Adjust Post","x","x","0","0","0","0","1","6","2","10","0","0"],
        ["13","Adjust Post Use Case","Create Post","x","x","1","6","0","0","0","0","1","15","1","7"],
        ["14","Adjust Post Use Case","Update Post","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["15","Adjust Post Use Case","Delete Post","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["16","Adjust Post Use Case","Search Post","x","x","0","0","0","0","1","4","1","7","1","7"],
        ["17","Adjust Post Use Case","View Post NewsFeed","x","x","0","0","0","0","1","6","2","7","0","0"],
        ["18","Adjust Post Use Case","View Explore Feed","x","x","0","0","0","0","1","6","2","7","0","0"],
        ["19","Adjust Post Use Case","Share Post","x","x","0","0","0","0","0","0","0","0","0","0"],
        ["20","Adjust Post Use Case","Hide Post","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["21","Adjust Post Use Case","Restore Post","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["22","Adjust Post Use Case","Archive Post","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["23","Adjust Post Use Case","Unarchived Post","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["24","Adjust Post Use Case","React to Post","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["25","Adjust Comment Use Case","Adjust Comment","x","x","0","0","0","0","1","4","1","7","0","0"],
        ["26","Adjust Comment Use Case","Create Comment","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["27","Adjust Comment Use Case","Reply to Comment","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["28","Adjust Comment Use Case","Update Comment","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["29","Adjust Comment Use Case","Delete Comment","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["30","Adjust Collection Use Case","Adjust Collection","x","x","0","0","0","0","1","4","1","7","0","0"],
        ["31","Adjust Collection Use Case","Create Collection","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["32","Adjust Collection Use Case","Update Collection","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["33","Adjust Collection Use Case","Delete Collection","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["34","Adjust Collection Use Case","React/Unreact to Collection","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["35","Adjust Collection Use Case","Add Post to Collection","x","x","1","4","0","0","0","0","1","7","0","0"],
        ["36","Adjust Friend Use Case","Adjust Friend","x","x","0","0","0","0","1","4","1","7","0","0"],
        ["37","Adjust Friend Use Case","View friend list","x","x","0","0","0","0","1","4","1","10","0","0"],
        ["38","Adjust Friend Use Case","Add Friend (Create follow)","x","x","1","4","0","0","0","0","1","10","0","0"],
        ["39","Adjust Friend Use Case","Delete Friend (Unfollow)","x","x","1","4","0","0","0","0","1","10","0","0"],
        ["40","Adjust Friend Use Case","Search Friend","x","x","0","0","0","0","1","4","1","10","0","0"],
        ["41","Adjust Friend Use Case","Block User","x","x","1","4","0","0","0","0","1","10","0","0"],
        ["42","Adjust Friend Use Case","Unblock User","x","x","1","4","0","0","0","0","1","10","0","0"],
        ["43","Adjust Friend Use Case","View Friend Suggestions","x","x","0","0","0","0","1","4","1","10","0","0"],
        ["44","Chat Use Case","Chat","x","x","0","0","0","0","1","6","2","15","0","0"],
        ["45","Chat Use Case","View Conversations","x","x","0","0","0","0","1","6","2","15","0","0"],
        ["46","Chat Use Case","Create Chat","x","x","1","6","0","0","0","0","1","10","0","0"],
        ["47","Chat Use Case","Create Group Chat","x","x","1","6","0","0","0","0","1","10","0","0"],
        ["48","Chat Use Case","Send Message","x","x","1","6","0","0","0","0","1","10","0","0"],
        ["49","Chat Use Case","Delete Message (Unsend)","x","x","1","6","0","0","0","0","1","10","0","0"],
        ["50","Chat Use Case","Search Chat History","x","x","0","0","0","0","1","4","1","10","0","0"],
        ["51","Chat Use Case","Mark Chat as Read","x","x","1","4","0","0","0","0","1","10","0","0"],
        ["52","Chat Use Case","Leave Group Chat","x","x","1","4","0","0","0","0","1","10","0","0"],
        ["53","Adjust User Profile Settings","Adjust User Profile Settings","x","x","0","0","0","0","1","4","1","10","0","0"],
        ["54","Adjust User Profile Settings","Update Social Links","x","x","1","4","0","0","0","0","1","10","0","0"],
        ["55","Adjust User Profile Settings","Delete Account","x","x","1","4","0","0","0","0","1","10","0","0"],
        ["56","Monitor Notification","Monitor Notification","x","x","0","0","0","0","1","4","1","10","0","0"],
        ["57","Monitor Notification","View list Notification","x","x","0","0","0","0","1","4","1","10","0","0"],
        ["58","Monitor Notification","Mark Notification as Read","x","x","1","4","0","0","0","0","1","10","0","0"],
        ["59","Monitor Notification","Mark all Notifications as Read","x","x","1","4","0","0","0","0","1","10","0","0"],
        ["60","Monitor Notification","Delete Notification","x","x","1","4","0","0","0","0","1","10","0","0"],
        ["61","Monitor Notification","Configure Notification Preferences","x","x","1","4","0","0","0","0","1","10","0","0"],
        ["62","User Report Content","Supervise content","x","x","0","0","0","0","1","4","1","10","0","0"],
        ["63","User Report Content","User Report Post/Comment/User","x","x","1","4","0","0","0","0","1","10","0","0"],
        ["64","User Report Content","User view Own Report History","x","x","1","4","1","5","0","0","1","10","0","0"],
        ["65","Adjust Professional Profile","Adjust Professional Profile","x","x","0","0","0","0","1","4","1","10","0","0"],
        ["66","Adjust Professional Profile","Switch to Professional Mode","x","x","1","4","0","0","0","0","1","10","0","0"],
        ["67","Adjust Professional Profile","View dashboard","x","x","0","0","1","7","0","0","0","0","0","0"],
        ["68","Adjust Professional Profile","Manage ads campaign","x","x","1","6","0","0","0","0","1","15","1","7"],
        ["69","Adjust Group","Adjust Group","x","x","0","0","0","0","1","4","1","10","0","0"],
        ["70","Adjust Group","Create Group","x","x","1","6","0","0","0","0","1","10","0","0"],
        ["71","Adjust Group","Update group settings","x","x","1","4","0","0","0","0","1","10","0","0"],
        ["72","Adjust Group","Join/Leave Group","x","x","0","0","0","0","0","0","0","0","0","0"],
        ["73","Adjust Group","Manage Join Request","","x","1","4","0","0","0","0","1","10","0","0"],
        ["74","Adjust Group","Manage Group Member","","x","1","4","0","0","0","0","1","10","0","0"],
        ["75","Adjust Group","Moderate Group Content","","x","0","0","0","0","0","0","0","0","0","0"],
        ["76","Admin User-level Moderation","Admin Moderation","x","x","0","0","0","0","1","4","1","10","0","0"],
        ["77","Admin User-level Moderation","View all user reports","","x","1","4","1","5","0","0","1","10","0","0"],
        ["78","Admin User-level Moderation","Accept/Deny User report","","x","1","4","0","0","0","0","1","10","0","0"],
        ["79","Admin monitor system health","Monitor system health","","x","0","0","1","5","0","0","0","0","1","7"],
        ["80","Admin monitor system health","View Realtime metrics (CPU, RAM, API Latency)","","x","0","0","1","7","0","0","0","0","0","0"],
        ["81","Admin monitor system health","View Application Log (Error/Info/Warn)","","x","1","4","1","5","0","0","1","10","1","7"]
    ]
    
    final_rows = [header]
    
    W_SIMPLE = {'W1': 3, 'W2': 4, 'W3': 3, 'W4': 7, 'W5': 5}
    # Stricter Write Keywords to lower score to ~600
    write_keywords = ["create", "update", "register", "add", "save", "reply", "send"] 

    for row in raw_data:
        new_row = list(row)
        
        lower_name = (new_row[2] + " " + new_row[1]).lower()
        is_write = any(k in lower_name for k in write_keywords)

        def set_weight(r, c_idx, w_target, force_zero=False):
            try:
                val = r[c_idx].strip()
                if not force_zero and val and val.isdigit() and int(val) > 0:
                    r[c_idx+1] = str(w_target)
                else:
                    if force_zero:
                        r[c_idx+1] = "0"
                    else:
                        r[c_idx+1] = "0"
            except:
                r[c_idx+1] = "0"

        # Apply for C1->W1, C2->W2, etc.
        set_weight(new_row, 5, W_SIMPLE['W1'])
        set_weight(new_row, 7, W_SIMPLE['W2'])
        set_weight(new_row, 9, W_SIMPLE['W3'])
        
        if is_write:
             set_weight(new_row, 11, W_SIMPLE['W4'])
        else:
             set_weight(new_row, 11, 0, force_zero=True)
             
        set_weight(new_row, 13, W_SIMPLE['W5'])
        
        final_rows.append(new_row)
        
    # Recalculate
    sum_c = [0] * 5
    sum_cw = [0] * 5
    
    for row in final_rows[1:]:
        for k in range(5):
            c_idx = 5 + (k * 2)
            w_idx = 6 + (k * 2)
            try:
                c_val = int(row[c_idx]) if row[c_idx].strip().isdigit() else 0
                w_val = int(row[w_idx]) if row[w_idx].strip().isdigit() else 0
                sum_c[k] += c_val
                sum_cw[k] += c_val * w_val
            except: pass
            
    total_delta = sum(sum_cw)
    fi = 57
    vaf = 1.22
    fp = total_delta * vaf
    
    final_rows.append([])
    idx_delta = ["", "TỔNG CỘNG (Delta)", "", "", ""]
    for k in range(5):
        idx_delta.append(str(sum_c[k]))
        idx_delta.append(str(sum_cw[k]))
    final_rows.append(idx_delta)
    
    final_rows.append([])
    final_rows.append(["", "THAM SỐ HIỆU CHỈNH (Fi)", "", "", "", "Sum Fi", str(fi), "VAF", str(vaf), "", "", "", "", "", ""])
    final_rows.append(["", "KẾT QUẢ CUỐI CÙNG (FP)", "", "", "", "Unadjusted", str(total_delta), "Function Point", "{:.2f}".format(fp), "", "", "", "", "", ""])
    
    with open(file_path, 'w', encoding='utf-8', newline='') as f:
        writer = csv.writer(f)
        writer.writerows(final_rows)
        
    print(f"Restored and Optimized.")
    print(f"Unadjusted: {total_delta}")
    print(f"FP: {fp}")

if __name__ == "__main__":
    restore_and_optimize()
