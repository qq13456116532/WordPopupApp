from fastapi import FastAPI
from pydantic import BaseModel, Field
from typing import List, Optional
import os
from langchain_google_genai import ChatGoogleGenerativeAI
from langchain_core.output_parsers import StrOutputParser
from langchain.prompts import ChatPromptTemplate

app = FastAPI()

class YoudaoDefinition(BaseModel):
    part_of_speech: str
    definition: str

class YoudaoPhrase(BaseModel):
    phrase: str
    definition: str

class YoudaoSentence(BaseModel):
    example_en: str
    example_cn: str

class WordCard(BaseModel):
    word: str
    phonetic: str = ""
    source: str = "Gemini"
    audio_url: str = ""
    definitions: List[YoudaoDefinition] = Field(default_factory=list)
    phrases: List[YoudaoPhrase] = Field(default_factory=list)
    sentences: List[YoudaoSentence] = Field(default_factory=list)

class Query(BaseModel):
    query: str

LLM = ChatGoogleGenerativeAI(
    model="gemini-2.0-flash",
    google_api_key= "AIzaSyB2KNWGXZvJ38ObgJsPu_0NE3oG1D68lAQ" ,  # os.getenv("GOOGLE_API_KEY"),
    temperature=0.4,
)

PROMPT = ChatPromptTemplate.from_messages([
    ("system", """你是英汉词典编辑。请输出严格 JSON：
{{
  "word": string,
  "phonetic": string,
  "source": "Gemini",
  "audio_url": string,
  "definitions": [{{"part_of_speech": string, "definition": string}}],
  "phrases": [{{"phrase": string, "definition": string}}],
  "sentences": [{{"example_en": string, "example_cn": string}}]
}}
每类最多 5 条，中文自然精炼，只输出 JSON。"""),
    ("user", "词或短语：{q}")
])

chain = PROMPT | LLM | StrOutputParser()

@app.post("/generate_word_card", response_model=WordCard)
def generate(q: Query):
    raw = chain.invoke({"q": q.query})
    # 简单清洗 code block
    txt = raw.strip().strip("`")
    start = txt.find("{")
    if start > 0:
        txt = txt[start:]
    import json
    data = json.loads(txt)
    # 兜底
    data.setdefault("source", "Gemini")
    data.setdefault("audio_url", "")
    return WordCard(**data)
