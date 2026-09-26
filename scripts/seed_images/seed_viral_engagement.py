import random
import uuid
from datetime import datetime, timezone, timedelta
import psycopg2
from psycopg2.extras import execute_values

def main():
    print("--- Seeding Viral Reactions and 1000+ Followers/Followees ---")
    conn = psycopg2.connect("host=localhost port=5432 dbname=favi user=favi password=favi")
    cur = conn.cursor()

    # 1. Fetch all profile IDs and post IDs
    cur.execute('SELECT "Id" FROM "Profiles" ORDER BY "Id"')
    profile_ids = [r[0] for r in cur.fetchall()]
    print(f"Total profiles: {len(profile_ids)}")

    cur.execute('SELECT "Id", "CreatedAt" FROM "Posts" ORDER BY "CreatedAt" DESC')
    post_rows = cur.fetchall()
    post_ids = [r[0] for r in post_rows]
    post_created_map = {r[0]: r[1] for r in post_rows}
    print(f"Total posts: {len(post_ids)}")

    random.seed(42)

    # =========================================================================
    # PART 1: At least 100 posts with 1000+ reactions
    # =========================================================================
    target_viral_posts = 105
    target_reactions_per_post = 1020

    viral_post_ids = post_ids[:target_viral_posts]
    print(f"Selecting {len(viral_post_ids)} posts to reach 1000+ reactions...")

    # Fetch existing reactions on these posts
    cur.execute("""
        SELECT "PostId", "ProfileId"
        FROM "Reactions"
        WHERE "PostId" = ANY(%s::uuid[])
    """, (viral_post_ids,))
    existing_post_reactions = set(cur.fetchall())
    print(f"Existing reactions on these viral posts: {len(existing_post_reactions)}")

    new_reactions = []
    reaction_types = [0, 0, 0, 0, 0, 0, 1, 1, 1, 2, 3, 4, 5] # weighted: 0=Like, 1=Love, 2=Haha, 3=Wow, 4=Sad, 5=Angry

    for pid in viral_post_ids:
        post_created = post_created_map.get(pid, datetime.now(timezone.utc) - timedelta(days=30))
        if post_created.tzinfo is None:
            post_created = post_created.replace(tzinfo=timezone.utc)

        current_reactors = {prof_id for (p, prof_id) in existing_post_reactions if p == pid}
        needed = target_reactions_per_post - len(current_reactors)
        if needed <= 0:
            continue

        available_profiles = [p for p in profile_ids if p not in current_reactors]
        chosen_profiles = random.sample(available_profiles, min(needed, len(available_profiles)))

        now = datetime.now(timezone.utc)
        delta_seconds = max(60, int((now - post_created).total_seconds()))

        for prof in chosen_profiles:
            existing_post_reactions.add((pid, prof))
            rand_sec = random.randint(10, delta_seconds)
            created_at = post_created + timedelta(seconds=rand_sec)
            rx_type = random.choice(reaction_types)
            new_reactions.append((
                str(uuid.uuid4()),
                pid,
                None, # RepostId
                None, # CommentId
                None, # CollectionId
                prof,
                rx_type,
                created_at
            ))

    print(f"Inserting {len(new_reactions)} new reactions...")
    batch_size = 10000
    for i in range(0, len(new_reactions), batch_size):
        chunk = new_reactions[i:i + batch_size]
        execute_values(
            cur,
            """
            INSERT INTO "Reactions" ("Id", "PostId", "RepostId", "CommentId", "CollectionId", "ProfileId", "Type", "CreatedAt")
            VALUES %s
            ON CONFLICT ("PostId", "ProfileId") DO NOTHING
            """,
            chunk
        )
        conn.commit()
        print(f"  Inserted {min(i + batch_size, len(new_reactions))}/{len(new_reactions)} reactions...")

    # =========================================================================
    # PART 2: At least 100 user accounts with 1000+ followers
    # =========================================================================
    target_celebrities = 105
    target_followers_per_user = 1030

    celebrity_profile_ids = profile_ids[:target_celebrities]
    print(f"Selecting {len(celebrity_profile_ids)} celebrity accounts for 1000+ followers...")

    cur.execute("""
        SELECT "FollowerId", "FolloweeId"
        FROM "Follows"
        WHERE "FolloweeId" = ANY(%s::uuid[])
    """, (celebrity_profile_ids,))
    existing_follower_edges = set(cur.fetchall())

    new_follows = []
    base_time = datetime.now(timezone.utc) - timedelta(days=90)

    for celeb_id in celebrity_profile_ids:
        current_followers = {f_id for (f_id, ce_id) in existing_follower_edges if ce_id == celeb_id}
        needed = target_followers_per_user - len(current_followers)
        if needed <= 0:
            continue

        available_followers = [p for p in profile_ids if p != celeb_id and p not in current_followers]
        chosen_followers = random.sample(available_followers, min(needed, len(available_followers)))

        for f_id in chosen_followers:
            existing_follower_edges.add((f_id, celeb_id))
            rand_days = random.randint(1, 89)
            rand_secs = random.randint(0, 86400)
            created_at = base_time + timedelta(days=rand_days, seconds=rand_secs)
            new_follows.append((f_id, celeb_id, created_at))

    print(f"Inserting {len(new_follows)} new follow edges for celebrity followers...")
    for i in range(0, len(new_follows), batch_size):
        chunk = new_follows[i:i + batch_size]
        execute_values(
            cur,
            """
            INSERT INTO "Follows" ("FollowerId", "FolloweeId", "CreatedAt")
            VALUES %s
            ON CONFLICT ("FollowerId", "FolloweeId") DO NOTHING
            """,
            chunk
        )
        conn.commit()
        print(f"  Inserted {min(i + batch_size, len(new_follows))}/{len(new_follows)} follow edges...")

    # =========================================================================
    # PART 3: At least 100 user accounts with 1000+ followees (following)
    # =========================================================================
    target_curators = 105
    target_followees_per_user = 1030

    curator_profile_ids = profile_ids[100:100 + target_curators]
    print(f"Selecting {len(curator_profile_ids)} curator accounts for 1000+ followings...")

    cur.execute("""
        SELECT "FollowerId", "FolloweeId"
        FROM "Follows"
        WHERE "FollowerId" = ANY(%s::uuid[])
    """, (curator_profile_ids,))
    existing_curator_edges = set(cur.fetchall())

    new_curator_follows = []
    for cur_id in curator_profile_ids:
        current_followees = {fe_id for (f_id, fe_id) in existing_curator_edges if f_id == cur_id}
        needed = target_followees_per_user - len(current_followees)
        if needed <= 0:
            continue

        available_followees = [p for p in profile_ids if p != cur_id and p not in current_followees]
        chosen_followees = random.sample(available_followees, min(needed, len(available_followees)))

        for fe_id in chosen_followees:
            existing_curator_edges.add((cur_id, fe_id))
            rand_days = random.randint(1, 89)
            rand_secs = random.randint(0, 86400)
            created_at = base_time + timedelta(days=rand_days, seconds=rand_secs)
            new_curator_follows.append((cur_id, fe_id, created_at))

    print(f"Inserting {len(new_curator_follows)} new follow edges for curator followings...")
    for i in range(0, len(new_curator_follows), batch_size):
        chunk = new_curator_follows[i:i + batch_size]
        execute_values(
            cur,
            """
            INSERT INTO "Follows" ("FollowerId", "FolloweeId", "CreatedAt")
            VALUES %s
            ON CONFLICT ("FollowerId", "FolloweeId") DO NOTHING
            """,
            chunk
        )
        conn.commit()
        print(f"  Inserted {min(i + batch_size, len(new_curator_follows))}/{len(new_curator_follows)} curator follow edges...")

    # =========================================================================
    # PART 4: Verification
    # =========================================================================
    cur.execute("""
        SELECT COUNT(*) FROM (
            SELECT "PostId", COUNT(*) as c
            FROM "Reactions"
            WHERE "PostId" IS NOT NULL
            GROUP BY "PostId"
            HAVING COUNT(*) >= 1000
        ) t
    """)
    posts_1000_rx = cur.fetchone()[0]

    cur.execute("""
        SELECT COUNT(*) FROM (
            SELECT "FolloweeId", COUNT(*) as c
            FROM "Follows"
            GROUP BY "FolloweeId"
            HAVING COUNT(*) >= 1000
        ) t
    """)
    users_1000_followers = cur.fetchone()[0]

    cur.execute("""
        SELECT COUNT(*) FROM (
            SELECT "FollowerId", COUNT(*) as c
            FROM "Follows"
            GROUP BY "FollowerId"
            HAVING COUNT(*) >= 1000
        ) t
    """)
    users_1000_followees = cur.fetchone()[0]

    cur.execute('SELECT COUNT(*) FROM "Reactions"')
    total_rx = cur.fetchone()[0]

    cur.execute('SELECT COUNT(*) FROM "Follows"')
    total_follows = cur.fetchone()[0]

    print("--- VERIFICATION METRICS ---")
    print(f"Posts with >= 1000 reactions:   {posts_1000_rx} (Target: >= 100) -> {'PASS' if posts_1000_rx >= 100 else 'FAIL'}")
    print(f"Users with >= 1000 followers:   {users_1000_followers} (Target: >= 100) -> {'PASS' if users_1000_followers >= 100 else 'FAIL'}")
    print(f"Users with >= 1000 followings:  {users_1000_followees} (Target: >= 100) -> {'PASS' if users_1000_followees >= 100 else 'FAIL'}")
    print(f"Total reactions in DB:          {total_rx}")
    print(f"Total follows in DB:            {total_follows}")

    conn.close()

if __name__ == "__main__":
    main()
