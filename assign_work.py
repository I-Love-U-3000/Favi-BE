import csv

fp_file = r"c:\Users\tophu\source\repos\Favi-BE\favi_function_points.csv"
out_file = r"c:\Users\tophu\source\repos\Favi-BE\favi_work_assignment.csv"

def assign_work():
    rows = []
    with open(fp_file, 'r', encoding='utf-8') as f:
        reader = csv.reader(f)
        rows = list(reader)

    header = rows[0]
    # Header format: Stt, Main, Sub, Mobile, Web, C1, W1...
    
    tasks = []
    
    # FP Constants
    VAF = 1.22
    
    # 1. Calculate FP per row and Collect Data
    for i, row in enumerate(rows):
        if i == 0: continue
        if not row: continue
        if "TỔNG CỘNG" in row[1]: break
        
        # Calculate Row Delta
        row_delta = 0
        try:
            for k in range(5):
                c_idx = 5 + (k * 2)
                w_idx = 6 + (k * 2)
                c_val = int(row[c_idx]) if row[c_idx].strip().isdigit() else 0
                w_val = int(row[w_idx]) if row[w_idx].strip().isdigit() else 0
                row_delta += c_val * w_val
        except:
             pass
             
        row_fp = row_delta * VAF
        
        # Determine likely domain
        # "View", "UI", "Search" -> Frontend leaining
        # "Manage", "Create", "Auth", "API", "System" -> Backend/Fullstack leaning
        
        lower_sub = row[2].lower()
        lower_main = row[1].lower()
        full_name = lower_main + " " + lower_sub
        
        domain = "Fullstack"
        if "view" in lower_sub or "search" in lower_sub or "monitor" in lower_sub:
            domain = "Frontend"
        elif "admin" in lower_main or "system" in lower_main or "auth" in lower_main or "manage" in lower_sub:
             domain = "Backend"
             
        tasks.append({
            "stt": row[0],
            "main": row[1],
            "sub": row[2],
            "fp": row_fp,
            "domain": domain
        })
        
    # 2. Assign
    # Contributors: 
    # Minh Quang (Strong FE, Good BE)
    # Phu Quy (Strong BE, Some FE)
    
    assigned_tasks = []
    
    fp_quang = 0
    fp_quy = 0
    
    # Sort tasks by FP descending to pack bin
    # No, keep chronological order for readability, assign greedily or logic based
    
    # Logic:
    # 1. Give pure FE tasks to Quang
    # 2. Give pure BE tasks to Quy
    # 3. Balance remainders
    
    for t in tasks:
        assignee = ""
        
        if "Frontend" == t['domain']:
            assignee = "Minh Quang"
        elif "Backend" == t['domain']:
            assignee = "Phú Quý"
        else:
            # Fullstack / Undecided
            # Check Balance
            if fp_quy < fp_quang:
                assignee = "Phú Quý"
            else:
                assignee = "Minh Quang"
        
        # Refine balance if one side gets too heavy
        # Current logic is loose. Let's do a second pass adjustment?
        # No, let's just create the list first.
        
        if assignee == "Minh Quang":
            fp_quang += t['fp']
        else:
            fp_quy += t['fp']
            
        t['assignee'] = assignee
        assigned_tasks.append(t)
        
    # 3. Balance Check
    # If gap is too large, swap some tasks
    # Determine Avg
    total = fp_quang + fp_quy
    target = total / 2
    
    # Iterative Swapping to balance
    # Try to move tasks from Heavy -> Light
    sorted_tasks = sorted(assigned_tasks, key=lambda x: x['fp'], reverse=True)
    
    for t in sorted_tasks:
        current_gap = abs(fp_quy - fp_quang)
        if current_gap < 5: break # Close enough
        
        who_has = t['assignee']
        points = t['fp']
        
        if who_has == "Minh Quang" and fp_quang > target:
            # Try giving to Quy
            # Check if domain allows (don't give strict FE to BE if avoidable, but they are fullstack)
            # Favi context: Both commit everywhere.
            new_quang = fp_quang - points
            new_quy = fp_quy + points
            if abs(new_quy - new_quang) < current_gap:
                # Swap
                t['assignee'] = "Phú Quý"
                fp_quang = new_quang
                fp_quy = new_quy
                
        elif who_has == "Phú Quý" and fp_quy > target:
            new_quy = fp_quy - points
            new_quang = fp_quang + points
            if abs(new_quy - new_quang) < current_gap:
                # Swap
                t['assignee'] = "Minh Quang"
                fp_quy = new_quy
                fp_quang = new_quang

    # Sort back by STT
    assigned_tasks.sort(key=lambda x: int(x['stt']))
    
    # Write Output
    with open(out_file, 'w', encoding='utf-8-sig', newline='') as f:
        writer = csv.writer(f)
        writer.writerow(["STT", "Chức Năng Chính", "Chức Năng Con", "Function Points", "Người Phụ Trách", "Giờ làm việc (Est. Hours)"])
        
        # Assumption: 4 Hours per Function Point (0.25 FP/Hr)
        HOURS_PER_FP = 4
        
        sum_hours_quy = 0
        sum_hours_quang = 0
        
        for t in assigned_tasks:
            hours = t['fp'] * HOURS_PER_FP
            writer.writerow([t['stt'], t['main'], t['sub'], "{:.2f}".format(t['fp']), t['assignee'], "{:.2f}".format(hours)])
            
            if t['assignee'] == "Minh Quang":
               sum_hours_quang += hours
            else:
               sum_hours_quy += hours
            
        # Summary Row
        writer.writerow([])
        writer.writerow(["TỔNG HỢP", "", "", "", "", ""])
        writer.writerow(["Phú Quý", "", "", "{:.2f}".format(fp_quy), "", "{:.2f}".format(sum_hours_quy)])
        writer.writerow(["Minh Quang", "", "", "{:.2f}".format(fp_quang), "", "{:.2f}".format(sum_hours_quang)])
        
        total_fp = fp_quy + fp_quang
        total_hours = sum_hours_quy + sum_hours_quang
        writer.writerow(["TOTAL", "", "", "{:.2f}".format(total_fp), "", "{:.2f}".format(total_hours)])
        
    print(f"Assignment Complete.")
    print(f"Phú Quý: {fp_quy:.2f} FP / {sum_hours_quy:.2f} Hours")
    print(f"Minh Quang: {fp_quang:.2f} FP / {sum_hours_quang:.2f} Hours")

if __name__ == "__main__":
    assign_work()
