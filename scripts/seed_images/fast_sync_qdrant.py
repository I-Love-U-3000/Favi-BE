import json
import psycopg2
import httpx

def sync_qdrant():
    print("--- Synchronizing Qdrant with 5,000 Posts ---")
    conn = psycopg2.connect("host=localhost port=5432 dbname=favi user=favi password=favi")
    cur = conn.cursor()

    cur.execute('''
        SELECT p."Id", p."ProfileId", p."Privacy", p."Caption", p."IsNSFW", p."LocationName", pm."Url"
        FROM "Posts" p
        JOIN "PostMedias" pm ON p."Id" = pm."PostId"
    ''')
    rows = cur.fetchall()
    conn.close()

    print(f"Loaded {len(rows)} posts from PostgreSQL.")
    post_map = {}
    for r in rows:
        post_id = str(r[0])
        post_map[post_id] = {
            "post_id": post_id,
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

    # 1. Scroll through all existing Qdrant points
    offset = None
    all_points = []
    while True:
        body = {"limit": 1000, "with_payload": True, "with_vector": False}
        if offset:
            body["offset"] = offset
        res = httpx.post(f"{qdrant_url}/collections/{collection_name}/points/scroll", json=body, timeout=10)
        data = res.json().get("result", {})
        pts = data.get("points", [])
        if not pts:
            break
        all_points.extend(pts)
        offset = data.get("next_page_offset")
        if not offset:
            break

    print(f"Total points in Qdrant: {len(all_points)}")

    points_to_delete = []
    points_to_update = []

    for pt in all_points:
        pid = pt.get("payload", {}).get("post_id")
        if not pid or pid not in post_map:
            points_to_delete.append(pt["id"])
        else:
            updated_info = post_map[pid]
            # check if different
            cur_pl = pt.get("payload", {})
            if (cur_pl.get("image_url") != updated_info["image_url"] 
                or cur_pl.get("caption") != updated_info["caption"]):
                merged_pl = dict(cur_pl)
                merged_pl.update(updated_info)
                points_to_update.append((pt["id"], merged_pl))

    print(f"Points to delete (trimmed): {len(points_to_delete)}")
    print(f"Points to update payload:   {len(points_to_update)}")

    if points_to_delete:
        # Delete in chunks
        for i in range(0, len(points_to_delete), 500):
            chunk = points_to_delete[i:i + 500]
            httpx.post(f"{qdrant_url}/collections/{collection_name}/points/delete", json={"points": chunk}, timeout=10)
        print("Deleted trimmed points from Qdrant.")

    if points_to_update:
        # Update payloads in parallel batches
        batch_size = 50
        for i in range(0, len(points_to_update), batch_size):
            chunk = points_to_update[i:i + batch_size]
            for pt_id, pl in chunk:
                httpx.post(
                    f"{qdrant_url}/collections/{collection_name}/points/payload",
                    json={"payload": pl, "points": [pt_id]},
                    timeout=5
                )
            if i % 500 == 0:
                print(f"  Updated {i}/{len(points_to_update)} points...")

        print(f"Successfully updated all {len(points_to_update)} points in Qdrant!")

    # Verify final count in Qdrant
    res = httpx.get(f"{qdrant_url}/collections/{collection_name}", timeout=5)
    print("Final Qdrant collection info:", res.json()["result"]["points_count"], "points.")

if __name__ == "__main__":
    sync_qdrant()
