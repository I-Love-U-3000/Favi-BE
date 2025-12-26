import csv
import os

file_path = r"c:\Users\tophu\source\repos\Favi-BE\favi_function_points.csv"

def calculate_and_update():
    rows = []
    with open(file_path, 'r', encoding='utf-8') as f:
        reader = csv.reader(f)
        rows = list(reader)

    header = rows[0]
    data_rows = []
    
    # Identify data rows (indices 1 to 81 based on previous file viewing, look for valid ID)
    start_row = 1
    end_row = 0
    
    # Find the range of functional rows
    for i, row in enumerate(rows):
        if not row: continue # Skip empty rows
        if i == 0: continue
        # simple check if first column is an integer
        if row[0].isdigit():
            data_rows.append(row)
            end_row = i
        elif "TỔNG CỘNG" in row[1]:
            break
            
    # Initialize sums
    # Columns 5,6 (C1, W1), 7,8 (C2, W2), 9,10 (C3, W3), 11,12 (C4, W4), 13,14 (C5, W5)
    sum_c = [0] * 5
    sum_cw = [0] * 5
    
    for row in data_rows:
        try:
            # Pairs logic: 5&6, 7&8, 9&10, 11&12, 13&14
            for i in range(5):
                c_idx = 5 + (i * 2)
                w_idx = 6 + (i * 2)
                
                c_val = int(row[c_idx]) if row[c_idx].strip() else 0
                w_val = int(row[w_idx]) if row[w_idx].strip() else 0
                
                row_product = c_val * w_val
                
                sum_c[i] += c_val
                sum_cw[i] += row_product
        except ValueError:
            continue

    total_delta = sum(sum_cw)
    fi = 57
    vaf = 1.22 # As per user request (0.65 + 0.01 * 57 = 1.22)
    fp = total_delta * vaf
    
    # Construct Summary Rows
    # Existing rows might be empty placeholders, we will replace or append
    
    # Base structure for summary rows, ensuring alignment
    # Stt, Main, Sub, Mob, Web, C1, W1, C2, W2, C3, W3, C4, W4, C5, W5
    
    summary_delta = ["", "TỔNG CỘNG (Delta)", "", "", ""]
    # Add sums to the row
    for i in range(5):
        summary_delta.append(str(sum_c[i]))
        summary_delta.append(str(sum_cw[i]))
        
    summary_fi = ["", "THAM SỐ HIỆU CHỈNH (Fi)", "", "", "", "Sum Fi", str(fi), "VAF", str(vaf), "", "", "", "", "", ""]
    
    # Format FP to 2 decimal places
    fp_str = "{:.2f}".format(fp)
    summary_fp = ["", "KẾT QUẢ CUỐI CÙNG (FP)", "", "", "", "Unadjusted", str(total_delta), "Function Point", fp_str, "", "", "", "", "", ""]

    # Reconstruct file content
    # Headers + Data Rows + Blank + Summaries
    
    new_rows = [header] + data_rows
    new_rows.append([]) # Blank line
    new_rows.append(summary_delta)
    new_rows.append([]) # Blank line
    new_rows.append(summary_fi)
    new_rows.append(summary_fp)
    
    with open(file_path, 'w', encoding='utf-8', newline='') as f:
        writer = csv.writer(f)
        writer.writerows(new_rows)
        
    print(f"Calculated UFP: {total_delta}")
    print(f"Calculated FP: {fp_str}")

if __name__ == "__main__":
    calculate_and_update()
