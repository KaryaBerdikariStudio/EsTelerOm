import os
import sys

def fix_filename_casing(folder_path):
    for filename in os.listdir(folder_path):
        old_path = os.path.join(folder_path, filename)
        if os.path.isdir(old_path):
            continue

        name, ext = os.path.splitext(filename)
        new_name = name.title()
        new_filename = new_name + ext
        new_path = os.path.join(folder_path, new_filename)

        if old_path != new_path:
            os.rename(old_path, new_path)
            print(f"Renamed: {filename} -> {new_filename}")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python filenamerfix.py <folder_path>")
        sys.exit(1)

    folder = sys.argv[1]
    if not os.path.exists(folder):
        print("Folder does not exist.")
        sys.exit(1)

    fix_filename_casing(folder)
