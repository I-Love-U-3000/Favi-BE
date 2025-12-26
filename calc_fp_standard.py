
import csv

# Standard Weights from User's Image
WEIGHTS = {
    'EI': {'Simple': 3, 'Average': 4, 'Complex': 6},
    'EO': {'Simple': 4, 'Average': 5, 'Complex': 7},
    'EQ': {'Simple': 3, 'Average': 4, 'Complex': 6},
    'ILF': {'Simple': 7, 'Average': 10, 'Complex': 15},
    'EIF': {'Simple': 5, 'Average': 7, 'Complex': 10}
}

# Adjustment Factors (Fi) for Favi (Modern Social Network)
# Rationale: High online interaction, complex processing, performance critical.
FI_SCORES = {
    'Backup & Recovery': 4,
    'Data Communication': 5, # Real-time chat, heavy API
    'Distributed Processing': 4, # Cloud, Microservices (implied)
    'Performance': 5, # Critical for retention
    'Heavily Used Config': 3, 
    'Online Data Entry': 5, # Core feature
    'Operational Ease': 4,
    'Online Update': 5, # Real-time feed/likes
    'Complex Interface': 4, # Media uploads, dashboards
    'Complex Processing': 5, # AI Algo, Ranking
    'Reusability': 3,
    'Installation Ease': 2, # Web/Mobile standard
    'Multiple Sites': 4, # Global access
    'Facilitate Change': 4 # Agile
}
SUM_FI = sum(FI_SCORES.values()) # Should be ~57

# Calculation Function
def calc_row(name, sub, ei_c, ei_w_key, eo_c, eo_w_key, eq_c, eq_w_key, ilf_c, ilf_w_key, eif_c, eif_w_key):
    # Weights - Only apply if Count > 0
    w1 = WEIGHTS['EI'][ei_w_key] if ei_c > 0 else 0
    w2 = WEIGHTS['EO'][eo_w_key] if eo_c > 0 else 0
    w3 = WEIGHTS['EQ'][eq_w_key] if eq_c > 0 else 0
    w4 = WEIGHTS['ILF'][ilf_w_key] if ilf_c > 0 else 0
    w5 = WEIGHTS['EIF'][eif_w_key] if eif_c > 0 else 0
    
    # Row Data
    return [
        name, sub, 'x', 'x', 
        ei_c, w1, 
        eo_c, w2, 
        eq_c, w3, 
        ilf_c, w4, 
        eif_c, w5
    ]

# Use Cases Definition (Applying "Harder = Higher Points" logic)
# Categories: Simple, Average, Complex
data_rows = []
idx = 1

# 1. Auth (Standard)
data_rows.append(calc_row("User Authentication", "Sign In", 1, 'Average', 1, 'Simple', 1, 'Simple', 1, 'Average', 0, 'Simple'))
data_rows.append(calc_row("User Authentication", "Sign Up", 1, 'Complex', 1, 'Average', 0, 'Simple', 1, 'Average', 0, 'Simple')) # Complex Insert
data_rows.append(calc_row("User Authentication", "Forgot Password", 1, 'Average', 1, 'Simple', 0, 'Simple', 0, 'Simple', 1, 'Simple')) # Email Interface
data_rows.append(calc_row("User Authentication", "Change Password", 1, 'Average', 1, 'Simple', 0, 'Simple', 1, 'Average', 0, 'Simple'))

# 2. Profile (Avg to Complex)
data_rows.append(calc_row("User Profile", "View Profile", 0, 'Simple', 1, 'Average', 1, 'Average', 1, 'Average', 1, 'Simple')) # Read Image
data_rows.append(calc_row("User Profile", "Update Profile", 1, 'Complex', 1, 'Average', 0, 'Simple', 1, 'Complex', 1, 'Average')) # Image Upload
data_rows.append(calc_row("User Profile", "Search Profiles", 1, 'Average', 1, 'Average', 1, 'Average', 1, 'Average', 0, 'Simple'))

# 3. Post (Very Complex - Core)
data_rows.append(calc_row("Post Management", "View Newsfeed (Algo)", 0, 'Simple', 1, 'Complex', 1, 'Complex', 2, 'Complex', 0, 'Simple')) # Joins, Algo
data_rows.append(calc_row("Post Management", "View Explore (Trending)", 0, 'Simple', 1, 'Complex', 1, 'Complex', 2, 'Complex', 0, 'Simple'))
data_rows.append(calc_row("Post Management", "Create Post (Media)", 1, 'Complex', 1, 'Average', 0, 'Simple', 1, 'Complex', 1, 'Average')) # Cloudinary Upload
data_rows.append(calc_row("Post Management", "Update Post", 1, 'Average', 1, 'Average', 0, 'Simple', 1, 'Average', 0, 'Simple'))
data_rows.append(calc_row("Post Management", "Delete Post", 1, 'Simple', 1, 'Simple', 0, 'Simple', 1, 'Average', 0, 'Simple'))
data_rows.append(calc_row("Post Management", "Search AI/Semantic", 1, 'Complex', 1, 'Complex', 1, 'Complex', 1, 'Complex', 1, 'Complex')) # Vector DB Interface
data_rows.append(calc_row("Post Management", "Share Post", 1, 'Average', 1, 'Simple', 0, 'Simple', 1, 'Average', 0, 'Simple'))
data_rows.append(calc_row("Post Management", "React to Post", 1, 'Average', 0, 'Simple', 0, 'Simple', 1, 'Average', 0, 'Simple'))

# 4. Comments (Avg)
data_rows.append(calc_row("Comment Management", "Create Comment", 1, 'Average', 1, 'Simple', 0, 'Simple', 1, 'Average', 0, 'Simple'))
data_rows.append(calc_row("Comment Management", "Reply to Comment", 1, 'Average', 1, 'Simple', 0, 'Simple', 1, 'Average', 0, 'Simple'))
data_rows.append(calc_row("Comment Management", "View Comments", 0, 'Simple', 1, 'Average', 1, 'Average', 1, 'Average', 0, 'Simple'))

# 5. Collection (Avg)
data_rows.append(calc_row("Collection Management", "Create Collection", 1, 'Average', 1, 'Simple', 0, 'Simple', 1, 'Average', 1, 'Simple')) # Cover image
data_rows.append(calc_row("Collection Management", "Add to Collection", 1, 'Simple', 1, 'Simple', 0, 'Simple', 1, 'Average', 0, 'Simple'))

# 6. Friend (Avg)
data_rows.append(calc_row("Friend Management", "Follow/Unfollow", 1, 'Simple', 0, 'Simple', 0, 'Simple', 1, 'Average', 0, 'Simple'))
data_rows.append(calc_row("Friend Management", "View Connections", 0, 'Simple', 1, 'Average', 1, 'Average', 1, 'Average', 0, 'Simple'))

# 7. Chat (Complex Real-time)
data_rows.append(calc_row("Chat System", "Send Message (RT)", 1, 'Complex', 1, 'Average', 0, 'Simple', 1, 'Complex', 1, 'Average')) # SignalR
data_rows.append(calc_row("Chat System", "View Inbox", 0, 'Simple', 1, 'Complex', 1, 'Average', 1, 'Complex', 0, 'Simple'))
data_rows.append(calc_row("Chat System", "Create Group Chat", 1, 'Average', 1, 'Average', 0, 'Simple', 1, 'Average', 0, 'Simple'))

# 8. Notifications
data_rows.append(calc_row("Notification System", "View Notifications", 0, 'Simple', 1, 'Average', 1, 'Average', 1, 'Average', 0, 'Simple'))

# 9. Professional (Complex)
data_rows.append(calc_row("Professional Tools", "View Dashboard", 0, 'Simple', 1, 'Complex', 1, 'Complex', 1, 'Complex', 0, 'Simple')) # Aggregates
data_rows.append(calc_row("Professional Tools", "Manage Ads", 1, 'Complex', 1, 'Complex', 0, 'Simple', 1, 'Complex', 1, 'Average')) # Payment GW

# 10. Groups (Avg-Complex)
data_rows.append(calc_row("Community Groups", "Create Group", 1, 'Average', 1, 'Simple', 0, 'Simple', 1, 'Average', 0, 'Simple'))
data_rows.append(calc_row("Community Groups", "Moderate Content", 1, 'Average', 1, 'Average', 0, 'Simple', 1, 'Average', 0, 'Simple'))

# 11. System Health (Admin)
data_rows.append(calc_row("System Health", "View Real-time Metrics", 0, 'Simple', 1, 'Complex', 1, 'Complex', 0, 'Simple', 1, 'Complex')) # Prometheus
data_rows.append(calc_row("System Health", "View Logs", 0, 'Simple', 1, 'Complex', 1, 'Average', 1, 'Complex', 1, 'Average')) # Seq/Serilog

# Write CSV
with open('favi_function_points_standard.csv', 'w', newline='', encoding='utf-8') as f:
    writer = csv.writer(f)
    writer.writerow(['Stt', 'Tên chức năng chính', 'Tên chức năng con', 'Mobile', 'Web', 'C1 (EI)', 'W1 (Input)', 'C2 (EO)', 'W2 (Output)', 'C3 (EQ)', 'W3 (Inquiry)', 'C4 (ILF)', 'W4 (File)', 'C5 (EIF)', 'W5 (Interface)'])
    
    col_sums = [0] * 10
    
    for i, row in enumerate(data_rows, 1):
        writer.writerow([i] + row)
        # Sum columns 4 to 13 (indices in row are 4,5,6,7,8,9,10,11,12,13)
        # Header has 15 cols. row has 14 elements (name, sub, mob, web, c1, w1...)
        # row[4] is C1.
        for j in range(10):
            col_sums[j] += row[4+j]

    # Calculate Totals
    total_delta = 0
    # Delta = Sum(C * W) for each pair
    # Pairs are at indices (0,1), (2,3), (4,5), (6,7), (8,9) in col_sums
    for k in range(0, 10, 2):
        # This global sum approach is wrong if counting individual products per row.
        # But actually Sum(C*W) is mathematically equivalent to Sum(C)*W O N L Y if W is constant.
        # W is NOT constant. It varies per row.
        # So Calculate Delta row by row.
        pass

    # Recalculate Delta accurately
    row_deltas = []
    for row in data_rows:
        d = (row[4]*row[5]) + (row[6]*row[7]) + (row[8]*row[9]) + (row[10]*row[11]) + (row[12]*row[13])
        row_deltas.append(d)
    
    total_delta = sum(row_deltas)
    
    # Final Formula
    # FP = Delta * (0.65 + 0.01 * SumFi)
    vaf = 0.65 + (0.01 * SUM_FI)
    final_fp = total_delta * vaf
    
    writer.writerow([])
    writer.writerow(['', 'TỔNG CỘNG (Delta)', '', '', '', col_sums[0], col_sums[1], col_sums[2], col_sums[3], col_sums[4], col_sums[5], col_sums[6], col_sums[7], col_sums[8], col_sums[9]])
    writer.writerow([])
    writer.writerow(['', 'THAM SỐ HIỆU CHỈNH (Fi)', '', '', '', 'Sum Fi', SUM_FI, 'VAF', round(vaf, 2)])
    writer.writerow(['', 'KẾT QUẢ CUỐI CÙNG (FP)', '', '', '', 'Unadjusted', total_delta, 'Function Points', round(final_fp, 2)])

print(f"Generated CSV with Total Delta: {total_delta}, VAF: {vaf}, Final FP: {final_fp}")
