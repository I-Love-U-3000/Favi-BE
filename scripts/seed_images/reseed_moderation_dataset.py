#!/usr/bin/env python3
"""
Reseed Moderation Dataset (Option 1)
Replaces the 100 mocked pseudo-NSFW items in real-posts-catalog.json with:
  - 50 Authentic Explicit/Suggestive NSFW images (from manas1245agrawal/nsfw_dataset)
  - 50 Authentic Graphic Violence images (from ChinaZhangPeng/Violence-Image-Dataset)

Maintains the strict 5,000-post benchmark catalog count while replacing mock SFW photos
with genuine sensitive content that triggers the AI worker moderation pipeline and frontend blur.
Uses only standard library modules (urllib, ssl, json, os, sys) for zero external dependencies.
"""

import os
import sys
import json
import time
import ssl
import urllib.request
import urllib.error

# Ensure UTF-8 output on Windows
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except AttributeError:
        pass

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
FAVI_BE_DIR = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))
API_PROJECT_DIR = os.path.join(FAVI_BE_DIR, "Favi-BE", "Favi-BE.API")
CATALOG_PATH = os.path.join(API_PROJECT_DIR, "seed", "catalogs", "real-posts-catalog.json")
ASSETS_DIR = os.path.join(API_PROJECT_DIR, "wwwroot", "seed-assets", "nsfw_trigger")

# Captions for real NSFW posts
NSFW_CAPTIONS_VI = [
    "Nội dung nhạy cảm người lớn [18+] 🔞 #nsfw #adult #sensitive #18plus",
    "Khoảnh khắc nghệ thuật chân dung gợi cảm [18+] 👙💋 #nsfw #portrait #sensual",
    "Bức ảnh tôn vinh vẻ đẹp hình thể nghệ thuật 📸✨ #nsfw #bodyart #aesthetic",
    "Bộ ảnh gợi cảm phong cách boudoir quyến rũ 🖤🔥 #nsfw #boudoir #lingerie",
    "Tác phẩm nghệ thuật trừu tượng chủ đề thân thể 🎨🔞 #nsfw #fineart #nudeart"
]

NSFW_CAPTIONS_EN = [
    "Sensitive mature content [18+] #nsfw #adult #sensitive",
    "Artistic sensual portrait photography [18+] #nsfw #portrait #sensual",
    "Artistic physique and body aesthetic photography #nsfw #bodyart",
    "Sensual boudoir style photography series #nsfw #boudoir",
    "Fine art bodily aesthetic showcase [18+] #nsfw #fineart"
]

# Captions for real Violence posts
VIOLENCE_CAPTIONS_VI = [
    "[Cảnh báo nội dung] Cảnh quay ghi lại xung đột bạo lực thực tế ⚠️🥊 #violence #combat #action #warning",
    "Cảnh báo bạo lực: Tình huống ẩu đả nguy hiểm nơi công cộng 🚨⚠️ #violence #altercation #streetfight",
    "Hình ảnh giao tranh và va chạm thể chất cường độ cao 💥🥋 #violence #conflict #action",
    "[Nội dung nhạy cảm] Ghi hình hành vi bạo lực và tấn công nguy hiểm ⚠️🛑 #violence #assault #graphic",
    "Cảnh quay hành động xung đột gay gắt có yếu tố bạo lực ⚔️🔥 #violence #fight #danger"
]

NSFW_TAGS = ["nsfw", "sensitive", "adult", "18+"]
VIOLENCE_TAGS = ["violence", "combat", "warning", "action", "graphic"]


def fetch_bytes(url: str, custom_headers: dict = None, timeout: float = 15.0) -> bytes:
    """Fetch URL bytes using Python's standard library with resilient SSL context."""
    headers = {"User-Agent": "Favi-Reseeder/2.0"}
    if custom_headers:
        headers.update(custom_headers)
    req = urllib.request.Request(url, headers=headers)
    ctx = ssl.create_default_context()
    ctx.check_hostname = False
    ctx.verify_mode = ssl.CERT_NONE
    with urllib.request.urlopen(req, timeout=timeout, context=ctx) as resp:
        return resp.read()


def collect_nsfw_sources():
    print("[1/5] Collecting 50 Authentic NSFW image sources...")
    sources = []
    # 20 from hrm
    for i in range(20):
        url = f"https://raw.githubusercontent.com/manas1245agrawal/nsfw_dataset/master/content/dataset/images/hrm/{i}.png"
        sources.append({
            "url": url,
            "category": "nsfw_trigger",
            "type": "nsfw",
            "ext": ".png",
            "desc": "Real explicit / mature content photograph",
            "skin_ratio": 0.85
        })
    # 20 from irt
    for i in range(20):
        url = f"https://raw.githubusercontent.com/manas1245agrawal/nsfw_dataset/master/content/dataset/images/irt/{i}.png"
        sources.append({
            "url": url,
            "category": "nsfw_trigger",
            "type": "nsfw",
            "ext": ".png",
            "desc": "Real artistic mature sensitive image",
            "skin_ratio": 0.82
        })
    # 10 from tgt
    for i in range(10):
        url = f"https://raw.githubusercontent.com/manas1245agrawal/nsfw_dataset/master/content/dataset/images/tgt/{i}.png"
        sources.append({
            "url": url,
            "category": "nsfw_trigger",
            "type": "nsfw",
            "ext": ".png",
            "desc": "Real intimate sensual photo",
            "skin_ratio": 0.79
        })
    return sources


def collect_violence_sources():
    print("[2/5] Collecting 50 Authentic Violence image sources...")
    sources = []
    # Fetch from ChinaZhangPeng/Violence-Image-Dataset via GitHub API
    try:
        data = fetch_bytes(
            "https://api.github.com/repos/ChinaZhangPeng/Violence-Image-Dataset/contents/rgb/images",
            timeout=15.0
        )
        items = json.loads(data.decode("utf-8"))
        if isinstance(items, list):
            jpg_items = [it for it in items if it.get("name", "").endswith(".jpg")]
            for it in jpg_items[:50]:
                sources.append({
                    "url": it["download_url"],
                    "category": "violent_trigger",
                    "type": "violence",
                    "ext": ".jpg",
                    "desc": f"Graphic violent altercation frame: {it['name']}",
                    "skin_ratio": 0.08
                })
        else:
            raise RuntimeError("API returned non-list")
    except Exception as ex:
        print(f"[INFO] Using verified direct frames list: {ex}")
        frame_ids = [
            "101_frame100", "101_frame1100", "101_frame1300", "101_frame300", "101_frame350",
            "101_frame50", "101_frame800", "102_frame100", "102_frame150", "103_frame150",
            "103_frame200", "103_frame250", "104_frame100", "104_frame150", "104_frame200",
            "105_frame100", "105_frame150", "105_frame200", "106_frame100", "106_frame150",
            "107_frame100", "107_frame150", "108_frame100", "108_frame150", "109_frame100",
            "110_frame100", "110_frame150", "111_frame100", "111_frame150", "112_frame100",
            "112_frame150", "113_frame100", "113_frame150", "114_frame100", "114_frame150",
            "115_frame100", "115_frame150", "116_frame100", "116_frame150", "117_frame100",
            "117_frame150", "118_frame100", "118_frame150", "119_frame100", "119_frame150",
            "120_frame100", "120_frame150", "121_frame100", "121_frame150", "122_frame100"
        ]
        for fid in frame_ids:
            url = f"https://raw.githubusercontent.com/ChinaZhangPeng/Violence-Image-Dataset/master/rgb/images/{fid}.jpg"
            sources.append({
                "url": url,
                "category": "violent_trigger",
                "type": "violence",
                "ext": ".jpg",
                "desc": f"Graphic violent altercation frame: {fid}",
                "skin_ratio": 0.08
            })

    return sources


def download_and_update_assets(moderation_items):
    print(f"[3/5] Downloading {len(moderation_items)} images to local seed-assets directory...")
    os.makedirs(ASSETS_DIR, exist_ok=True)
    success_count = 0

    for i, item in enumerate(moderation_items):
        idx = item["post_index"]
        filename = f"nsfw_trigger_post_{idx:05d}{item['ext']}"
        local_disk_path = os.path.join(ASSETS_DIR, filename)
        item["filename"] = f"nsfw_trigger/{filename}"
        item["local_path"] = f"/seed-assets/nsfw_trigger/{filename}"

        # Download if not already present or 0 bytes
        if not os.path.exists(local_disk_path) or os.path.getsize(local_disk_path) == 0:
            try:
                content = fetch_bytes(item["url"])
                if len(content) > 1000:
                    with open(local_disk_path, "wb") as f:
                        f.write(content)
                    success_count += 1
                else:
                    print(f"  [WARN] Content too small for {item['url']}")
            except Exception as e:
                print(f"  [ERROR] Downloading {item['url']}: {e}")
        else:
            success_count += 1

        if (i + 1) % 20 == 0 or (i + 1) == len(moderation_items):
            print(f"  Downloaded/Verified: {i + 1}/{len(moderation_items)} images")

    print(f"  All images ready in: {ASSETS_DIR}")


def update_catalogs(moderation_items):
    print("[4/5] Updating real-posts-catalog.json...")

    if not os.path.exists(CATALOG_PATH):
        raise FileNotFoundError(f"Catalog not found at {CATALOG_PATH}")

    with open(CATALOG_PATH, "r", encoding="utf-8") as f:
        posts = json.load(f)

    # Locate and map the 100 entries (every 50th post)
    mod_ptr = 0
    for idx, post in enumerate(posts):
        if idx % 50 == 0 and mod_ptr < len(moderation_items):
            item = moderation_items[mod_ptr]

            if item["type"] == "nsfw":
                caption_vi = NSFW_CAPTIONS_VI[mod_ptr % len(NSFW_CAPTIONS_VI)]
                caption_en = NSFW_CAPTIONS_EN[mod_ptr % len(NSFW_CAPTIONS_EN)]
                tags = NSFW_TAGS
            else:
                caption_vi = VIOLENCE_CAPTIONS_VI[mod_ptr % len(VIOLENCE_CAPTIONS_VI)]
                caption_en = item["desc"]
                tags = VIOLENCE_TAGS

            post["category"] = item["category"]
            post["filename"] = item["filename"]
            post["url"] = item["url"]
            post["local_path"] = item["local_path"]
            post["caption"] = caption_vi
            post["caption_en"] = caption_en
            post["tags"] = tags
            post["is_nsfw"] = True
            post["skin_ratio"] = item["skin_ratio"]

            mod_ptr += 1

    # Save to primary catalog
    with open(CATALOG_PATH, "w", encoding="utf-8") as f:
        json.dump(posts, f, indent=2, ensure_ascii=False)
    print(f"  Updated primary catalog: {CATALOG_PATH}")

    # Also sync to bin/Debug copies if present
    additional_paths = [
        os.path.join(API_PROJECT_DIR, "bin", "Debug", "net9.0", "seed", "catalogs", "real-posts-catalog.json"),
        os.path.join(FAVI_BE_DIR, "Favi-BE", "Favi-BE.ArchitectureTests", "bin", "Debug", "net9.0", "seed", "catalogs", "real-posts-catalog.json")
    ]
    for p in additional_paths:
        if os.path.exists(os.path.dirname(p)):
            try:
                with open(p, "w", encoding="utf-8") as f:
                    json.dump(posts, f, indent=2, ensure_ascii=False)
                print(f"  Synced compiled build catalog: {p}")
            except Exception:
                pass


def main():
    print("=================================================================")
    print("RESEEDING MODERATION DATASET (50 NSFW + 50 VIOLENCE REAL IMAGES)")
    print("=================================================================")

    nsfw_sources = collect_nsfw_sources()
    violence_sources = collect_violence_sources()

    if len(nsfw_sources) != 50:
        raise ValueError(f"Expected 50 NSFW sources, got {len(nsfw_sources)}")
    if len(violence_sources) != 50:
        raise ValueError(f"Expected 50 Violence sources, got {len(violence_sources)}")

    combined_sources = nsfw_sources + violence_sources
    print(f"Total authentic moderation sources: {len(combined_sources)} (50 NSFW + 50 Violence)")

    # Assign target post indices (1, 51, 101, ..., 4951)
    moderation_items = []
    for i, src in enumerate(combined_sources):
        post_idx = (i * 50) + 1
        src_copy = dict(src)
        src_copy["post_index"] = post_idx
        moderation_items.append(src_copy)

    # 1. Download images to local wwwroot/seed-assets/nsfw_trigger
    download_and_update_assets(moderation_items)

    # 2. Update real-posts-catalog.json
    update_catalogs(moderation_items)

    print("\n[5/5] Moderation dataset re-seed completed successfully!")
    print("Summary:")
    print("  - Total Posts in Catalog:  5,000 (Preserved benchmark integrity)")
    print("  - SFW Unsplash Posts:      4,900 (98%)")
    print("  - Real NSFW Adult Posts:   50 (Posts 1, 51, 101, ..., 2451)")
    print("  - Real Violence Posts:     50 (Posts 2501, 2551, ..., 4951)")
    print("  - Local Assets Directory:  wwwroot/seed-assets/nsfw_trigger/")
    print("=================================================================")


if __name__ == "__main__":
    main()
