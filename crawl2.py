import requests
from bs4 import BeautifulSoup
import json
import re
import sys

def smart_trim_definition(defn, max_len):
    """智能裁剪释义，避免截断单词或在不合适的地方结束。"""
    if len(defn) <= max_len:
        return defn
    cut_str = defn[:max_len]
    last_semicolon = cut_str.rfind('；')
    if last_semicolon != -1:
        return defn[:last_semicolon] + "..."
    else:
        return cut_str.rstrip("；，,") + "..."

def get_word_card(word, max_definitions=5, max_def_length=30, max_sentences=3, max_phrases=3):
    """
    从有道词典获取单词信息卡片。
    """
    url = f"https://dict.youdao.com/w/{word}"
    headers = {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36',
        'Referer': 'https://dict.youdao.com/',
    }

    try:
        response = requests.get(url, headers=headers, timeout=10)
        response.raise_for_status()
    except requests.RequestException as e:
        return None

    soup = BeautifulSoup(response.text, 'html.parser')

    word_card = {
        "word": word,
        "phonetic": "N/A",
        "source": "N/A",
        "audio_url": None,
        "definitions": [],
        "phrases": [],
        "sentences": []
    }

    # --- 1. 抓取基础释义和音标 ---
    basic_def_container = soup.select_one('#phrsListTab .trans-container')
    if basic_def_container:
        word_card["source"] = "Youdao Basic"
        phonetic_nodes = soup.select('#phrsListTab .pronounce')
        phonetic_texts = [node.get_text(strip=True) for node in phonetic_nodes]
        if phonetic_texts:
            word_card["phonetic"] = " | ".join(phonetic_texts)
        
        simple_defs = basic_def_container.select('ul li')
        for li in simple_defs[:max_definitions]:
            full_text = li.get_text(strip=True)
            match = re.match(r'^([a-z]+\.)\s*(.*)', full_text, re.IGNORECASE)
            if match:
                pos, defn = match.groups()
            else:
                pos, defn = "N/A", full_text
            trimmed_defn = smart_trim_definition(defn, max_def_length)
            word_card["definitions"].append({
                "part_of_speech": pos,
                "definition": trimmed_defn,
            })

    # --- 2. 抓取发音音频 ---
    audio_us = soup.select_one('.pronounce a[data-rel*="type=2"]')
    if audio_us and audio_us.get('data-rel'):
        word_card["audio_url"] = f"http://dict.youdao.com/dictvoice?audio={word}&type=2"
    else:
        audio_uk = soup.select_one('.pronounce a[data-rel*="type=1"]')
        if audio_uk and audio_uk.get('data-rel'):
            word_card["audio_url"] = f"http://dict.youdao.com/dictvoice?audio={word}&type=1"

    # --- 3. 抓取柯林斯例句 ---
    collins_result = soup.select_one('#collinsResult')
    if collins_result:
        expression = collins_result.select_one('h4 .title')
        if expression:
            word_card["word"] = expression.get_text(strip=True)
        phonetic = collins_result.select_one('h4 .phonetic')
        if phonetic:
            word_card["phonetic"] = phonetic.get_text(strip=True)
        lis = collins_result.select('.ol li')
        for li in lis:
            example_en_node = li.select_one('.exampleLists .examples p:nth-of-type(1)')
            example_cn_node = li.select_one('.exampleLists .examples p:nth-of-type(2)')
            if example_en_node and example_cn_node:
                word_card["sentences"].append({
                    "example_en": example_en_node.get_text(strip=True),
                    "example_cn": example_cn_node.get_text(strip=True)
                })
            if len(word_card["sentences"]) >= max_sentences:
                break
    
    # --- 4. 抓取网络短语 ---
    web_phrase_container = soup.select_one('#webPhrase')
    if web_phrase_container:
        phrase_nodes = web_phrase_container.select('.wordGroup')
        for node in phrase_nodes[:max_phrases]:
            phrase_title_node = node.select_one('.contentTitle')
            if phrase_title_node:
                phrase_title = phrase_title_node.get_text(strip=True)
                full_text = node.get_text(" ", strip=True)
                raw_definition = full_text.replace(phrase_title, "", 1).strip()
                
                cleaned_parts = [part.strip() for part in raw_definition.split(';') if part.strip()]
                cleaned_definition = ' ; '.join(cleaned_parts)

                word_card["phrases"].append({
                    "phrase": phrase_title,
                    "definition": cleaned_definition
                })

    if not word_card["definitions"] and not word_card["sentences"]:
        return None

    return word_card

if __name__ == "__main__":
    sys.stdout.reconfigure(encoding='utf-8')

    if len(sys.argv) > 1:
        word_to_search = sys.argv[1]
        card_data = get_word_card(word_to_search, max_phrases=5, max_sentences=5)
        if card_data:
            print(json.dumps(card_data, ensure_ascii=False))
        else:
            print(json.dumps({}))
    else:
        word_to_search = "make"
        card_data = get_word_card(word_to_search, max_phrases=5, max_sentences=5)
        if card_data:
            print(f"--- 单词卡片: {word_to_search} ---")
            print(json.dumps(card_data, indent=2, ensure_ascii=False))
        else:
            print(f"未能为 '{word_to_search}' 生成单词卡片。")