"""Render the complete generated conversation and measured role activity."""
from collections import Counter
import json
from pathlib import Path
import sys
from community_policy import gini


def write_review(root):
    report = json.loads((root / "community-report.json").read_text(encoding="utf-8"))
    snapshot = json.loads((root / "export" / "snapshot.json").read_text(encoding="utf-8"))
    events = json.loads((root / "events.json").read_text(encoding="utf-8"))
    people = {p["id"]: p for p in report["initial_population"]}
    users = {p["id"]: p["source_id"] for p in snapshot["users"]}
    actions = [event for event in events if "action" in event and not event["bootstrap"]]
    per_user = Counter(event["agent"] for event in actions)
    lines = ["# Kết quả cộng đồng nhỏ — dữ liệu thực của lượt chạy", "",
             f"Mode: {report['mode']}", "",
             f"100 users; {report['ticks']} ticks; counts: {report['counts']}",
             f"LLM: {report['llm_calls']} calls, {report['prompt_tokens']} input + {report['completion_tokens']} output tokens.",
             f"Rejected generated content: {len(report['rejected_content'])}; no retries or rewriting.",
             f"Non-bootstrap mutation Gini: {gini([per_user[i] for i in people]):.4f}", "",
             "200 initial follows and 3 opening posts are scenario bootstrap, not emergent results.", "",
             "| Role | People | Activated turns | Later mutations |", "|---|---:|---:|---:|"]
    for role in ("reader", "contributor", "creator"):
        ids = {i for i, p in people.items() if p["role"] == role}
        activations = sum("decision" in e and e["agent"] in ids for e in events)
        mutations = sum(e["agent"] in ids for e in actions)
        lines.append(f"| {role} | {len(ids)} | {activations} | {mutations} |")
    lines += ["", "## Toàn bộ bài và bình luận nguyên bản", "",
              "Nội dung là synthetic; các khẳng định trong bài không phải dữ kiện đã fact-check.", ""]
    for post in snapshot["posts"]:
        author = users[post["user_id"]]
        likes = sum(like["post_id"] == post["id"] for like in snapshot["likes"])
        lines += [f"### Post {post['source_id']} — Member {author:03d} — {likes} likes", "", post["content"], ""]
        comments = [c for c in snapshot["comments"] if c["post_id"] == post["id"]]
        for comment in comments:
            member = users[comment["user_id"]]
            lines += [f"**Member {member:03d} — {people[member]['lens']}:**", "", comment["content"], ""]
        if not comments:
            lines += ["Chưa có bình luận.", ""]
    lines += ["## Giới hạn", ""] + [f"- {item}" for item in report["limitations"]]
    output = root / "review.md"
    output.write_text("\n".join(lines) + "\n", encoding="utf-8")
    return output


if __name__ == "__main__":
    print(write_review(Path(sys.argv[1]).resolve()))
