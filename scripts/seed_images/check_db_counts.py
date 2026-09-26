import psycopg2

conn = psycopg2.connect("host=localhost port=5432 dbname=favi user=favi password=favi")
cur = conn.cursor()

tables = ["AspNetUsers", "Profiles", "Follows", "Posts", "PostMedias", "Comments", "Reactions", "Reposts", "Tags", "PostTags", "Stories", "Collections"]
for t in tables:
    try:
        cur.execute(f'SELECT COUNT(*) FROM "{t}"')
        cnt = cur.fetchone()[0]
        print(f"{t}: {cnt}")
    except Exception as e:
        conn.rollback()
        print(f"{t}: Error {e}")

conn.close()
