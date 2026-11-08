import hashlib, json, os, subprocess, sys, time, zipfile
from pathlib import Path
from datetime import datetime, timezone
ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
LEASE=ROOT/'.tools/adaptive-worker.json'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def save(p,v): p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
def now(): return datetime.now(timezone.utc).isoformat()
def lease(status,child=None):
    save(LEASE,dict(updated_utc=now(),owner='adaptive-confirmatory-ranking-session',status=status,process_ids=[os.getpid()]+([child] if child else []),process_sessions=[],milestone='six dataset frozen adaptive benchmark',state_file=str(OUT/'batch.json')))
def main():
    lease('running')
    gen=json.loads((ROOT/'document/benchmarks/adaptive-confirmatory-data-20261108/generation.json').read_text())
    state=dict(status='preflight',started_utc=now(),git_commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),datasets=[])
    save(OUT/'batch.json',state)
    try:
        for name,h in gen['source_inventory'].items():
            if sha(ROOT/name)!=h: raise RuntimeError('Source drift: '+name)
        inventory={name:sha(ROOT/name) for name in gen['source_inventory']}
        inventory[Path(__file__).relative_to(ROOT).as_posix()]=sha(Path(__file__))
        save(OUT/'source-inventory.json',inventory)
        with zipfile.ZipFile(OUT/'frozen-source.zip','x',zipfile.ZIP_DEFLATED) as z:
            for name in inventory: z.write(ROOT/name,name)
        with zipfile.ZipFile(OUT/'frozen-source.zip') as z:
            assert all(hashlib.sha256(z.read(n)).hexdigest()==h for n,h in inventory.items())
        state.update(source_archive_sha256=sha(OUT/'frozen-source.zip'),preflight=[])
        for seed in (20261111,20261112,20261113):
            for scenario in ('main','sparse'):
                name=f'adaptive-confirmatory-{scenario}-{seed}'
                dataset=ROOT/'.tools/datasets'/name
                archive=ROOT/'.tools/dataset-archives'/(name+'.zip')
                receipt=json.loads(archive.with_suffix('.zip.json').read_text())
                assert sha(archive)==receipt['sha256']
                manifest=json.loads((dataset/'dataset-manifest.json').read_text())
                files={n.replace('\\','/'):h for n,h in manifest['files'].items()}
                files['dataset-manifest.json']=sha(dataset/'dataset-manifest.json')
                with zipfile.ZipFile(archive) as z:
                    assert len(z.namelist())==receipt['members']==len(files)
                    for n,h in files.items():
                        assert sha(dataset/n)==h,n
                        assert hashlib.sha256(z.read(name+'/'+n)).hexdigest()==h,n
                state['preflight'].append(dict(dataset=name,archive_sha256=receipt['sha256'],manifest_sha256=files['dataset-manifest.json'],members=len(files),status='PASS'))
                save(OUT/'batch.json',state)
        state['status']='running'
        for entry in state['preflight']:
            name=entry['dataset']
            output=OUT/name
            cmd=[sys.executable,'-X','utf8',str(ROOT/'tools/oasis-smoke/run_adaptive_benchmark.py'),'--dataset',str(ROOT/'.tools/datasets'/name),'--output',str(output)]
            record=dict(dataset=name,cli=cmd,status='running',started_utc=now())
            state['datasets'].append(record); save(OUT/'batch.json',state)
            print('START '+name,flush=True)
            start=time.perf_counter()
            with (OUT/(name+'.log')).open('x',encoding='utf-8') as log:
                p=subprocess.Popen(cmd,cwd=ROOT,stdout=log,stderr=subprocess.STDOUT)
                lease('running',p.pid)
                record['pid']=p.pid; save(OUT/'batch.json',state)
                while p.poll() is None:
                    lease('running',p.pid); time.sleep(20)
                if p.returncode: raise RuntimeError(f'{name} exit {p.returncode}; batch stopped')
            for n,h in inventory.items():
                if sha(ROOT/n)!=h: raise RuntimeError('Source drift after run: '+n)
            manifest=json.loads((output/'manifest.json').read_text())
            assert all(sha(output/n)==h for n,h in manifest.items())
            record.update(status='PASS',elapsed_seconds=time.perf_counter()-start,completed_utc=now(),manifest_sha256=sha(output/'manifest.json'),report_sha256=sha(output/'report.json'))
            save(OUT/'batch.json',state); print('PASS '+name,flush=True)
        state['status']='PASS'
    except BaseException as e:
        state.update(status='FAIL',error=repr(e)); raise
    finally:
        state['updated_utc']=now(); save(OUT/'batch.json',state); lease('benchmark-finished' if state['status']=='PASS' else 'benchmark-failed')
if __name__=='__main__': main()
