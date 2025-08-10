#!/usr/bin/env python3
"""
Script to batch-update MP3 metadata title tag to each file's own filename (excluding extension).

Usage:
    python edit_mp3_metadata.py <folder_path> [--dry-run]

Requires:
    pip install mutagen
"""
import os
import sys
import argparse
from mutagen.easyid3 import EasyID3
from mutagen.id3 import ID3

def set_title_to_filename(file_path, dry_run=False):
    """
    Set the MP3 title tag to the file's base name (without extension).
    """
    base_name = os.path.splitext(os.path.basename(file_path))[0]
    try:
        audio = EasyID3(file_path)
    except Exception:
        audio = ID3()

    current_title = audio.get('title', [''])[0]
    if current_title != base_name:
        print(f"Updating title for '{os.path.basename(file_path)}': '{current_title}' -> '{base_name}'")
        if not dry_run:
            audio['title'] = base_name
            audio.save(file_path)
    else:
        print(f"Title already matches for '{os.path.basename(file_path)}', skipping.")


def process_folder(folder_path, dry_run=False):
    """
    Walk through folder_path and update every .mp3 file found.
    """
    for root, _, files in os.walk(folder_path):
        for file in files:
            if file.lower().endswith('.mp3'):
                full_path = os.path.join(root, file)
                set_title_to_filename(full_path, dry_run)


def main():
    parser = argparse.ArgumentParser(
        description="Set each MP3 file's title metadata to its filename."
    )
    parser.add_argument('folder', help='Directory containing MP3 files')
    parser.add_argument('--dry-run', '-n', action='store_true', help='Preview changes without writing')
    args = parser.parse_args()

    folder = args.folder
    if not os.path.isdir(folder):
        print(f"Error: '{folder}' is not a valid directory.")
        sys.exit(1)

    process_folder(folder, dry_run=args.dry_run)

if __name__ == '__main__':
    main()
