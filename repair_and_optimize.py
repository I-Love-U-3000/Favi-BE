import csv

file_path = r"c:\Users\tophu\source\repos\Favi-BE\favi_function_points.csv"

def repair_and_optimize():
    rows = []
    with open(file_path, 'r', encoding='utf-8') as f:
        reader = csv.reader(f)
        rows = list(reader)

    if not rows: return

    header = rows[0]
    
    # Check for missing Mobile column
    # Expected: Stt, Main, Sub, Mobile, Web, C1...
    # Indices: 0, 1, 2, 3, 4, 5...
    # Current Bad: Stt, Main, Sub, Web, C1...
    
    needs_repair = False
    if header[3].strip() == "Web":
        print("Detected missing Mobile column. Repairing...")
        needs_repair = True
        # Fix header
        header.insert(3, "Mobile")
        
    final_rows = []
    final_rows.append(header)
    
    # Optimization Constants
    W_SIMPLE = {'W1': 3, 'W2': 4, 'W3': 3, 'W4': 7, 'W5': 5}
    write_keywords = ["create", "update", "delete", "add", "register", "adjust", "switch", "manage", "reply", "send", "mark", "block", "unblock", "hide", "restore", "archive", "unarchived", "react", "save", "configure"]

    for i, row in enumerate(rows):
        if i == 0: continue # Skip header handled above
        if not row: continue
        
        # Stop at summary
        if "TỔNG CỘNG" in row[1]:
            break
            
        current_row = list(row)
        
        # Repair row if needed
        if needs_repair:
            # Insert 'x' at index 3
            current_row.insert(3, "x")
            
        # Ensure length padding (15 columns: 0-14. W5 is 14)
        while len(current_row) <= 14:
            current_row.append("")
            
        # Optimize Weights
        # C1:5, W1:6
        # C2:7, W2:8
        # C3:9, W3:10
        # C4:11, W4:12
        # C5:13, W5:14
        
        lower_name = (current_row[2] + " " + current_row[1]).lower()
        is_write = any(k in lower_name for k in write_keywords)

        def set_weight(r, c_idx, w_target, force_zero=False):
            try:
                val = r[c_idx].strip()
                # Check for digit (some rows might have 'x' elsewhere? no C columns are ints)
                if not force_zero and val and val.isdigit() and int(val) > 0:
                    r[c_idx+1] = str(w_target)
                else:
                    if force_zero:
                        r[c_idx+1] = "0"
                    else:
                        # If count is 0, weight is 0
                        r[c_idx+1] = "0"
            except:
                r[c_idx+1] = "0"

        set_weight(current_row, 5, W_SIMPLE['W1'])
        set_weight(current_row, 7, W_SIMPLE['W2'])
        set_weight(current_row, 9, W_SIMPLE['W3'])
        
        if is_write:
            set_weight(current_row, 11, W_SIMPLE['W4'])
        else:
             set_weight(current_row, 11, 0, force_zero=True)
             
        set_weight(current_row, 13, W_SIMPLE['W5'])
        
        final_rows.append(current_row)
        
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
    
    # Append Summaries
    summary_delta = ["", "TỔNG CỘNG (Delta)", "", "", ""]
    for k in range(5):
        summary_delta.append(str(sum_c[k]))
        summary_delta.append(str(sum_cw[k]))
        
    summary_fi = ["", "THAM SỐ HIỆU CHỈNH (Fi)", "", "", "", "Sum Fi", str(fi), "VAF", str(vaf), "", "", "", "", "", ""]
    summary_fp = ["", "KẾT QUẢ CUỐI CÙNG (FP)", "", "", "", "Unadjusted", str(total_delta), "Function Point", "{:.2f}".format(fp), "", "", "", "", "", ""]
    
    final_rows.append([])
    final_rows.append(summary_delta)
    final_rows.append([])
    final_rows.append(summary_fi)
    final_rows.append(summary_fp)
    
    with open(file_path, 'w', encoding='utf-8', newline='') as f:
        writer = csv.writer(f)
        writer.writerows(final_rows)
        
    print(f"Repair and Optimization Complete.")
    print(f"Unadjusted: {total_delta}")
    print(f"FP: {fp}")

if __name__ == "__main__":
    repair_and_optimize()
