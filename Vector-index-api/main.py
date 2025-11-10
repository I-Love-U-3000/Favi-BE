import uuid, time, os, io
from collections import defaultdict, deque
from typing import Optional, List
from fastapi import FastAPI, HTTPException, Request, Depends
from pydantic import BaseModel, Field
import httpx

import torch
import torch.nn.functional as F
import open_clip
from PIL import Image
import requests

from qdrant_client import QdrantClient
from qdrant_client.http.models import VectorParams, Distance, Filter, FieldCondition, MatchAny, MatchValue, SearchParams

QDRANT_URL = os.getenv("QDRANT_URL", "http://localhost:6333")
FRIENDS_URL = os.getenv("FRIENDS_URL", "http://localhost:8002")
COLLECTION = os.getenv("COLLECTION_NAME", "posts_demo")
MODEL_NAME = os.getenv("MODEL_NAME", "ViT-B-32")
PRETRAINED = os.getenv("PRETRAINED", "openai")

RL_SEARCH = int(os.getenv("RL_SEARCH_PER_MIN", "60"))
RL_POST = int(os.getenv("RL_POST_PER_MIN", "20"))
RL_BULK = int(os.getenv("RL_BULK_PER_MIN", "6"))
BULK_EMBED_BATCH = int(os.getenv("BULK_EMBED_BATCH", "32"))

app = FastAPI(title="Multimodal Retrieval API (Monolith)", version="2.1")

device = "cuda" if torch.cuda.is_available() else "cpu"
model, _, preprocess = open_clip.create_model_and_transforms(MODEL_NAME, pretrained=PRETRAINED)
tokenizer = open_clip.get_tokenizer(MODEL_NAME)
model = model.to(device).eval()

def l2_normalize(x: torch.Tensor) -> torch.Tensor:
    return F.normalize(x, p=2, dim=-1)

def encode_text_vec(text: str):
    with torch.no_grad():
        toks = tokenizer([text]).to(device)
        z = model.encode_text(toks)
        z = l2_normalize(z)
    return z[0].detach().cpu().numpy().tolist()

def encode_image_vec(image_url: str):
    resp = requests.get(image_url, timeout=30)
    resp.raise_for_status()
    img = Image.open(io.BytesIO(resp.content)).convert("RGB")
    with torch.no_grad():
        x = preprocess(img).unsqueeze(0).to(device)
        z = model.encode_image(x)
        z = l2_normalize(z)
    return z[0].detach().cpu().numpy().tolist()

def encode_post_vec(image_urls: List[str], caption: str, alpha: float):
    """
    Encode bài đăng với NHIỀU ảnh.
    
    Args:
        image_urls: List các URL ảnh (có thể 1 hoặc nhiều ảnh)
        caption: Caption của bài đăng
        alpha: Trọng số giữa ảnh và caption (0-1)
    
    Returns:
        Vector embedding đã được normalize
    """
    if not image_urls:
        raise ValueError("Phải có ít nhất 1 ảnh")
    
    with torch.no_grad():
        # Encode tất cả các ảnh
        image_embeddings = []
        for img_url in image_urls:
            resp = requests.get(img_url, timeout=30)
            resp.raise_for_status()
            img = Image.open(io.BytesIO(resp.content)).convert("RGB")
            x = preprocess(img).unsqueeze(0).to(device)
            zi = model.encode_image(x)
            zi = l2_normalize(zi)
            image_embeddings.append(zi)
        
        # Tính trung bình các embedding ảnh
        zi_avg = torch.mean(torch.stack(image_embeddings), dim=0)
        zi_avg = l2_normalize(zi_avg)
        
        # Encode caption nếu có
        if caption and caption.strip():
            toks = tokenizer([caption]).to(device)
            zt = model.encode_text(toks)
            zt = l2_normalize(zt)
        else:
            zt = torch.zeros_like(zi_avg)
        
        # Kết hợp ảnh và caption theo alpha
        a = float(alpha)
        z = l2_normalize(a * zi_avg + (1 - a) * zt)
    
    return z[0].detach().cpu().numpy().tolist()

client = QdrantClient(url=QDRANT_URL)

def ensure_collection():
    collections = [c.name for c in client.get_collections().collections]
    if COLLECTION not in collections:
        client.create_collection(
            collection_name=COLLECTION,
            vectors_config=VectorParams(size=512, distance=Distance.COSINE)
        )

ensure_collection()

class Limiter:
    def __init__(self, max_per_min: int):
        self.max = max_per_min
        self.buckets = defaultdict(deque)

    def allow(self, key: str) -> bool:
        now = time.time()
        dq = self.buckets[key]
        while dq and now - dq[0] > 60:
            dq.popleft()
        if len(dq) >= self.max:
            return False
        dq.append(now)
        return True

limiter_search = Limiter(RL_SEARCH)
limiter_post = Limiter(RL_POST)
limiter_bulk = Limiter(RL_BULK)

def rate_limit(limiter: Limiter):
    async def dep(request: Request):
        user_id = request.query_params.get("user_id") or request.headers.get("X-User-Id") or ""
        ip = request.client.host if request.client else "unknown"
        key = user_id or ip
        if not limiter.allow(key):
            raise HTTPException(429, "Rate limit exceeded")
    return dep

class PostIn(BaseModel):
    post_id: str = Field(..., description="ID bài viết")
    owner_id: str = Field(..., description="ID owner")
    privacy: str = Field(..., pattern="^(Public|Followers|Private)$")
    image_urls: List[str] = Field(..., description="List URL các ảnh (ít nhất 1)")
    caption: Optional[str] = ""
    alpha: float = 0.5
    
    class Config:
        json_schema_extra = {
            "example": {
                "post_id": "post123",
                "owner_id": "user456",
                "privacy": "Public",
                "image_urls": [
                    "https://picsum.photos/id/1011/800/600",
                    "https://picsum.photos/id/1012/800/600"
                ],
                "caption": "Chuyến đi biển tuyệt vời",
                "alpha": 0.5
            }
        }

class BulkIn(BaseModel):
    items: List[PostIn]
    batch_size: Optional[int] = None

class SearchOut(BaseModel):
    post_id: str
    owner_id: str
    privacy: str
    image_urls: List[str]
    caption: str
    score: float

async def get_friends(user_id: str) -> List[str]:
    if not user_id:
        return []
    async with httpx.AsyncClient(timeout=10) as sx:
        resp = await sx.get(f"{FRIENDS_URL}/friends/{user_id}")
    if resp.status_code != 200:
        return []
    return resp.json().get("friends", [])

def to_point_id(post_id: str) -> str:
    try:
        return str(uuid.UUID(post_id))
    except ValueError:
        return str(uuid.uuid5(uuid.NAMESPACE_DNS, post_id))

@app.get("/healthz")
async def healthz():
    return {"ok": True}

@app.get("/depsz")
async def depsz():
    try:
        _ = client.get_collections()
        return {"ok": True}
    except Exception as e:
        raise HTTPException(status_code=503, detail=str(e))

@app.post("/posts", dependencies=[Depends(rate_limit(limiter_post))])
async def index_post(body: PostIn):
    """
    Index một bài đăng với nhiều ảnh.
    """
    ensure_collection()
    
    if not body.image_urls:
        raise HTTPException(400, "Phải có ít nhất 1 ảnh")
    
    vec = encode_post_vec(body.image_urls, body.caption or "", body.alpha)
    payload = {
        "post_id": body.post_id,
        "owner_id": body.owner_id,
        "privacy": body.privacy,
        "image_urls": body.image_urls,
        "caption": body.caption or ""
    }
    client.upsert(
        collection_name=COLLECTION,
        points=[{
            "id": to_point_id(body.post_id),
            "vector": vec,
            "payload": payload
        }]
    )
    return {"ok": True, "post_id": body.post_id, "image_count": len(body.image_urls)}

@app.post("/bulk_posts", dependencies=[Depends(rate_limit(limiter_bulk))])
async def bulk_posts(body: BulkIn):
    """
    Bulk index nhiều bài đăng, mỗi bài có thể có nhiều ảnh.
    """
    ensure_collection()
    items = body.items or []
    bsz = body.batch_size or BULK_EMBED_BATCH
    if bsz <= 0:
        bsz = 32
    inserted = 0
    for i in range(0, len(items), bsz):
        chunk = items[i:i+bsz]
        vectors, payloads, ids = [], [], []
        for p in chunk:
            if not p.image_urls:
                continue  # Skip bài không có ảnh
            vec = encode_post_vec(p.image_urls, p.caption or "", p.alpha)
            vectors.append(vec)
            payloads.append({
                "post_id": p.post_id,
                "owner_id": p.owner_id,
                "privacy": p.privacy,
                "image_urls": p.image_urls,
                "caption": p.caption or ""
            })
            ids.append(to_point_id(p.post_id))
        client.upsert(
            collection_name=COLLECTION,
            points=[{"id": i, "vector": v, "payload": pl} for i, v, pl in zip(ids, vectors, payloads)]
        )
        inserted += len(chunk)
    return {"ok": True, "inserted": inserted, "batch_size": bsz}

@app.get("/search", response_model=List[SearchOut], dependencies=[Depends(rate_limit(limiter_search))])
async def search(user_id: Optional[str] = None, q: str = "", k: int = 10):
    ensure_collection()
    qvec = encode_text_vec(q or "")
    friends: List[str] = await get_friends(user_id) if user_id else []

    should = []

    # 1) Ai cũng thấy bài Public
    should.append(FieldCondition(key="privacy", match=MatchValue(value="Public")))

    # 2) Chủ bài luôn thấy bài của CHÍNH MÌNH (bất kể privacy)
    if user_id:
        should.append(FieldCondition(key="owner_id", match=MatchValue(value=user_id)))

    # 3) Followers: owner ∈ (friends(user) ∪ {user})
    if user_id:
        owners_ok = (friends or []) + [user_id]
        should.append(
            Filter(must=[
                FieldCondition(key="privacy", match=MatchValue(value="Followers")),
                FieldCondition(key="owner_id", match=MatchAny(any=owners_ok)),
            ])
        )

    query_filter = Filter(should=should) if should else None

    res = client.search(
        collection_name=COLLECTION,
        query_vector=qvec,
        limit=k,
        with_payload=True,
        score_threshold=None,
        query_filter=query_filter,
        search_params=SearchParams(hnsw_ef=128),
    )

    outputs: List[SearchOut] = []
    for p in res:
        pl = p.payload or {}
        outputs.append(SearchOut(
            post_id=str(pl.get("post_id")),
            owner_id=str(pl.get("owner_id")),
            privacy=str(pl.get("privacy")),
            image_urls=pl.get("image_urls", []),
            caption=str(pl.get("caption","")),
            score=float(p.score)
        ))
    return outputs