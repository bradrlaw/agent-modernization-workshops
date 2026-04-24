FROM python:3.11-slim

WORKDIR /app

COPY src/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY src/ ./src/
COPY data/ ./data/
COPY web/ ./web/

WORKDIR /app/src

EXPOSE 5000

CMD ["python", "chat_web.py"]
