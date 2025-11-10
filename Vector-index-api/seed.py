import os, asyncio, httpx, time

API = os.getenv("API", "http://localhost:8080")

# Dữ liệu seed mới với nhiều ảnh cho mỗi bài đăng
posts = [
    {
        "post_id": "p1",
        "owner_id": "user_alice",
        "privacy": "Public",
        "image_urls": [
            "https://picsum.photos/id/1011/800/600",  # Biển
            "https://picsum.photos/id/1015/800/600",  # Bầu trời
        ],
        "caption": "Biển xanh và bầu trời trong vắt - chuyến du lịch tuyệt vời",
        "alpha": 0.5
    },
    {
        "post_id": "p2",
        "owner_id": "user_bob",
        "privacy": "Followers",
        "image_urls": [
            "https://picsum.photos/id/1035/800/600",  # Rừng
            "https://picsum.photos/id/1036/800/600",  # Cây cối
            "https://picsum.photos/id/1037/800/600",  # Thiên nhiên
        ],
        "caption": "Đi bộ đường dài trong rừng thông mát mẻ cùng bạn bè",
        "alpha": 0.5
    },
    {
        "post_id": "p3",
        "owner_id": "user_cara",
        "privacy": "Private",
        "image_urls": [
            "https://picsum.photos/id/1025/800/600",  # Chó
        ],
        "caption": "Chó nhỏ đáng yêu nằm trên bãi cỏ",
        "alpha": 0.5
    },
    {
        "post_id": "p4",
        "owner_id": "user_deno",
        "privacy": "Followers",
        "image_urls": [
            "https://picsum.photos/id/1040/800/600",  # Núi
            "https://picsum.photos/id/1041/800/600",  # Hoàng hôn
            "https://picsum.photos/id/1042/800/600",  # Mây
            "https://picsum.photos/id/1043/800/600",  # Cảnh đẹp
        ],
        "caption": "Hoàng hôn trên núi với mây màu cam - khoảnh khắc kỳ diệu",
        "alpha": 0.5
    },
    {
        "post_id": "p5",
        "owner_id": "user_eve",
        "privacy": "Public",
        "image_urls": [
            "https://picsum.photos/id/1050/800/600",  # Thành phố
            "https://picsum.photos/id/1051/800/600",  # Đường phố
        ],
        "caption": "Khám phá thành phố về đêm - ánh đèn lung linh",
        "alpha": 0.6
    },
    {
        "post_id": "p6",
        "owner_id": "user_frank",
        "privacy": "Public",
        "image_urls": [
            "https://picsum.photos/id/1060/800/600",  # Cà phê
            "https://picsum.photos/id/1061/800/600",  # Bánh ngọt
            "https://picsum.photos/id/1062/800/600",  # Đồ uống
            "https://picsum.photos/id/1063/800/600",  # Bàn cafe
            "https://picsum.photos/id/1064/800/600",  # Không gian
        ],
        "caption": "Buổi sáng thư giãn với cà phê và bánh ngọt tại quán yêu thích",
        "alpha": 0.4
    },
    {
        "post_id": "p7",
        "owner_id": "user_alice",
        "privacy": "Followers",
        "image_urls": [
            "https://picsum.photos/id/1070/800/600",  # Hoa
            "https://picsum.photos/id/1071/800/600",  # Vườn
        ],
        "caption": "Vườn hoa mùa xuân nở rộ đủ màu sắc",
        "alpha": 0.55
    },
    {
        "post_id": "p8",
        "owner_id": "user_bob",
        "privacy": "Public",
        "image_urls": [
            "https://picsum.photos/id/1080/800/600",  # Xe đạp
        ],
        "caption": "Đạp xe quanh công viên - tập thể dục buổi sáng",
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
    Seed dữ liệu mẫu với nhiều ảnh
    """
    print("🌱 Bắt đầu seed dữ liệu...")
    print(f"📍 API URL: {API}")
    
    # Đợi API sẵn sàng
    await wait_ready(API, "/healthz", 180)
    
    # Seed dữ liệu
    async with httpx.AsyncClient(timeout=120) as sx:
        print(f"📤 Đang gửi {len(posts)} bài đăng...")
        print(f"   Tổng số ảnh: {sum(len(p['image_urls']) for p in posts)}")
        
        r = await sx.post(
            f"{API}/bulk_posts",
            json={"items": posts, "batch_size": 2}
        )
        
        if r.status_code == 200:
            result = r.json()
            print(f"✅ Seed thành công!")
            print(f"   - Đã insert: {result.get('inserted', 0)} bài")
            print(f"   - Batch size: {result.get('batch_size', 0)}")
        else:
            print(f"❌ Lỗi: {r.status_code}")
            print(f"   {r.text}")

if __name__ == "__main__":
    asyncio.run(main())