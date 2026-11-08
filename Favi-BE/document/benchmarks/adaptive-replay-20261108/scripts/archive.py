"""Archive successful replay plus retained failed preflight; verify every byte."""
import hashlib, json, shutil, zipfile
from pathlib import Path
OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
TRACK=ROOT/'document/benchmarks/adaptive-replay-20261108'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def save(p,v): p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
assert read(OUT/'batch.json')['status']=='PASS' and read(OUT/'audit.json')['status']=='PASS'
assert not TRACK.exists()
archive=ROOT/'.tools/dataset-archives/adaptive-replay-20261108.zip'
failed=OUT.with_name(OUT.name+'-preflight-failed-01')
inventory={}
with zipfile.ZipFile(archive,'x',zipfile.ZIP_DEFLATED) as z:
    for folder in (OUT,failed):
        for p in sorted(folder.rglob('*')):
            if p.is_file():
                name=p.relative_to(OUT.parent).as_posix(); inventory[name]=sha(p); z.write(p,name)
with zipfile.ZipFile(archive) as z:
    assert len(z.namelist())==len(inventory)
    for name,h in inventory.items(): assert hashlib.sha256(z.read(name)).hexdigest()==h,name
receipt=dict(archive=str(archive),sha256=sha(archive),bytes=archive.stat().st_size,members=len(inventory),all_member_readback='PASS',files=inventory)
save(archive.with_suffix('.zip.json'),receipt)
TRACK.mkdir(parents=True)
save(TRACK/'archive.json',receipt)
for name in ('batch.json','source-inventory.json','summary.json','audit.json','REPORT_VI.md'):
    shutil.copyfile(OUT/name,TRACK/name)
(TRACK/'scripts').mkdir()
for p in OUT.glob('*.py'): shutil.copyfile(p,TRACK/'scripts'/p.name)
for entry in read(OUT/'batch.json')['datasets']:
    destination=TRACK/entry['dataset']; destination.mkdir()
    for name in ('report.json','manifest.json','inputs.json','source-inventory.json'):
        shutil.copyfile(OUT/entry['dataset']/name,destination/name)
(TRACK/'preflight-failed-01').mkdir()
for name in ('batch.json','run_batch.py','source-inventory.json'):
    shutil.copyfile(failed/name,TRACK/'preflight-failed-01'/name)
print(json.dumps({k:v for k,v in receipt.items() if k!='files'},indent=2))
