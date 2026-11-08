using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

internal static class AdaptiveGatewayProbe
{
    // Single upstream lab proxy. The production rate and burst are preserved;
    // this does not claim multi-replica failover or SignalR verification.
    public static async Task RunAsync(Uri upstream, AuthenticationHeaderValue authorization, string logs)
    {
        var name="favi-adaptive-gateway-"+Guid.NewGuid().ToString("N");
        var config=Path.GetFullPath(Path.Combine(logs,"adaptive-nginx.conf"));
        await File.WriteAllTextAsync(config,"""
            events { worker_connections 4096; }
            http {
              limit_req_zone $binary_remote_addr zone=api_limit:10m rate=150r/m;
              limit_req_status 429;
              upstream favi_api_cluster { least_conn; server host.docker.internal:UPSTREAM_PORT; keepalive 64; }
              server {
                listen 80;
                location /api/ {
                  limit_req zone=api_limit burst=30 nodelay;
                  proxy_http_version 1.1;
                  proxy_set_header Connection "";
                  proxy_set_header Host $host;
                  proxy_set_header X-Request-ID $request_id;
                  add_header X-Request-ID $request_id always;
                  proxy_pass http://favi_api_cluster;
                }
              }
            }
            """.Replace("UPSTREAM_PORT",upstream.Port.ToString()));
        try
        {
            await Docker("run","--detach","--name",name,"--publish","127.0.0.1::80","--mount",$"type=bind,source={config},target=/etc/nginx/nginx.conf,readonly","nginx:alpine");
            var mapping=(await Docker("port",name,"80/tcp")).Trim();
            using var client=new HttpClient {BaseAddress=new Uri("http://"+mapping),Timeout=TimeSpan.FromSeconds(60)};
            client.DefaultRequestHeaders.Authorization=authorization;
            foreach(var method in new[]{"B1","R"})
            {
                async Task<(double Ms,int Status,bool Safe)> Measure()
                {
                    var timer=Stopwatch.StartNew();using var response=await client.GetAsync($"/api/discovery/experiments/feed?method={method}&pageSize=10");
                    var text=await response.Content.ReadAsStringAsync();var safe=false;
                    if(response.StatusCode==HttpStatusCode.OK)
                    {
                        var body=JsonDocument.Parse(text).RootElement;
                        safe=body.GetProperty("source").GetString()==method&&body.GetProperty("data").EnumerateArray().All(p=>!p.GetProperty("isNSFW").GetBoolean());
                    }
                    return(timer.Elapsed.TotalMilliseconds,(int)response.StatusCode,safe);
                }
                async Task<(double Ms,int Status,bool Safe)[]> Batch(int count)
                {
                    var samples=new(double Ms,int Status,bool Safe)[count];
                    await Parallel.ForEachAsync(Enumerable.Range(0,count),new ParallelOptions{MaxDegreeOfParallelism=10},async(i,ct)=>samples[i]=await Measure());return samples;
                }
                var warmup=await Batch(100);var samples=await Batch(1000);var sorted=samples.Select(s=>s.Ms).Order().ToArray();
                var valid=warmup.Concat(samples).All(s=>s.Status==200&&s.Safe);var p95=sorted[949];
                await File.WriteAllTextAsync(Path.Combine(logs,$"adaptive-gateway-{method}.json"),JsonSerializer.Serialize(new{transport="nginx-single-upstream",method,concurrency=10,warmup=100,measured=1000,ratePerMinute=150,burst=30,p95Ms=p95,valid,gatePassed=valid&&p95<=100,statusCounts=samples.GroupBy(s=>s.Status).ToDictionary(g=>g.Key,g=>g.Count()),samples},new JsonSerializerOptions{WriteIndented=true,IncludeFields=true}));
            }
        }
        catch(Exception error)
        {
            await File.WriteAllTextAsync(Path.Combine(logs,"adaptive-gateway-error.json"),JsonSerializer.Serialize(new{gatePassed=false,error=error.ToString()}));
        }
        finally
        {
            try {await Docker("rm","--force",name);}
            catch(Exception error) {await File.WriteAllTextAsync(Path.Combine(logs,"adaptive-gateway-cleanup-error.txt"),error.ToString());}
        }
    }

    private static async Task<string> Docker(params string[] args)
    {
        var start=new ProcessStartInfo("docker"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(var arg in args)start.ArgumentList.Add(arg);
        using var process=Process.Start(start)??throw new InvalidOperationException("Cannot start docker CLI");
        var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        try {await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(90));}
        catch {if(!process.HasExited)process.Kill(true);throw;}
        var output=await stdout;var error=await stderr;
        if(process.ExitCode!=0)throw new InvalidOperationException($"docker {args[0]}: {error}");return output;
    }
}
