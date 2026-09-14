#!/usr/bin/env python3
"""
Production-Grade Asynchronous Batch Downloader & Semantic Dataset Generator
Seeds real images with authentic descriptions, hashtags, and NSFW sensitivity tags.
Implements:
  - 100-item batch sessions with max session timeout and inter-session idle delays.
  - Per-item retry ladder (2s -> 5s) and single shared global waiting queue (DLQ).
  - Terminal 4xx HTTP client error immediate discard.
  - Whole-session failure protection with automatic item requeue.
  - Final single-pass drain of the waiting queue.
  - Image header validation (magic bytes).
"""

import os
import sys
import json
import time
import asyncio
import argparse
from typing import List, Dict, Any, Optional, Set, Tuple

# Ensure UTF-8 output on Windows consoles
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except AttributeError:
        pass

try:
    import httpx  # type: ignore
except ImportError:
    httpx = None

# Curated base categories with semantic captions, tags, and realistic Unsplash/Wikimedia image IDs
CATEGORIES = {
    "food_cafe": {
        "captions": [
            ("Ly cà phê latte vẽ hình lá tinh tế tại quán quen ☕", "A finely crafted latte art at my favorite morning cafe ☕"),
            ("Pizza nướng củi phô mai tan chảy béo ngậy 🍕", "Wood-fired pizza with bubbling hot mozzarella 🍕"),
            ("Tô phở bò gia truyền thơm lừng hành ngò 🍜", "Traditional Vietnamese beef pho with fragrant herbs 🍜"),
            ("Bữa sáng healthy với bánh mì sourdough bơ nghiền và trứng chần 🥑🍳", "Healthy breakfast with smashed avocado on sourdough and poached eggs 🥑🍳"),
            ("Sushi cá hồi tươi ngon chuẩn vị Nhật Bản 🍣", "Fresh salmon sashimi and nigiri sushi 🍣"),
            ("Bánh sừng bò croissant bơ Pháp giòn rụm 🥐", "Crispy golden French butter croissant 🥐"),
            ("Ly cocktail mùa hè mát lạnh hương cam chanh 🍹", "Refreshing summer citrus cocktail with crushed ice 🍹")
        ],
        "tags": ["food", "cafe", "coffee", "delicious", "yummy", "breakfast", "culinary"],
        "urls": [
            "https://images.unsplash.com/photo-1509042239860-f550ce710b93?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1513104890138-7c749659a591?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1525351484163-7529414344d8?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1579871494447-9811cf80d66c?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1555507036-ab1f4038808a?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&fit=crop&q=80"
        ],
        "is_nsfw": False,
        "skin_ratio": 0.01
    },
    "travel_nature": {
        "captions": [
            ("Bình minh trên biển xanh cát trắng nắng vàng tuyệt đẹp 🌅🏖️", "Golden sunrise on tropical white sand beach 🌅🏖️"),
            ("Trekking rừng thông Đà Lạt không khí se lạnh trong lành 🌲🏕️", "Trekking through misty pine forests in the cool highlands 🌲🏕️"),
            ("Đỉnh núi phủ mây trắng hùng vĩ nhìn từ trên cao 🏔️", "Majestic mountain peak rising through a sea of clouds 🏔️"),
            ("Hoàng hôn đỏ rực buông xuống mặt hồ phẳng lặng 🌇🛶", "Vibrant crimson sunset reflecting on a serene mountain lake 🌇🛶"),
            ("Dòng thác nước trong vắt cuồn cuộn giữa rừng già 💧🌿", "Crystal clear jungle waterfall plunging into a natural pool 💧🌿"),
            ("Thung lũng ruộng bậc thang mùa lúa chín vàng óng 🌾✨", "Terraced rice fields glowing golden in harvest season 🌾✨")
        ],
        "tags": ["travel", "nature", "wanderlust", "landscape", "adventure", "mountains", "explore"],
        "urls": [
            "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1448375240586-882707db888b?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1506744038136-46273834b3fb?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1432405972618-c60b0225b8f9?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?w=800&fit=crop&q=80"
        ],
        "is_nsfw": False,
        "skin_ratio": 0.02
    },
    "pets_animals": {
        "captions": [
            ("Chú cún Golden Retriever tinh nghịch đón chủ về nhà 🐕🐾", "Playful golden retriever greeting with wagging tail 🐕🐾"),
            ("Bé mèo mướp nằm sưởi nắng bên bậu cửa sổ lười biếng 🐈☀️", "Cozy tabby cat napping in the warm afternoon sunlight 🐈☀️"),
            ("Đôi mắt long lanh của chú mèo con mới thức dậy 🐾✨", "Curious kitten with wide bright eyes looking at camera 🐾✨"),
            ("Cún cưng chạy nhảy tung tăng trên bãi cỏ công viên 🐶🎾", "Happy puppy playing fetch on green park lawn 🐶🎾")
        ],
        "tags": ["pets", "dogs", "cats", "cute", "animalovers", "puppy", "kitten"],
        "urls": [
            "https://images.unsplash.com/photo-1552053831-71594a27632d?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1514888286974-6c03e2ca1dba?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1533738363-b7f9aef128ce?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1583511655857-d19b40a7a54e?w=800&fit=crop&q=80"
        ],
        "is_nsfw": False,
        "skin_ratio": 0.01
    },
    "tech_workspace": {
        "captions": [
            ("Góc làm việc tối giản với bàn gỗ và màn hình công thái học 💻🎧", "Minimalist developer desk setup with dual monitors and mechanical keyboard 💻🎧"),
            ("Đêm muộn fix bug, cà phê và code là bạn đồng hành 👨‍💻🌙", "Late night programming session with dark theme IDE and coffee 👨‍💻🌙"),
            ("Thiết bị công nghệ thế hệ mới hiệu năng vượt trội 📱⚡", "Next generation technology hardware on clean desk 📱⚡"),
            ("Học kiến trúc hệ thống phân tán và modular monolith 📚🔧", "Deep diving into distributed systems and clean software architecture 📚🔧")
        ],
        "tags": ["technology", "coding", "developer", "workspace", "setup", "programmer", "clean"],
        "urls": [
            "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1498050108023-c5249f4df085?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1526738549149-8e07eca6c147?w=800&fit=crop&q=80"
        ],
        "is_nsfw": False,
        "skin_ratio": 0.03
    },
    "city_urban": {
        "captions": [
            ("Ánh đèn rực rỡ của thành phố khi màn đêm buông xuống 🏙️✨", "Vibrant city skyline glowing with illuminated skyscrapers at night 🏙️✨"),
            ("Góc phố cổ kính trầm mặc trong một chiều mưa thu 🌧️🏛️", "Historic European cobblestone street on a tranquil rainy afternoon 🌧️🏛️"),
            ("Cầu dây văng hiện đại vươn mình qua dòng sông lớn 🌉", "Architectural suspension bridge connecting modern city districts 🌉")
        ],
        "tags": ["city", "urban", "architecture", "nightlife", "metropolis", "streetphoto"],
        "urls": [
            "https://images.unsplash.com/photo-1477959858617-67f30bc75b82?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1513635269975-59663e0ac1ad?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1502602898657-3e91760cbb34?w=800&fit=crop&q=80"
        ],
        "is_nsfw": False,
        "skin_ratio": 0.01
    },
    "sports_fitness": {
        "captions": [
            ("Chinh phục giới hạn bản thân trong buổi tập gym hôm nay 💪🏋️", "Pushing past physical limits in high intensity gym workout 💪🏋️"),
            ("Chạy bộ buổi sáng ven hồ đón luồng gió sớm trong lành 🏃‍♂️🌅", "Morning lake run catching early morning refreshing breeze 🏃‍♂️🌅"),
            ("Buổi tập yoga tĩnh tâm tái tạo nguồn năng lượng tích cực 🧘‍♀️✨", "Mindful sunset yoga session revitalizing inner positive energy 🧘‍♀️✨")
        ],
        "tags": ["fitness", "workout", "gym", "health", "running", "yoga", "motivation"],
        "urls": [
            "https://images.unsplash.com/photo-1517838277536-f5f99be501cd?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1486218119243-13883505764c?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1506126613408-eca07ce68773?w=800&fit=crop&q=80"
        ],
        "is_nsfw": False,
        "skin_ratio": 0.08
    },
    "nsfw_trigger": {
        "captions": [
            ("Tắm nắng mùa hè trên bờ cát vàng rực rỡ ☀️🌊 #beach #summer", "Summer sunbathing on warm golden beach sands ☀️🌊 #beach #summer"),
            ("Buổi tập thể hình chuyên nghiệp trên sân khấu cơ bắp 💪🏆", "Professional bodybuilding pose showcase under stage lights 💪🏆"),
            ("Khoảnh khắc nghệ thuật chân dung gợi cảm bên bãi biển 🏖️👙", "Artistic summer portrait in swimwear along the coastline 🏖️👙")
        ],
        "tags": ["beach", "summer", "swimwear", "sunbath", "physique", "vacation"],
        "urls": [
            "https://images.unsplash.com/photo-1534447677768-be436bb09401?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=800&fit=crop&q=80",
            "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=800&fit=crop&q=80"
        ],
        "is_nsfw": True,
        "skin_ratio": 0.78
    },
    "comments_reactions": {
        "captions": [
            ("Nâng ly chúc mừng thành công nhé! Tuyệt vời quá 🥂🎉", "Cheers to the success! Absolutely wonderful 🥂🎉"),
            ("Chuẩn luôn không cần chỉnh, đồng ý cả hai tay! 👍💯", "Spot on! Completely agree 100%! 👍💯"),
            ("Trông món này hấp dẫn quá, mình cũng vừa nấu thử! 🍲😋", "Looks so appetizing, I also cooked this recently! 🍲😋"),
            ("Bức ảnh đẹp xuất sắc, góc chụp đỉnh thật sự 📸🔥", "Stunning shot, incredible composition! 📸🔥"),
            ("Haha cười xỉu với bài này luôn 😂🤣", "Haha laughing out loud at this, so relatable 😂🤣")
        ],
        "tags": ["reaction", "cheers", "agree", "food", "photography", "meme"],
        "urls": [
            "https://images.unsplash.com/photo-1510812431401-41d2bd2722f3?w=500&fit=crop&q=80",
            "https://images.unsplash.com/photo-1582213782179-e0d53f98f2ca?w=500&fit=crop&q=80",
            "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=500&fit=crop&q=80",
            "https://images.unsplash.com/photo-1516035069371-29a1b244cc32?w=500&fit=crop&q=80",
            "https://images.unsplash.com/photo-1527525443983-6e60c75fff46?w=500&fit=crop&q=80"
        ],
        "is_nsfw": False,
        "skin_ratio": 0.05
    }
}


def is_valid_image(data: bytes) -> bool:
    """Validate image magic bytes (JPEG, PNG, WebP, GIF)"""
    if len(data) < 12:
        return False
    # JPEG
    if data[:3] == b'\xff\xd8\xff':
        return True
    # PNG
    if data[:8] == b'\x89PNG\r\n\x1a\n':
        return True
    # WebP
    if data[:4] == b'RIFF' and data[8:12] == b'WEBP':
        return True
    # GIF
    if data[:6] in (b'GIF87a', b'GIF89a'):
        return True
    return False


async def download_item_with_retry(
    client: Any,
    item: Dict[str, Any],
    target_dir: str,
    waiting_queue: List[Dict[str, Any]],
    semaphore: asyncio.Semaphore,
    stats: Dict[str, int]
) -> bool:
    """
    Downloads an item with retry ladder:
    - On error: wait 2s -> retry 1 -> wait 5s -> retry 2 -> enqueue into waiting_queue.
    - If status code is 4xx client error (400, 404, 410, 403): drop immediately.
    """
    url = item["url"]
    filename = item["filename"]
    out_path = os.path.join(target_dir, filename)

    # If already downloaded and valid, skip
    if os.path.exists(out_path) and os.path.getsize(out_path) > 1024:
        stats["already_cached"] += 1
        item["local_path"] = f"/seed-assets/{item['category']}/{filename}"
        return True

    delays = [0, 2, 5]
    for attempt, delay in enumerate(delays):
        if delay > 0:
            await asyncio.sleep(delay)

        try:
            async with semaphore:
                resp = await client.get(url, timeout=20.0, follow_redirects=True)

            # Terminal 4xx client errors -> drop immediately
            if resp.status_code in (400, 403, 404, 410):
                print(f"  [TERMINAL_ERROR] HTTP {resp.status_code} for {url} -> Dropping item.")
                stats["dropped_4xx"] += 1
                return False

            if resp.status_code == 200:
                content = resp.content
                if is_valid_image(content):
                    os.makedirs(os.path.dirname(out_path), exist_ok=True)
                    with open(out_path, "wb") as f:
                        f.write(content)
                    stats["downloaded"] += 1
                    item["local_path"] = f"/seed-assets/{item['category']}/{filename}"
                    return True
                else:
                    print(f"  [CORRUPT_BYTES] Image verification failed for {url}")
        except Exception as ex:
            if attempt == len(delays) - 1:
                # Still failed after 2s and 5s retries -> move to waiting queue
                print(f"  [LADDER_FAILED] Retries exhausted for {filename} ({ex}). Moving to waiting queue.")
                waiting_queue.append(item)
                stats["queued_waiting"] += 1
                return False

    return False


async def run_batch_session(
    client: Any,
    session_items: List[Dict[str, Any]],
    session_num: int,
    total_sessions: int,
    target_dir: str,
    waiting_queue: List[Dict[str, Any]],
    semaphore: asyncio.Semaphore,
    session_timeout: float,
    stats: Dict[str, int]
):
    """Executes a 100-item session bounded by max session timeout"""
    print(f"\n[SESSION {session_num}/{total_sessions}] Starting session with {len(session_items)} items...")
    start_time = time.time()

    tasks = [
        download_item_with_retry(client, item, target_dir, waiting_queue, semaphore, stats)
        for item in session_items
    ]

    try:
        # Enforce static max session time
        await asyncio.wait_for(asyncio.gather(*tasks, return_exceptions=True), timeout=session_timeout)
        elapsed = time.time() - start_time
        print(f"[SESSION {session_num}/{total_sessions}] Completed in {elapsed:.2f}s")
    except asyncio.TimeoutError:
        print(f"[SESSION {session_num}/{total_sessions}] EXCEEDED TIMEOUT ({session_timeout}s)! Aborting remaining items to waiting queue.")
        stats["session_timeouts"] += 1
        for item in session_items:
            out_path = os.path.join(target_dir, item["filename"])
            if not (os.path.exists(out_path) and os.path.getsize(out_path) > 1024):
                if item not in waiting_queue:
                    waiting_queue.append(item)


def generate_manifest_data(total_posts: int, total_comments: int) -> Tuple[List[Dict[str, Any]], List[Dict[str, Any]]]:
    """Generates 10,000 post records and 2,500 comment records mapped to verified categories"""
    posts_manifest = []
    comments_manifest = []

    cat_keys = ["food_cafe", "travel_nature", "pets_animals", "tech_workspace", "city_urban", "sports_fitness", "nsfw_trigger"]
    
    # 1. Generate Posts
    for i in range(total_posts):
        # Category distribution: 2% NSFW triggers, remainder spread evenly
        if i % 50 == 0:
            cat = "nsfw_trigger"
        else:
            cat = cat_keys[i % (len(cat_keys) - 1)]

        data = CATEGORIES[cat]
        url_idx = i % len(data["urls"])
        caption_idx = i % len(data["captions"])
        caption_vi, caption_en = data["captions"][caption_idx]

        item_id = f"post_{i + 1:05d}"
        filename = f"{cat}/{cat}_{item_id}.jpg"

        posts_manifest.append({
            "id": item_id,
            "index": i + 1,
            "category": cat,
            "filename": filename,
            "url": data["urls"][url_idx],
            "local_path": f"/seed-assets/{filename}",
            "caption": f"{caption_vi} #{data['tags'][0]} #{data['tags'][1]}",
            "caption_en": caption_en,
            "tags": data["tags"],
            "is_nsfw": data["is_nsfw"],
            "skin_ratio": data["skin_ratio"]
        })

    # 2. Generate Comments (with ~25% having real media)
    comment_data = CATEGORIES["comments_reactions"]
    for i in range(total_comments):
        has_media = (i % 4 == 0) # 25% comments have media
        c_idx = i % len(comment_data["captions"])
        c_vi, c_en = comment_data["captions"][c_idx]
        url_idx = i % len(comment_data["urls"])

        item_id = f"cmt_{i + 1:05d}"
        filename = f"comments/{item_id}.jpg"

        comments_manifest.append({
            "id": item_id,
            "index": i + 1,
            "has_media": has_media,
            "category": "comments_reactions",
            "filename": filename,
            "url": comment_data["urls"][url_idx],
            "local_path": f"/seed-assets/{filename}" if has_media else None,
            "content": c_vi,
            "tags": comment_data["tags"]
        })

    return posts_manifest, comments_manifest


async def main():
    parser = argparse.ArgumentParser(description="Session-Based Batch Downloader for Favi Seed Images")
    parser.add_argument("--target-dir", default="Favi-BE/Favi-BE.API/wwwroot/seed-assets", help="Target assets directory")
    parser.add_argument("--catalog-dir", default="Favi-BE/Favi-BE.API/seed/catalogs", help="Catalog output directory")
    parser.add_argument("--batch-size", type=int, default=100, help="Batch items per session")
    parser.add_argument("--session-timeout", type=float, default=60.0, help="Max seconds per session")
    parser.add_argument("--idle-seconds", type=float, default=3.0, help="Idle seconds between sessions")
    parser.add_argument("--concurrency", type=int, default=8, help="Concurrent workers per session")
    parser.add_argument("--total-posts", type=int, default=10000, help="Total posts to generate")
    parser.add_argument("--total-comments", type=int, default=2500, help="Total comments to generate")
    parser.add_argument("--limit-download", type=int, default=0, help="Limit downloads for rapid testing (0 = full)")
    parser.add_argument("--generate-catalog-only", action="store_true", help="Generate JSON manifests without downloading")
    parser.add_argument("--verify-flow", action="store_true", help="Verify retry and queue drain state machine")

    args = parser.parse_args()

    # Resolve target paths
    base_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
    target_dir = os.path.join(base_dir, args.target_dir)
    catalog_dir = os.path.join(base_dir, args.catalog_dir)
    os.makedirs(target_dir, exist_ok=True)
    os.makedirs(catalog_dir, exist_ok=True)

    print("=================================================================")
    print("FAVI SEED DATASET GENERATOR & ASYNC BATCH DOWNLOADER")
    print(f"Target Assets Dir: {target_dir}")
    print(f"Catalog Dir:       {catalog_dir}")
    print(f"Total Posts:       {args.total_posts}")
    print(f"Total Comments:    {args.total_comments}")
    print("=================================================================")

    # 1. Generate Metadata Catalogs
    posts_manifest, comments_manifest = generate_manifest_data(args.total_posts, args.total_comments)

    posts_cat_path = os.path.join(catalog_dir, "real-posts-catalog.json")
    comments_cat_path = os.path.join(catalog_dir, "real-comments-catalog.json")

    with open(posts_cat_path, "w", encoding="utf-8") as f:
        json.dump(posts_manifest, f, indent=2, ensure_ascii=False)
    with open(comments_cat_path, "w", encoding="utf-8") as f:
        json.dump(comments_manifest, f, indent=2, ensure_ascii=False)

    print(f"[OK] Created posts catalog: {posts_cat_path} ({len(posts_manifest)} records)")
    print(f"[OK] Created comments catalog: {comments_cat_path} ({len(comments_manifest)} records)")

    if args.generate_catalog_only:
        print("\n[DONE] Manifest generated successfully (--generate-catalog-only active).")
        return

    # Verify flow test simulation
    if args.verify_flow:
        print("\n[TEST MODE] Testing retry ladder and waiting queue drain flow...")
        queue = []
        stats = {"downloaded": 0, "dropped_4xx": 0, "queued_waiting": 0, "already_cached": 0, "session_timeouts": 0}
        # Simulate items
        test_items = [
            {"url": "http://httpstat.us/404", "filename": "test/404.jpg", "category": "test"},
            {"url": "http://httpstat.us/500", "filename": "test/500.jpg", "category": "test"},
            {"url": "https://images.unsplash.com/photo-1509042239860-f550ce710b93?w=100", "filename": "test/ok.jpg", "category": "test"}
        ]
        if httpx:
            async with httpx.AsyncClient() as client:
                sem = asyncio.Semaphore(2)
                for item in test_items:
                    await download_item_with_retry(client, item, target_dir, queue, sem, stats)
            print(f"Test stats: {stats}, Queue size: {len(queue)}")
            print("✓ State machine test complete!")
        return

    if not httpx:
        print("\n[WARN] httpx is not installed. To execute live downloads, run: pip install httpx")
        print("Manifests are ready for seeding.")
        return

    # 2. Collect unique items to download
    items_to_download = []
    seen_urls: Set[str] = set()

    for p in posts_manifest:
        if p["url"] not in seen_urls:
            seen_urls.add(p["url"])
            items_to_download.append(p)

    for c in comments_manifest:
        if c.get("has_media") and c["url"] not in seen_urls:
            seen_urls.add(c["url"])
            items_to_download.append(c)

    if args.limit_download > 0:
        items_to_download = items_to_download[:args.limit_download]
        print(f"[LIMIT] Restricting download run to first {len(items_to_download)} unique items.")

    total_items = len(items_to_download)
    sessions = [items_to_download[i:i + args.batch_size] for i in range(0, total_items, args.batch_size)]
    print(f"\nUnique media files to download: {total_items} across {len(sessions)} sessions (100/session).")

    stats = {
        "downloaded": 0,
        "dropped_4xx": 0,
        "queued_waiting": 0,
        "already_cached": 0,
        "session_timeouts": 0,
        "queue_recovered": 0,
        "permanent_failures": 0
    }
    waiting_queue = []
    semaphore = asyncio.Semaphore(args.concurrency)

    async with httpx.AsyncClient(headers={"User-Agent": "FaviSeedBot/2.0"}) as client:
        for idx, session_batch in enumerate(sessions):
            await run_batch_session(
                client=client,
                session_items=session_batch,
                session_num=idx + 1,
                total_sessions=len(sessions),
                target_dir=target_dir,
                waiting_queue=waiting_queue,
                semaphore=semaphore,
                session_timeout=args.session_timeout,
                stats=stats
            )
            # Inter-session idle pause
            if idx < len(sessions) - 1:
                print(f"Pausing {args.idle_seconds}s before next session...")
                await asyncio.sleep(args.idle_seconds)

        # 3. Final Pass: Drain waiting queue
        if waiting_queue:
            print(f"\n=================================================================")
            print(f"FINAL PASS: Draining {len(waiting_queue)} items from waiting queue (single attempt)...")
            print("=================================================================")
            while waiting_queue:
                q_item = waiting_queue.pop(0)
                try:
                    async with semaphore:
                        resp = await client.get(q_item["url"], timeout=20.0, follow_redirects=True)
                    if resp.status_code == 200 and is_valid_image(resp.content):
                        out_path = os.path.join(target_dir, q_item["filename"])
                        os.makedirs(os.path.dirname(out_path), exist_ok=True)
                        with open(out_path, "wb") as f:
                            f.write(resp.content)
                        stats["queue_recovered"] += 1
                    else:
                        stats["permanent_failures"] += 1
                except Exception:
                    stats["permanent_failures"] += 1

    print("\n=================================================================")
    print("DOWNLOAD RUN SUMMARY")
    print(f"Total Unique Targets:   {total_items}")
    print(f"Downloaded Now:         {stats['downloaded']}")
    print(f"Already Cached:         {stats['already_cached']}")
    print(f"Recovered from Queue:   {stats['queue_recovered']}")
    print(f"Dropped via 4xx:        {stats['dropped_4xx']}")
    print(f"Permanent Failures:     {stats['permanent_failures']}")
    print("=================================================================")


if __name__ == "__main__":
    asyncio.run(main())
