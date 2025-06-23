import os

# 排除的文件夹名（忽略大小写）
exclude_folders = {'bin', '.vs', 'obj'}

# 输出文件名
output_file = 'all_files.txt'

with open(output_file, 'w', encoding='utf-8') as outfile:
    for root, dirs, files in os.walk('.'):
        # 修改dirs可以跳过不想遍历的文件夹
        dirs[:] = [d for d in dirs if d.lower() not in exclude_folders]
        for filename in files:
            filepath = os.path.join(root, filename)
            # 跳过输出文件本身
            if os.path.abspath(filepath) == os.path.abspath(output_file):
                continue
            try:
                with open(filepath, 'r', encoding='utf-8') as infile:
                    content = infile.read()
            except Exception as e:
                content = f"[无法读取文件: {e}]"
            outfile.write(f"{filepath}：\n【{content}】\n\n")
print(f"文件内容已输出到 {output_file}")
