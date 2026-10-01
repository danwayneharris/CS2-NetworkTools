"""Read-only live MCP check for the optional NetworkTools provider; no activation."""
import argparse,asyncio,json,os,sys,uuid
from pathlib import Path
from mcp import Client
from mcp.client.stdio import StdioServerParameters

async def run(args):
    output=Path(args.output);output.mkdir(parents=True,exist_ok=False)
    server=Path(args.bridge).resolve()/'adapter/server.py'
    params=StdioServerParameters(command=sys.executable,args=[str(server),'--mailbox',str(Path(os.environ['LOCALAPPDATA'])/'CitiesIIAgentBridge'),'--intents',str(output.resolve()/'intents')])
    async with Client(params) as client:
        listing=await client.list_tools()
        tools=listing.tools if hasattr(listing,'tools') else listing
        tool=next(t for t in tools if t.title=='networktools/state')
        if not tool.annotations.read_only_hint:raise RuntimeError('State not declared read-only')
        status=await client.call_tool('bridge_status',{})
        if status.is_error:raise RuntimeError(status.structured_content)
        token={k:status.structured_content[k] for k in ('session','citySession')}
        token['intent']=uuid.uuid4().hex
        result=await client.call_tool(tool.name,{'_bridge':token})
        (output/'result.json').write_text(json.dumps({'status':status.structured_content,'result':result.structured_content,'error':result.is_error},indent=2))
        if result.is_error:raise RuntimeError(result.structured_content)
        assert result.structured_content['apiVersion']==1
        print('PASS live MCP stdio discovery and read-only NetworkTools state')
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--bridge',default='../cities2-agent-bridge-ndc');p.add_argument('--output',required=True)
    asyncio.run(run(p.parse_args()))
