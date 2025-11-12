import os, asyncio, httpx, time

API = os.getenv("API", "http://localhost:8080")

# Dữ liệu seed với ảnh và caption LIÊN QUAN NHAU
posts = [
    # ===== CHUYẾN ĐI BIỂN =====
    {
        "post_id": "p1",
        "owner_id": "user_alice",
        "privacy": "Public",
        "image_urls": [
            "https://picsum.photos/id/1011/800/600",  # River/water landscape
            "https://picsum.photos/id/1018/800/600",  # Mountain path
        ],
        "caption": "Chuyến đi biển cuối tuần - sóng biển, bầu trời xanh và không khí trong lành",
        "alpha": 0.5
    },
    
    # ===== THIÊN NHIÊN RỪNG XANH =====
    {
        "post_id": "p2",
        "owner_id": "user_bob",
        "privacy": "Followers",
        "image_urls": [
            "https://picsum.photos/id/1019/800/600",  # Mountain landscape
            "https://picsum.photos/id/1020/800/600",  # Nature scene
            "https://picsum.photos/id/1021/800/600",  # Forest path
        ],
        "caption": "Trekking trong rừng thông - không khí mát lành, tiếng chim hót và cây cối xanh mướt",
        "alpha": 0.5
    },
    
    # ===== THÚ CƯNG DỄ THƯƠNG =====
    {
        "post_id": "p3",
        "owner_id": "user_cara",
        "privacy": "Private",
        "image_urls": [
            "https://picsum.photos/id/1025/800/600",  # Dog/pet
        ],
        "caption": "Cún cưng của mình - đáng yêu, ngoan ngoãn và rất thích chơi đùa",
        "alpha": 0.5
    },
    
    # ===== HOÀNG HÔN ĐẸP =====
    {
        "post_id": "p4",
        "owner_id": "user_deno",
        "privacy": "Followers",
        "image_urls": [
            "https://picsum.photos/id/1031/800/600",  # Sunset/dawn scene
            "https://picsum.photos/id/1033/800/600",  # Sky view
            "https://picsum.photos/id/1036/800/600",  # Landscape
        ],
        "caption": "Hoàng hôn tuyệt đẹp trên núi - bầu trời đỏ cam, mây lững lờ và ánh nắng vàng",
        "alpha": 0.5
    },
    
    # ===== ĐÔ THỊ VỀ ĐÊM =====
    {
        "post_id": "p5",
        "owner_id": "user_eve",
        "privacy": "Public",
        "image_urls": [
            "https://picsum.photos/id/1034/800/600",  # Urban scene
            "https://picsum.photos/id/1037/800/600",  # City view
        ],
        "caption": "Thành phố về đêm - đèn neon lung linh, đường phố nhộn nhịp và cuộc sống sôi động",
        "alpha": 0.6
    },
    
    # ===== CAFE SÁNG =====
    {
        "post_id": "p6",
        "owner_id": "user_frank",
        "privacy": "Public",
        "image_urls": [
            "https://picsum.photos/id/1060/800/600",  # Coffee/food
            "https://picsum.photos/id/1061/800/600",  # Cafe setting
            "https://picsum.photos/id/1062/800/600",  # Breakfast
        ],
        "caption": "Bữa sáng thư giãn - cà phê đậm đà, bánh mì giòn tan và không gian quán cafe ấm cúng",
        "alpha": 0.4
    },
    
    # ===== VƯỜN HOA MÙA XUÂN =====
    {
        "post_id": "p7",
        "owner_id": "user_alice",
        "privacy": "Followers",
        "image_urls": [
            "https://picsum.photos/id/1068/800/600",  # Flowers/garden
            "https://picsum.photos/id/1069/800/600",  # Nature close-up
        ],
        "caption": "Vườn hoa mùa xuân - hoa đủ màu sắc, hương thơm ngát và ong bướm bay lượn",
        "alpha": 0.55
    },
    
    # ===== THỂ THAO BUỔI SÁNG =====
    {
        "post_id": "p8",
        "owner_id": "user_bob",
        "privacy": "Public",
        "image_urls": [
            "https://picsum.photos/id/1073/800/600",  # Activity/sport
        ],
        "caption": "Đạp xe quanh công viên - tập thể dục buổi sáng, không khí trong lành và cảm giác sảng khoái",
        "alpha": 0.5
    },
    
    # ===== ẨM THỰC ĐƯỜNG PHỐ =====
    {
        "post_id": "p9",
        "owner_id": "user_cara",
        "privacy": "Public",
        "image_urls": [
            "https://picsum.photos/id/1080/800/600",  # Food/street
            "https://picsum.photos/id/1081/800/600",  # Cuisine
        ],
        "caption": "Ăn vặt đường phố - món ngon, giá rẻ và không gian ẩm thực địa phương",
        "alpha": 0.5
    },
    
    # ===== LÀM VIỆC REMOTE =====
    {
        "post_id": "p10",
        "owner_id": "user_eve",
        "privacy": "Public",
        "image_urls": [
            "https://picsum.photos/id/0/800/600",      # Laptop/workspace
        ],
        "caption": "Làm việc từ xa tại quán cafe - laptop, wifi nhanh và không gian yên tĩnh",
        "alpha": 0.5
    },
    
    # ===== KIẾN TRÚC CỔ =====
    {
        "post_id": "p11",
        "owner_id": "user_frank",
        "privacy": "Followers",
        "image_urls": [
            "https://picsum.photos/id/10/800/600",     # Architecture/building
            "https://picsum.photos/id/15/800/600",     # Historic structure
        ],
        "caption": "Khám phá kiến trúc cổ - tòa nhà lịch sử, chi tiết tinh xảo và vẻ đẹp thời gian",
        "alpha": 0.6
    },
    
    # ===== PICNIC CÔNG VIÊN =====
    {
        "post_id": "p12",
        "owner_id": "user_alice",
        "privacy": "Public",
        "image_urls": [
            "https://picsum.photos/id/20/800/600",     # Park/outdoor
            "https://picsum.photos/id/21/800/600",     # Nature setting
            "https://picsum.photos/id/22/800/600",     # Leisure
        ],
        "caption": "Dã ngoại cuối tuần - bạn bè, đồ ăn ngon và trò chơi vui nhộn trên bãi cỏ xanh",
        "alpha": 0.5
    },
    
    # ===== NHIẾP ẢNH MACRO =====
    {
        "post_id": "p13",
        "owner_id": "user_bob",
        "privacy": "Public",
        "image_urls": [
            "https://picsum.photos/id/25/800/600",     # Close-up/macro
        ],
        "caption": "Nhiếp ảnh cận cảnh - chi tiết nhỏ, màu sắc sống động và góc nhìn độc đáo",
        "alpha": 0.7
    },
    
    # ===== ĐỌC SÁCH THƯ GIÃN =====
    {
        "post_id": "p14",
        "owner_id": "user_deno",
        "privacy": "Followers",
        "image_urls": [
            "https://picsum.photos/id/24/800/600",     # Reading/books
            "https://picsum.photos/id/26/800/600",     # Cozy setting
        ],
        "caption": "Đọc sách cuối tuần - câu chuyện hấp dẫn, không gian yên tĩnh và tách trà nóng",
        "alpha": 0.5
    },
    
    # ===== DU LỊCH MẠO HIỂM =====
    {
        "post_id": "p15",
        "owner_id": "user_cara",
        "privacy": "Public",
        "image_urls": [
            "https://picsum.photos/id/30/800/600",     # Adventure/travel
            "https://picsum.photos/id/31/800/600",     # Exploration
            "https://picsum.photos/id/32/800/600",     # Journey
            "https://picsum.photos/id/33/800/600",     # Discovery
        ],
        "caption": "Hành trình khám phá - địa điểm mới, trải nghiệm thú vị và kỷ niệm đáng nhớ",
        "alpha": 0.5
    },
]

async def wait_ready(url: str, path: str = "/healthz", timeout_sec: int = 180):
    """
    Đợi API sẵn sàng trước khi seed dữ liệu
    """
    start = time.time()
    async with httpx.AsyncClient(timeout=5) as sx:
        while time.time() - start < timeout_sec:
            try:
                r = await sx.get(f"{url}{path}")
                if r.status_code == 200:
                    print(f"✓ API sẵn sàng tại {url}")
                    return
            except Exception as e:
                pass
            await asyncio.sleep(1)
    raise RuntimeError(f"{url}{path} không sẵn sàng sau {timeout_sec}s")

async def main():
    """
    Seed dữ liệu mẫu với nhiều ảnh và caption liên quan
    """
    print("🌱 Bắt đầu seed dữ liệu...")
    print(f"📍 API URL: {API}")
    
    # Đợi API sẵn sàng
    await wait_ready(API, "/healthz", 180)
    
    # Seed dữ liệu
    async with httpx.AsyncClient(timeout=180) as sx:  # Tăng timeout vì có nhiều bài hơn
        print(f"📤 Đang gửi {len(posts)} bài đăng...")
        print(f"   Tổng số ảnh: {sum(len(p['image_urls']) for p in posts)}")
        
        # Thêm thông tin các chủ đề
        print(f"\n📚 Các chủ đề:")
        print(f"   - Biển/thiên nhiên: 4 bài")
        print(f"   - Ẩm thực/cafe: 3 bài")
        print(f"   - Thành phố/kiến trúc: 2 bài")
        print(f"   - Hoạt động/thể thao: 2 bài")
        print(f"   - Đời sống/giải trí: 4 bài")
        
        r = await sx.post(
            f"{API}/bulk_posts",
            json={"items": posts, "batch_size": 3}
        )
        
        if r.status_code == 200:
            result = r.json()
            print(f"\n✅ Seed thành công!")
            print(f"   - Đã insert: {result.get('inserted', 0)} bài")
            print(f"   - Batch size: {result.get('batch_size', 0)}")
            
            print(f"\n🔍 Test search:")
            print(f"   - 'biển đẹp' → nên trả về p1 (chuyến đi biển)")
            print(f"   - 'cafe sáng' → nên trả về p6, p10 (cafe/làm việc)")
            print(f"   - 'thiên nhiên' → nên trả về p2, p7 (rừng/vườn hoa)")
            print(f"   - 'hoàng hôn' → nên trả về p4 (hoàng hôn núi)")
            print(f"   - 'ăn uống' → nên trả về p6, p9 (cafe/ẩm thực)")
        else:
            print(f"❌ Lỗi: {r.status_code}")
            print(f"   {r.text}")

if __name__ == "__main__":
    asyncio.run(main())