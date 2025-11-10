FROM python:3.11-slim

RUN apt-get update && apt-get install -y --no-install-recommends     git libglib2.0-0 libsm6 libxext6 libxrender1 ca-certificates     && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY requirements.txt /app/requirements.txt
RUN pip install --no-cache-dir --upgrade pip &&     pip install --no-cache-dir --extra-index-url https://download.pytorch.org/whl/cpu       torch==2.4.1 torchvision==0.19.1 torchaudio==2.4.1 &&     pip install --no-cache-dir -r requirements.txt

COPY warmup.py /app/warmup.py
RUN python /app/warmup.py

COPY main.py /app/main.py
COPY seed.py /app/seed.py

ENV QDRANT_URL=http://qdrant:6333
ENV FRIENDS_URL=http://mock_friends:8002
ENV COLLECTION_NAME=posts_demo
ENV MODEL_NAME=ViT-B-32
ENV PRETRAINED=openai
ENV RL_SEARCH_PER_MIN=60
ENV RL_POST_PER_MIN=20
ENV RL_BULK_PER_MIN=6
ENV BULK_EMBED_BATCH=32

EXPOSE 8080
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8080"]