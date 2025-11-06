
SenseGlove LLM Dataset & Training Guide
======================================

Generated: 2025-11-04T20:08:30.166665Z
Base model: meta-llama/Meta-Llama-3.1-8B-Instruct
Entries: train=7790, eval=410, total=8200

Files
-----
- train_senseglove_full.jsonl
- eval_senseglove_full.jsonl

LLaMA-Factory (QLoRA)
---------------------
pip install -U llama-factory
llamafactory-cli train \
  --model_name_or_path meta-llama/Meta-Llama-3.1-8B-Instruct \
  --stage sft --do_train true \
  --finetuning_type lora --template llama3 \
  --dataset_dir . \
  --dataset train_senseglove_full.jsonl \
  --eval_dataset eval_senseglove_full.jsonl \
  --output_dir senseglove-8b-lora \
  --per_device_train_batch_size 2 --gradient_accumulation_steps 8 \
  --learning_rate 2e-4 --num_train_epochs 2 \
  --cutoff_len 2048 --fp16 --packing true

Unsloth (QLoRA)
----------------
pip install unsloth
# See Unsloth docs for JSONL loading; adapt to your infra.

Merging & Quantization (optional)
---------------------------------
# Merge LoRA (example tooling)
python merge_lora.py --base meta-llama/Meta-Llama-3.1-8B-Instruct --adapter senseglove-8b-lora --save merged-senseglove

# Quantize with llama.cpp
./quantize merged-senseglove/ggml-model-f16.gguf q4_K_M senseglove.q4.gguf

Ollama Integration
------------------
Create Modelfile:
FROM ./senseglove.q4.gguf
PARAMETER temperature 0.5
PARAMETER top_p 0.9
SYSTEM """You are SenseGloveGPT, an expert on the SenseGlove Unity SDK and VR haptics."""

ollama create senseglove -f Modelfile
ollama run senseglove

App Prompting Tips
------------------
- Always prepend a short SYSTEM instruction aligning to SenseGlove domain.
- Enable RAG over your local SDK index for freshness.

