import psycopg2

conn = psycopg2.connect("host=localhost port=5432 dbname=favi user=favi password=favi")
cur = conn.cursor()

cur.execute("""
    SELECT COUNT(*) FROM (
        SELECT "PostId", COUNT(*) as c
        FROM "Reactions"
        WHERE "PostId" IS NOT NULL
        GROUP BY "PostId"
        HAVING COUNT(*) >= 1000
    ) t
""")
print("Posts with >= 1000 reactions:", cur.fetchone()[0])

cur.execute("""
    SELECT COUNT(*) FROM (
        SELECT "FolloweeId", COUNT(*) as c
        FROM "Follows"
        GROUP BY "FolloweeId"
        HAVING COUNT(*) >= 1000
    ) t
""")
print("Users with >= 1000 followers:", cur.fetchone()[0])

cur.execute("""
    SELECT COUNT(*) FROM (
        SELECT "FollowerId", COUNT(*) as c
        FROM "Follows"
        GROUP BY "FollowerId"
        HAVING COUNT(*) >= 1000
    ) t
""")
print("Users with >= 1000 followings:", cur.fetchone()[0])

cur.execute('SELECT COUNT(*) FROM "Reactions"')
print("Total reactions in DB:", cur.fetchone()[0])

cur.execute('SELECT COUNT(*) FROM "Follows"')
print("Total follows in DB:", cur.fetchone()[0])

# Top reaction count on any post
cur.execute("""
    SELECT "PostId", COUNT(*) as c
    FROM "Reactions"
    WHERE "PostId" IS NOT NULL
    GROUP BY "PostId"
    ORDER BY c DESC
    LIMIT 5
""")
print("Top 5 posts by reactions:", cur.fetchall())

# Top follower count
cur.execute("""
    SELECT "FolloweeId", COUNT(*) as c
    FROM "Follows"
    GROUP BY "FolloweeId"
    ORDER BY c DESC
    LIMIT 5
""")
print("Top 5 users by followers:", cur.fetchall())

conn.close()
