import csv

file_path = r"c:\Users\tophu\source\repos\Favi-BE\favi_function_points.csv"

def optimize_weights():
    rows = []
    with open(file_path, 'r', encoding='utf-8') as f:
        reader = csv.reader(f)
        rows = list(reader)

    header = rows[0]
    data_rows = []
    
    # Simple Complexity Weights from User
    W_SIMPLE = {
        'W1': 3,  # Input
        'W2': 4,  # Output
        'W3': 3,  # Query
        'W4': 7,  # Internal File
        'W5': 5   # External Interface
    }

    # Indices (0-based)
    # C1:5, W1:6
    # C2:7, W2:8
    # C3:9, W3:10
    # C4:11, W4:12
    # C5:13, W5:14
    
    recalc_rows = []

    for i, row in enumerate(rows):
        if not row: continue
        if i == 0: 
            recalc_rows.append(row)
            continue
            
        # Stop at summary
        if "TỔNG CỘNG" in row[1]:
            break
            
        if not row[0].isdigit():
            if "TỔNG CỘNG" in row[1]:
                break
            continue
            
        new_row = list(row)
        # Ensure row has enough columns (up to index 14 for W5)
        while len(new_row) <= 14:
            new_row.append("")
            
        lower_name = (new_row[2] + " " + new_row[1]).lower()
        
        # Keywords that imply Data Persistence impact (Write/Modify)
        write_keywords = ["create", "update", "delete", "add", "register", "adjust", "switch", "manage", "reply", "send", "mark", "block", "unblock", "hide", "restore", "archive", "unarchived", "react", "save", "configure"]
        
        is_write = any(k in lower_name for k in write_keywords)

        # Helper to safely set weight
        def set_weight(r, c_idx, w_target, force_zero=False):
            try:
                # If we want to keep existing C value:
                val = r[c_idx].strip()
                if not force_zero and val and val.isdigit() and int(val) > 0:
                    r[c_idx+1] = str(w_target)
                else:
                    # If forcing zero, we might also want to zero the Count? 
                    # User said "reduce points... C 0-5". 
                    # If we zero the weight, the points are zero. 
                    # To be clean, if we zero weight, we can imply C is irrelevant to the calc, 
                    # but let's keep C as 1 to show feature exists, just weight is 0? 
                    # No, usually if weight is 0, it means it doesn't count.
                    # Standard practice: If it doesn't count, W=0.
                    r[c_idx+1] = "0"
            except:
                r[c_idx+1] = "0"

        # Apply for C1->W1, C2->W2, etc.
        set_weight(new_row, 5, W_SIMPLE['W1'])
        set_weight(new_row, 7, W_SIMPLE['W2'])
        set_weight(new_row, 9, W_SIMPLE['W3'])
        
        # W4 (File) - Only for Writes
        if is_write:
             set_weight(new_row, 11, W_SIMPLE['W4'])
        else:
             # For Read-only, reduce W4 to 0 (effectively removing ILF complexity contribution)
             set_weight(new_row, 11, 0, force_zero=True)

        set_weight(new_row, 13, W_SIMPLE['W5'])
             
        recalc_rows.append(new_row)

    # Recalculate Totals
    sum_c = [0] * 5
    sum_cw = [0] * 5
    
    for row in recalc_rows[1:]: # Skip header
        for i in range(5):
            c_idx = 5 + (i * 2)
            w_idx = 6 + (i * 2)
            
            try:
                c_val = int(row[c_idx]) if row[c_idx].strip() else 0
                w_val = int(row[w_idx]) if row[w_idx].strip() else 0
                
                sum_c[i] += c_val
                sum_cw[i] += c_val * w_val
            except ValueError:
                pass

    total_delta = sum(sum_cw)
    fi = 57
    vaf = 1.22
    fp = total_delta * vaf

    # Summary Rows
    summary_delta = ["", "TỔNG CỘNG (Delta)", "", "", ""]
    for i in range(5):
        summary_delta.append(str(sum_c[i]))
        summary_delta.append(str(sum_cw[i]))
        
    summary_fi = ["", "THAM SỐ HIỆU CHỈNH (Fi)", "", "", "", "Sum Fi", str(fi), "VAF", str(vaf), "", "", "", "", "", ""]
    summary_fp = ["", "KẾT QUẢ CUỐI CÙNG (FP)", "", "", "", "Unadjusted", str(total_delta), "Function Point", "{:.2f}".format(fp), "", "", "", "", "", ""]

    recalc_rows.append([])
    recalc_rows.append(summary_delta)
    recalc_rows.append([])
    recalc_rows.append(summary_fi)
    recalc_rows.append(summary_fp)
    
    with open(file_path, 'w', encoding='utf-8', newline='') as f:
        writer = csv.writer(f)
        writer.writerows(recalc_rows)
        
    print(f"Optimization Complete.")
    print(f"Unadjusted: {total_delta}")
    print(f"Final FP: {fp}")

if __name__ == "__main__":
    optimize_weights()
