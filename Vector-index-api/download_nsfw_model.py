#!/usr/bin/env python3
"""
Download pre-trained NSFW detection model weights.
This script downloads a ResNet50 model fine-tuned on NSFW content.
"""
import os
import sys
import urllib.request
from pathlib import Path

# URL for pre-trained NSFW model weights (ResNet50 fine-tuned on NSFW data)
# This model was trained on the NSFW dataset with 5 categories:
# safe, suggestive, hentai, pornography, violent
NSFW_MODEL_URL = "https://github.com/youtubeapi/nsfw-detection/releases/download/v1.0/resnet50_nsfw.pth"
MODEL_PATH = "/app/nsfw_model_weights.pth"

def download_file(url, dest_path):
    """Download file with progress bar"""
    print(f"Downloading {url}")
    print(f"Saving to {dest_path}")

    def progress_hook(block_num, block_size, total_size):
        """Show download progress"""
        downloaded = block_num * block_size
        percent = (downloaded / total_size) * 100 if total_size > 0 else 0
        downloaded_mb = downloaded / (1024 * 1024)
        total_mb = total_size / (1024 * 1024)
        sys.stdout.write(f"\rProgress: {percent:.1f}% ({downloaded_mb:.1f}/{total_mb:.1f} MB)")
        sys.stdout.flush()

    try:
        urllib.request.urlretrieve(url, dest_path, reporthook=progress_hook)
        print("\n✓ Download complete!")
        return True
    except Exception as e:
        print(f"\n✗ Download failed: {e}")
        return False

def main():
    print("=" * 60)
    print("NSFW Model Weights Downloader")
    print("=" * 60)

    # Check if model already exists
    if os.path.exists(MODEL_PATH):
        print(f"✓ Model weights already exist at {MODEL_PATH}")
        return 0

    # Create directory if needed
    os.makedirs(os.path.dirname(MODEL_PATH), exist_ok=True)

    # Download the model
    success = download_file(NSFW_MODEL_URL, MODEL_PATH)

    if not success:
        print("\n⚠ Warning: Failed to download NSFW model weights.")
        print("The API will still work but will use ImageNet weights,")
        print("which are NOT accurate for NSFW detection.")
        return 1

    print(f"\n✓ NSFW model weights saved to {MODEL_PATH}")
    print("✓ Ready for accurate NSFW detection!")
    return 0

if __name__ == "__main__":
    sys.exit(main())
