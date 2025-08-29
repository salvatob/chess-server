#!/usr/bin/env python3
import os
import argparse

# Directories to skip entirely (so we don't descend into obj/, bin/, .git/, etc.)
IGNORED_DIRS = {'bin', 'obj', '.git', '.vs'}

def get_args():
    parser = argparse.ArgumentParser(
        description="Walk a C# solution tree, find each project (.csproj), "
                    "and sum up the sizes of all .cs files in that folder."
    )
    parser.add_argument(
        "root",
        help="Path to the root of your solution directory",
        type=str,
    )
    return parser.parse_args()

def main():
    args = get_args()
    root_dir = os.path.abspath(args.root)
    total_all_projects = 0

    for dirpath, dirnames, filenames in os.walk(root_dir):
        # Prune ignored dirs so os.walk won't descend into them
        dirnames[:] = [d for d in dirnames if d not in IGNORED_DIRS]

        # Check for a .csproj file in this folder
        csproj_files = [f for f in filenames if f.lower().endswith('.csproj')]
        if not csproj_files:
            continue

        # Sum sizes of all .cs files in this same folder (not subfolders)
        cs_files = [f for f in filenames if f.lower().endswith('.cs')]
        project_size = sum(
            os.path.getsize(os.path.join(dirpath, csf))
            for csf in cs_files
        )

        proj_name = csproj_files[0]
        print(f"Project '{proj_name}' (at {dirpath}):")
        if cs_files:
            for csf in cs_files:
                sz = os.path.getsize(os.path.join(dirpath, csf))
                print(f"  {csf:30s} {sz:8,d} bytes")
        else:
            print("  (No .cs files found in this folder.)")
        print(f"  → Project total: {project_size:,} bytes\n")

        total_all_projects += project_size

    print("=" * 60)
    print(f"Grand total across all projects: {total_all_projects:,} bytes")

if __name__ == "__main__":
    main()