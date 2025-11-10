import open_clip, torch
model, _, _ = open_clip.create_model_and_transforms('ViT-B-32', pretrained='openai')
tok = open_clip.get_tokenizer('ViT-B-32')(['warmup'])
with torch.no_grad():
    _ = model.encode_text(tok)
print('Warmup done')