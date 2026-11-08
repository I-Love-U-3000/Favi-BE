"""Compare Python references to source-linked/extracted C# ranking on fixed fixtures."""
import argparse
import hashlib
import json
import math
from pathlib import Path
import random
import subprocess
import uuid
from xml.sax.saxutils import escape
from favi_rankers import warm_rank, cold_rank


def guid(n):
    return str(uuid.UUID(int=n))


def fixtures():
    rng = random.Random(20261107)
    warm = []
    for case in range(20):
        posts = [dict(id=guid(i + 1), author=guid(rng.randrange(1, 7)), reactions=rng.randrange(40),
                      comments=rng.randrange(12), tags=rng.choice([0, 1, 5, 8]), created=rng.choice([-24, -1, 0, 1])) for i in range(25)]
        if case == 0:
            posts = [dict(id=guid(i), author=guid(2), reactions=0, comments=0, tags=0, created=0) for i in (9, 8, 7, 6)]
        viewer = guid(1)
        for post in posts:
            post['self'] = post['author'] == viewer
        warm.append(dict(name=f'warm-{case}', viewer=viewer, posts=posts,
                         followed=[guid(2), guid(3)], followers=[guid(2)],
                         interactions={guid(2): 2, guid(3): 9}, now=0))
    cold = []
    for case in range(25):
        candidates = [dict(id=guid(i+1), author=guid(rng.randrange(1, 6)), score=rng.choice([0, 1, 2]),
                           topics=rng.choice([[], ['food'], ['music'], ['food', 'music']])) for i in range(20)]
        candidates += [dict(candidates[0], score=10), dict(id=guid(0), author=guid(1), score=100, topics=['food'])]
        cold.append(dict(name=f'cold-{case}', candidates=candidates, topics=rng.choice([[], ['FOOD', 'food', 'music'], ['absent']]),
                         limit=rng.choice([0, 1, 10, 300])))
    return dict(warm=warm, cold=cold)


CS = r'''
using System.Text.Json;
using Favi_BE.Modules.ContentDiscovery.Application.ColdStart;
var root = JsonDocument.Parse(File.ReadAllText(args[0])).RootElement;
var warm = root.GetProperty("warm").EnumerateArray().Select(RunWarm).ToArray();
var cold = root.GetProperty("cold").EnumerateArray().Select(t => new {
    name=t.GetProperty("name").GetString(),
    ids=ColdStartRanker.Rank(t.GetProperty("candidates").EnumerateArray().Select(c => new ColdStartCandidate(
        c.GetProperty("id").GetGuid(), c.GetProperty("author").GetGuid(), c.GetProperty("score").GetDouble(),
        c.GetProperty("topics").EnumerateArray().Select(x=>x.GetString()!).ToArray())),
        t.GetProperty("topics").EnumerateArray().Select(x=>x.GetString()!).ToArray(), t.GetProperty("limit").GetInt32())
}).ToArray();
Console.WriteLine(JsonSerializer.Serialize(new {warm,cold}));
static object RunWarm(JsonElement t) {
    var now = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);
    var viewer = t.GetProperty("viewer").GetGuid();
    var followedIds=t.GetProperty("followed").EnumerateArray().Select(x=>x.GetGuid()).ToHashSet();
    var followerIds=t.GetProperty("followers").EnumerateArray().Select(x=>x.GetGuid()).ToHashSet();
    var authorInteractionCounts=t.GetProperty("interactions").EnumerateObject().ToDictionary(x=>Guid.Parse(x.Name),x=>x.Value.GetInt32());
    var posts=t.GetProperty("posts").EnumerateArray().Select(p=>new FeedCandidateDto(
        p.GetProperty("id").GetGuid(),p.GetProperty("author").GetGuid(),p.GetProperty("reactions").GetInt32(),
        p.GetProperty("comments").GetInt32(),p.GetProperty("tags").GetInt32(),now.AddHours(p.GetProperty("created").GetDouble())));
    var scoredPosts=new List<(FeedCandidateDto Post,double Score)>();
    foreach(var post in posts) {
        var authorId=post.ProfileId;
        var isSelf=authorId==viewer;
        var isFollowed=followedIds.Contains(authorId);
        var isMutual=followerIds.Contains(authorId);
        /*WARM_SOURCE*/
    /*SORT_SOURCE*/
    return new {name=t.GetProperty("name").GetString(), ids=finalRanked.Select(p=>p.Id).ToArray(),
        scores=scoredPosts.ToDictionary(x=>x.Post.Id.ToString(),x=>x.Score)};
}
record FeedCandidateDto(Guid Id,Guid ProfileId,int ReactionsCount,int CommentsCount,int TagCount,DateTime CreatedAt);
'''


def run(output):
    solution = Path(__file__).resolve().parents[2]
    if output.exists():
        raise ValueError('Use a new output directory')
    output.mkdir(parents=True)
    adapter = solution / 'Favi-BE.API/Application/ContentDiscovery/ContentDiscoveryQueryReaderAdapter.cs'
    cold = solution / 'Favi-BE.Modules.ContentDiscovery/Application/ColdStart/ColdStartRanker.cs'
    source = adapter.read_text(encoding='utf-8-sig')
    # Fail closed if these source boundaries change. No hand-copied scoring in C#.
    start = source.index('// A. Social Affinity Score')
    sort = source.index('// Sort and apply Diversity', start)
    end = source.index('        var total = finalRanked.Count;', sort)
    scoring, sorting = source[start:sort], source[sort:end]
    if scoring.count('scoredPosts.Add') != 1 or sorting.count('.OrderByDescending') != 2:
        raise ValueError('Unexpected production ranking structure')
    (output / 'Program.cs').write_text(CS.replace('/*WARM_SOURCE*/', scoring).replace('/*SORT_SOURCE*/', sorting), encoding='utf-8')
    (output / 'Parity.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><Compile Include="' + escape(str(cold)) + '" Link="ColdStartRanker.cs" /></ItemGroup></Project>', encoding='utf-8')
    data = fixtures()
    fixture_path = output / 'fixtures.json'
    fixture_path.write_text(json.dumps(data, indent=2), encoding='utf-8')
    proc = subprocess.run(['dotnet', 'run', '--project', str(output / 'Parity.csproj'), '--', str(fixture_path)],
                          text=True, capture_output=True, timeout=180)
    (output / 'dotnet.stdout.txt').write_text(proc.stdout, encoding='utf-8')
    (output / 'dotnet.stderr.txt').write_text(proc.stderr, encoding='utf-8')
    if proc.returncode:
        raise RuntimeError(f'.NET parity failed; see {output}')
    actual = json.loads(proc.stdout.strip().splitlines()[-1])
    checks = 0
    for test, got in zip(data['warm'], actual['warm'], strict=True):
        ids, scores = warm_rank(test['posts'], set(test['followed']), set(test['followers']), test['interactions'], test['now'])
        if got['name'] != test['name'] or got['ids'] != ids:
            raise ValueError('Warm rank mismatch: ' + test['name'])
        for item, expected in scores.items():
            if not math.isclose(got['scores'][item], expected, rel_tol=1e-12, abs_tol=1e-12):
                raise ValueError('Warm score mismatch')
            checks += 1
        checks += 1
    for test, got in zip(data['cold'], actual['cold'], strict=True):
        if got['name'] != test['name'] or got['ids'] != cold_rank(test['candidates'], test['topics'], test['limit']):
            raise ValueError('Cold rank mismatch: ' + test['name'])
        checks += 1
    report = dict(passed=True, checks=checks, warm_fixtures=len(data['warm']), cold_fixtures=len(data['cold']),
                  sources={str(p.relative_to(solution)): hashlib.sha256(p.read_bytes()).hexdigest() for p in (adapter,cold,Path(__file__),Path(__file__).with_name('favi_rankers.py'))},
                  scope='Source-extracted warm scoring and linked cold ranker; excludes SQL retrieval, authorization, hydration, routing, reaction history construction and existing legacy benchmark input contract')
    (output / 'report.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(report))


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output',type=Path,required=True)
    run(parser.parse_args().output.resolve())
