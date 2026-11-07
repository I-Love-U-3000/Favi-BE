"""Pure ranking references; candidate retrieval and authorization are separate."""
import math
import uuid


def warm_rank(posts, followed, followers, interactions, now):
    scored = []
    for p in posts:
        affinity = 1 + (1 if p.get('self', False) else 1.5 + (.5 if p['author'] in followers else 0) if p['author'] in followed else 0)
        affinity += min(2, .4 * interactions.get(p['author'], 0))
        quality = math.log10(1 + 1.1 * p['reactions'] + 2 * p['comments'])
        score = (2.5 * affinity + 1.5 * quality + min(1.5, .3 * p['tags'])) * math.exp(-.05 * max(0, now - p['created']))
        scored.append((p, score))
    scored.sort(key=lambda x: -x[1])  # C# stable input ordering, deliberately no ID tie-break.
    counts, adjusted = {}, []
    for p, score in scored:
        n = counts.get(p['author'], 0)
        counts[p['author']] = n + 1
        adjusted.append((p['id'], score * (1 if n == 0 else .65 if n == 1 else .4)))
    adjusted.sort(key=lambda x: -x[1])
    return [i for i, _ in adjusted], {p['id']: score for p, score in scored}


def cold_rank(candidates, topics, limit=300):
    if limit <= 0:
        return []
    pool = sorted((c for c in candidates if uuid.UUID(c['id']).int and math.isfinite(c['score'])),
                  key=lambda c: (-c['score'], uuid.UUID(c['id']).int))
    seen, unique = set(), []
    for c in pool:
        if c['id'] not in seen:
            unique.append(c)
            seen.add(c['id'])
    selected, authors, result = set(), {}, []
    topic_order = list(dict.fromkeys(t.lower() for t in topics))
    for cap in (2, 2147483647):
        queues = [[c for c in unique if t in [x.lower() for x in c['topics']] and c['id'] not in selected] for t in topic_order]
        progress = True
        while progress:
            progress = False
            for queue in queues:
                while queue:
                    c = queue.pop(0)
                    if c['id'] in selected or authors.get(c['author'], 0) >= cap:
                        continue
                    selected.add(c['id'])
                    authors[c['author']] = authors.get(c['author'], 0) + 1
                    result.append(c['id'])
                    progress = True
                    break
                if len(result) >= limit:
                    return result
    for c in unique:
        if len(result) >= limit:
            break
        if c['id'] not in selected:
            selected.add(c['id'])
            result.append(c['id'])
    return result
