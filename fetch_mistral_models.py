#!/usr/bin/env python3
"""
Fetch full details of all Mistral models via API and save only working ones to a file.
Enriches with known context length and pricing data.
Saves to: mistral/models_full_details.json
"""

import os
import json
import requests
from datetime import datetime

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

# Known model specifications (context length and pricing for vibe scope)
# Based on Mistral documentation and user's config.toml
KNOWN_MODELS = {
    # Format: "model_id": {"context_length": int, "input_price": float, "output_price": float, "description": str}
    "mistral-medium-latest": {
        "context_length": 32768,
        "input_price": 1.50,
        "output_price": 7.50,
        "description": "Medium-sized general-purpose model. Default for Vibe."
    },
    "mistral-medium": {
        "context_length": 32768,
        "input_price": 1.50,
        "output_price": 7.50,
        "description": "Medium-sized general-purpose model."
    },
    "mistral-medium-2505": {
        "context_length": 32768,
        "input_price": 1.50,
        "output_price": 7.50,
        "description": "Medium model version 2505."
    },
    "mistral-medium-2508": {
        "context_length": 32768,
        "input_price": 1.50,
        "output_price": 7.50,
        "description": "Medium model version 2508."
    },
    "mistral-medium-2604": {
        "context_length": 32768,
        "input_price": 1.50,
        "output_price": 7.50,
        "description": "Medium model version 2604."
    },
    "mistral-medium-3": {
        "context_length": 32768,
        "input_price": 1.50,
        "output_price": 7.50,
        "description": "Medium model version 3."
    },
    "mistral-medium-3.5": {
        "context_length": 32768,
        "input_price": 1.50,
        "output_price": 7.50,
        "description": "Medium 3.5 model - combines Medium 3.1, Magistral, and Devstral 2. Default for Vibe CLI."
    },
    "mistral-medium-3-5": {
        "context_length": 32768,
        "input_price": 1.50,
        "output_price": 7.50,
        "description": "Medium 3.5 model variant."
    },
    "mistral-medium-c21211-r0-75": {
        "context_length": 32768,
        "input_price": 1.50,
        "output_price": 7.50,
        "description": "Medium model experimental variant."
    },
    "mistral-small-latest": {
        "context_length": 32768,
        "input_price": 0.10,
        "output_price": 0.30,
        "description": "Small-sized general-purpose model. Cost-effective for simple tasks."
    },
    "mistral-small-2603": {
        "context_length": 32768,
        "input_price": 0.10,
        "output_price": 0.30,
        "description": "Small model version 2603."
    },
    "mistral-small-2506": {
        "context_length": 32768,
        "input_price": 0.10,
        "output_price": 0.30,
        "description": "Small model version 2506."
    },
    "devstral-2512": {
        "context_length": 32768,
        "input_price": 0.25,
        "output_price": 0.70,
        "description": "Dev-focused model specialized for coding tasks."
    },
    "devstral-medium-latest": {
        "context_length": 32768,
        "input_price": 0.25,
        "output_price": 0.70,
        "description": "Medium-sized dev-focused model."
    },
    "devstral-latest": {
        "context_length": 32768,
        "input_price": 0.25,
        "output_price": 0.70,
        "description": "Latest dev-focused model."
    },
    "magistral-small-latest": {
        "context_length": 32768,
        "input_price": 0.10,
        "output_price": 0.30,
        "description": "Small model optimized for structured tasks."
    },
    "magistral-medium-latest": {
        "context_length": 32768,
        "input_price": 1.50,
        "output_price": 7.50,
        "description": "Medium model optimized for structured tasks."
    },
    "mistral-vibe-cli-latest": {
        "context_length": 32768,
        "input_price": 1.50,
        "output_price": 7.50,
        "description": "Vibe CLI optimized model. Combines Medium 3.1, Magistral, and Devstral 2."
    },
    "mistral-vibe-cli-with-tools": {
        "context_length": 32768,
        "input_price": 1.50,
        "output_price": 7.50,
        "description": "Vibe CLI model with tools support."
    },
    "mistral-vibe-cli-fast": {
        "context_length": 32768,
        "input_price": 0.10,
        "output_price": 0.30,
        "description": "Fast Vibe CLI model."
    },
    "open-mixtral-8x22b": {
        "context_length": 32768,
        "input_price": 0.00,
        "output_price": 0.00,
        "description": "Open-source Mixture of Experts model (8x22B). Free tier."
    },
    "open-mixtral-8x7b": {
        "context_length": 32768,
        "input_price": 0.00,
        "output_price": 0.00,
        "description": "Open-source Mixture of Experts model (8x7B). Free tier."
    },
    # Default values for unknown models
    "default": {
        "context_length": 32768,
        "input_price": None,
        "output_price": None,
        "description": "Unknown model specifications."
    }
}


def get_api_key():
    """Get Mistral API key from environment variables."""
    api_key = os.getenv("MISTRAL_API_KEY")
    if not api_key:
        raise ValueError("MISTRAL_API_KEY not found in environment variables")
    return api_key


def fetch_all_models(api_key):
    """Fetch the full list of models with details from Mistral API."""
    headers = {**HEADERS, "Authorization": f"Bearer {api_key}"}
    
    try:
        response = requests.get(MODELS_API_URL, headers=headers, timeout=30)
        if response.status_code == 200:
            return response.json().get("data", [])
        else:
            print(f"Error fetching models: {response.status_code} - {response.text}")
            return []
    except Exception as e:
        print(f"Error fetching models: {e}")
        return []


def check_model_works(model_id, api_key):
    """Check if a model works by sending a 1-token prompt."""
    payload = {
        "model": model_id,
        "messages": PROMPT,
        "max_tokens": 1,
        "temperature": 0,
    }
    
    headers = {**HEADERS, "Authorization": f"Bearer {api_key}"}
    
    try:
        response = requests.post(CHAT_API_URL, json=payload, headers=headers, timeout=30)
        return response.status_code == 200
    except:
        return False


def enrich_model_data(model):
    """Add known context length and pricing to model data."""
    model_id = model.get("id", "")
    known = KNOWN_MODELS.get(model_id, KNOWN_MODELS["default"])
    
    # Create enriched model data
    enriched = {
        "id": model.get("id"),
        "object": model.get("object", "model"),
        "created": model.get("created"),
        "description": known.get("description", model.get("description", "N/A")),
        "context_length": known.get("context_length"),
        "input_price_per_1m": known.get("input_price"),
        "output_price_per_1m": known.get("output_price"),
        "pricing": {
            "prompt": known.get("input_price"),
            "completion": known.get("output_price")
        } if known.get("input_price") is not None else None,
    }
    
    # Add original fields if they exist
    for key in ["owned_by", "parent", "root"]:
        if key in model:
            enriched[key] = model[key]
    
    return enriched


def main():
    print("Fetching Mistral models and checking availability...")
    
    api_key = get_api_key()
    print(f"API key: {api_key[:8]}...{api_key[-4:]}")
    
    # Fetch all models with details
    print("\nFetching model list...")
    all_models = fetch_all_models(api_key)
    print(f"Found {len(all_models)} models in API")
    
    # Check which models work
    working_models = []
    
    for i, model in enumerate(all_models):
        model_id = model["id"]
        print(f"[{i+1}/{len(all_models)}] Checking {model_id}...", end=" ", flush=True)
        
        if check_model_works(model_id, api_key):
            print("✅ Working")
            # Enrich with known data
            enriched_model = enrich_model_data(model)
            working_models.append(enriched_model)
        else:
            print("❌ Not available")
    
    # Save only working models with full details
    os.makedirs("mistral", exist_ok=True)
    output_path = "mistral/models_full_details.json"
    
    output_data = {
        "timestamp": datetime.now().isoformat(),
        "api_key_scope": "vibe",
        "total_models_in_api": len(all_models),
        "working_models_count": len(working_models),
        "models": working_models,
    }
    
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(output_data, f, indent=2, ensure_ascii=False)
    
    print(f"\n✅ Results saved to: {output_path}")
    print(f"Working models: {len(working_models)}/{len(all_models)}")
    
    # Print summary table
    print("\n" + "="*120)
    print("WORKING MODELS - FULL DETAILS")
    print("="*120)
    print(f"{'Model ID':<35} | {'Context':<10} | {'Price (in/out)':<20} | {'Description'}")
    print("-"*120)
    
    for model in working_models:
        model_id = model.get("id", "N/A")
        context = model.get("context_length", "N/A")
        input_price = model.get("input_price_per_1m", "?")
        output_price = model.get("output_price_per_1m", "?")
        desc = model.get("description", "N/A")[:45]
        
        print(f"{model_id:<35} | {str(context):<10} | ${input_price}/${output_price:<14} | {desc}")


if __name__ == "__main__":
    main()
