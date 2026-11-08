"""Render preserved native-world evidence without rewriting generated content."""
import argparse
from collections import Counter
import json
from pathlib import Path


def render(root):
    report = json.loads((root / "native-report.json").read_text(encoding="utf-8"))
    raw = json.loads((root / "oasis-all-tables.json").read_text(encoding="utf-8"))
    attempts = json.loads((root / "tool-attempts.json").read_text(encoding="utf-8"))
    follows = {(r["follower_id"], r["followee_id"]) for r in raw["follow"]}
    names = {u["user_id"]: u["name"] for u in raw["user"]}
    posts = {p["post_id"]: p for p in raw["post"]}
    originals = [p for p in raw["post"] if p["original_post_id"] is None]
    metrics = {"original_posts": len(originals),
               "unique_original_post_texts_casefold_trim": len({p["content"].strip().casefold() for p in originals}),
               "duplicate_original_post_rows_casefold_trim": len(originals) - len({p["content"].strip().casefold() for p in originals}),
               "unique_comment_texts_casefold_trim": len({c["content"].strip().casefold() for c in raw["comment"]}),
               "reciprocal_follow_pairs": sum((b, a) in follows for a, b in follows) // 2,
               "comment_authors": len({c["user_id"] for c in raw["comment"]}),
               "commented_posts": len({c["post_id"] for c in raw["comment"]}),
               "comment_author_target_pairs": len({(c["user_id"], posts[c["post_id"]]["user_id"]) for c in raw["comment"]}),
               "attempt_errors": [a for a in attempts if a.get("error")],
               "failure_reasons": dict(Counter(str(a["result"].get("error", a["result"].get("message"))) for a in report["failed_tool_results"]))}
    lines = ["# Native OASIS: complete public world", "", "Raw output is retained verbatim; IDs distinguish duplicate names.", "",
             "```json", json.dumps({k: report[k] for k in ("raw_counts", "calls", "prompt_tokens", "completion_tokens", "model_requested_tools", "duplicate_post_rows_casefold_trim", "duplicate_comment_rows_casefold_trim", "reflection_count")}, ensure_ascii=False, indent=2), "```", "",
             "## Interaction and failure metrics", "", "```json", json.dumps(metrics, ensure_ascii=False, indent=2), "```", "", "## People", ""]
    for user in raw["user"]:
        lines += [f"- ID {user['user_id']}: {names[user['user_id']]} — {user.get('bio', '')}"]
    lines += ["", "## Posts and comments", ""]
    for post in raw["post"]:
        lines += [f"### Post {post['post_id']} · actor {post['user_id']} · tick {post['created_at']} · original {post.get('original_post_id')}", "", post.get("content") or "", ""]
        for comment in raw["comment"]:
            if comment["post_id"] == post["post_id"]:
                lines += [f"Comment {comment['comment_id']} · actor {comment['user_id']}:", "", comment.get("content") or "", ""]
    lines += ["## Groups and messages", "", "```json", json.dumps({t: raw[t] for t in ("chat_group", "group_members", "group_messages")}, ensure_ascii=False, indent=2), "```", "",
              "## Projection boundary", "", "```json", json.dumps(report["favi_projection"], ensure_ascii=False, indent=2), "```", ""]
    (root / "review.md").write_text("\n".join(lines), encoding="utf-8")
    (root / "interaction-metrics.json").write_text(json.dumps(metrics, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(metrics, ensure_ascii=False))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("root", type=Path)
    render(parser.parse_args().root)
