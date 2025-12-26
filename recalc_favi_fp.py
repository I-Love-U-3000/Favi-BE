
import csv

# Weights from Standard (Users Image)
WEIGHTS = {
    'EI': {'Simple': 3, 'Average': 4, 'Complex': 6},
    'EO': {'Simple': 4, 'Average': 5, 'Complex': 7},
    'EQ': {'Simple': 3, 'Average': 4, 'Complex': 6},
    'ILF': {'Simple': 7, 'Average': 10, 'Complex': 15},
    'EIF': {'Simple': 5, 'Average': 7, 'Complex': 10}
}

# Heuristic Mapping for Complexity & Counts
def get_counts_and_weights(sub_func):
    sub = sub_func.lower()
    
    # Defaults
    c1, w1 = 0, 0
    c2, w2 = 0, 0
    c3, w3 = 0, 0
    c4, w4 = 0, 0
    c5, w5 = 0, 0
    
    # 1. Inputs (EI) & Internal Files (ILF)
    # Create/Update/Delete/Sign Up/Sign In/Login/Logout/Process/Manage/Add/Follow/Block
    if any(k in sub for k in ['sign', 'log', 'create', 'update', 'delete', 'manage', 'add', 'follow', 'block', 'report', 'switch', 'reply', 'send', 'react', 'hide', 'restore', 'archive', 'mark', 'configure', 'process', 'pay', 'handle']):
        c1 = 1
        # Complexity
        if any(k in sub for k in ['media', 'multi', 'campaign', 'semantic', 'payment', 'gateway']):
            w1 = WEIGHTS['EI']['Complex']
            c4 = 1; w4 = WEIGHTS['ILF']['Complex']
        elif any(k in sub for k in ['sign up', 'create group', 'update profile', 'chat', 'send message']):
            w1 = WEIGHTS['EI']['Complex']
            c4 = 1; w4 = WEIGHTS['ILF']['Average']
        else:
            w1 = WEIGHTS['EI']['Average'] # Default 4
            c4 = 1; w4 = WEIGHTS['ILF']['Average'] # Default 10

    # 2. Outputs (EO) vs Inquiry (EQ)
    # View/Search/Track/Monitor
    if any(k in sub for k in ['view', 'search', 'track', 'monitor', 'look', 'access']):
        # If it's analytical/reports -> Output (EO)
        if any(k in sub for k in ['dashboard', 'report', 'analytic', 'stat', 'metric', 'log']):
             c2 = 1
             w2 = WEIGHTS['EO']['Complex'] if 'real-time' in sub or 'dashboard' in sub else WEIGHTS['EO']['Average']
        # If it's a list/feed with algo -> EQ Complex + ILF
        elif 'newsfeed' in sub or 'explore' in sub or 'inbox' in sub:
             c3 = 1; w3 = WEIGHTS['EQ']['Complex']
             c4 = 2; w4 = WEIGHTS['ILF']['Complex'] # Hits multiple tables
        else:
             c3 = 1
             w3 = WEIGHTS['EQ']['Average']
             c4 = 1; w4 = WEIGHTS['ILF']['Average']

    # 3. External Interfaces (EIF)
    if any(k in sub for k in ['media', 'cloudinary', 'payment', 'gateway', 'semantic', 'ai', 'log', 'monitor']):
         c5 = 1
         w5 = WEIGHTS['EIF']['Average']

    # Special adjustments for specific rows based on knowledge
    if 'Search AI' in sub: w5 = WEIGHTS['EIF']['Complex'] # Vector DB
    if 'Create Post' in sub: w5 = WEIGHTS['EIF']['Complex'] # Cloudinary
    if 'Send Message' in sub: w1 = WEIGHTS['EI']['Complex'] # Realtime
    
    return [c1, w1, c2, w2, c3, w3, c4, w4, c5, w5]

# Read original file
input_rows = []
with open('favi_function_points.csv', 'r', encoding='utf-8') as f:
    reader = csv.reader(f)
    for row in reader:
        input_rows.append(row)

# Process
output_rows = []
header = input_rows[0]
data = input_rows[1:-2] # Skip header and old footer

col_sums = [0] * 10
total_delta = 0

for row in data:
    if not row or len(row) < 3: continue
    
    # Keep Metadata
    stt, main, sub, mobile, web = row[0], row[1], row[2], row[3], row[4]
    
    # Calculate Standard Counts/Weights
    new_metrics = get_counts_and_weights(sub) # [c1, w1, c2, w2, c3, w3, c4, w4, c5, w5]
    
    # Construct New Row
    new_row = [stt, main, sub, mobile, web] + new_metrics
    output_rows.append(new_row)
    
    # Sum Cols
    for i in range(10):
        col_sums[i] += new_metrics[i]
        
    # Sum Row Delta
    row_delta = (new_metrics[0]*new_metrics[1]) + (new_metrics[2]*new_metrics[3]) + (new_metrics[4]*new_metrics[5]) + (new_metrics[6]*new_metrics[7]) + (new_metrics[8]*new_metrics[9])
    total_delta += row_delta

# Calculate Finals
SUM_FI = 57
VAF = 0.65 + (0.01 * SUM_FI)
FINAL_FP = total_delta * VAF

# Write Back
with open('favi_function_points.csv', 'w', newline='', encoding='utf-8') as f:
    writer = csv.writer(f)
    writer.writerow(header)
    writer.writerows(output_rows)
    writer.writerow([])
    # Footer
    writer.writerow(['', 'TỔNG CỘNG (Delta)', '', '', ''] + col_sums)
    writer.writerow([])
    writer.writerow(['', 'THAM SỐ HIỆU CHỈNH (Fi)', '', '', '', 'Sum Fi', SUM_FI, 'VAF', round(VAF, 2)])
    writer.writerow(['', 'KẾT QUẢ CUỐI CÙNG (FP)', '', '', '', 'Unadjusted', total_delta, 'Function Point', round(FINAL_FP, 2)])

print(f"Done. FP: {FINAL_FP}")
