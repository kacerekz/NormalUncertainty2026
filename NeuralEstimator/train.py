import torch
import torch.nn as nn
import torch.optim as optim
import pandas as pd
import numpy as np
from sklearn.model_selection import train_test_split
from sklearn.metrics import mean_absolute_error, r2_score
import copy 

# --- Configuration ---
DATA_FILE = "data/dataset.csv"
EPOCHS = 10000  
PATIENCE = 100 
MIN_DELTA = 0.00001 
BATCH_SIZE = 1024 
LEARNING_RATE = 0.001

# --- Neural Network (15 Inputs -> 1 Output) ---
class UncertaintyNet3D(nn.Module):
    def __init__(self):
        super(UncertaintyNet3D, self).__init__()
        self.model = nn.Sequential(
            nn.Linear(15, 128),
            nn.ReLU(),
            nn.Linear(128, 128),
            nn.ReLU(),
            nn.Linear(128, 64),
            nn.ReLU(),
            nn.Linear(64, 1)
        )

    def forward(self, x):
        return self.model(x)

def main():
    print(f"Loading 3D data from {DATA_FILE}...")
    try:
        df = pd.read_csv(DATA_FILE)
    except FileNotFoundError:
        print("Error: CSV not found.")
        return

    # Split Features (0 to 14) and Target (15)
    X = df.iloc[:, 0:15].values.astype(np.float32)
    y = df.iloc[:, 15].values.astype(np.float32).reshape(-1, 1)

    X_train, X_val, y_train, y_val = train_test_split(X, y, test_size=0.2, random_state=42)

    X_train_tensor = torch.tensor(X_train)
    y_train_tensor = torch.tensor(y_train)
    X_val_tensor = torch.tensor(X_val)
    y_val_tensor = torch.tensor(y_val)

    train_dataset = torch.utils.data.TensorDataset(X_train_tensor, y_train_tensor)
    train_loader = torch.utils.data.DataLoader(train_dataset, batch_size=BATCH_SIZE, shuffle=True)

    model = UncertaintyNet3D()
    criterion = nn.MSELoss()
    optimizer = optim.Adam(model.parameters(), lr=LEARNING_RATE)

    best_val_loss = float('inf')
    epochs_no_improve = 0
    best_model_weights = None

    print(f"Starting 3D training with Early Stopping (Patience: {PATIENCE})...")
    
    for epoch in range(EPOCHS):
        model.train()
        train_loss = 0.0
        
        for inputs, targets in train_loader:
            optimizer.zero_grad()
            outputs = model(inputs)
            loss = criterion(outputs, targets)
            loss.backward()
            optimizer.step()
            train_loss += loss.item() * inputs.size(0)
        
        train_loss /= len(train_loader.dataset)

        model.eval()
        with torch.no_grad():
            val_predictions = model(X_val_tensor)
            val_loss = criterion(val_predictions, y_val_tensor).item()

        if (epoch+1) % 10 == 0:
            print(f"Epoch [{epoch+1}/{EPOCHS}] | Train Loss: {train_loss:.6f} | Val Loss: {val_loss:.6f}")

        if val_loss < (best_val_loss - MIN_DELTA):
            best_val_loss = val_loss
            epochs_no_improve = 0
            best_model_weights = copy.deepcopy(model.state_dict())
        else:
            epochs_no_improve += 1
            if epochs_no_improve >= PATIENCE:
                print(f"\nEarly stopping triggered at epoch {epoch+1}!")
                print(f"Best Validation Loss: {best_val_loss:.6f}")
                break

    if best_model_weights is not None:
        model.load_state_dict(best_model_weights)
        print("Restored model to best weights found during training.")

    model.eval()
    with torch.no_grad():
        test_predictions = model(X_val_tensor)
    
    y_true = y_val
    y_pred = test_predictions.numpy()

    mae = mean_absolute_error(y_true, y_pred)
    r2 = r2_score(y_true, y_pred)

    print("\n--- 3D Validation Results (Best Model) ---")
    print(f"MAE: {mae:.6f} (radians)")
    print(f"MAE: {mae * (180/3.14159):.4f} (degrees)")
    print(f"R2 Score: {r2:.4f}")

    dummy = torch.randn(1, 15)
    torch.onnx.export(model, dummy, "uncertainty_model_3d.onnx", 
                      input_names=['input'], output_names=['output'],
                      dynamic_axes={'input': {0: 'batch_size'}, 'output': {0: 'batch_size'}})
    print("Model exported to ONNX.")

if __name__ == "__main__":
    main()