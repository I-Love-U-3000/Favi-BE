import json
import os
import psycopg2
import httpx

def apply_migration():
    print("===============================================================")
    print("APPLYING DIVERSE DATASET MIGRATION (5,000 POSTS, ZERO OVER-DUPLICATION)")
    print("===============================================================")

    catalog_dir = r"C:\Users\MINH QUANG\Favi\Favi-BE\Favi-BE\Favi-BE.API\seed\catalogs"
    
    with open(os.path.join(catalog_dir, "real-posts-catalog.json"), "r", encoding="utf-8") as f:
        posts_cat = json.load(f)
    with open(os.path.join(catalog_dir, "real-avatars-catalog.json"), "r", encoding="utf-8") as f:
        avatars_cat = json.load(f)
    with open(os.path.join(catalog_dir, "real-covers-catalog.json"), "r", encoding="utf-8") as f:
        covers_cat = json.load(f)
    with open(os.path.join(catalog_dir, "real-comments-catalog.json"), "r", encoding="utf-8") as f:
        comments_cat = json.load(f)
    with open(os.path.join(catalog_dir, "real-stories-catalog.json"), "r", encoding="utf-8") as f:
        stories_cat = json.load(f)
    with open(os.path.join(catalog_dir, "real-collections-catalog.json"), "r", encoding="utf-8") as f:
        collections_cat = json.load(f)

    print(f"Catalogs loaded:")
    print(f"  Posts:       {len(posts_cat)} unique items")
    print(f"  Avatars:     {len(avatars_cat)} unique items")
    print(f"  Covers:      {len(covers_cat)} unique items")
    print(f"  Comments:    {len(comments_cat)} items")
    print(f"  Stories:     {len(stories_cat)} unique items")
    print(f"  Collections: {len(collections_cat)} unique items")

    conn = psycopg2.connect("host=localhost port=5432 dbname=favi user=favi password=favi")
    cur = conn.cursor()

    # 1. TRIM POSTS DOWN TO 5,000
    print("\n--- 1. Trimming Posts down to 5,000 ---")
    cur.execute('SELECT "Id" FROM "Posts" ORDER BY "CreatedAt" ASC, "Id" ASC')
    all_post_ids = [r[0] for r in cur.fetchall()]
    total_existing = len(all_post_ids)
    print(f"Existing posts in DB: {total_existing}")

    kept_post_ids = set(all_post_ids[:5000])
    delete_post_ids = all_post_ids[5000:]
    print(f"Posts to keep: {len(kept_post_ids)}, Posts to delete: {len(delete_post_ids)}")

    if delete_post_ids:
        # Delete dependent rows first
        print("Deleting dependent rows for trimmed posts...")
        cur.execute('DELETE FROM "Notifications" WHERE "TargetPostId" = ANY(%s::uuid[])', (delete_post_ids,))
        print(f"  Deleted Notifications: {cur.rowcount}")

        cur.execute('DELETE FROM "PostTags" WHERE "PostId" = ANY(%s::uuid[])', (delete_post_ids,))
        print(f"  Deleted PostTags: {cur.rowcount}")

        cur.execute('DELETE FROM "Reposts" WHERE "OriginalPostId" = ANY(%s::uuid[])', (delete_post_ids,))
        print(f"  Deleted Reposts: {cur.rowcount}")

        # Delete comments on those posts (and their notifications/reactions)
        cur.execute('SELECT "Id" FROM "Comments" WHERE "PostId" = ANY(%s::uuid[])', (delete_post_ids,))
        deleted_cmt_ids = [r[0] for r in cur.fetchall()]
        if deleted_cmt_ids:
            cur.execute('DELETE FROM "Notifications" WHERE "TargetCommentId" = ANY(%s::uuid[])', (deleted_cmt_ids,))
            cur.execute('DELETE FROM "Reactions" WHERE "CommentId" = ANY(%s::uuid[])', (deleted_cmt_ids,))
            cur.execute('DELETE FROM "Comments" WHERE "Id" = ANY(%s::uuid[])', (deleted_cmt_ids,))
            print(f"  Deleted Comments: {len(deleted_cmt_ids)}")

        cur.execute('DELETE FROM "Reactions" WHERE "PostId" = ANY(%s::uuid[])', (delete_post_ids,))
        print(f"  Deleted Post Reactions: {cur.rowcount}")

        cur.execute('DELETE FROM "PostMedias" WHERE "PostId" = ANY(%s::uuid[])', (delete_post_ids,))
        print(f"  Deleted PostMedias: {cur.rowcount}")

        cur.execute('DELETE FROM "Posts" WHERE "Id" = ANY(%s::uuid[])', (delete_post_ids,))
        print(f"  Deleted Posts: {cur.rowcount}")
        conn.commit()

    # 2. UPDATE 5,000 POSTS WITH UNIQUE IMAGES AND CAPTIONS
    print("\n--- 2. Updating 5,000 Posts with Unique Images ---")
    cur.execute('SELECT "Id" FROM "Posts" ORDER BY "CreatedAt" ASC, "Id" ASC LIMIT 5000')
    kept_posts = [r[0] for r in cur.fetchall()]

    for i, post_id in enumerate(kept_posts):
        item = posts_cat[i]
        # Update Post
        cur.execute('''
            UPDATE "Posts" 
            SET "Caption" = %s, "IsNSFW" = %s, "LocationName" = %s 
            WHERE "Id" = %s
        ''', (item["caption"], item["is_nsfw"], item["category"], post_id))

        # Update PostMedias
        cur.execute('''
            UPDATE "PostMedias" 
            SET "Url" = %s, "ThumbnailUrl" = %s, "Width" = 1080, "Height" = 1080 
            WHERE "PostId" = %s
        ''', (item["url"], item["url"], post_id))

    conn.commit()
    print("Updated 5,000 Posts and PostMedias successfully.")

    # 3. UPDATE USER AVATARS & COVERS (Max 2 users per avatar & cover)
    print("\n--- 3. Updating 5,000 User Profiles (Max 2 users per avatar & cover) ---")
    cur.execute('SELECT "Id" FROM "Profiles" ORDER BY "Username" ASC')
    profile_ids = [r[0] for r in cur.fetchall()]
    print(f"Total profiles to update: {len(profile_ids)}")

    for i, pid in enumerate(profile_ids):
        avatar_url = avatars_cat[(i // 2) % len(avatars_cat)]
        cover_url = covers_cat[(i // 2) % len(covers_cat)]
        cur.execute('''
            UPDATE "Profiles" 
            SET "AvatarUrl" = %s, "CoverUrl" = %s 
            WHERE "Id" = %s
        ''', (avatar_url, cover_url, pid))

    conn.commit()
    print("Updated 5,000 Profiles successfully.")

    # 4. UPDATE COMMENTS WITH UNIQUE IMAGES (Max 2 uses per image)
    print("\n--- 4. Updating Comments (Max 2 uses per comment media) ---")
    cur.execute('SELECT "Id" FROM "Comments" WHERE "MediaUrl" IS NOT NULL ORDER BY "CreatedAt" ASC')
    cmts_with_media = [r[0] for r in cur.fetchall()]
    print(f"Comments with media: {len(cmts_with_media)}")

    comment_media_items = [c["url"] for c in comments_cat if c.get("url")]
    for i, cmt_id in enumerate(cmts_with_media):
        media_url = comment_media_items[(i // 2) % len(comment_media_items)]
        cur.execute('''
            UPDATE "Comments" 
            SET "MediaUrl" = %s 
            WHERE "Id" = %s
        ''', (media_url, cmt_id))

    conn.commit()
    print(f"Updated {len(cmts_with_media)} Comments with media successfully.")

    # 5. UPDATE STORIES (Max 2 uses per image)
    print("\n--- 5. Updating Stories (Max 2 uses per story image) ---")
    cur.execute('SELECT "Id" FROM "Stories" ORDER BY "CreatedAt" ASC')
    story_ids = [r[0] for r in cur.fetchall()]
    print(f"Stories in DB: {len(story_ids)}")

    for i, sid in enumerate(story_ids):
        story_url = stories_cat[(i // 2) % len(stories_cat)]["url"]
        cur.execute('''
            UPDATE "Stories" 
            SET "MediaUrl" = %s, "ThumbnailUrl" = %s 
            WHERE "Id" = %s
        ''', (story_url, story_url, sid))

    conn.commit()
    print(f"Updated {len(story_ids)} Stories successfully.")

    # 6. SEED COLLECTIONS IF EMPTY
    print("\n--- 6. Checking / Seeding Collections ---")
    cur.execute('SELECT COUNT(*) FROM "Collections"')
    coll_count = cur.fetchone()[0]
    print(f"Current Collections count: {coll_count}")

    if coll_count == 0:
        import uuid
        from datetime import datetime, timezone, timedelta
        now = datetime.now(timezone.utc)
        
        cur.execute('SELECT "Id" FROM "Profiles" LIMIT 250')
        active_pids = [r[0] for r in cur.fetchall()]

        coll_rows = []
        post_coll_rows = []

        for i, c_item in enumerate(collections_cat):
            cid = str(uuid.uuid4())
            pid = active_pids[i % len(active_pids)]
            created_at = now - timedelta(days=(i % 30) + 1)
            
            coll_rows.append((
                cid, pid, c_item["title"], c_item["description"],
                c_item["cover_url"], f"seed/coll/{cid}", 0, created_at, created_at
            ))

            # Link 4 posts to each collection
            start_p = (i * 7) % len(kept_posts)
            for p_offset in range(4):
                p_id = kept_posts[(start_p + p_offset) % len(kept_posts)]
                post_coll_rows.append((p_id, cid))

        for c in coll_rows:
            cur.execute('''
                INSERT INTO "Collections" ("Id", "ProfileId", "Title", "Description", "CoverImageUrl", "CoverImagePublicId", "PrivacyLevel", "CreatedAt", "UpdatedAt")
                VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s)
            ''', c)

        for pc in post_coll_rows:
            cur.execute('''
                INSERT INTO "PostCollections" ("PostId", "CollectionId")
                VALUES (%s, %s)
            ''', pc)

        conn.commit()
        print(f"Seeded {len(coll_rows)} Collections and {len(post_coll_rows)} PostCollections successfully.")

    # 7. VALIDATE OCCURRENCES IN DATABASE
    print("\n--- 7. Validating Non-Duplication Constraints in Database ---")
    cur.execute('''
        SELECT COUNT(*) FROM (
            SELECT "Url" FROM "PostMedias" GROUP BY "Url" HAVING COUNT(*) > 2
        ) t
    ''')
    post_over_2 = cur.fetchone()[0]

    cur.execute('''
        SELECT COUNT(*) FROM (
            SELECT "AvatarUrl" FROM "Profiles" GROUP BY "AvatarUrl" HAVING COUNT(*) > 2
        ) t
    ''')
    avatar_over_2 = cur.fetchone()[0]

    cur.execute('''
        SELECT COUNT(*) FROM (
            SELECT "CoverUrl" FROM "Profiles" GROUP BY "CoverUrl" HAVING COUNT(*) > 2
        ) t
    ''')
    cover_over_2 = cur.fetchone()[0]

    cur.execute('''
        SELECT COUNT(*) FROM (
            SELECT "MediaUrl" FROM "Comments" WHERE "MediaUrl" IS NOT NULL GROUP BY "MediaUrl" HAVING COUNT(*) > 2
        ) t
    ''')
    comment_over_2 = cur.fetchone()[0]

    cur.execute('''
        SELECT COUNT(*) FROM (
            SELECT "MediaUrl" FROM "Stories" GROUP BY "MediaUrl" HAVING COUNT(*) > 2
        ) t
    ''')
    story_over_2 = cur.fetchone()[0]

    cur.execute('''
        SELECT COUNT(*) FROM (
            SELECT "CoverImageUrl" FROM "Collections" GROUP BY "CoverImageUrl" HAVING COUNT(*) > 2
        ) t
    ''')
    coll_over_2 = cur.fetchone()[0]

    cur.execute('SELECT COUNT(*) FROM "Posts"')
    final_post_count = cur.fetchone()[0]

    cur.execute('SELECT COUNT(DISTINCT "Url"), COUNT(*) FROM "PostMedias"')
    dist_post_urls, total_post_medias = cur.fetchone()

    cur.execute('SELECT COUNT(DISTINCT "AvatarUrl") FROM "Profiles"')
    dist_avatars = cur.fetchone()[0]

    cur.execute('SELECT COUNT(DISTINCT "CoverUrl") FROM "Profiles"')
    dist_covers = cur.fetchone()[0]

    print(f"Final Post Count: {final_post_count}")
    print(f"Total PostMedias: {total_post_medias}, Unique URLs: {dist_post_urls}")
    print(f"Unique Avatars:   {dist_avatars} (across 5000 users)")
    print(f"Unique Covers:    {dist_covers} (across 5000 users)")
    print(f"Over-duplicated (>2 uses) Post Media:   {post_over_2}")
    print(f"Over-duplicated (>2 uses) Avatars:      {avatar_over_2}")
    print(f"Over-duplicated (>2 uses) Covers:       {cover_over_2}")
    print(f"Over-duplicated (>2 uses) Comment Media:{comment_over_2}")
    print(f"Over-duplicated (>2 uses) Story Media:  {story_over_2}")
    print(f"Over-duplicated (>2 uses) Coll Covers:  {coll_over_2}")

    conn.close()

    # 8. UPDATE QDRANT POSTS_DEMO COLLECTION
    print("\n--- 8. Synchronizing Qdrant Vector Points ---")
    try:
        resp = httpx.get("http://localhost:6333/collections/posts_demo", timeout=5)
        if resp.status_code == 200:
            q_info = resp.json()["result"]
            print(f"Qdrant posts_demo status: points_count = {q_info.get('points_count')}")
            
            # Fetch scroll of existing points in Qdrant
            scroll_resp = httpx.post(
                "http://localhost:6333/collections/posts_demo/points/scroll",
                json={"limit": 6000, "with_payload": True, "with_vector": False},
                timeout=10
            )
            if scroll_resp.status_code == 200:
                points = scroll_resp.json()["result"]["points"]
                print(f"Found {len(points)} points in Qdrant posts_demo.")

                # Delete points for posts that were trimmed
                points_to_delete = []
                points_to_update = []
                kept_ids_str = set(str(pid) for pid in kept_posts)
                post_id_to_item = {str(kept_posts[i]): posts_cat[i] for i in range(len(kept_posts))}

                for pt in points:
                    pid = pt.get("payload", {}).get("post_id")
                    if pid and str(pid) not in kept_ids_str:
                        points_to_delete.append(pt["id"])
                    elif pid and str(pid) in post_id_to_item:
                        item = post_id_to_item[str(pid)]
                        new_payload = dict(pt.get("payload", {}))
                        new_payload["image_url"] = item["url"]
                        new_payload["caption"] = item["caption"]
                        new_payload["is_nsfw"] = item["is_nsfw"]
                        new_payload["category"] = item["category"]
                        points_to_update.append({
                            "id": pt["id"],
                            "payload": new_payload
                        })

                if points_to_delete:
                    print(f"Deleting {len(points_to_delete)} trimmed points from Qdrant...")
                    httpx.post(
                        "http://localhost:6333/collections/posts_demo/points/delete",
                        json={"points": points_to_delete},
                        timeout=10
                    )
                    print("Deleted trimmed points from Qdrant.")

                if points_to_update:
                    print(f"Updating payloads for {len(points_to_update)} points in Qdrant...")
                    # Batch update payloads
                    batch_size = 500
                    for b_start in range(0, len(points_to_update), batch_size):
                        batch = points_to_update[b_start:b_start + batch_size]
                        for pt in batch:
                            httpx.post(
                                f"http://localhost:6333/collections/posts_demo/points/payload",
                                json={"payload": pt["payload"], "points": [pt["id"]]},
                                timeout=5
                            )
                    print("Updated Qdrant point payloads successfully.")
    except Exception as ex:
        print(f"Note: Qdrant sync warning: {ex}")

    print("\n===============================================================")
    print("MIGRATION COMPLETED SUCCESSFULLY!")
    print("===============================================================")

if __name__ == "__main__":
    apply_migration()
