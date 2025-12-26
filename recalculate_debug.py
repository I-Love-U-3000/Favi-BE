import csv

file_path = r'c:\Users\tophu\source\repos\Favi-BE\favi_function_points.csv'

def parse_int(val):
    if not val:
        return 0
    try:
        return int(val)
    except ValueError:
        return 0

with open(file_path, mode='r', encoding='utf-8-sig') as f:
    reader = csv.reader(f)
    rows = list(reader)

# Check a few rows
print(f"Header: {rows[0]}")
c_w_indices = [(5,6), (7,8), (9,10), (11,12), (13,14)]

for i in range(1, 15): # First 15 rows
    row = rows[i]
    if len(row) < 15: continue
    print(f"Row {i} ({row[2]}):")
    total_row = 0
    for idx, (c_i, w_i) in enumerate(c_w_indices):
        c = parse_int(row[c_i])
        w = parse_int(row[w_i])
        prod = c*w
        total_row += prod
        if prod > 0:
            print(f"  G{idx+1}: C={c}, W={w}, Prod={prod}")
    print(f"  Total Row: {total_row}")

# Check for anomalies
print("Checking for large values...")
for i in range(1, len(rows)):
    row = rows[i]
    if len(row) < 15: continue
    if "TỔNG CỘNG" in row[1]: break
    
    for idx, (c_i, w_i) in enumerate(c_w_indices):
        c = parse_int(row[c_i])
        w = parse_int(row[w_i])
        if c * w > 100:
             print(f"Anomaly at Row {i+1} Group {idx+1}: C={c} W={w} Prod={c*w}")
