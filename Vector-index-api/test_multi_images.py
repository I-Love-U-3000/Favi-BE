"""
Test script cho API với hỗ trợ nhiều ảnh
"""
import asyncio
import httpx

API_URL = "http://localhost:8080"

async def test_single_image():
    """Test với 1 ảnh (như cũ)"""
    print("\n" + "="*60)
    print("TEST 1: Bài đăng với 1 ảnh")
    print("="*60)
    
    post_data = {
        "post_id": "test_single",
        "owner_id": "test_user",
        "privacy": "Public",
        "image_urls": ["https://picsum.photos/id/100/800/600"],
        "caption": "Test với một ảnh duy nhất",
        "alpha": 0.5
    }
    
    async with httpx.AsyncClient(timeout=60) as client:
        response = await client.post(f"{API_URL}/posts", json=post_data)
        print(f"Status: {response.status_code}")
        print(f"Response: {response.json()}")
    
    return response.status_code == 200

async def test_multiple_images():
    """Test với nhiều ảnh"""
    print("\n" + "="*60)
    print("TEST 2: Bài đăng với 3 ảnh")
    print("="*60)
    
    post_data = {
        "post_id": "test_multi",
        "owner_id": "test_user",
        "privacy": "Public",
        "image_urls": [
            "https://picsum.photos/id/101/800/600",
            "https://picsum.photos/id/102/800/600",
            "https://picsum.photos/id/103/800/600"
        ],
        "caption": "Test với ba ảnh khác nhau - biển, núi, rừng",
        "alpha": 0.5
    }
    
    async with httpx.AsyncClient(timeout=60) as client:
        response = await client.post(f"{API_URL}/posts", json=post_data)
        print(f"Status: {response.status_code}")
        print(f"Response: {response.json()}")
    
    return response.status_code == 200

async def test_five_images():
    """Test với 5 ảnh"""
    print("\n" + "="*60)
    print("TEST 3: Bài đăng với 5 ảnh")
    print("="*60)
    
    post_data = {
        "post_id": "test_five",
        "owner_id": "test_user",
        "privacy": "Public",
        "image_urls": [
            "https://picsum.photos/id/104/800/600",
            "https://picsum.photos/id/10/800/600",
            "https://picsum.photos/id/106/800/600",
            "https://picsum.photos/id/107/800/600",
            "https://picsum.photos/id/108/800/600"
        ],
        "caption": "Test với năm ảnh - album du lịch",
        "alpha": 0.6
    }
    
    async with httpx.AsyncClient(timeout=180) as client:
        response = await client.post(f"{API_URL}/posts", json=post_data)
        print(f"Status: {response.status_code}")
        print(f"Response: {response.json()}")
    
    return response.status_code == 200

async def test_bulk_mixed_images():
    """Test bulk với số lượng ảnh khác nhau"""
    print("\n" + "="*60)
    print("TEST 4: Bulk insert với số ảnh khác nhau")
    print("="*60)
    
    bulk_data = {
        "items": [
            {
                "post_id": "bulk_1img",
                "owner_id": "user1",
                "privacy": "Public",
                "image_urls": ["https://picsum.photos/id/110/800/600"],
                "caption": "Bulk - 1 ảnh",
                "alpha": 0.5
            },
            {
                "post_id": "bulk_2img",
                "owner_id": "user2",
                "privacy": "Public",
                "image_urls": [
                    "https://picsum.photos/id/111/800/600",
                    "https://picsum.photos/id/112/800/600"
                ],
                "caption": "Bulk - 2 ảnh",
                "alpha": 0.5
            },
            {
                "post_id": "bulk_4img",
                "owner_id": "user3",
                "privacy": "Public",
                "image_urls": [
                    "https://picsum.photos/id/113/800/600",
                    "https://picsum.photos/id/114/800/600",
                    "https://picsum.photos/id/115/800/600",
                    "https://picsum.photos/id/116/800/600"
                ],
                "caption": "Bulk - 4 ảnh",
                "alpha": 0.5
            }
        ],
        "batch_size": 2
    }
    
    async with httpx.AsyncClient(timeout=120) as client:
        response = await client.post(f"{API_URL}/bulk_posts", json=bulk_data)
        print(f"Status: {response.status_code}")
        print(f"Response: {response.json()}")
    
    return response.status_code == 200

async def test_search():
    """Test search để xem kết quả có đúng format không"""
    print("\n" + "="*60)
    print("TEST 5: Search và kiểm tra response format")
    print("="*60)
    
    async with httpx.AsyncClient(timeout=30) as client:
        # Search với query text
        response = await client.get(
            f"{API_URL}/search",
            params={"q": "ảnh đẹp", "k": 5}
        )
        print(f"Status: {response.status_code}")
        
        if response.status_code == 200:
            results = response.json()
            print(f"Số kết quả: {len(results)}")
            
            # Kiểm tra format của kết quả đầu tiên
            if results:
                print("\n📋 Kết quả đầu tiên:")
                first = results[0]
                print(f"  - post_id: {first.get('post_id')}")
                print(f"  - owner_id: {first.get('owner_id')}")
                print(f"  - privacy: {first.get('privacy')}")
                print(f"  - image_urls: {first.get('image_urls')}")
                print(f"  - caption: {first.get('caption')}")
                print(f"  - score: {first.get('score')}")
                
                # Kiểm tra image_urls là list
                if isinstance(first.get('image_urls'), list):
                    print(f"  ✅ image_urls là list với {len(first['image_urls'])} ảnh")
                else:
                    print(f"  ❌ image_urls KHÔNG phải list!")
                    return False
        
    return response.status_code == 200

async def test_error_empty_images():
    """Test lỗi khi không có ảnh"""
    print("\n" + "="*60)
    print("TEST 6: Error handling - không có ảnh")
    print("="*60)
    
    post_data = {
        "post_id": "test_error",
        "owner_id": "test_user",
        "privacy": "Public",
        "image_urls": [],  # Rỗng - phải báo lỗi
        "caption": "Test lỗi không có ảnh",
        "alpha": 0.5
    }
    
    async with httpx.AsyncClient(timeout=30) as client:
        response = await client.post(f"{API_URL}/posts", json=post_data)
        print(f"Status: {response.status_code}")
        print(f"Response: {response.text}")
        
        # Expect lỗi 400 hoặc 422
        if response.status_code in [400, 422]:
            print("✅ API đã từ chối đúng cách")
            return True
        else:
            print("❌ API không báo lỗi như mong đợi")
            return False

async def check_health():
    """Kiểm tra API có sẵn sàng không"""
    print("\n🏥 Kiểm tra health...")
    try:
        async with httpx.AsyncClient(timeout=5) as client:
            response = await client.get(f"{API_URL}/healthz")
            if response.status_code == 200:
                print("✅ API đang hoạt động")
                return True
            else:
                print(f"❌ API trả về status {response.status_code}")
                return False
    except Exception as e:
        print(f"❌ Không thể kết nối API: {e}")
        return False

async def main():
    """Chạy tất cả tests"""
    print("""
╔════════════════════════════════════════════════════════════╗
║               TEST SUITE - Multi-Image Posts               ║
║                                                            ║
║  Testing API với hỗ trợ nhiều ảnh cho mỗi bài đăng         ║
╚════════════════════════════════════════════════════════════╝
    """)
    
    # Kiểm tra API trước
    if not await check_health():
        print("\n❌ API không sẵn sàng. Vui lòng khởi động API trước.")
        return
    
    # Chạy các tests
    tests = [
        ("Single Image", test_single_image),
        ("Multiple Images (3)", test_multiple_images),
        ("Five Images", test_five_images),
        ("Bulk Mixed Images", test_bulk_mixed_images),
        ("Search Format", test_search),
        ("Error Handling", test_error_empty_images),
    ]
    
    results = []
    for name, test_func in tests:
        try:
            result = await test_func()
            results.append((name, result))
        except Exception as e:
            print(f"❌ Lỗi trong test '{name}': {e}")
            results.append((name, False))
        
        # Delay giữa các tests
        await asyncio.sleep(2)
    
    # Summary
    print("\n" + "="*60)
    print("📊 KẾT QUẢ TEST")
    print("="*60)
    
    passed = sum(1 for _, result in results if result)
    total = len(results)
    
    for name, result in results:
        status = "✅ PASS" if result else "❌ FAIL"
        print(f"{status}  {name}")
    
    print("="*60)
    print(f"Tổng: {passed}/{total} tests passed")
    
    if passed == total:
        print("🎉 TẤT CẢ TESTS ĐỀU PASS!")
    else:
        print("⚠️  Có tests thất bại, vui lòng kiểm tra lại")

if __name__ == "__main__":
    asyncio.run(main())