#!/usr/bin/env python3
"""
Tool to check which Mistral models are available with the current API key.
First fetches the official model list from Mistral API, then tests each model from the user list.
Results are saved to: mistral/available_models.json
"""

import os
import json
import requests
from datetime import datetime

# List of models to check (user-provided)
MODELS_TO_CHECK = [
    "mistral/codestral-latest",
    "mistral/devstral-2512",
    "mistral/devstral-medium-2507",
    "mistral/devstral-medium-latest",
    "mistral/devstral-small-2505",
    "mistral/devstral-small-2507",
    "mistral/labs-devstral-small-2512",
    "mistral/magistral-medium-latest",
    "mistral/magistral-small",
    "mistral/ministral-3b-latest",
    "mistral/ministral-8b-latest",
    "mistral/mistral-embed",
    "mistral/mistral-large-2411",
    "mistral/mistral-large-2512",
    "mistral/mistral-large-latest",
    "mistral/mistral-medium-2505",
    "mistral/mistral-medium-2508",
    "mistral/mistral-medium-2604",
    "mistral/mistral-medium-latest",
    "mistral/mistral-nemo",
    "mistral/mistral-small-2506",
    "mistral/mistral-small-2603",
    "mistral/mistral-small-latest",
    "mistral/open-mistral-7b",
    "mistral/open-mixtral-8x22b",
    "mistral/open-mixtral-8x7b",
    "mistral/pixtral-12b",
    "mistral/pixtral-large-latest",
]

# Mistral API endpoints
MODELS_API_URL = "https://api.mistral.ai/v1/models"
CHAT_API_URL = "https://api.mistral.ai/v1/chat/completions"

# Simple 1-token prompt
PROMPT = [{"role": "user", "content": "a"}]

# Headers
HEADERS = {
    "Content-Type": "application/json",
    "Accept": "application/json",
}


def get_api_key():
    """Get Mistral API key from environment variables."""
    api_key = os.getenv("MISTRAL_API_KEY")
    if not api_key:
        raise ValueError("MISTRAL_API_KEY not found in environment variables")
    return api_key


def get_official_models(api_key):
    """Fetch the official list of available models from Mistral API."""
    headers = {**HEADERS, "Authorization": f"Bearer {api_key}"}
    
    try:
        response = requests.get(MODELS_API_URL, headers=headers, timeout=30)
        if response.status_code == 200:
            data = response.json()
            return [model["id"] for model in data.get("data", [])]
        else:
            print(f"Warning: Could not fetch official models list. Status: {response.status_code}")
            return []
    except Exception as e:
        print(f"Warning: Error fetching official models: {e}")
        return []


def check_model(model_id, api_key):
    """Check if a model is available by sending a 1-token prompt."""
    payload = {
        "model": model_id,
        "messages": PROMPT,
        "max_tokens": 1,
        "temperature": 0,
    }
    
    headers = {**HEADERS, "Authorization": f"Bearer {api_key}"}
    
    try:
        response = requests.post(CHAT_API_URL, json=payload, headers=headers, timeout=30)
        
        if response.status_code == 200:
            return {
                "model": model_id,
                "available": True,
                "status_code": 200,
                "error": None,
            }
        else:
            error_msg = response.text[:200] if response.text else str(response.status_code)
            return {
                "model": model_id,
                "available": False,
                "status_code": response.status_code,
                "error": error_msg,
            }
    except requests.exceptions.Timeout:
        return {
            "model": model_id,
            "available": False,
            "status_code": None,
            "error": "Request timeout",
        }
    except requests.exceptions.RequestException as e:
        return {
            "model": model_id,
            "available": False,
            "status_code": None,
            "error": str(e),
        }


def main():
    print("Checking Mistral model availability...")
    
    api_key = get_api_key()
    print(f"Found API key: {api_key[:8]}...{api_key[-4:]}")
    
    # First, get the official list of models
    print("\nFetching official models list...")
    official_models = get_official_models(api_key)
    print(f"Official models available: {len(official_models)}")
    if official_models:
        print("Official model IDs:")
        for model in official_models[:10]:  # Print first 10
            print(f"  - {model}")
        if len(official_models) > 10:
            print(f"  ... and {len(official_models) - 10} more")
    
    # Check each model from the user's list
    print("\nChecking user-specified models...")
    results = []
    
    for model in MODELS_TO_CHECK:
        # Try with the user's model name first
        print(f"Checking {model}...", end=" ", flush=True)
        result = check_model(model, api_key)
        
        # If not available, try without the "mistral/" prefix
        if not result["available"] and model.startswith("mistral/"):
            alt_model = model.replace("mistral/", "")
            print(f"Retrying {alt_model}...", end=" ", flush=True)
            result = check_model(alt_model, api_key)
        
        results.append(result)
        
        if result["available"]:
            print("✅ Available")
        else:
            print(f"❌ {result['error']}")
    
    # Also check which official models match the user's list
    print("\nMatching official models with user list...")
    matching_models = []
    for official_model in official_models:
        # Check if any part of the official model ID matches any user model
        for user_model in MODELS_TO_CHECK:
            if user_model in official_model or official_model in user_model:
                matching_models.append({
                    "user_query": user_model,
                    "official_id": official_model,
                })
                break
    
    # Save results
    os.makedirs("mistral", exist_ok=True)
    output_path = "mistral/available_models.json"
    
    output_data = {
        "timestamp": datetime.now().isoformat(),
        "api_key": f"{api_key[:8]}...{api_key[-4:]}",
        "total_user_models": len(MODELS_TO_CHECK),
        "official_models_count": len(official_models),
        "official_models": official_models,
        "user_models_results": results,
        "available_user_models": [r["model"] for r in results if r["available"]],
        "unavailable_user_models": [r["model"] for r in results if not r["available"]],
        "matching_models": matching_models,
    }
    
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(output_data, f, indent=2, ensure_ascii=False)
    
    print(f"\nResults saved to: {output_path}")
    print(f"Available user models: {len(output_data['available_user_models'])}/{len(MODELS_TO_CHECK)}")
    
    # Print summary
    if output_data["available_user_models"]:
        print("\n=== Available User Models ===")
        for model in output_data["available_user_models"]:
            print(f"  ✅ {model}")
    
    if output_data["unavailable_user_models"]:
        print("\n=== Unavailable User Models ===")
        for model in output_data["unavailable_user_models"]:
            print(f"  ❌ {model}")
    
    if matching_models:
        print("\n=== Matching Official Models ===")
        for match in matching_models:
            print(f"  🔗 User: {match['user_query']} → Official: {match['official_id']}")


if __name__ == "__main__":
    main()
