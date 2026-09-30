"""Explicit, phased history migration for Dan's two forks; never checks out files.

archive: preserve remote main and PR histories before changing references.
merge-open: squash the reviewed NT stack through GitHub, restacking exact trees.
rebuild: construct and verify local single-parent history (no published rewrite).
publish: push verified main candidates with explicit expected-SHA leases.
A failed/uncertain operation stops; inspect remote state before retrying.
"""
import argparse,json,os,pathlib,subprocess,urllib.request
P=argparse.ArgumentParser();P.add_argument('phase',choices=['archive','merge-open','rebuild','publish']);P.add_argument('--parent',required=True);P.add_argument('--git',required=True);P.add_argument('--record',required=True);a=P.parse_args()
root=pathlib.Path(a.parent);out=pathlib.Path(a.record);out.mkdir(parents=True,exist_ok=True)
repos=['CS2-NetworkTools','cities2-agent-bridge-ndc'];owner='danwayneharris'
def git(repo,*args,input=None,env=None):
 p=subprocess.run([a.git,'-C',str(root/repo),*args],input=input,env=env,capture_output=True,text=True,encoding='utf-8')
 if p.returncode:raise RuntimeError(p.stderr)
 return p.stdout.strip()
def api(path,data=None,method=None):
 p=subprocess.run([a.git,'credential','fill'],input='protocol=https\nhost=github.com\n\n',capture_output=True,text=True,check=True)
 c=dict(l.split('=',1) for l in p.stdout.splitlines() if '=' in l)
 req=urllib.request.Request('https://api.github.com/'+path,data=json.dumps(data).encode() if data is not None else None,headers={'Authorization':'Bearer '+c['password'],'Accept':'application/vnd.github+json','User-Agent':'NetworkTools-history-migration'},method=method)
 with urllib.request.urlopen(req,timeout=60) as r:return json.load(r)
def endpoint(repo):return f'repos/{owner}/{repo}'
def save(name,data):(out/(name+'.json')).write_text(json.dumps(data,indent=2,ensure_ascii=False),encoding='utf-8')
def load(name):return json.loads((out/(name+'.json')).read_text(encoding='utf-8'))
def remote(repo,ref):
 s=git(repo,'ls-remote','origin',ref)
 return s.split()[0] if s else None
def tree(repo,sha):return git(repo,'rev-parse',sha+'^{tree}')
def tag(repo,name,sha):
 ref='refs/tags/'+name
 exists=git(repo,'tag','--list',name)
 if exists:
  if git(repo,'rev-parse',ref+'^{}')!=sha:raise RuntimeError('Archive tag collision')
 else:git(repo,'tag','-a',name,sha,'-m','Original history preserved before authorized squash-history migration, 2026-09-29.')
 git(repo,'push','origin',ref)
 if remote(repo,ref+'^{}')!=sha:raise RuntimeError('Remote archive verification failed')
def details(repo):
 result=[]
 for page in range(1,100):
  rows=api(endpoint(repo)+f'/pulls?state=all&per_page=100&page={page}')
  if not rows:break
  for row in rows:
   if row['user']['login']==owner:result.append(api(endpoint(repo)+f"/pulls/{row['number']}"))
 return result
def commit(repo,old,parent,message):
 env=os.environ.copy()
 for field,fmt in [('NAME','%an'),('EMAIL','%ae'),('DATE','%aI')]:env['GIT_AUTHOR_'+field]=git(repo,'show','-s','--format='+fmt,old)
 new=git(repo,'commit-tree',tree(repo,old),'-p',parent,input=message.rstrip()+'\n',env=env)
 if tree(repo,new)!=tree(repo,old):raise RuntimeError('Tree mismatch')
 return new
def message(pr):return pr['title']+f" (#{pr['number']})\n\n"+(pr['body'] or '')
if a.phase=='archive':
 for repo in repos:
  git(repo,'fetch','origin');tip=remote(repo,'refs/heads/main');prs=details(repo)
  save(repo+'-before',{'main':tip,'prs':prs})
  tag(repo,'archive/main-before-squash-20260929',tip)
  for pr in prs:
   if pr['merged_at']:
    tag(repo,f"archive/pr-{pr['number']}-original-merge",pr['merge_commit_sha'])
   elif pr['state']=='open':tag(repo,f"archive/pr-{pr['number']}-head-before-squash",pr['head']['sha'])
  print(repo,'archives verified',flush=True)
elif a.phase=='merge-open':
 repo=repos[0];state=load(repo+'-before');expected=state['main'];results=[]
 for pr in sorted((p for p in state['prs'] if p['state']=='open'),key=lambda p:p['number']):
  n=pr['number'];head=pr['head']['ref'];old=pr['head']['sha']
  if remote(repo,'refs/heads/main')!=expected:raise RuntimeError('Main changed unexpectedly')
  if remote(repo,'refs/heads/'+head)!=old:raise RuntimeError('PR branch changed unexpectedly')
  new=commit(repo,old,expected,message(pr))
  git(repo,'push',f'--force-with-lease=refs/heads/{head}:{old}','origin',new+':refs/heads/'+head)
  api(endpoint(repo)+f'/pulls/{n}',{'base':'main'},'PATCH')
  result=api(endpoint(repo)+f'/pulls/{n}/merge',{'merge_method':'squash','sha':new,'commit_title':pr['title']+f' (#{n})','commit_message':pr['body'] or ''},'PUT')
  save(repo+f'-merge-{n}',result)
  if not result.get('merged'):raise RuntimeError('Merge not confirmed')
  git(repo,'fetch','origin');expected=result['sha']
  if tree(repo,expected)!=tree(repo,old):raise RuntimeError('Merged PR tree differs from approved snapshot')
  tag(repo,f'archive/pr-{n}-github-squash',expected)
  results.append({'pr':n,'original':old,'restacked':new,'merged':expected,'tree':tree(repo,old)})
  save(repo+'-merged-open',results);print('Squash merged',repo,n,expected,flush=True)
elif a.phase=='rebuild':
 for repo in repos:
  git(repo,'fetch','origin');tip=remote(repo,'refs/heads/main');prs=details(repo);save(repo+'-after-merges',{'main':tip,'prs':prs})
  tag(repo,'archive/main-before-rewrite-20260929',tip)
  mapped={p['merge_commit_sha']:p for p in prs if p['merged_at'] and p['base']['ref']=='main'}
  chain=git(repo,'rev-list','--first-parent','--reverse',tip).splitlines();start=next(i for i,h in enumerate(chain) if h in mapped)
  parent=git(repo,'rev-parse',chain[start]+'^1');base=parent;rows=[]
  for old in chain[start:]:
   if old not in mapped:raise RuntimeError('Unexpected non-PR commit: '+old)
   pr=mapped[old];msg=message(pr)
   nested=[]
   for q in prs:
    if q['merged_at'] and q['base']['ref']!='main':
     sha=q['merge_commit_sha'];anc=subprocess.run([a.git,'-C',str(root/repo),'merge-base','--is-ancestor',sha,old]).returncode==0
     prev=subprocess.run([a.git,'-C',str(root/repo),'merge-base','--is-ancestor',sha,old+'^1']).returncode==0
     if anc and not prev:nested.append(q);msg+='\n\n---\nIncluded stacked PR: '+message(q)
   new=commit(repo,old,parent,msg);rows.append({'pr':pr['number'],'old':old,'new':new,'tree':tree(repo,new),'included_prs':[q['number'] for q in nested]});parent=new
  if tree(repo,parent)!=tree(repo,tip):raise RuntimeError('Final tree mismatch')
  git(repo,'update-ref','refs/heads/review/squashed-main-20260929',parent)
  save(repo+'-rewrite',{'old_main':tip,'new_main':parent,'unchanged_base':base,'commits':rows,'tree_equal':True})
  print(repo,json.dumps({'old':tip,'new':parent,'verified_checkpoints':len(rows),'tree_equal':True}),flush=True)
elif a.phase=='publish':
 # Verify both plans before updating either remote.
 for repo in repos:
  plan=load(repo+'-rewrite')
  if remote(repo,'refs/heads/main')!=plan['old_main']:raise RuntimeError('Remote main moved; abort')
  if tree(repo,plan['new_main'])!=tree(repo,plan['old_main']):raise RuntimeError('Tree mismatch')
  if remote(repo,'refs/tags/archive/main-before-rewrite-20260929^{}')!=plan['old_main']:raise RuntimeError('Remote backup missing')
 for repo in repos:
  plan=load(repo+'-rewrite');old=plan['old_main'];new=plan['new_main']
  git(repo,'push',f'--force-with-lease=refs/heads/main:{old}','origin',new+':refs/heads/main')
  if remote(repo,'refs/heads/main')!=new:raise RuntimeError('Published head mismatch')
  git(repo,'fetch','origin')
  # Update local main only if no worktree has it checked out.
  if 'branch refs/heads/main' in git(repo,'worktree','list','--porcelain'):raise RuntimeError('Local main checked out; inspect before aligning')
  local=git(repo,'rev-parse','refs/heads/main');git(repo,'update-ref','refs/heads/main',new,local)
  git(repo,'branch','--set-upstream-to=origin/main','main')
  save(repo+'-published',{'old':old,'new':new,'verified_remote':True,'local_main_aligned':True})
  print(repo,'published and verified',new,flush=True)
