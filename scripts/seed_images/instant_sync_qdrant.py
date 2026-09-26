import httpx
import psycopg2
from concurrent.futures import ThreadPoolExecutor

def sync():
    print("--- Instant Qdrant Sync ---")
    conn = psycopg2.connect("host=localhost port=5432 dbname=favi user=favi password=favi")
    cur = conn.cursor()
    cur.execute('''
        SELECT p."Id", p."ProfileId", p."Privacy", p."Caption", p."IsNSFW", p."LocationName", pm."Url"
        FROM "Posts" p
        JOIN "PostMedias" pm ON p."Id" = pm."PostId"
    ''')
    rows = cur.fetchall()
    conn.close()

    post_map = {}
    for r in rows:
        pid = str(r[0])
        post_map[pid] = {
            "post_id": pid,
            "owner_id": str(r[1]),
            "privacy": r[2],
            "caption": r[3],
            "is_nsfw": r[4],
            "category": r[5],
            "image_url": r[6],
            "image_urls": [r[6]]
        }

    qdrant_url = "http://localhost:6333"
    collection_name = "posts_demo"

    # Scroll all points
    offset = None
    all_points = []
    with httpx.Client(timeout=10) as client:
        while True:
            res = client.post(
                f"{qdrant_url}/collections/{collection_name}/points/scroll",
                json={"limit": 1000, "offset": offset, "with_payload": True, "with_vector": False}
            ).json()["result"]
            pts = res.get("points", [])
            if not pts:
                break
            all_points.extend(pts)
            offset = res.get("next_page_offset")
            if not offset:
                break

    to_update = []
    for pt in all_points:
        pid = pt.get("payload", {}).get("post_id")
        if pid in post_map:
            updated_info = post_map[pid]
            cur_pl = pt.get("payload", {})
            if cur_pl.get("image_url") != updated_info["image_url"] or cur_pl.get("caption") != updated_info["caption"]:
                merged_pl = dict(cur_pl)
                merged_pl.update(updated_info)
                to_update.append((pt["id"], merged_pl))

    print(f"Total points: {len(all_points)}. Points needing update: {len(to_update)}")

    if not to_update:
        print("All points are already up-to-date!")
        return

    # Update in parallel with ThreadPoolExecutor
    client = httpx.Client(limits=httpx.Limits(max_keepalive_connections=50, max_connections=50), timeout=10)
    def update_point(item):
        pt_id, pl = item
        client.post(
            f"{qdrant_url}/collections/{collection_name}/points/payload",
            json={"payload": pl, "points": [pt_id]}
        )

    with ThreadPoolExecutor(max_workers=30) as executor:
        list(executor.map(update_point, to_update))
    client.close()

    print(f"Successfully updated all {len(to_update)} points in Qdrant!")

if __name__ == "__main__":
    sync()
