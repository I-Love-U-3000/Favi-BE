"""Render audited fixed-selection replay evidence without selecting methods."""
import json
from pathlib import Path
OUT=Path(__file__).resolve().parent
s=json.loads((OUT/'summary.json').read_text())
assert json.loads((OUT/'audit.json').read_text())['status']=='PASS'
lines=['# Fixed-selection replay: robustness và ablations','',
'08/11/2026, Asia/Saigon. Secondary analysis theo adaptive-v1-20261108; source e5c23f0. Selection, model parameters, exact epochs và optimizer seeds giữ nguyên; không retune. Primary practical gate từ ranking gốc vẫn **FAIL** (main mean R−B1 +0.000334784 <0.001). Replay không thay kết luận đó.','',
'Cả sáu datasets 5.000 users/2.000 posts/500 ticks giữ nguyên events, request400/targets[400,500), users/candidates/targets. Primary predictions được so từng ranked ID với evidence gốc cho đủ chín methods và optimizer seeds. Optimizer repeats gộp trong dataset trước; các bảng aggregate là mean đều ba dataset seeds, std là sample std của ba dataset means.','']
for scenario,sc in s['scenarios'].items():
    lines += ['## '+scenario,'','| Mode | B1 nDCG@10 | R nDCG@10 | R−B1 mean | Dataset std delta | R−primary |','|---|---:|---:|---:|---:|---:|']
    for mode,methods in sc['aggregate'].items():
        delta=sc['deltas'][mode]
        lines.append(f"| {mode} | {methods['B1']['ndcg']['mean']:.9f} | {methods['R']['ndcg']['mean']:.9f} | {delta['R-B1']['mean']:.9f} | {delta['R-B1']['std']:.9f} | {delta['R-primary']['mean']:.9f} |")
    lines += ['','### Đủ methods trên primary và ba sidecars','','| Mode | Method | Mean nDCG@10 | Dataset std | Mean Recall@10 |','|---|---|---:|---:|---:|']
    for mode,methods in sc['aggregate'].items():
        for label,m in methods.items(): lines.append(f"| {mode} | {label} | {m['ndcg']['mean']:.9f} | {m['ndcg']['std']:.9f} | {m['recall']['mean']:.9f} |")
    lines += ['','### Ablation tách routing khỏi joint selection','','| Seed | R−B1 | R−R-no-routing | R-no-routing−B1 |','|---:|---:|---:|---:|']
    for d in sc['datasets']:
        ds=d['deltas']['primary']; lines.append(f"| {d['seed']} | {ds['R-B1']:.9f} | {ds['R-R-no-routing']:.9f} | {ds['R-no-routing-B1']:.9f} |")
    ds=sc['deltas']['primary']; lines.append(f"| Mean | {ds['R-B1']['mean']:.9f} | {ds['R-R-no-routing']['mean']:.9f} | {ds['R-no-routing-B1']['mean']:.9f} |")
    lines += ['','No-routing chỉ đổi threshold R thành1, giữ R model/config/epoch. No-topics giữ general cold fallback, không ép collaborative lên zero-positive users. Hiệu số trên cùng model cô lập threshold trong replay; không phải causal effect.','',
'### Paired population, overlap và chuyển cohort','','| Seed | Mode | Paired users | Changed topic users | Overlap/declared slots | Overlap rate |','|---:|---|---:|---:|---|---:|']
    for d in sc['datasets']:
        for mode,m in d['metadata'].items():
            rate='N/A' if m['overlap_rate'] is None else f"{m['overlap_rate']:.6f}"
            lines.append(f"| {d['seed']} | {mode} | {m['paired_users']} | {m['changed_topic_users']} | {m['overlap_with_primary']}/{m['declared_slots']} | {rate} |")
    lines += ['','Transitions đầy đủ theo từng seed:','']
    for d in sc['datasets']:
        for mode,m in d['metadata'].items(): lines.append(f"- {d['seed']} {mode}: "+'; '.join(f'{k}={v}' for k,v in m['cohort_transitions'].items())+'.')
    lines += ['','### Cohort: cả phân nhóm gốc và phân nhóm hiện tại','','| Mode | Partition | Cohort | Dataset count đủ ≥30 | Mean R−B1 | R−primary trên primary cohort |','|---|---|---|---:|---:|---:|']
    for mode,fields in sc['cohorts'].items():
        for field,cohorts in fields.items():
            for c,m in cohorts.items():
                change='N/A' if m['R_minus_primary'] is None else f"{m['R_minus_primary']['mean']:.9f}"
                lines.append(f"| {mode} | {field} | {c} | {m['dataset_count']} | {m['delta_R_B1']['mean']:.9f} | {change} |")
    lines += ['','Không so current-cohort means với primary-cohort means như cùng một nhóm người. JSON giữ denominators và optimizer std của từng method/mode/dataset; cohort không có target là N/A.','']
lines += ['## Diễn giải kết quả secondary','',
'Main: R−B1 đổi từ +0.000334784 với primary/informative thành −0.001044392 khi noisy, −0.001499580 khi independent và −0.001437935 khi bỏ topics. Cả ba dataset deltas đều âm trong ba mode sau; đây là dấu hiệu router đã chọn phụ thuộc chất lượng topics trong thiết kế synthetic này. Không chọn lại threshold theo sidecar để cứu kết quả.','',
'Ablation main: giữ cấu hình R, đổi threshold từ1 sang threshold đã chọn cho mean +0.000416166; phần khác biệt R-no-routing−B1 là −0.000081382. Thành phần threshold không thắng mọi seed (min −0.000143273); tổng joint R vẫn không đạt practical gate. Các hiệu số này là mô tả replay, không causal effect hoặc significance.','',
'Sparse: R=B1 trong mọi mode vì selection gốc trùng nhau ở threshold1. Informative tăng cả hai so primary +0.000886063 nhưng sidecar là pre-scarcity và chuyển cohort; không diễn giải thành lợi ích routing hoặc cải thiện trên cùng current cohort C3.','',
'## Provenance và kiểm chứng','',
'Cả sáu preflight kiểm ZIP receipt SHA256, mọi dataset member, original ranking manifests và tracked batch/report/selection/source hashes trước fit. Source/config/lock/CLI inventory và frozen-source.zip lưu trong batch. Một preflight attempt lỗi trước model do script yêu cầu thêm tracked protocol.json không tồn tại; giữ nguyên ở adaptive-replay-20261108-preflight-failed-01. Sửa kiểm chứng đường dẫn, không sửa dữ liệu/model/selection; batch mới dừng ngay nếu lỗi.','',
'Independent audit tính lại mọi per-user nDCG/Recall từ ranked IDs, cohort means theo hai partition, common original users/candidates/targets, primary exact prediction parity, overlap/transitions, frozen choices và optimizer hierarchy. Không fit lại để kiểm số học. Xem audit.json, summary.json, batch.json, source-inventory.json và archive.json cạnh báo cáo. Raw/logs/ZIP ở solution .tools, không đưa vào Git.','',
'## Giới hạn và việc còn lại','',
'Sparse primary dùng music-only scarcity, trong khi informative/noisy/independent sidecars là metadata trước scarcity. Vì vậy sparse informative khác primary và có thể chuyển C3 sang C1/C2. Giữ mọi paired users, không sửa sidecars hay chọn subset để bảo toàn cohort.','',
'Đây là synthetic metadata replay với event log cố định, không đo thay đổi hành vi/exposure hoặc causal engagement. Ba dataset seeds chỉ mô tả ổn định, không chứng minh significance/novelty. Primary practical gate thất bại vẫn được giữ. Refit seconds không phải full training cost, peak RAM hoặc HTTP latency.','',
'Full cost và HTTP lifecycle/fallback/privacy/resource gates còn phải làm. Endpoint thử nghiệm hiện chỉ EASE/PPR/threshold1; không gọi HTTP evidence cũ là serving R/LightGCN. Giới hạn sample ≤1.000 users/16MiB/300 candidates vẫn giữ, không gọi là full5k deployment. Không merge/deploy tự ý.','']
(OUT/'REPORT_VI.md').write_text('\n'.join(lines),encoding='utf-8')
