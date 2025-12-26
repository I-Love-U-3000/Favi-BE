import csv
import os

file_path = r'c:\Users\tophu\source\repos\Favi-BE\favi_function_points.csv'
output_path = r'c:\Users\tophu\source\repos\Favi-BE\favi_function_points_updated.csv'

def parse_int(val):
    if not val: return 0
    try: return int(val.replace(',', ''))
    except: return 0

def parse_float(val):
    if not val: return 0.0
    try: return float(val.replace(',', ''))
    except: return 0.0

with open(file_path, mode='r', encoding='utf-8-sig') as f:
    reader = csv.reader(f)
    rows = list(reader)

# Indices based on previous inspection
c_w_indices = [(5,6), (7,8), (9,10), (11,12), (13,14)]

# Calculate sums
total_c = [0] * 5
total_cw = [0] * 5

# Identify data rows (start at 1, end before 'TỔNG CỘNG')
data_rows = []
footer_start_idx = -1

for i, row in enumerate(rows):
    if i == 0: continue
    if len(row) > 1 and "TỔNG CỘNG" in row[1]:
        footer_start_idx = i
        break
    if len(row) >= 15:
        data_rows.append(i)

# Recalculate each row and accumulation
for r_idx in data_rows:
    row = rows[r_idx]
    row_sum_cw = 0
    for i, (c_idx, w_idx) in enumerate(c_w_indices):
        c = parse_int(row[c_idx])
        w = parse_int(row[w_idx])
        cw = c * w
        
        total_c[i] += c
        total_cw[i] += cw

# Sum of all weighted values
unadjusted_fp = sum(total_cw)
sum_fi = 57 # As determined
vaf = 0.65 + (0.01 * sum_fi)
final_fp = unadjusted_fp * vaf

print(f"Unadjusted FP: {unadjusted_fp}")
print(f"VAF: {vaf}")
print(f"Final FP: {final_fp}")

# Update Footer Rows
# Assuming the structure exists
if footer_start_idx != -1:
    # Update Totals Row
    # row format: ,TỔNG CỘNG (Delta),,,,40,178,6,34,14,62,57,565,6,42
    # indices:                     5  6   7  8   9  10 11  12  13 14
    
    total_row = rows[footer_start_idx]
    # Keep first few columns empty
    total_row[5] = str(total_c[0])
    total_row[6] = str(total_cw[0])
    total_row[7] = str(total_c[1])
    total_row[8] = str(total_cw[1])
    total_row[9] = str(total_c[2])
    total_row[10] = str(total_cw[2])
    total_row[11] = str(total_c[3])
    total_row[12] = str(total_cw[3])
    total_row[13] = str(total_c[4])
    total_row[14] = str(total_cw[4])
    
    # Update Fi/VAF Row
    # 67: ,THAM SỐ HIỆU CHỈNH (Fi),,,,Sum Fi,57,VAF,1.22
    # usually 2 rows down from footer start?
    # Let's search for it
    fi_row_idx = footer_start_idx + 2
    if fi_row_idx < len(rows):
        fi_row = rows[fi_row_idx]
        # Update Sum Fi and VAF
        # Assuming format: , , , , , Sum Fi, 57, VAF, 1.22
        # indices vary, let's look for keywords or hardcode based on previous view
        try:
           # Based on view: ,THAM SỐ HIỆU CHỈNH (Fi),,,,Sum Fi,57,VAF,1.22
           # col 1: THAM SỐ...
           # col 5: Sum Fi
           # col 6: 57
           # col 7: VAF
           # col 8: 1.22
           fi_row[6] = str(sum_fi)
           fi_row[8] = f"{vaf:.2f}"
        except:
            print("Could not update VAF row exactly")

    # Update Final Result Row
    # 68: ,KẾT QUẢ CUỐI CÙNG (FP),,,,Unadjusted,926,Function Point,1129.72
    fp_row_idx = footer_start_idx + 3
    if fp_row_idx < len(rows):
        fp_row = rows[fp_row_idx]
        try:
            # col 5: Unadjusted
            # col 6: value
            # col 7: Function Point
            # col 8: value
            fp_row[6] = str(unadjusted_fp)
            fp_row[8] = f"{final_fp:.2f}"
        except:
             print("Could not update Final FP row exactly")

# Write to file
with open(file_path, 'w', encoding='utf-8-sig', newline='') as f:
    writer = csv.writer(f)
    writer.writerows(rows)

print("Updated file successfully.")
