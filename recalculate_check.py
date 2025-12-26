import csv
import os

file_path = r'c:\Users\tophu\source\repos\Favi-BE\favi_function_points.csv'

def parse_int(val):
    if not val:
        return 0
    try:
        return int(val)
    except ValueError:
        return 0

def parse_float(val):
    if not val:
        return 0.0
    try:
        return float(val)
    except ValueError:
        return 0.0

rows = []
with open(file_path, mode='r', encoding='utf-8-sig') as f:
    reader = csv.reader(f)
    rows = list(reader)

# Find header
header_idx = 0
header = rows[header_idx] 
# Headers: Stt, Tên..., Mobile, Web, C1, W1, C2, W2, C3, W3, C4, W4, C5, W5

# Identify Column Indices
# C1: 5, W1: 6
# C2: 7, W2: 8
# C3: 9, W3: 10
# C4: 11, W4: 12
# C5: 13, W5: 14

c_w_indices = [(5,6), (7,8), (9,10), (11,12), (13,14)]

data_start_idx = 1
data_end_idx = -1

# Find where data ends (empty line or TỔNG CỘNG)
for i in range(data_start_idx, len(rows)):
    if not rows[i] or not rows[i][0] or "TỔNG CỘNG" in rows[i][1]:
        data_end_idx = i
        break

if data_end_idx == -1:
    data_end_idx = len(rows)

print(f"Propcessing rows {data_start_idx} to {data_end_idx}")

total_c = [0] * 5
total_cw = [0] * 5

for i in range(data_start_idx, data_end_idx):
    row = rows[i]
    # Ensure row has enough columns
    if len(row) < 15:
        continue
    
    for idx, (c_idx, w_idx) in enumerate(c_w_indices):
        c = parse_int(row[c_idx])
        w = parse_int(row[w_idx])
        
        total_c[idx] += c
        total_cw[idx] += (c * w)

print("Calculated Totals:")
for idx, (c_sum, cw_sum) in enumerate(zip(total_c, total_cw)):
    print(f"Group {idx+1}: Sum C = {c_sum}, Sum TxW = {cw_sum}")

unadjusted_fp = sum(total_cw)
print(f"Unadjusted FP: {unadjusted_fp}")

# Find Row with 'Sum Fi' to get VAF
vaf = 1.22 # Default
sum_fi_row_idx = -1
unadjusted_row_idx = -1

for i in range(len(rows)):
    row = rows[i]
    if len(row) > 6 and "VAF" in row[6]: # Try to find VAF
         # Maybe in column 6?
         # "Sum Fi", "57", "VAF", "1.22"
         pass

# Inspecting file content earlier:
# 67: ,THAM SỐ HIỆU CHỈNH (Fi),,,,Sum Fi,57,VAF,1.22
# 68: ,KẾT QUẢ CUỐI CÙNG (FP),,,,Unadjusted,926,Function Point,1129.72

# Hardcode search for these rows
row_67 = rows[66] # 0-indexed
if "VAF" in row_67:
    try:
        vaf_idx = row_67.index("VAF")
        vaf = parse_float(row_67[vaf_idx+1])
    except:
        pass

print(f"VAF: {vaf}")
final_fp = unadjusted_fp * vaf
print(f"Final FP: {final_fp}")

