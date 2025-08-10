import os
import csv

def check_file_readiness(csv_path, image_dir, delimiter, output_file, bahasa):
    missing = []
    ready   = []

    # Read CSV and collect missing filenames
    with open(csv_path, newline='', encoding='utf-8') as csvfile:
        reader = csv.reader(csvfile, delimiter=delimiter)
        for row in reader:
            if len(row) < 4 or all(cell.strip() == "" for cell in row):
                continue
            base_name = row[3].strip()
            filename  = f"{base_name}.png"
            filepath  = os.path.join(image_dir, filename)
            if os.path.isfile(filepath):
                ready.append(filename)
            else:
                missing.append(filename)

    # If output_file is a directory or has no extension, treat it as a folder
    root, ext = os.path.splitext(output_file)
    if (not ext) or os.path.isdir(output_file):
        output_dir = output_file if os.path.isdir(output_file) else os.path.dirname(output_file)
        if not output_dir:
            output_dir = os.getcwd()
        os.makedirs(output_dir, exist_ok=True)
        output_file = os.path.join(output_dir, "missing_assets" + bahasa +".txt")

    # Write missing filenames to the text file
    try:
        with open(output_file, 'w', encoding='utf-8') as f:
            for fname in missing:
                f.write(fname + '\n')
    except IOError as e:
        print(f"Error writing to '{output_file}': {e}")
        return

    # Summary
    total = len(ready) + len(missing)
    print("\n=== File Readiness Report ===")
    print(f"Total checked: {total}")
    print(f" ✔ Ready:   {len(ready)}")
    print(f" ✘ Missing: {len(missing)}")
    print(f"Missing assets list written to: {output_file}")

if __name__ == "__main__":
    bahasa     = input("Enter Bahasa: ").strip()
    csv_path   = input("Enter path to your CSV file: ").strip()
    image_dir  = input("Enter path to your image folder: ").strip()
    delim      = input("Enter CSV delimiter (default is ';'): ").strip() or ";"
    output_txt = input("Enter path or folder for missing assets text file (e.g., missing_assets.txt): ").strip()

    # Validate inputs
    if not os.path.isfile(csv_path):
        print(f"Error: CSV file not found: {csv_path}")
        exit(1)
    if not os.path.isdir(image_dir):
        print(f"Error: Image directory not found: {image_dir}")
        exit(1)

    check_file_readiness(csv_path, image_dir, delim, output_txt, bahasa)
