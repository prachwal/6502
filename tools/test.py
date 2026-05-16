from openai import OpenAI

client = OpenAI(
  base_url = "https://integrate.api.nvidia.com/v1",
  api_key = "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"
)


completion = client.chat.completions.create(
  model="deepseek-ai/deepseek-v4-pro",
  messages=[{"role":"user","content":""}],
  temperature=1,
  top_p=0.95,
  max_tokens=16384,
  extra_body={"chat_template_kwargs":{"thinking":False}},
  stream=True
)

for chunk in completion:
  if not getattr(chunk, "choices", None):
    continue
  if chunk.choices and chunk.choices[0].delta.content is not None:
    print(chunk.choices[0].delta.content, end="")
  

