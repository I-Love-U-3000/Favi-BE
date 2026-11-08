import hashlib, json, os, subprocess, sys, time, zipfile
from pathlib import Path
from datetime import datetime, timezone
ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
OLD=ROOT/'.tools/benchmarks/adaptive-confirmatory-ranking-20261108'
TRACK=ROOT/'document/benchmarks/adaptive-confirmatory-ranking-20261108'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def save(p,v): p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
def now(): return datetime.now(timezone.utc).isoformat()
def lease(status,child=None):
    save(ROOT/'.tools/adaptive-worker.json',dict(updated_utc=now(),owner='adaptive-replay-execution-session',status=status,process_ids=[os.getpid()]+([child] if child else []),process_sessions=[],milestone='six dataset fixed-selection replay',state_file=str(OUT/'batch.json')))
def check_manifest(folder):
    for n,h in read(folder/'manifest.json').items():
        assert (folder/n).parent==folder and sha(folder/n)==h,n
def main():
    if (OUT/'batch.json').exists(): raise RuntimeError('Never overwrite batch evidence')
    state=dict(status='preflight',started_utc=now(),git_commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),preflight=[],datasets=[])
    save(OUT/'batch.json',state); lease('running')
    try:
        original=read(TRACK/'batch.json')
        assert original['status']=='PASS'
        assert sha(OLD/'batch.json')==sha(TRACK/'batch.json')
        inventory=read(TRACK/'source-inventory.json')
        for n,h in inventory.items(): assert sha(ROOT/n)==h,n
        for n in ('run_adaptive_replay.py','test_adaptive_replay.py'):
            name='tools/oasis-smoke/'+n; inventory[name]=sha(ROOT/name)
        inventory[Path(__file__).relative_to(ROOT).as_posix()]=sha(Path(__file__))
        save(OUT/'source-inventory.json',inventory)
        with zipfile.ZipFile(OUT/'frozen-source.zip','x',zipfile.ZIP_DEFLATED) as z:
            for n in inventory: z.write(ROOT/n,n)
        with zipfile.ZipFile(OUT/'frozen-source.zip') as z:
            assert all(hashlib.sha256(z.read(n)).hexdigest()==h for n,h in inventory.items())
        state['source_archive_sha256']=sha(OUT/'frozen-source.zip')
        for record in original['datasets']:
            name=record['dataset']; ranking=OLD/name; dataset=ROOT/'.tools/datasets'/name
            assert sha(ranking/'manifest.json')==record['manifest_sha256']==sha(TRACK/name/'manifest.json')
            assert sha(ranking/'report.json')==record['report_sha256']==sha(TRACK/name/'report.json')
            check_manifest(ranking)
            for n in ('selection.json','source-inventory.json','protocol.json'):
                assert sha(ranking/n)==sha(TRACK/name/n),n
            archive=ROOT/'.tools/dataset-archives'/(name+'.zip'); receipt=read(archive.with_suffix('.zip.json'))
            assert sha(archive)==receipt['sha256']
            files={n.replace('\\','/'):h for n,h in read(dataset/'dataset-manifest.json')['files'].items()}
            files['dataset-manifest.json']=sha(dataset/'dataset-manifest.json')
            with zipfile.ZipFile(archive) as z:
                assert len(z.namelist())==receipt['members']==len(files)
                for n,h in files.items():
                    assert sha(dataset/n)==h,n
                    assert hashlib.sha256(z.read(name+'/'+n)).hexdigest()==h,n
            state['preflight'].append(dict(dataset=name,status='PASS',archive_sha256=receipt['sha256'],members=len(files),ranking_manifest_sha256=sha(ranking/'manifest.json')))
            save(OUT/'batch.json',state); lease('running')
        state['status']='running'
        for entry in state['preflight']:
            name=entry['dataset']; output=OUT/name
            cmd=[sys.executable,'-X','utf8',str(ROOT/'tools/oasis-smoke/run_adaptive_replay.py'),'--dataset',str(ROOT/'.tools/datasets'/name),'--ranking',str(OLD/name),'--output',str(output)]
            record=dict(dataset=name,cli=cmd,status='running',started_utc=now())
            state['datasets'].append(record); save(OUT/'batch.json',state)
            print('START '+name,flush=True); begun=time.perf_counter()
            with (OUT/(name+'.log')).open('x',encoding='utf-8') as log:
                p=subprocess.Popen(cmd,cwd=ROOT,stdout=log,stderr=subprocess.STDOUT)
                record['pid']=p.pid; save(OUT/'batch.json',state)
                while p.poll() is None:
                    lease('running',p.pid); time.sleep(20)
                if p.returncode: raise RuntimeError(f'{name} exit {p.returncode}; batch stopped')
            for n,h in inventory.items(): assert sha(ROOT/n)==h,n
            check_manifest(output); check_manifest(OLD/name)
            record.update(status='PASS',elapsed_seconds=time.perf_counter()-begun,completed_utc=now(),manifest_sha256=sha(output/'manifest.json'),report_sha256=sha(output/'report.json'))
            save(OUT/'batch.json',state); print('PASS '+name,flush=True)
        state['status']='PASS'
    except BaseException as e:
        state.update(status='FAIL',error=repr(e)); raise
    finally:
        state['updated_utc']=now(); save(OUT/'batch.json',state); lease('replay-finished' if state['status']=='PASS' else 'replay-failed')
if __name__=='__main__': main()
