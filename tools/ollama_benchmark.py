#!/usr/bin/env python3
"""Benchmark Ollama response speed for available models.

The script queries the Ollama server for available models, sends the same
prompt to each one, and prints a ranked report with tokens per second.
"""

from __future__ import annotations

import argparse
import os
import sys
import time
from dataclasses import dataclass
from typing import Any

import requests
from rich.console import Console
from rich.table import Table


DEFAULT_PROMPT = (
    "Napisz krótki, rzeczowy akapit po polsku o tym, jak działa pamięć podręczna "
    "w aplikacjach webowych. Odpowiedź ma mieć dokładnie 120-160 słów i nie "
    "zawierać punktów."
)


@dataclass(slots=True)
class BenchmarkResult:
    model: str
    prompt_tokens: int | None
    completion_tokens: int | None
    total_tokens: int | None
    seconds: float
    tokens_per_second: float | None
    status: str
    error: str | None = None


def build_base_url() -> str:
    base_url = (
        os.getenv("OLLAMA_CLOUD_URL")
        or os.getenv("OLLAMA_HOST")
        or os.getenv("OLLAMA_BASE_URL")
        or "http://localhost:11434"
    )
    return base_url.rstrip("/")


def build_headers() -> dict[str, str]:
    headers: dict[str, str] = {"Content-Type": "application/json"}
    api_key = os.getenv("OLLAMA_API_KEY")
    if api_key:
        headers["Authorization"] = f"Bearer {api_key}"
    return headers


def list_models(base_url: str, timeout: float) -> list[str]:
    response = requests.get(f"{base_url}/api/tags", headers=build_headers(), timeout=timeout)
    response.raise_for_status()
    payload = response.json()
    models = payload.get("models", [])
    return [item["name"] for item in models if "name" in item]


def run_benchmark(
    base_url: str,
    model: str,
    prompt: str,
    timeout: float,
    num_predict: int,
) -> BenchmarkResult:
    payload: dict[str, Any] = {
        "model": model,
        "prompt": prompt,
        "stream": False,
        "options": {
            "num_predict": num_predict,
            "temperature": 0,
        },
    }

    started = time.perf_counter()
    try:
        response = requests.post(
            f"{base_url}/api/generate",
            headers=build_headers(),
            json=payload,
            timeout=timeout,
        )
        response.raise_for_status()
        data = response.json()
        elapsed = time.perf_counter() - started
        completion_tokens = data.get("eval_count")
        prompt_tokens = data.get("prompt_eval_count")
        total_tokens = None
        if isinstance(completion_tokens, int) and isinstance(prompt_tokens, int):
            total_tokens = completion_tokens + prompt_tokens
        tokens_per_second = None
        if isinstance(completion_tokens, int) and elapsed > 0:
            tokens_per_second = completion_tokens / elapsed
        return BenchmarkResult(
            model=model,
            prompt_tokens=prompt_tokens if isinstance(prompt_tokens, int) else None,
            completion_tokens=completion_tokens if isinstance(completion_tokens, int) else None,
            total_tokens=total_tokens,
            seconds=elapsed,
            tokens_per_second=tokens_per_second,
            status="ok",
        )
    except Exception as exc:  # noqa: BLE001
        elapsed = time.perf_counter() - started
        return BenchmarkResult(
            model=model,
            prompt_tokens=None,
            completion_tokens=None,
            total_tokens=None,
            seconds=elapsed,
            tokens_per_second=None,
            status="error",
            error=str(exc),
        )


def format_value(value: Any) -> str:
    if value is None:
        return "-"
    if isinstance(value, float):
        return f"{value:.2f}"
    return str(value)


def print_report(results: list[BenchmarkResult], base_url: str, prompt: str) -> None:
    console = Console()
    table = Table(title="Ollama Cloud Benchmark", show_lines=False)
    table.add_column("#", justify="right", style="cyan", no_wrap=True)
    table.add_column("Model", style="bold")
    table.add_column("Status", justify="center")
    table.add_column("Prompt tok.", justify="right")
    table.add_column("Completion tok.", justify="right")
    table.add_column("Total tok.", justify="right")
    table.add_column("Seconds", justify="right")
    table.add_column("Tok/s", justify="right")
    table.add_column("Error", overflow="fold")

    sorted_results = sorted(
        results,
        key=lambda item: item.tokens_per_second or -1.0,
        reverse=True,
    )
    for index, result in enumerate(sorted_results, start=1):
        status_style = "green" if result.status == "ok" else "red"
        table.add_row(
            str(index),
            result.model,
            f"[{status_style}]{result.status}[/{status_style}]",
            format_value(result.prompt_tokens),
            format_value(result.completion_tokens),
            format_value(result.total_tokens),
            f"{result.seconds:.2f}",
            format_value(result.tokens_per_second),
            result.error or "",
        )

    console.print(table)
    console.print(f"[dim]Base URL:[/dim] {base_url}")
    console.print(f"[dim]Prompt:[/dim] {prompt}")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Benchmark token generation speed for Ollama models."
    )
    parser.add_argument(
        "--base-url",
        default=build_base_url(),
        help="Ollama base URL, defaults to OLLAMA_CLOUD_URL, OLLAMA_HOST, OLLAMA_BASE_URL, or localhost.",
    )
    parser.add_argument(
        "--model",
        action="append",
        dest="models",
        help="Benchmark only selected model(s). Can be provided multiple times.",
    )
    parser.add_argument(
        "--prompt",
        default=DEFAULT_PROMPT,
        help="Prompt used for every benchmark run.",
    )
    parser.add_argument(
        "--timeout",
        type=float,
        default=120.0,
        help="HTTP timeout in seconds.",
    )
    parser.add_argument(
        "--num-predict",
        type=int,
        default=256,
        help="Maximum number of tokens to generate during benchmark.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    base_url = args.base_url.rstrip("/")

    try:
        available_models = list_models(base_url, args.timeout)
    except Exception as exc:  # noqa: BLE001
        print(f"Failed to list models from {base_url}: {exc}", file=sys.stderr)
        return 1

    models = args.models or available_models
    if not models:
        print("No models found.", file=sys.stderr)
        return 1

    results = [
        run_benchmark(base_url, model, args.prompt, args.timeout, args.num_predict)
        for model in models
    ]
    print_report(results, base_url, args.prompt)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
