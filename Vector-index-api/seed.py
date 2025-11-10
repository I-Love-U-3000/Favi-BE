import os, asyncio, httpx, time

API = os.getenv("API", "http://localhost:8080")

posts = [
    {"post_id":"p1","owner_id":"user_alice","privacy":"Public",
     "image_url":"https://picsum.photos/id/1011/800/600","caption":"Biển xanh và bầu trời trong vắt","alpha":0.5},
    {"post_id":"p2","owner_id":"user_bob","privacy":"Followers",
     "image_url":"https://picsum.photos/id/1035/800/600","caption":"Đi bộ đường dài trong rừng thông mát mẻ","alpha":0.5},
    {"post_id":"p3","owner_id":"user_cara","privacy":"Private",
     "image_url":"https://picsum.photos/id/1025/800/600","caption":"Chó nhỏ đáng yêu nằm trên bãi cỏ","alpha":0.5},
    {"post_id":"p4","owner_id":"user_deno","privacy":"Followers",
     "image_url":"https://picsum.photos/id/1040/800/600","caption":"Hoàng hôn trên núi với mây màu cam","alpha":0.5},
]

async def wait_ready(url: str, path: str = "/healthz", timeout_sec: int = 180):
    start = time.time()
    async with httpx.AsyncClient(timeout=5) as sx:
        while time.time() - start < timeout_sec:
            try:
                r = await sx.get(f"{url}{path}")
                if r.status_code == 200:
                    return
            except Exception:
                pass
            await asyncio.sleep(1)
    raise RuntimeError(f"{url}{path} not ready after {timeout_sec}s")

async def main():
    await wait_ready(API, "/healthz", 180)
    async with httpx.AsyncClient(timeout=120) as sx:
        r = await sx.post(f"{API}/bulk_posts", json={"items": posts, "batch_size": 2})
        print("bulk:", r.status_code, r.text)

if __name__ == "__main__":
    asyncio.run(main())