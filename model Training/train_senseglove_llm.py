from unsloth import FastLanguageModel
from trl import SFTTrainer
from datasets import load_dataset
from transformers import TrainingArguments

model, tokenizer = FastLanguageModel.from_pretrained(
    "unsloth/meta-llama-3.1-8b-instruct",
    load_in_4bit=True
)

train = load_dataset("json", data_files="train_senseglove_full.jsonl")["train"]
evals = load_dataset("json", data_files="eval_senseglove_full.jsonl")["train"]

trainer = SFTTrainer(
    model=model,
    tokenizer=tokenizer,
    train_dataset=train,
    eval_dataset=evals,
    dataset_text_field="instruction",
    max_seq_length=2048,
    packing=False,
    args=TrainingArguments(
        per_device_train_batch_size=1,
        gradient_accumulation_steps=4,
        num_train_epochs=3,
        learning_rate=2e-5,
        fp16=True,
        evaluation_strategy="steps",
        eval_steps=500,
        output_dir="senseglove_model",
        save_total_limit=2,
        logging_steps=20,
    ),
)
trainer.train()
model.save_pretrained("senseglove_model")
tokenizer.save_pretrained("senseglove_model")
print("✅ Training complete — model saved to senseglove_model/")
