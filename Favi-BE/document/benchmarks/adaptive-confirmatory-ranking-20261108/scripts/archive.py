"""Archive completed ranking evidence and read every member back."""
import hashlib, json, zipfile
from pathlib import Path
OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
def sha(data): return hashlib.sha256(data).hexdigest()
def main():
    state=json.loads((OUT/'batch.json').read_text())
    assert state['status']=='PASS' and len(state['datasets'])==6
    assert (OUT/'summary.json').is_file()
    target=ROOT/'.tools/dataset-archives/adaptive-confirmatory-ranking-20261108.zip'
    receipt=target.with_suffix('.zip.json')
    assert not target.exists() and not receipt.exists()
    files={p.relative_to(OUT).as_posix():sha(p.read_bytes()) for p in sorted(OUT.rglob('*')) if p.is_file()}
    with zipfile.ZipFile(target,'x',zipfile.ZIP_DEFLATED) as z:
        for name in files: z.write(OUT/name,name)
    with zipfile.ZipFile(target) as z:
        assert len(z.namelist())==len(files)
        assert all(sha(z.read(n))==h for n,h in files.items())
    result=dict(archive=str(target),bytes=target.stat().st_size,sha256=sha(target.read_bytes()),members=len(files),member_sha256=files,readback_passed=True)
    receipt.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(dict(archive=str(target),members=len(files),bytes=result['bytes'],sha256=result['sha256'],readback_passed=True)))
if __name__=='__main__': main()
