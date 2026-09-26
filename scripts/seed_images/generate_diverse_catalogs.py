#!/usr/bin/env python3
"""
Generate Diverse Catalogs from Unsplash-25k dataset.
Produces:
1. real-posts-catalog.json: exactly 5,000 posts with 5,000 unique images (max 1 post per image).
2. real-avatars-catalog.json: 2,500 unique avatar images (max 2 users per image for 5,000 users).
3. real-covers-catalog.json: 2,500 unique panoramic cover images (max 2 users per image for 5,000 users).
4. real-comments-catalog.json: 5,000 comments with 2,500 unique images for comments with media (max 2 comments per image).
5. real-stories-catalog.json: 1,250 unique vertical story images (max 2 stories per image).
6. real-collections-catalog.json: 250 unique collection covers (max 2 collections per image).
"""

import os
import glob
import json
import pandas as pd

def build_catalogs():
    cache_pattern = os.path.expanduser(r"~\.cache\huggingface\hub\datasets--1aurent--unsplash-lite\snapshots\*\data\train-*.parquet")
    files = sorted(glob.glob(cache_pattern))
    if not files:
        raise RuntimeError("No cached parquet files found in ~/.cache/huggingface/hub/")
        
    print(f"Loading {len(files)} parquet files...")
    dfs = [pd.read_parquet(f) for f in files]
    full_df = pd.concat(dfs, ignore_index=True)
    total_photos = len(full_df)
    print(f"Loaded {total_photos} total photos.")

    # Target directory
    catalog_dir = r"C:\Users\MINH QUANG\Favi\Favi-BE\Favi-BE\Favi-BE.API\seed\catalogs"
    os.makedirs(catalog_dir, exist_ok=True)

    # Categories for posts
    categories = [
        "travel_nature", "food_cafe", "city_urban", "tech_workspace",
        "pets_animals", "sports_fitness", "art_lifestyle"
    ]
    
    cat_captions_vi = {
        "travel_nature": [
            "Bình minh trên biển xanh cát trắng nắng vàng tuyệt đẹp 🌅🏖️",
            "Trekking rừng thông Đà Lạt không khí se lạnh trong lành 🌲🏕️",
            "Đỉnh núi phủ mây trắng hùng vĩ nhìn từ trên cao 🏔️",
            "Hoàng hôn đỏ rực buông xuống mặt hồ phẳng lặng 🌇🛶",
            "Dòng thác nước trong vắt cuồn cuộn giữa rừng già 💧🌿",
            "Thung lũng ruộng bậc thang mùa lúa chín vàng óng 🌾✨"
        ],
        "food_cafe": [
            "Ly cà phê latte vẽ hình lá tinh tế tại quán quen ☕",
            "Pizza nướng củi phô mai tan chảy béo ngậy 🍕",
            "Tô phở bò gia truyền thơm lừng hành ngò 🍜",
            "Bữa sáng healthy với bánh mì sourdough bơ nghiền và trứng chần 🥑🍳",
            "Sushi cá hồi tươi ngon chuẩn vị Nhật Bản 🍣",
            "Bánh sừng bò croissant bơ Pháp giòn rụm 🥐",
            "Ly cocktail mùa hè mát lạnh hương cam chanh 🍹"
        ],
        "city_urban": [
            "Ánh đèn rực rỡ của thành phố khi màn đêm buông xuống 🏙️✨",
            "Góc phố cổ kính trầm mặc trong một chiều mưa thu 🌧️🏛️",
            "Cầu dây văng hiện đại vươn mình qua dòng sông lớn 🌉",
            "Nhịp sống hối hả nơi đại lộ trung tâm thành phố 🚏🚶",
            "Kiến trúc tối giản góc phố chiều hoàng hôn 🌆"
        ],
        "tech_workspace": [
            "Góc làm việc tối giản với bàn gỗ và màn hình công thái học 💻🎧",
            "Đêm muộn fix bug, cà phê và code là bạn đồng hành 👨‍💻🌙",
            "Thiết bị công nghệ thế hệ mới hiệu năng vượt trội 📱⚡",
            "Học kiến trúc hệ thống phân tán và modular monolith 📚🔧",
            "Setup góc bàn làm việc aesthetic tràn đầy cảm hứng sáng tạo 🖥️🪴"
        ],
        "pets_animals": [
            "Chú cún Golden Retriever tinh nghịch đón chủ về nhà 🐕🐾",
            "Bé mèo mướp nằm sưởi nắng bên bậu cửa sổ lười biếng 🐈☀️",
            "Đôi mắt long lanh của chú mèo con mới thức dậy 🐾✨",
            "Cún cưng chạy nhảy tung tăng trên bãi cỏ công viên 🐶🎾",
            "Khoảnh khắc đáng yêu của chú mèo béo ngủ gật 💤🐱"
        ],
        "sports_fitness": [
            "Chinh phục giới hạn bản thân trong buổi tập gym hôm nay 💪🏋️",
            "Chạy bộ buổi sáng ven hồ đón luồng gió sớm trong lành 🏃‍♂️🌅",
            "Buổi tập yoga tĩnh tâm tái tạo nguồn năng lượng tích cực 🧘‍♀️✨",
            "Đạp xe cuối tuần khám phá những cung đường ngoại ô 🚴‍♂️🍃"
        ],
        "art_lifestyle": [
            "Không gian nghệ thuật triển lãm đầy màu sắc đương đại 🎨🖼️",
            "Một góc đọc sách yên bình bên ly trà hoa cúc 📖☕",
            "Góc ban công ngập tràn cây xanh và ánh nắng sớm 🌿🪴",
            "Khoảnh khắc thảnh thơi tận hưởng trọn vẹn ngày cuối tuần 🍃✨"
        ],
        "nsfw_trigger": [
            "Tắm nắng mùa hè trên bờ cát vàng rực rỡ ☀️🌊 #beach #summer",
            "Buổi tập thể hình chuyên nghiệp trên sân khấu cơ bắp 💪🏆",
            "Khoảnh khắc nghệ thuật chân dung gợi cảm bên bãi biển 🏖️👙"
        ]
    }

    # Extract clean Unsplash photo records
    photos = []
    for i in range(total_photos):
        row = full_df.iloc[i]
        p = row['photo']
        ai = row['ai'] if isinstance(row['ai'], dict) else {}
        base_url = p['image_url']
        desc = p.get('description')
        if not desc or desc == 'nan':
            desc = ai.get('description', '')
            if not desc or desc == 'nan':
                desc = ''
                
        kw = row['keywords'] if isinstance(row['keywords'], list) else []
        tags = [k.get('keyword') for k in kw if isinstance(k, dict) and k.get('keyword')]
        
        photos.append({
            "id": p['id'],
            "base_url": base_url,
            "desc": desc,
            "tags": tags[:8] if tags else ["favi", "lifestyle"]
        })

    print(f"Total extracted clean photos: {len(photos)}")

    # Slice partitions:
    # 0..4999 (5000): Posts
    # 5000..7499 (2500): Avatars
    # 7500..9999 (2500): Covers
    # 10000..12499 (2500): Comments (Media)
    # 12500..13749 (1250): Stories
    # 13750..13999 (250): Collections
    
    posts_slice = photos[0:5000]
    avatars_slice = photos[5000:7500]
    covers_slice = photos[7500:10000]
    comments_slice = photos[10000:12500]
    stories_slice = photos[12500:13750]
    collections_slice = photos[13750:14000]

    # 1. GENERATE REAL-POSTS-CATALOG.JSON (5,000 posts, 5,000 unique URLs)
    posts_catalog = []
    for i, p in enumerate(posts_slice):
        idx = i + 1
        is_nsfw = (i % 50 == 0) # 2% calibrated NSFW triggers
        if is_nsfw:
            cat = "nsfw_trigger"
            skin_ratio = 0.78
        else:
            cat = categories[i % len(categories)]
            skin_ratio = 0.02
            
        captions = cat_captions_vi[cat]
        base_vi = captions[i % len(captions)]
        
        caption_en = p['desc'] if p['desc'] else f"A stunning {cat.replace('_', ' ')} photograph"
        caption = f"{base_vi} #{cat.split('_')[0]} #{cat.split('_')[-1]}"
        
        url = f"{p['base_url']}?w=800&fit=crop&q=80"
        filename = f"{cat}/{cat}_post_{idx:05d}.jpg"
        
        posts_catalog.append({
            "id": f"post_{idx:05d}",
            "index": idx,
            "category": cat,
            "filename": filename,
            "url": url,
            "local_path": f"/seed-assets/{filename}",
            "caption": caption,
            "caption_en": caption_en,
            "tags": p['tags'] if p['tags'] else [cat.split('_')[0]],
            "is_nsfw": is_nsfw,
            "skin_ratio": skin_ratio
        })

    posts_cat_path = os.path.join(catalog_dir, "real-posts-catalog.json")
    with open(posts_cat_path, "w", encoding="utf-8") as f:
        json.dump(posts_catalog, f, indent=2, ensure_ascii=False)
    print(f"Generated {len(posts_catalog)} posts in {posts_cat_path} (Unique URLs: {len(set(x['url'] for x in posts_catalog))})")

    # 2. GENERATE REAL-AVATARS-CATALOG.JSON (2,500 unique URLs, max 2 users per URL)
    avatars_catalog = [f"{p['base_url']}?w=400&fit=crop&crop=faces&q=80" for p in avatars_slice]
    avatars_cat_path = os.path.join(catalog_dir, "real-avatars-catalog.json")
    with open(avatars_cat_path, "w", encoding="utf-8") as f:
        json.dump(avatars_catalog, f, indent=2, ensure_ascii=False)
    print(f"Generated {len(avatars_catalog)} avatars in {avatars_cat_path} (Unique URLs: {len(set(avatars_catalog))})")

    # 3. GENERATE REAL-COVERS-CATALOG.JSON (2,500 unique URLs, max 2 users per URL)
    covers_catalog = [f"{p['base_url']}?w=1200&h=400&fit=crop&q=80" for p in covers_slice]
    covers_cat_path = os.path.join(catalog_dir, "real-covers-catalog.json")
    with open(covers_cat_path, "w", encoding="utf-8") as f:
        json.dump(covers_catalog, f, indent=2, ensure_ascii=False)
    print(f"Generated {len(covers_catalog)} covers in {covers_cat_path} (Unique URLs: {len(set(covers_catalog))})")

    # 4. GENERATE REAL-COMMENTS-CATALOG.JSON (5,000 comments, 2,500 with media, unique media URLs)
    comment_templates = [
        "Nâng ly chúc mừng thành công nhé! Tuyệt vời quá 🥂🎉",
        "Chuẩn luôn không cần chỉnh, đồng ý cả hai tay! 👍💯",
        "Trông món này hấp dẫn quá, mình cũng vừa nấu thử! 🍲😋",
        "Bức ảnh đẹp xuất sắc, góc chụp đỉnh thật sự 📸🔥",
        "Haha cười xỉu với bài này luôn 😂🤣",
        "Rất truyền cảm hứng, cảm ơn bạn đã chia sẻ! ✨🙌",
        "Góc nhìn thú vị ghê, để mình thử áp dụng xem sao 💡",
        "Chuyến đi tuyệt vời quá, cảnh đẹp mê hồn! 🏞️😍",
        "Setup bàn làm việc gọn gàng và xịn quá bạn ơi 💻👌",
        "Em cún/mèo đáng yêu xỉu, nhìn cưng quá đi thôi 🐾🥰"
    ]
    
    comments_catalog = []
    media_ptr = 0
    for i in range(5000):
        idx = i + 1
        has_media = (i % 2 == 0) # 50% comments have media (2,500 comments with media)
        cmt_url = None
        local_path = None
        if has_media:
            cmt_p = comments_slice[media_ptr]
            media_ptr += 1
            cmt_url = f"{cmt_p['base_url']}?w=500&fit=crop&q=80"
            local_path = f"/seed-assets/comments/cmt_{idx:05d}.jpg"
            
        comments_catalog.append({
            "id": f"cmt_{idx:05d}",
            "index": idx,
            "has_media": has_media,
            "category": "comments_reactions",
            "filename": f"comments/cmt_{idx:05d}.jpg",
            "url": cmt_url,
            "local_path": local_path,
            "content": comment_templates[i % len(comment_templates)],
            "tags": ["reaction", "cheers", "lifestyle"]
        })
        
    comments_cat_path = os.path.join(catalog_dir, "real-comments-catalog.json")
    with open(comments_cat_path, "w", encoding="utf-8") as f:
        json.dump(comments_catalog, f, indent=2, ensure_ascii=False)
    cmts_with_media = [c['url'] for c in comments_catalog if c['url']]
    print(f"Generated {len(comments_catalog)} comments in {comments_cat_path} (Unique Media URLs: {len(set(cmts_with_media))})")

    # 5. GENERATE REAL-STORIES-CATALOG.JSON (1,250 unique vertical story URLs)
    stories_catalog = []
    for i, p in enumerate(stories_slice):
        idx = i + 1
        url = f"{p['base_url']}?w=1080&h=1920&fit=crop&q=80"
        stories_catalog.append({
            "id": f"story_{idx:04d}",
            "index": idx,
            "url": url,
            "caption": f"Story moment #{idx} ✨"
        })
        
    stories_cat_path = os.path.join(catalog_dir, "real-stories-catalog.json")
    with open(stories_cat_path, "w", encoding="utf-8") as f:
        json.dump(stories_catalog, f, indent=2, ensure_ascii=False)
    print(f"Generated {len(stories_catalog)} stories in {stories_cat_path} (Unique URLs: {len(set(s['url'] for s in stories_catalog))})")

    # 6. GENERATE REAL-COLLECTIONS-CATALOG.JSON (250 unique collection covers)
    collection_titles = [
        ("Góc Cafe Ưa Thích", "Bộ sưu tập những quán cafe chill và đồ uống ngon nhất."),
        ("Khoảnh Khắc Mùa Hè", "Những chuyến đi biển, nắng vàng và bờ cát trắng rực rỡ."),
        ("Workspaces & Coding Setups", "Không gian làm việc tối giản, góc máy bàn công thái học."),
        ("Chuyến Đi Đáng Nhớ", "Trekking, núi rừng hùng vĩ và những cung đường khám phá."),
        ("Thú Cưng Đáng Yêu", "Những bé cún, bé mèo ngộ nghĩnh đáng yêu nhất."),
        ("Kiến Trúc Đô Thị", "Góc phố, tòa nhà hiện đại và ánh đèn thành phố về đêm."),
        ("Ẩm Thực Bốn Phương", "Các món ăn ngon truyền thống và ẩm thực đường phố."),
        ("Sunset & Sunrise Vibes", "Khoảnh khắc bình minh và hoàng hôn tuyệt đẹp trên khắp thế giới."),
        ("Nghệ Thuật & Sống Đẹp", "Không gian triển lãm nghệ thuật, tranh vẽ và phong cách sống."),
        ("Fitness & Sức Khỏe", "Động lực tập luyện mỗi ngày, gym, chạy bộ và yoga.")
    ]
    
    collections_catalog = []
    for i, p in enumerate(collections_slice):
        idx = i + 1
        title, desc = collection_titles[i % len(collection_titles)]
        url = f"{p['base_url']}?w=800&h=600&fit=crop&q=80"
        collections_catalog.append({
            "id": f"coll_{idx:03d}",
            "index": idx,
            "title": f"{title} #{idx}",
            "description": desc,
            "cover_url": url
        })
        
    collections_cat_path = os.path.join(catalog_dir, "real-collections-catalog.json")
    with open(collections_cat_path, "w", encoding="utf-8") as f:
        json.dump(collections_catalog, f, indent=2, ensure_ascii=False)
    print(f"Generated {len(collections_catalog)} collections in {collections_cat_path} (Unique URLs: {len(set(c['cover_url'] for c in collections_catalog))})")

    print("\nALL DIVERSE CATALOGS GENERATED SUCCESSFULLY!")

if __name__ == "__main__":
    build_catalogs()
