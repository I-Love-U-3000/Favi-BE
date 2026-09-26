import httpx
import psycopg2

def main():
    conn = psycopg2.connect("host=localhost port=5432 dbname=favi user=favi password=favi")
    cur = conn.cursor()
    cur.execute('SELECT "Id" FROM "Posts"')
    db_posts = set(str(r[0]) for r in cur.fetchall())
    conn.close()

    res = httpx.get("http://localhost:6333/collections/posts_demo")
    info = res.json()["result"]
    print("Qdrant points_count reported:", info.get("points_count"))

    offset = None
    total = 0
    unsplash = 0
    in_db = 0

    while True:
        res = httpx.post(
            "http://localhost:6333/collections/posts_demo/points/scroll",
            json={"limit": 1000, "offset": offset, "with_payload": True, "with_vector": False},
            timeout=10
        ).json()["result"]
        pts = res.get("points", [])
        if not pts:
            break
        total += len(pts)
        for p in pts:
            pl = p.get("payload", {})
            if "images.unsplash.com" in str(pl.get("image_url")):
                unsplash += 1
            if pl.get("post_id") in db_posts:
                in_db += 1
        offset = res.get("next_page_offset")
        if not offset:
            break

    print(f"Scrolled Total: {total}")
    print(f"Unsplash:       {unsplash}")
    print(f"In 5,000 DB:    {in_db}")

if __name__ == "__main__":
    main()
